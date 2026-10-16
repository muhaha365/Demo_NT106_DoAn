using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using UitChat.Core;

namespace UitChat.Server;

public sealed class ChatServer : IAsyncDisposable
{
    private readonly Storage storage;
    private readonly X509Certificate2 certificate;
    private readonly TcpListener listener;
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, Peer> peers = new();
    private readonly ConcurrentDictionary<Guid, Task> tasks = new();
    private readonly ConcurrentDictionary<string, (int Count, DateTimeOffset Reset)> authAttempts = new();
    private Task? acceptTask;
    public event Action<string>? Log;
    public string Fingerprint { get; }
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public int OnlineCount => peers.Values.Where(p => p.Username is not null).Select(p => p.Username).Distinct().Count();

    public ChatServer(string directory, IPAddress address, int port)
    {
        storage = new Storage(directory);
        try
        {
            certificate = ServerCertificate.LoadOrCreate(directory);
            Fingerprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);
            File.WriteAllText(Path.Combine(directory, "fingerprint.txt"), Fingerprint);
            listener = new TcpListener(address, port);
        }
        catch { storage.Dispose(); throw; }
    }

    public void Start()
    {
        listener.Start(128);
        acceptTask = AcceptAsync();
        Log?.Invoke($"Server đang lắng nghe {listener.LocalEndpoint} • TCP + TLS");
    }

    private async Task AcceptAsync()
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                var tcp = await listener.AcceptTcpClientAsync(lifetime.Token);
                if (peers.Count >= 100) { tcp.Dispose(); continue; }
                var peer = new Peer(tcp, lifetime.Token);
                peers[peer.Id] = peer;
                var task = Task.Run(() => HandleAsync(peer));
                tasks[peer.Id] = task;
                _ = task.ContinueWith(_ => tasks.TryRemove(peer.Id, out var ignored), TaskScheduler.Default);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
        { if (!lifetime.IsCancellationRequested) Log?.Invoke("Listener: " + ex.Message); }
    }

    private async Task HandleAsync(Peer peer)
    {
        Task? writer = null;
        try
        {
            using var handshake = CancellationTokenSource.CreateLinkedTokenSource(peer.Token);
            handshake.CancelAfter(TimeSpan.FromSeconds(10));
            await peer.Stream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
            {
                ServerCertificate = certificate, EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, handshake.Token);
            writer = peer.WriteLoopAsync();
            while (!peer.Token.IsCancellationRequested)
            {
                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(peer.Token);
                readTimeout.CancelAfter(TimeSpan.FromSeconds(peer.Username is null ? 30 : 75));
                var packet = await FrameCodec.ReadAsync(peer.Stream, readTimeout.Token);
                if (packet.Id.Length == 0) throw new InvalidDataException("Thiếu request ID.");
                // Limit all commands, including typing, independently of authentication.
                if (!peer.AllowCommand()) { peer.Reply(packet, new Notice(""), "Thao tác quá nhanh. Hãy chờ một chút."); continue; }
                try
                {
                    if (packet.Type is "register" or "login") await AuthenticateAsync(peer, packet);
                    else
                    {
                        await gate.WaitAsync(peer.Token);
                        try { Dispatch(peer, packet); }
                        finally { gate.Release(); }
                    }
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or JsonException or InvalidDataException or FormatException)
                { peer.Reply(packet, new Notice(""), ex is JsonException ? "Dữ liệu JSON không đúng định dạng." : ex.Message); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Log?.Invoke("Lỗi lưu trữ: " + ex.Message);
                    peer.Reply(packet, new Notice(""), "Không thể lưu dữ liệu. Server chưa xác nhận thao tác này.");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or AuthenticationException or OperationCanceledException or ObjectDisposedException or JsonException)
        { if (peer.Username is not null) Log?.Invoke($"Ngắt kết nối: {peer.Username}"); }
        catch (Exception ex) { Log?.Invoke("Lỗi kết nối: " + ex.GetType().Name); }
        finally
        {
            peer.Close();
            if (writer is not null) await writer;
            await gate.WaitAsync();
            try
            {
                peers.TryRemove(peer.Id, out _);
                if (peer.Username is not null && !lifetime.IsCancellationRequested) BroadcastState();
            }
            finally { gate.Release(); peer.Dispose(); }
        }
    }

    private async Task AuthenticateAsync(Peer peer, Packet packet)
    {
        if (peer.Username is not null) throw new InvalidOperationException("Bạn đã đăng nhập.");
        var input = Json.Read<Credentials>(packet.Data);
        string username = Normalize(input.Username);
        if (input.Password is null || input.Password.Length is < 8 or > 128)
            throw new ArgumentException("Mật khẩu cần từ 8 đến 128 ký tự.");
        var now = DateTimeOffset.UtcNow;
        var attempts = authAttempts.AddOrUpdate(peer.Address, (1, now.AddMinutes(1)), (_, old) =>
            old.Reset <= now ? (1, now.AddMinutes(1)) : (old.Count + 1, old.Reset));
        if (attempts.Count > 20) throw new InvalidOperationException("Quá nhiều lần xác thực từ IP này. Thử lại sau 1 phút.");
        // Expire old IP buckets, keeping memory bounded across long demos.
        foreach (var entry in authAttempts.Where(x => x.Value.Reset < now)) authAttempts.TryRemove(entry.Key, out _);
        if (packet.Type == "register")
        {
            var hashed = await Task.Run(() => Passwords.Hash(input.Password), peer.Token);
            await gate.WaitAsync(peer.Token);
            try
            {
                if (storage.Data.Accounts.Any(a => a.Username == username)) throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
                if (storage.Data.Accounts.Count >= 1000) throw new InvalidOperationException("Bản demo hỗ trợ tối đa 1.000 tài khoản.");
                var next = storage.Copy();
                next.Accounts.Add(new Account { Username = username, Salt = hashed.Salt, PasswordHash = hashed.Hash });
                storage.Commit(next);
                peer.Reply(packet, new Notice("Đăng ký thành công. Bạn có thể đăng nhập."));
                BroadcastState();
                Log?.Invoke("Đăng ký: " + username);
            }
            finally { gate.Release(); }
        }
        else
        {
            Account? account;
            await gate.WaitAsync(peer.Token);
            try { account = storage.Data.Accounts.FirstOrDefault(a => a.Username == username); }
            finally { gate.Release(); }
            // Same PBKDF2 cost for a missing account.
            bool valid = await Task.Run(() => Passwords.Verify(input.Password,
                account?.Salt ?? Convert.ToBase64String(new byte[16]),
                account?.PasswordHash ?? Convert.ToBase64String(new byte[32])), peer.Token);
            if (!valid || account is null) throw new InvalidOperationException("Tên đăng nhập hoặc mật khẩu chưa đúng.");
            await gate.WaitAsync(peer.Token);
            try
            {
                peer.Username = username;
                peer.Reply(packet, State(username));
                BroadcastState();
                Log?.Invoke("Đăng nhập: " + username);
            }
            finally { gate.Release(); }
        }
    }

    private void Dispatch(Peer peer, Packet packet)
    {
        string me = peer.Username ?? throw new InvalidOperationException("Hãy đăng nhập trước.");
        var db = storage.Data;
        switch (packet.Type)
        {
            case "ping": peer.Reply(packet, new Notice("pong")); return;
            case "sync": peer.Reply(packet, State(me)); return;
            case "logout":
                peer.Username = null;
                peer.Reply(packet, new Notice("Đã đăng xuất.")); BroadcastState(); return;
            case "history":
            {
                var q = Json.Read<HistoryQuery>(packet.Data); RequireConversation(db, q.ConversationId, me);
                if (q.Search is null || q.Search.Length > 100) throw new ArgumentException("Từ khóa tối đa 100 ký tự.");
                var messages = db.Messages.Where(m => m.ConversationId == q.ConversationId && m.Id < q.Before &&
                    (m.Text.Contains(q.Search, StringComparison.OrdinalIgnoreCase) || (m.FileName?.Contains(q.Search, StringComparison.OrdinalIgnoreCase) ?? false)))
                    .OrderByDescending(m => m.Id).Take(Limits.HistoryPageSize + 1).ToList();
                peer.Reply(packet, new HistoryPage(messages.Take(Limits.HistoryPageSize).Reverse().ToList(), messages.Count > Limits.HistoryPageSize)); return;
            }
            case "typing":
            {
                var q = Json.Read<TypingRequest>(packet.Data); var c = RequireConversation(db, q.ConversationId, me);
                Publish(c.Members.Where(u => u != me), "typing", new TypingEvent(c.Id, me, q.IsTyping));
                peer.Reply(packet, new Notice("ok")); return;
            }
            case "download":
            {
                var q = Json.Read<FileRequest>(packet.Data);
                var m = db.Messages.FirstOrDefault(m => m.Id == q.MessageId) ?? throw new ArgumentException("Không tìm thấy tệp.");
                RequireConversation(db, m.ConversationId, me);
                if (m.FileName is null) throw new ArgumentException("Tin nhắn không có tệp.");
                peer.Reply(packet, new FileReply(m.FileName, File.ReadAllBytes(storage.AttachmentPath(m.Id)))); return;
            }
        }

        // Transactions work on a copy so an I/O failure cannot leak an uncommitted change.
        var next = storage.Copy();
        var self = next.Accounts.Single(a => a.Username == me);
        object response = new Notice("Đã cập nhật.");
        Action? afterCommit = null;
        bool stateChanged = true;
        switch (packet.Type)
        {
            case "friend.request":
            {
                string target = RequireUser(next, Json.Read<UserTarget>(packet.Data).Username, me);
                if (self.Friends.Contains(target)) throw new InvalidOperationException("Hai bạn đã là bạn bè.");
                if (next.Requests.Any(r => (r.From == me && r.To == target) || (r.From == target && r.To == me)))
                    throw new InvalidOperationException("Đã có lời mời giữa hai bạn. Kiểm tra mục lời mời.");
                next.Requests.Add(new(me, target));
                afterCommit = () => Publish([target], "notice", new Notice($"{me} đã gửi lời mời kết bạn.")); break;
            }
            case "friend.respond":
            {
                var q = Json.Read<FriendDecision>(packet.Data); string target = Normalize(q.Username);
                if (next.Requests.RemoveAll(r => r.From == target && r.To == me) == 0) throw new InvalidOperationException("Lời mời không còn tồn tại.");
                if (q.Accept)
                {
                    self.Friends.Add(target); next.Accounts.Single(a => a.Username == target).Friends.Add(me);
                    afterCommit = () => Publish([target], "notice", new Notice($"{me} đã chấp nhận lời mời kết bạn."));
                }
                break;
            }
            case "friend.remove":
            {
                string target = RequireUser(next, Json.Read<UserTarget>(packet.Data).Username, me);
                self.Friends.Remove(target); next.Accounts.Single(a => a.Username == target).Friends.Remove(me); break;
            }
            case "direct.open":
            {
                string target = RequireUser(next, Json.Read<UserTarget>(packet.Data).Username, me);
                var c = next.Conversations.FirstOrDefault(c => !c.IsGroup && c.Members.Contains(me) && c.Members.Contains(target));
                if (c is null) { c = new Conversation { Members = [me, target] }; next.Conversations.Add(c); }
                response = new ConversationReply(c.Id); break;
            }
            case "group.create":
            {
                var q = Json.Read<GroupCreate>(packet.Data);
                if (string.IsNullOrWhiteSpace(q.Name) || q.Name.Trim().Length > 60) throw new ArgumentException("Tên nhóm cần 1–60 ký tự.");
                if (q.Members is null || q.Members.Count > Limits.MaxMembers - 1) throw new ArgumentException("Nhóm tối đa 50 thành viên.");
                var members = q.Members.Select(u => RequireUser(next, u, me)).Append(me).Distinct().ToList();
                if (members.Count < 2) throw new ArgumentException("Chọn ít nhất một thành viên khác.");
                var c = new Conversation { Name = q.Name.Trim(), IsGroup = true, Owner = me, Members = members };
                next.Conversations.Add(c); response = new ConversationReply(c.Id); break;
            }
            case "group.member":
            {
                var q = Json.Read<GroupMember>(packet.Data); var c = RequireConversation(next, q.ConversationId, me);
                if (!c.IsGroup || c.Owner != me) throw new InvalidOperationException("Chỉ trưởng nhóm được quản lý thành viên.");
                string target = RequireUser(next, q.Username, me);
                if (q.Add)
                {
                    if (!c.Members.Contains(target))
                    { if (c.Members.Count >= Limits.MaxMembers) throw new InvalidOperationException("Nhóm đã đủ 50 người."); c.Members.Add(target); }
                }
                else { c.Members.Remove(target); c.ReadThrough.Remove(target); }
                break;
            }
            case "message.send":
            {
                var q = Json.Read<SendMessage>(packet.Data); var c = RequireConversation(next, q.ConversationId, me);
                if (!Guid.TryParse(q.ClientId, out _)) throw new ArgumentException("Mã tin nhắn không hợp lệ.");
                var previous = db.Messages.FirstOrDefault(m => m.Sender == me && m.ClientId == q.ClientId);
                if (previous is not null) { peer.Reply(packet, previous); return; }
                if (q.Text is null || q.Text.Length > Limits.MaxTextLength) throw new ArgumentException("Tin nhắn tối đa 4.000 ký tự.");
                bool attachment = q.FileBytes is not null;
                if (!attachment && string.IsNullOrWhiteSpace(q.Text)) throw new ArgumentException("Hãy nhập nội dung tin nhắn.");
                if (attachment && (q.FileBytes!.Length > Limits.MaxFileBytes || string.IsNullOrWhiteSpace(q.FileName) || q.FileName.Length > 180))
                    throw new ArgumentException("Tệp tối đa 5 MB, tên tệp tối đa 180 ký tự.");
                string? filename = attachment ? Path.GetFileName(q.FileName!.Replace('\\', '/')) : null;
                if (filename is not null && (filename.Length == 0 || filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)) throw new ArgumentException("Tên tệp không hợp lệ.");
                var message = new MessageView(next.NextMessageId++, c.Id, me, q.Text.Trim(), DateTimeOffset.UtcNow, [me], filename, q.FileBytes?.LongLength ?? 0, q.ClientId);
                next.Messages.Add(message);
                if (attachment) File.WriteAllBytes(storage.AttachmentPath(message.Id), q.FileBytes!);
                response = message;
                afterCommit = () => Publish(c.Members, "message", message); break;
            }
            case "read":
            {
                var q = Json.Read<ReadRequest>(packet.Data); var c = RequireConversation(next, q.ConversationId, me);
                if (!next.Messages.Any(m => m.ConversationId == c.Id && m.Id == q.ThroughId)) throw new ArgumentException("Mốc đã xem không tồn tại.");
                if (c.ReadThrough.GetValueOrDefault(me) >= q.ThroughId) { peer.Reply(packet, new Notice("ok")); return; }
                c.ReadThrough[me] = q.ThroughId;
                foreach (var m in next.Messages.Where(m => m.ConversationId == c.Id && m.Id <= q.ThroughId))
                    if (!m.SeenBy.Contains(me)) m.SeenBy.Add(me);
                afterCommit = () => Publish(c.Members, "read", new ReadEvent(c.Id, me, q.ThroughId)); break;
            }
            default: stateChanged = false; throw new ArgumentException("Lệnh không được hỗ trợ.");
        }
        storage.Commit(next);
        peer.Reply(packet, response);
        afterCommit?.Invoke();
        if (stateChanged) BroadcastState();
    }

    private static string Normalize(string? name)
    {
        string value = name?.Trim().ToLowerInvariant() ?? "";
        if (!Regex.IsMatch(value, "^[a-z0-9_]{3,24}$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Username: 3–24 ký tự a–z, 0–9 hoặc dấu gạch dưới.");
        return value;
    }
    private static string RequireUser(Database db, string name, string me)
    {
        name = Normalize(name);
        if (name == me) throw new ArgumentException("Hãy chọn một người dùng khác.");
        if (!db.Accounts.Any(a => a.Username == name)) throw new ArgumentException("Người dùng không tồn tại.");
        return name;
    }
    private static Conversation RequireConversation(Database db, string id, string me) =>
        db.Conversations.FirstOrDefault(c => c.Id == id && c.Members.Contains(me))
        ?? throw new InvalidOperationException("Bạn không có quyền truy cập cuộc trò chuyện này.");

    private Snapshot State(string me)
    {
        var db = storage.Data;
        var online = peers.Values.Where(p => p.Username is not null).Select(p => p.Username!).ToHashSet();
        var self = db.Accounts.Single(a => a.Username == me);
        var conversations = db.Conversations.Where(c => c.Members.Contains(me)).Select(c =>
        {
            var last = db.Messages.LastOrDefault(m => m.ConversationId == c.Id);
            string preview = last is null ? "Bắt đầu cuộc trò chuyện" : last.FileName is not null ? "Tệp: " + last.FileName : last.Text;
            if (preview.Length > 80) preview = preview[..80] + "…";
            return new ConversationView(c.Id, c.IsGroup ? c.Name : c.Members.First(u => u != me), c.IsGroup, c.Owner, c.Members.ToList(), preview,
                last?.SentAt, db.Messages.Count(m => m.ConversationId == c.Id && m.Sender != me && m.Id > c.ReadThrough.GetValueOrDefault(me)));
        }).OrderByDescending(c => c.UpdatedAt).ToList();
        return new Snapshot(me, db.Accounts.Where(a => a.Username != me).Select(a => new UserView(a.Username, online.Contains(a.Username), self.Friends.Contains(a.Username))).ToList(),
            db.Requests.Where(r => r.From == me || r.To == me).ToList(), conversations);
    }
    private void BroadcastState()
    {
        foreach (var group in peers.Values.Where(p => p.Username is not null).GroupBy(p => p.Username!))
        {
            var packet = Packet.Create("state", State(group.Key));
            foreach (var p in group) p.Enqueue(packet);
        }
    }
    private void Publish<T>(IEnumerable<string> users, string type, T data)
    {
        var targets = users.ToHashSet(); var packet = Packet.Create(type, data);
        foreach (var p in peers.Values.Where(p => p.Username is not null && targets.Contains(p.Username))) p.Enqueue(packet);
    }

    public async ValueTask DisposeAsync()
    {
        lifetime.Cancel(); listener.Stop();
        foreach (var peer in peers.Values) peer.Close();
        if (acceptTask is not null) await acceptTask;
        await Task.WhenAll(tasks.Values);
        certificate.Dispose(); storage.Dispose();
    }

    private sealed class Peer : IDisposable
    {
        private readonly TcpClient tcp;
        private readonly CancellationTokenSource stop;
        private readonly Channel<Packet> outgoing = Channel.CreateBounded<Packet>(new BoundedChannelOptions(32)
        { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        private DateTimeOffset window = DateTimeOffset.UtcNow;
        private int commands;
        public Guid Id { get; } = Guid.NewGuid();
        public string? Username { get; set; }
        public string Address { get; }
        public SslStream Stream { get; }
        public CancellationToken Token => stop.Token;
        public Peer(TcpClient tcp, CancellationToken parent)
        {
            this.tcp = tcp; tcp.NoDelay = true;
            Address = ((IPEndPoint)tcp.Client.RemoteEndPoint!).Address.ToString();
            stop = CancellationTokenSource.CreateLinkedTokenSource(parent);
            Stream = new SslStream(tcp.GetStream(), false);
        }
        public bool AllowCommand()
        {
            if (DateTimeOffset.UtcNow - window > TimeSpan.FromSeconds(1)) { window = DateTimeOffset.UtcNow; commands = 0; }
            return ++commands <= 40;
        }
        public void Reply(Packet request, object data, string? error = null) =>
            Enqueue(new Packet("response", request.Id, Json.Element(data), error is null, error));
        public void Enqueue(Packet packet) { if (!outgoing.Writer.TryWrite(packet)) Close(); }
        public async Task WriteLoopAsync()
        {
            try
            {
                await foreach (var packet in outgoing.Reader.ReadAllAsync(Token))
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
                    timeout.CancelAfter(TimeSpan.FromSeconds(20));
                    await FrameCodec.WriteAsync(Stream, packet, timeout.Token);
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { Close(); }
        }
        public void Close() { stop.Cancel(); outgoing.Writer.TryComplete(); tcp.Dispose(); }
        public void Dispose() { Stream.Dispose(); stop.Dispose(); }
    }
}

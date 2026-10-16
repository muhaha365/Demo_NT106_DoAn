using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using UitChat.Core;
using UitChat.Server;

Console.OutputEncoding = Encoding.UTF8;
int assertions = 0;
void Check(bool condition, string title)
{
    if (!condition) throw new Exception("FAIL: " + title);
    assertions++; Console.WriteLine("PASS: " + title);
}
async Task Reject(Func<Task> action, string title)
{
    bool rejected = false;
    try { await action(); }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or InvalidDataException or System.Security.Authentication.AuthenticationException) { rejected = true; }
    Check(rejected, title);
}
async Task Until(Func<Task<bool>> predicate, string title)
{
    for (int i = 0; i < 60; i++) { if (await predicate()) { Check(true, title); return; } await Task.Delay(50); }
    Check(false, title);
}
string directory = Path.Combine(Path.GetTempPath(), "UITChat-tests-" + Guid.NewGuid().ToString("N"));
Console.WriteLine("Isolated test data: " + directory);
var clients = new List<ChatClient>();
ChatServer? server = null;
const string password = "TestPassword2026!";
try
{
    using var memory = new MemoryStream();
    await FrameCodec.WriteAsync(memory, Packet.Create("sample", new Notice("Tiếng Việt 🌻\nline two"), "1"));
    await FrameCodec.WriteAsync(memory, Packet.Create("sample", new Notice("second"), "2"));
    using var partial = new FragmentStream(memory.ToArray());
    Check(Json.Read<Notice>((await FrameCodec.ReadAsync(partial)).Data).Text == "Tiếng Việt 🌻\nline two", "Fragmented TCP frame preserves UTF-8, emoji and newline");
    Check((await FrameCodec.ReadAsync(partial)).Id == "2", "Coalesced frames decode independently");
    byte[] invalid = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(invalid, Limits.MaxFrameBytes + 1);
    await Reject(async () => await FrameCodec.ReadAsync(new MemoryStream(invalid)), "Oversized frame rejected before allocating body");
    BinaryPrimitives.WriteInt32BigEndian(invalid, -1);
    await Reject(async () => await FrameCodec.ReadAsync(new MemoryStream(invalid)), "Negative frame length rejected");
    await Reject(async () => await FrameCodec.ReadAsync(new MemoryStream(memory.ToArray()[..8])), "Truncated frame rejected");
    var h1 = Passwords.Hash(password); var h2 = Passwords.Hash(password);
    Check(h1.Hash != h2.Hash && h1.Salt != h2.Salt, "Per-account random password salts");
    Check(Passwords.Verify(password, h1.Salt, h1.Hash) && !Passwords.Verify("bad", h1.Salt, h1.Hash), "PBKDF2 password verification");

    server = new ChatServer(directory, IPAddress.Loopback, 0); server.Start();
    async Task<ChatClient> Connect()
    {
        var c = new ChatClient(); clients.Add(c); await c.ConnectAsync("127.0.0.1", server.Port, server.Fingerprint); return c;
    }
    var wrongPin = new ChatClient(); clients.Add(wrongPin);
    await Reject(() => wrongPin.ConnectAsync("127.0.0.1", server.Port, new string('0', 64)), "TLS rejects incorrect pinned certificate");
    var alice = await Connect(); var bob = await Connect(); var charlie = await Connect(); var outsider = await Connect();
    await Reject(async () => await outsider.RequestAsync<Snapshot>("sync", new { }), "Unauthenticated command rejected");
    foreach (var (c, name) in new[] { (alice, "alice"), (bob, "bob"), (charlie, "charlie"), (outsider, "outsider") })
        await c.RequestAsync<Notice>("register", new Credentials(name, password));
    Check(true, "Four accounts register over real TCP/TLS sockets");
    await Reject(async () => await outsider.RequestAsync<Notice>("register", new Credentials("ALICE", password)), "Case-insensitive duplicate username rejected");
    await Reject(async () => await alice.RequestAsync<Snapshot>("login", new Credentials("alice", "wrongpassword")), "Wrong password rejected");
    await Reject(async () => await alice.RequestAsync<Snapshot>("login", new Credentials("missing", password)), "Missing user rejected");
    foreach (var (c, name) in new[] { (alice, "alice"), (bob, "bob"), (charlie, "charlie"), (outsider, "outsider") })
        await c.RequestAsync<Snapshot>("login", new Credentials(name, password));
    Check((await alice.RequestAsync<Snapshot>("sync", new { })).Users.Count(u => u.Online) == 3, "Online presence for concurrent users");
    await Reject(async () => await alice.RequestAsync<Snapshot>("login", new Credentials("bob", password)), "Authenticated connection cannot change identity");
    await Reject(async () => await alice.RequestAsync<Notice>("friend.request", new UserTarget("alice")), "Self friendship rejected");
    await alice.RequestAsync<Notice>("friend.request", new UserTarget("bob"));
    Check((await bob.RequestAsync<Snapshot>("sync", new { })).Requests.Any(r => r.From == "alice"), "Friend request delivered");
    await Reject(async () => await bob.RequestAsync<Notice>("friend.request", new UserTarget("alice")), "Crossed duplicate friendship request rejected");
    await bob.RequestAsync<Notice>("friend.respond", new FriendDecision("alice", true));
    Check((await alice.RequestAsync<Snapshot>("sync", new { })).Users.Single(u => u.Username == "bob").IsFriend, "Friend acceptance is symmetric");
    var events = new ConcurrentQueue<Packet>(); bob.EventReceived += events.Enqueue;
    string dm = (await alice.RequestAsync<ConversationReply>("direct.open", new UserTarget("bob"))).Id;
    Check((await bob.RequestAsync<ConversationReply>("direct.open", new UserTarget("alice"))).Id == dm, "Opening direct chat is idempotent for both users");
    await Reject(async () => await outsider.RequestAsync<HistoryPage>("history", new HistoryQuery(dm)), "Outsider cannot read direct chat");
    string clientId = Guid.NewGuid().ToString("N");
    var first = await alice.RequestAsync<MessageView>("message.send", new SendMessage(dm, "Chào bạn 😊\nHọc NT106 nhé!", clientId));
    await Until(() => Task.FromResult(events.Any(e => e.Type == "message" && Json.Read<MessageView>(e.Data).Id == first.Id)), "Realtime delivery without polling");
    Check(first.Sender == "alice" && first.SentAt.Offset == TimeSpan.Zero, "Server assigns sender identity and UTC timestamp");
    var repeated = await alice.RequestAsync<MessageView>("message.send", new SendMessage(dm, "duplicate", clientId));
    Check(repeated.Id == first.Id && (await bob.RequestAsync<HistoryPage>("history", new HistoryQuery(dm))).Messages.Count == 1, "Retry with same client ID does not duplicate message");
    await alice.RequestAsync<Notice>("typing", new TypingRequest(dm, true));
    await Until(() => Task.FromResult(events.Any(e => e.Type == "typing")), "Typing event delivered");
    await Reject(async () => await outsider.RequestAsync<Notice>("typing", new TypingRequest(dm, true)), "Outsider cannot spoof typing in conversation");
    await bob.RequestAsync<Notice>("read", new ReadRequest(dm, first.Id));
    Check((await alice.RequestAsync<HistoryPage>("history", new HistoryQuery(dm))).Messages[0].SeenBy.Contains("bob"), "Seen receipt persisted");
    await Reject(async () => await bob.RequestAsync<Notice>("read", new ReadRequest(dm, long.MaxValue)), "Invalid future read cursor rejected");
    await Reject(async () => await alice.RequestAsync<MessageView>("message.send", new SendMessage(dm, "  ", Guid.NewGuid().ToString("N"))), "Blank messages rejected");
    await Reject(async () => await alice.RequestAsync<MessageView>("message.send", new SendMessage(dm, new string('a', 4001), Guid.NewGuid().ToString("N"))), "Text length limit enforced on server");
    var fileBytes = Encoding.UTF8.GetBytes("Sample attachment Tiếng Việt");
    var attachment = await alice.RequestAsync<MessageView>("message.send", new SendMessage(dm, "", Guid.NewGuid().ToString("N"), "../../demo.txt", fileBytes));
    var download = await bob.RequestAsync<FileReply>("download", new FileRequest(attachment.Id));
    Check(download.FileName == "demo.txt" && download.Bytes.SequenceEqual(fileBytes), "Attachment bytes round trip; client path is stripped");
    string blockedTemp = Path.Combine(directory, "chat.json.tmp");
    Directory.CreateDirectory(blockedTemp);
    try
    {
        await Reject(async () => await alice.RequestAsync<MessageView>("message.send", new SendMessage(dm, "should not commit", Guid.NewGuid().ToString("N"))), "Failed disk write is not acknowledged as success");
        Check((await bob.RequestAsync<HistoryPage>("history", new HistoryQuery(dm, Search: "should not commit"))).Messages.Count == 0, "Failed transaction leaves in-memory history unchanged");
    }
    finally { Directory.Delete(blockedTemp); }
    await Reject(async () => await outsider.RequestAsync<FileReply>("download", new FileRequest(attachment.Id)), "Outsider cannot download private attachment");
    await Reject(async () => await alice.RequestAsync<MessageView>("message.send", new SendMessage(dm, "", Guid.NewGuid().ToString("N"), "big.bin", new byte[Limits.MaxFileBytes + 1])), "5 MB file limit enforced on server");
    string group = (await alice.RequestAsync<ConversationReply>("group.create", new GroupCreate("Nhóm NT106", ["bob", "charlie"]))).Id;
    Check((await charlie.RequestAsync<Snapshot>("sync", new { })).Conversations.Any(c => c.Id == group && c.Members.Count == 3), "Group creation visible to all members");
    await Reject(async () => await bob.RequestAsync<Notice>("group.member", new GroupMember(group, "outsider", true)), "Non-owner cannot add members");
    await alice.RequestAsync<Notice>("group.member", new GroupMember(group, "outsider", true));
    await outsider.RequestAsync<MessageView>("message.send", new SendMessage(group, "Hello group", Guid.NewGuid().ToString("N")));
    await alice.RequestAsync<Notice>("group.member", new GroupMember(group, "outsider", false));
    await Reject(async () => await outsider.RequestAsync<HistoryPage>("history", new HistoryQuery(group)), "Removed member loses history access immediately");
    await Reject(async () => await outsider.RequestAsync<MessageView>("message.send", new SendMessage(group, "blocked", Guid.NewGuid().ToString("N"))), "Removed member cannot send group messages");
    await Reject(async () => await alice.RequestAsync<Notice>("group.member", new GroupMember(group, "alice", false)), "Owner cannot remove themself");
    await Task.Delay(1100);
    // Exercise simultaneous writes from three connections, exceeding one history page.
    async Task SendBatch(ChatClient c, string prefix)
    {
        for (int i = 0; i < 20; i++)
        {
            await c.RequestAsync<MessageView>("message.send", new SendMessage(group, prefix + " #" + i, Guid.NewGuid().ToString("N")));
            await Task.Delay(70);
        }
    }
    await Task.WhenAll(SendBatch(alice, "alice"), SendBatch(bob, "bob"), SendBatch(charlie, "charlie"));
    var page1 = await alice.RequestAsync<HistoryPage>("history", new HistoryQuery(group));
    var page2 = await alice.RequestAsync<HistoryPage>("history", new HistoryQuery(group, page1.Messages[0].Id));
    var all = page1.Messages.Concat(page2.Messages).ToList();
    Check(page1.Messages.Count == 50 && page1.HasMore && page2.Messages.Count == 11 && !page2.HasMore, "History pagination has no missing messages");
    Check(all.Select(m => m.Id).Distinct().Count() == 61, "Concurrent senders have distinct server sequence IDs");
    Check((await alice.RequestAsync<HistoryPage>("history", new HistoryQuery(group, Search: "CHARLIE #19"))).Messages.Count == 1, "Case-insensitive search covers stored history");
    await alice.RequestAsync<Notice>("friend.remove", new UserTarget("bob"));
    Check(!(await bob.RequestAsync<Snapshot>("sync", new { })).Users.Single(u => u.Username == "alice").IsFriend, "Friend removal is symmetric");
    await bob.RequestAsync<Notice>("logout", new { });
    await Reject(async () => await bob.RequestAsync<HistoryPage>("history", new HistoryQuery(dm)), "Logout invalidates connection authentication");
    Check(!(await alice.RequestAsync<Snapshot>("sync", new { })).Users.Single(u => u.Username == "bob").Online, "Logout updates offline presence");
    var charlieSecond = await Connect();
    await charlieSecond.RequestAsync<Snapshot>("login", new Credentials("charlie", password));
    await charlie.DisposeAsync();
    Check((await alice.RequestAsync<Snapshot>("sync", new { })).Users.Single(u => u.Username == "charlie").Online, "One closed device does not mark another active device offline");
    await charlieSecond.DisposeAsync();
    await Until(async () => !(await alice.RequestAsync<Snapshot>("sync", new { })).Users.Single(u => u.Username == "charlie").Online, "Abrupt disconnect updates offline presence");
    string disk = await File.ReadAllTextAsync(Path.Combine(directory, "chat.json"));
    Check(!disk.Contains(password) && disk.Contains("passwordHash"), "No plaintext passwords in database");
    string fingerprint = server.Fingerprint;
    await server.DisposeAsync(); server = null;
    await Until(() => Task.FromResult(!alice.IsConnected), "Server shutdown notifies clients");
    server = new ChatServer(directory, IPAddress.Loopback, 0); server.Start();
    Check(server.Fingerprint == fingerprint, "Server certificate survives restart");
    var restored = await Connect(); await restored.RequestAsync<Snapshot>("login", new Credentials("alice", password));
    Check((await restored.RequestAsync<HistoryPage>("history", new HistoryQuery(group))).Messages.Count == 50, "Group history survives restart");
    Check((await restored.RequestAsync<FileReply>("download", new FileRequest(attachment.Id))).Bytes.SequenceEqual(fileBytes), "Attachments survive restart");
    Check((await restored.RequestAsync<HistoryPage>("history", new HistoryQuery(dm))).Messages[0].SeenBy.Contains("bob"), "Read receipts survive restart");
    Console.WriteLine($"\nALL {assertions} CHECKS PASSED.");
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }
finally
{
    foreach (var client in clients) await client.DisposeAsync();
    if (server is not null) await server.DisposeAsync();
}

internal sealed class FragmentStream(byte[] data) : MemoryStream(data)
{
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => base.ReadAsync(buffer[..Math.Min(3, buffer.Length)], ct);
}

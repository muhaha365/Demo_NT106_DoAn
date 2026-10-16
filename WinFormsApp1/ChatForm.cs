using UitChat.Core;

namespace WinFormsApp1;

internal sealed class ChatForm : Form
{
    private readonly ChatClient client;
    private Snapshot state;
    private readonly ListBox people = new() { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 80, BorderStyle = BorderStyle.None, BackColor = Color.White, IntegralHeight = false };
    private readonly TextBox filter = Theme.Input("Tìm bạn bè, cuộc trò chuyện…");
    private readonly ComboBox mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 258, Font = Theme.Font(10) };
    private readonly Label connection = Theme.Label("●  Đã kết nối", 9, Theme.Teal, true);
    private readonly Label title = Theme.Label("Không gian trò chuyện", 17, bold: true);
    private readonly Label subtitle = Theme.Label("Chọn một người bạn để bắt đầu.", 9, Theme.Muted);
    private readonly Label typing = Theme.Label("", 9, Theme.Teal);
    private readonly FlowLayoutPanel messages = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(20), BackColor = Theme.Canvas };
    private readonly TextBox composer = new() { Multiline = true, BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Font = Theme.Font(11), MaxLength = Limits.MaxTextLength, BackColor = Color.White, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox search = Theme.Input("Tìm trong lịch sử…");
    private readonly Button more = Theme.Button("Tin nhắn trước", false, 140);
    private readonly Button send = Theme.Button("Gửi  →", true, 90);
    private readonly Button attach = Theme.Button("Đính kèm", false, 105);
    private readonly Button manage = Theme.Button("Thành viên", false, 125);
    private readonly Button friend = Theme.Button("Kết bạn", false, 125);
    private readonly Button respond = Theme.Button("Chấp nhận", true, 125);
    private readonly Button decline = Theme.Button("Từ chối", false, 100);
    private readonly Panel composePanel = new() { Dock = DockStyle.Bottom, Height = 145, Padding = new Padding(20, 6, 20, 16), BackColor = Color.White };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 500 };
    private readonly Dictionary<string, DateTimeOffset> typers = [];
    private readonly List<MessageView> loaded = [];
    private string? selectedId;
    private string activeSearch = "";
    private int version;
    private long readThrough;
    private bool sending, closing, rebuilding, readBusy, historyBusy;
    private DateTimeOffset lastTyping = DateTimeOffset.MinValue, lastKey = DateTimeOffset.MinValue;
    private bool typingSent;
    private Toast? toast;
    private readonly Dictionary<string, string> drafts = [];
    private int selectionRequest;

    private sealed record Entry(string Key, string Name, string Detail, bool Online, int Unread, bool Group);

    public ChatForm(ChatClient client, Snapshot initial)
    {
        this.client = client; state = initial;
        Theme.Setup(this, $"UIT Chat • {state.Username}", new Size(1200, 800)); MinimumSize = new Size(1030, 700);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 298)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var rail = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Teal, Margin = Padding.Empty };
        var mark = Theme.Label("u.", 28, Color.White, true); mark.Location = new Point(17, 26); rail.Controls.Add(mark);
        var railChat = Theme.Label("CHAT", 9, Color.White, true); railChat.Location = new Point(13, 123); rail.Controls.Add(railChat);
        var railCourse = Theme.Label("NT106", 8, Color.FromArgb(185, 220, 213), true); railCourse.Dock = DockStyle.Bottom; railCourse.Height = 45; railCourse.TextAlign = ContentAlignment.MiddleCenter; rail.Controls.Add(railCourse);
        root.Controls.Add(rail, 0, 0);

        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = Padding.Empty, Padding = new Padding(0, 0, 1, 0) };
        var sideTop = new Panel { Dock = DockStyle.Top, Height = 209, Padding = new Padding(20) };
        var inbox = Theme.Label("Tin nhắn", 23, bold: true); inbox.Location = new Point(20, 23);
        connection.Location = new Point(22, 69);
        filter.SetBounds(20, 104, 258, 30); filter.Font = Theme.Font(10);
        mode.Items.AddRange(["Trò chuyện", "Bạn bè", "Mọi người", "Lời mời kết bạn", "Đang online"]); mode.SelectedIndex = 0; mode.Location = new Point(20, 151);
        sideTop.Controls.AddRange([inbox, connection, filter, mode]);
        var sideBottom = new Panel { Dock = DockStyle.Bottom, Height = 132, Padding = new Padding(20) };
        var group = Theme.Button("+  Tạo nhóm mới", false, 258); group.Location = new Point(20, 6);
        var avatar = new Avatar { Caption = state.Username, Online = true, Location = new Point(20, 66) };
        var me = Theme.Label(state.Username, 11, bold: true); me.Location = new Point(74, 64);
        var logout = new LinkLabel { Text = "Đăng xuất", AutoSize = true, LinkColor = Theme.Muted, Location = new Point(75, 91) };
        sideBottom.Controls.AddRange([group, avatar, me, logout]); sidebar.Controls.Add(people); sidebar.Controls.Add(sideTop); sidebar.Controls.Add(sideBottom);
        root.Controls.Add(sidebar, 1, 0);

        var main = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        var header = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White, Padding = new Padding(24, 18, 24, 12) };
        title.Location = new Point(24, 17); title.AutoSize = false; title.Size = new Size(440, 35); title.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        subtitle.Location = new Point(26, 56); subtitle.AutoSize = false; subtitle.Size = new Size(630, 24); subtitle.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        var topActions = Theme.Flow(false); topActions.Dock = DockStyle.Right; topActions.Controls.Add(manage); topActions.Controls.Add(friend);
        manage.Visible = friend.Visible = false; header.Controls.AddRange([title, subtitle, topActions]);
        var searchRow = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(20, 8, 20, 8), BackColor = Color.White };
        var searchFlow = Theme.Flow(false); searchFlow.Dock = DockStyle.Fill;
        search.Width = 210; search.Font = Theme.Font(10); search.Margin = new Padding(0, 8, 8, 0);
        var find = Theme.Button("Tìm", false, 58); var clear = Theme.Button("Bỏ lọc", false, 82);
        searchFlow.Controls.AddRange([search, find, clear, more]); searchRow.Controls.Add(searchFlow); more.Enabled = false;
        var requestActions = Theme.Flow(false); requestActions.Dock = DockStyle.Bottom; requestActions.Padding = new Padding(20, 0, 0, 0);
        respond.Visible = decline.Visible = false; requestActions.Controls.AddRange([respond, decline]);
        var composeTools = Theme.Flow(false); composeTools.Dock = DockStyle.Bottom; composeTools.Height = 47;
        var emoji = Theme.Button("☺  Emoji", false, 105);
        composeTools.Controls.AddRange([emoji, attach, send]);
        var hint = Theme.Label("Enter gửi · Shift + Enter xuống dòng", 8, Theme.Muted); hint.Margin = new Padding(8, 14, 0, 0); composeTools.Controls.Add(hint);
        var typingPanel = new Panel { Dock = DockStyle.Top, Height = 25 }; typing.Location = new Point(0, 0); typingPanel.Controls.Add(typing);
        composePanel.Controls.Add(composer); composePanel.Controls.Add(composeTools); composePanel.Controls.Add(typingPanel);
        main.Controls.Add(messages); main.Controls.Add(requestActions); main.Controls.Add(composePanel); main.Controls.Add(searchRow); main.Controls.Add(header);
        root.Controls.Add(main, 2, 0); Controls.Add(root);
        composePanel.Enabled = false;

        people.DrawItem += DrawEntry;
        people.SelectedIndexChanged += async (_, _) => { if (!rebuilding) await Theme.Guard(this, OpenSelectedAsync); };
        filter.TextChanged += (_, _) => Populate(); mode.SelectedIndexChanged += (_, _) => Populate();
        group.Click += async (_, _) => await Theme.Guard(this, CreateGroupAsync);
        logout.Click += async (_, _) => { await Theme.Guard(this, async () => { if (client.IsConnected) await client.RequestAsync<Notice>("logout", new { }); }); Close(); };
        send.Click += async (_, _) => await SendAsync(); attach.Click += async (_, _) => await SendFileAsync();
        composer.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; await SendAsync(); } };
        composer.TextChanged += async (_, _) =>
        {
            if (selectedId is null || !composer.Focused) return;
            lastKey = DateTimeOffset.UtcNow;
            if (lastKey - lastTyping > TimeSpan.FromSeconds(2)) { lastTyping = lastKey; typingSent = true; await QuietTypingAsync(true); }
        };
        emoji.Click += (_, _) =>
        {
            var menu = new ContextMenuStrip();
            foreach (string value in new[] { "😊", "😂", "❤️", "👍", "🎉", "😎", "🙏", "🔥" })
                menu.Items.Add(value, null, (_, _) => { composer.SelectedText = value; composer.Focus(); });
            menu.Closed += (_, _) => menu.Dispose(); menu.Show(emoji, new Point(0, -menu.PreferredSize.Height));
        };
        find.Click += async (_, _) => await Theme.Guard(this, () => SearchAsync(search.Text.Trim()));
        search.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await Theme.Guard(this, () => SearchAsync(search.Text.Trim())); } };
        clear.Click += async (_, _) => { search.Clear(); await Theme.Guard(this, () => SearchAsync("")); };
        more.Click += async (_, _) => await Theme.Guard(this, () => LoadHistoryAsync(true));
        friend.Click += async (_, _) => await Theme.Guard(this, FriendActionAsync);
        respond.Click += async (_, _) => await Theme.Guard(this, () => RespondAsync(true));
        decline.Click += async (_, _) => await Theme.Guard(this, () => RespondAsync(false));
        manage.Click += async (_, _) => await Theme.Guard(this, ManageGroupAsync);
        timer.Tick += async (_, _) =>
        {
            foreach (var key in typers.Where(t => DateTimeOffset.UtcNow - t.Value > TimeSpan.FromSeconds(4)).Select(t => t.Key).ToList()) typers.Remove(key);
            typing.Text = string.Join(", ", typers.Keys) + (typers.Count > 0 ? " đang nhập…" : "");
            if (typingSent && DateTimeOffset.UtcNow - lastKey > TimeSpan.FromSeconds(2)) { typingSent = false; await QuietTypingAsync(false); }
            await MarkReadAsync();
        };
        messages.Resize += (_, _) => ResizeMessages();
        client.EventReceived += OnEvent; client.Disconnected += OnDisconnected;
        Shown += async (_, _) =>
        {
            timer.Start();
            await Theme.Guard(this, async () => { state = await client.RequestAsync<Snapshot>("sync", new { }); Populate(); if (people.Items.Count > 0) people.SelectedIndex = 0; });
        };
        FormClosed += async (_, _) =>
        {
            closing = true; timer.Dispose(); toast?.Close();
            client.EventReceived -= OnEvent; client.Disconnected -= OnDisconnected;
            await client.DisposeAsync();
        };
        Populate(); RenderMessages();
    }

    private ConversationView? Current => state.Conversations.FirstOrDefault(c => c.Id == selectedId);
    private string? SelectedUser => (people.SelectedItem as Entry)?.Key is string key && key.StartsWith("user:") ? key[5..] : Current is { IsGroup: false } c ? c.Name : null;
    private void Populate()
    {
        string? previous = (people.SelectedItem as Entry)?.Key;
        rebuilding = true; people.BeginUpdate(); people.Items.Clear();
        string query = filter.Text.Trim();
        IEnumerable<Entry> entries;
        if (mode.SelectedIndex == 0)
            entries = state.Conversations.Select(c => new Entry(c.Id, c.Name, c.Preview, !c.IsGroup && state.Users.Any(u => u.Username == c.Name && u.Online), c.Unread, c.IsGroup));
        else
        {
            var users = state.Users.AsEnumerable();
            if (mode.SelectedIndex == 1) users = users.Where(u => u.IsFriend);
            if (mode.SelectedIndex == 4) users = users.Where(u => u.Online);
            if (mode.SelectedIndex == 3) users = users.Where(u => state.Requests.Any(r => r.From == u.Username || r.To == u.Username));
            entries = users.OrderByDescending(u => u.Online).ThenBy(u => u.Username).Select(u =>
                new Entry("user:" + u.Username, u.Username,
                    mode.SelectedIndex == 3 ? state.Requests.Any(r => r.From == u.Username && r.To == state.Username) ? "Đã gửi lời mời cho bạn" : "Đang chờ chấp nhận" :
                    (u.Online ? "Đang hoạt động" : "Offline") + (u.IsFriend ? " · Bạn bè" : ""), u.Online, 0, false));
        }
        foreach (var entry in entries.Where(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase))) people.Items.Add(entry);
        for (int i = 0; i < people.Items.Count; i++) if (((Entry)people.Items[i]).Key == previous) people.SelectedIndex = i;
        people.EndUpdate(); rebuilding = false;
        if (selectedId is not null && Current is null) { selectedId = null; version++; loaded.Clear(); RenderMessages(); }
        UpdateHeader();
    }
    private void DrawEntry(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var item = (Entry)people.Items[e.Index]; bool selected = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(selected ? Theme.Pale : Color.White); e.Graphics.FillRectangle(bg, e.Bounds);
        using var avatar = new SolidBrush(selected ? Color.White : Theme.Pale);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.FillEllipse(avatar, 16, e.Bounds.Y + 17, 42, 42);
        using var initialFont = Theme.Font(13, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, item.Group ? "#" : item.Name[..1].ToUpperInvariant(), initialFont, new Rectangle(16, e.Bounds.Y + 17, 42, 42), Theme.Teal, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (item.Online) { using var dot = new SolidBrush(Color.SeaGreen); e.Graphics.FillEllipse(dot, 48, e.Bounds.Y + 48, 10, 10); }
        using var nameFont = Theme.Font(10, FontStyle.Bold); using var detailFont = Theme.Font(9);
        TextRenderer.DrawText(e.Graphics, item.Name, nameFont, new Rectangle(70, e.Bounds.Y + 17, e.Bounds.Width - 112, 25), Theme.Ink, TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, item.Detail.Replace('\n', ' '), detailFont, new Rectangle(70, e.Bounds.Y + 43, e.Bounds.Width - 88, 23), Theme.Muted, TextFormatFlags.EndEllipsis);
        if (item.Unread > 0) TextRenderer.DrawText(e.Graphics, item.Unread > 99 ? "99+" : item.Unread.ToString(), detailFont, new Rectangle(e.Bounds.Width - 40, e.Bounds.Y + 18, 34, 23), Theme.Teal);
    }
    private async Task OpenSelectedAsync()
    {
        if (people.SelectedItem is not Entry entry || !client.IsConnected) return;
        int request = ++selectionRequest;
        string id = entry.Key;
        if (id.StartsWith("user:")) id = (await client.RequestAsync<ConversationReply>("direct.open", new UserTarget(id[5..]))).Id;
        if (request != selectionRequest) return;
        await SelectConversationAsync(id);
    }
    internal async Task SelectConversationAsync(string id)
    {
        if (selectedId == id) { UpdateHeader(); return; }
        if (selectedId is not null) { drafts[selectedId] = composer.Text; _ = QuietTypingAsync(false); }
        selectedId = id; composer.Text = drafts.GetValueOrDefault(id, ""); version++; loaded.Clear(); activeSearch = ""; search.Clear(); typers.Clear(); readThrough = 0; historyBusy = false;
        UpdateHeader(); RenderMessages(); await LoadHistoryAsync(false);
    }
    private void UpdateHeader()
    {
        var c = Current;
        title.Text = c?.Name ?? "Không gian trò chuyện";
        subtitle.Text = c is null ? "Chọn mục Mọi người để tìm bạn và bắt đầu chat." : c.IsGroup ? $"{c.Members.Count} thành viên · Trưởng nhóm: {c.Owner}" : state.Users.Any(u => u.Username == c.Name && u.Online) ? "● Đang hoạt động" : "Offline · Tin nhắn vẫn được lưu để đọc sau";
        composePanel.Enabled = c is not null && client.IsConnected;
        manage.Visible = c?.IsGroup == true;
        string? user = SelectedUser;
        friend.Visible = user is not null;
        friend.Text = state.Users.Any(u => u.Username == user && u.IsFriend) ? "Xóa bạn" : "Kết bạn";
        bool pending = state.Requests.Any(r => r.From == user && r.To == state.Username);
        respond.Visible = decline.Visible = pending;
    }
    private async Task SearchAsync(string query)
    {
        if (selectedId is null) return;
        activeSearch = query; version++; loaded.Clear(); historyBusy = false; RenderMessages(); await LoadHistoryAsync(false);
    }
    private async Task LoadHistoryAsync(bool older)
    {
        if (selectedId is null || historyBusy) return;
        int requestVersion = version; string id = selectedId;
        historyBusy = true; more.Enabled = false;
        try
        {
            var page = await client.RequestAsync<HistoryPage>("history", new HistoryQuery(id, older && loaded.Count > 0 ? loaded.Min(m => m.Id) : long.MaxValue, activeSearch));
            if (requestVersion != version || IsDisposed) return;
            foreach (var m in page.Messages) Merge(m);
            more.Enabled = page.HasMore;
            RenderMessages(!older); await MarkReadAsync();
        }
        finally { if (requestVersion == version) historyBusy = false; }
    }
    private void Merge(MessageView m)
    {
        int index = loaded.FindIndex(x => x.Id == m.Id);
        if (index < 0) loaded.Add(m);
        else loaded[index] = m with { SeenBy = loaded[index].SeenBy.Union(m.SeenBy).ToList() };
        loaded.Sort((a, b) => a.Id.CompareTo(b.Id));
    }
    private bool AtBottom => messages.VerticalScroll.Value >= messages.VerticalScroll.Maximum - messages.VerticalScroll.LargeChange - 28;
    private void RenderMessages(bool bottom = false)
    {
        int offset = messages.VerticalScroll.Value;
        messages.SuspendLayout();
        foreach (Control control in messages.Controls.Cast<Control>().ToArray()) control.Dispose();
        messages.Controls.Clear();
        if (loaded.Count == 0)
        {
            var empty = Theme.Label(selectedId is null ? "Câu chuyện bắt đầu từ một lời chào.\n\nChọn một cuộc trò chuyện, hoặc tìm bạn\ntrong mục Mọi người." : activeSearch.Length > 0 ? "Không tìm thấy tin nhắn phù hợp." : "Chưa có tin nhắn. Hãy gửi lời chào!", 14, Theme.Muted);
            empty.Margin = new Padding(22, 80, 0, 0); messages.Controls.Add(empty);
        }
        else
        {
            DateOnly? date = null;
            foreach (var m in loaded)
            {
                var currentDate = DateOnly.FromDateTime(m.SentAt.LocalDateTime);
                if (date != currentDate)
                {
                    var label = Theme.Label(currentDate.ToString("dd/MM/yyyy"), 9, Theme.Muted); label.TextAlign = ContentAlignment.MiddleCenter; label.AutoSize = false; label.Height = 32; label.Width = Math.Max(220, messages.ClientSize.Width - 62); messages.Controls.Add(label); date = currentDate;
                }
                var bubble = new MessageBubble(m, m.Sender == state.Username, Current?.IsGroup == true);
                bubble.SetRowWidth(Math.Max(240, messages.ClientSize.Width - 62));
                if (m.FileName is not null) bubble.Click += async (_, _) => await Theme.Guard(this, () => OpenFileAsync(m));
                messages.Controls.Add(bubble);
            }
        }
        messages.ResumeLayout(true);
        if (bottom && messages.Controls.Count > 0) messages.ScrollControlIntoView(messages.Controls[^1]);
        else messages.AutoScrollPosition = new Point(0, offset);
    }
    private void ResizeMessages()
    {
        foreach (Control control in messages.Controls)
            if (control is MessageBubble bubble) bubble.SetRowWidth(Math.Max(240, messages.ClientSize.Width - 62));
            else if (control is Label { AutoSize: false }) control.Width = Math.Max(220, messages.ClientSize.Width - 62);
    }
    private async Task SendAsync()
    {
        if (sending || selectedId is null || string.IsNullOrWhiteSpace(composer.Text)) return;
        string id = selectedId, text = composer.Text;
        sending = true; send.Enabled = false;
        await Theme.Guard(this, async () =>
        {
            var message = await client.RequestAsync<MessageView>("message.send", new SendMessage(id, text, Guid.NewGuid().ToString("N")));
            if (selectedId == id)
            {
                if (composer.Text == text) composer.Clear();
                if (activeSearch.Length == 0) { Merge(message); RenderMessages(true); }
                composer.Focus(); await QuietTypingAsync(false);
            }
            drafts.Remove(id);
        });
        sending = false; if (!IsDisposed) send.Enabled = client.IsConnected;
    }
    private async Task SendFileAsync()
    {
        if (sending || selectedId is null) return;
        using var picker = new OpenFileDialog { Title = "Chọn tệp, ảnh hoặc video (tối đa 5 MB)", Filter = "Tất cả tệp|*.*" };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        string id = selectedId; sending = true; attach.Enabled = false;
        await Theme.Guard(this, async () =>
        {
            var info = new FileInfo(picker.FileName);
            if (info.Length > Limits.MaxFileBytes) throw new ArgumentException("Tệp vượt giới hạn 5 MB.");
            byte[] bytes = await File.ReadAllBytesAsync(picker.FileName);
            var result = await client.RequestAsync<MessageView>("message.send", new SendMessage(id, "", Guid.NewGuid().ToString("N"), info.Name, bytes));
            if (selectedId == id && activeSearch.Length == 0) { Merge(result); RenderMessages(true); }
        });
        sending = false; if (!IsDisposed) attach.Enabled = client.IsConnected;
    }
    private async Task OpenFileAsync(MessageView message)
    {
        var file = await client.RequestAsync<FileReply>("download", new FileRequest(message.Id));
        using var dialog = new AttachmentForm(file); dialog.ShowDialog(this);
    }
    private async Task MarkReadAsync()
    {
        if (readBusy || closing || !client.IsConnected || selectedId is null || activeSearch.Length > 0 || loaded.Count == 0 || !ContainsFocus || WindowState == FormWindowState.Minimized || !AtBottom) return;
        long last = loaded.Max(m => m.Id); if (last <= readThrough) return;
        string id = selectedId; readBusy = true;
        try { await client.RequestAsync<Notice>("read", new ReadRequest(id, last)); if (selectedId == id) readThrough = last; }
        catch (Exception) { /* A lost connection is shown by the disconnected event. */ }
        finally { readBusy = false; }
    }
    private async Task QuietTypingAsync(bool active)
    {
        if (selectedId is null || !client.IsConnected) return;
        try { await client.RequestAsync<Notice>("typing", new TypingRequest(selectedId, active)); }
        catch (Exception) { }
    }
    private void OnEvent(Packet packet) => Theme.Ui(this, () =>
    {
        switch (packet.Type)
        {
            case "state": state = Json.Read<Snapshot>(packet.Data); Populate(); break;
            case "message":
                var m = Json.Read<MessageView>(packet.Data);
                if (m.ConversationId == selectedId && activeSearch.Length == 0) { bool scroll = AtBottom; Merge(m); typers.Remove(m.Sender); RenderMessages(scroll); }
                if (m.Sender != state.Username && (m.ConversationId != selectedId || !ContainsFocus || WindowState == FormWindowState.Minimized)) Notify(m.Sender, m.FileName is null ? m.Text : "Đã gửi tệp: " + m.FileName);
                break;
            case "typing":
                var t = Json.Read<TypingEvent>(packet.Data);
                if (t.ConversationId == selectedId) { if (t.IsTyping) typers[t.Username] = DateTimeOffset.UtcNow; else typers.Remove(t.Username); }
                break;
            case "read":
                var r = Json.Read<ReadEvent>(packet.Data);
                if (r.ConversationId == selectedId)
                {
                    foreach (var message in loaded.Where(m => m.Id <= r.ThroughId)) if (!message.SeenBy.Contains(r.Username)) message.SeenBy.Add(r.Username);
                    RenderMessages();
                }
                break;
            case "notice": Notify("Thông báo", Json.Read<Notice>(packet.Data).Text); break;
        }
    });
    private void OnDisconnected(string reason) => Theme.Ui(this, () =>
    {
        connection.Text = "●  Mất kết nối"; connection.ForeColor = Color.Firebrick; composePanel.Enabled = false;
        subtitle.Text = reason + " Đóng cửa sổ rồi đăng nhập lại."; timer.Stop();
    });
    private void Notify(string caption, string text)
    {
        toast?.Close(); toast = new Toast(caption, text.Length > 110 ? text[..110] + "…" : text); toast.Show();
    }
    private async Task FriendActionAsync()
    {
        string? user = SelectedUser; if (user is null) return;
        bool remove = state.Users.Any(u => u.Username == user && u.IsFriend);
        if (remove && MessageBox.Show(this, $"Xóa {user} khỏi danh sách bạn bè?", "Bạn bè", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        await client.RequestAsync<Notice>(remove ? "friend.remove" : "friend.request", new UserTarget(user));
        if (!remove) Notify("Bạn bè", "Đã gửi lời mời kết bạn.");
    }
    private async Task RespondAsync(bool accept)
    {
        string? user = SelectedUser; if (user is null) return;
        await client.RequestAsync<Notice>("friend.respond", new FriendDecision(user, accept));
    }
    private async Task CreateGroupAsync()
    {
        using var dialog = new GroupForm(state);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var result = await client.RequestAsync<ConversationReply>("group.create", new GroupCreate(dialog.GroupName, dialog.SelectedMembers));
        state = await client.RequestAsync<Snapshot>("sync", new { }); mode.SelectedIndex = 0; Populate();
        await SelectConversationAsync(result.Id);
    }
    private async Task ManageGroupAsync()
    {
        if (Current is not { IsGroup: true } c) return;
        using var dialog = new MembersForm(client, state, c); dialog.ShowDialog(this);
        state = await client.RequestAsync<Snapshot>("sync", new { }); Populate();
    }
}

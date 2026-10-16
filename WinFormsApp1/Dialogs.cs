using UitChat.Core;

namespace WinFormsApp1;

internal sealed class GroupForm : Form
{
    private readonly TextBox name = Theme.Input("Tên nhóm học tập…");
    private readonly CheckedListBox members = new() { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.None };
    public string GroupName => name.Text.Trim();
    public List<string> SelectedMembers => members.CheckedItems.Cast<string>().ToList();
    public GroupForm(Snapshot state)
    {
        Theme.Setup(this, "Tạo nhóm", new Size(420, 510)); Padding = new Padding(24); MinimizeBox = MaximizeBox = false;
        var top = Theme.Flow(); top.Dock = DockStyle.Top; top.Controls.Add(Theme.Label("Cùng nhau trò chuyện", 19, bold: true)); top.Controls.Add(name); top.Controls.Add(Theme.Label("Chọn thành viên · Tối đa 50 người", 10, Theme.Muted));
        foreach (var user in state.Users.OrderBy(u => u.Username)) members.Items.Add(user.Username);
        var ok = Theme.Button("Tạo nhóm", true); ok.Dock = DockStyle.Bottom;
        ok.Click += (_, _) => { if (GroupName.Length is < 1 or > 60 || SelectedMembers.Count is < 1 or > 49) { MessageBox.Show(this, "Nhập tên nhóm (1–60 ký tự) và chọn 1–49 thành viên."); return; } DialogResult = DialogResult.OK; };
        Controls.Add(members); Controls.Add(top); Controls.Add(ok);
    }
}
internal sealed class MembersForm : Form
{
    private readonly ListBox list = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
    private readonly ComboBox available = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
    public MembersForm(ChatClient client, Snapshot state, ConversationView conversation)
    {
        Theme.Setup(this, "Thành viên • " + conversation.Name, new Size(450, 500)); Padding = new Padding(24);
        var title = Theme.Label(conversation.Name, 20, bold: true); title.Dock = DockStyle.Top;
        var note = Theme.Label("Trưởng nhóm: " + conversation.Owner, 10, Theme.Muted); note.Dock = DockStyle.Top; note.Height = 36;
        var actions = Theme.Flow(); actions.Dock = DockStyle.Bottom;
        var add = Theme.Button("Thêm thành viên", true, 180); var remove = Theme.Button("Xóa thành viên", false, 180);
        actions.Controls.AddRange([available, add, remove]); actions.Enabled = conversation.Owner == state.Username;
        void PopulateMembers(ConversationView current)
        {
            list.Items.Clear(); foreach (string user in current.Members) list.Items.Add(user);
            available.Items.Clear(); foreach (var user in state.Users.Where(u => !current.Members.Contains(u.Username))) available.Items.Add(user.Username);
            if (available.Items.Count > 0) available.SelectedIndex = 0;
        }
        async Task ChangeAsync(bool adding)
        {
            string? user = (adding ? available.SelectedItem : list.SelectedItem) as string; if (user is null) return;
            if (!adding && MessageBox.Show(this, $"Xóa {user} khỏi nhóm?", "Thành viên", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            await client.RequestAsync<Notice>("group.member", new GroupMember(conversation.Id, user, adding));
            var latest = await client.RequestAsync<Snapshot>("sync", new { });
            var c = latest.Conversations.FirstOrDefault(c => c.Id == conversation.Id); if (c is null) Close(); else PopulateMembers(c);
        }
        add.Click += async (_, _) => await Theme.Guard(this, () => ChangeAsync(true)); remove.Click += async (_, _) => await Theme.Guard(this, () => ChangeAsync(false));
        Controls.Add(list); Controls.Add(actions); Controls.Add(note); Controls.Add(title); PopulateMembers(conversation);
    }
}
internal sealed class AttachmentForm : Form
{
    private Image? preview;
    public AttachmentForm(FileReply file)
    {
        Theme.Setup(this, "Tệp đính kèm • " + file.FileName, new Size(720, 550)); Padding = new Padding(20);
        var name = Theme.Label(file.FileName, 15, bold: true); name.Dock = DockStyle.Top;
        var save = Theme.Button("Lưu tệp…", true, 150); save.Dock = DockStyle.Bottom;
        save.Click += async (_, _) => await Theme.Guard(this, async () =>
        {
            using var picker = new SaveFileDialog { FileName = Path.GetFileName(file.FileName), Filter = "Tất cả tệp|*.*", OverwritePrompt = true };
            if (picker.ShowDialog(this) == DialogResult.OK) { await File.WriteAllBytesAsync(picker.FileName, file.Bytes); MessageBox.Show(this, "Đã lưu tệp."); }
        });
        Control content = Theme.Label($"{file.Bytes.Length / 1024.0:N1} KB\n\nLưu tệp để mở bằng ứng dụng trên máy.\nVideo được gửi dưới dạng tệp, chưa phát trực tiếp trong ứng dụng.", 12, Theme.Muted);
        if (new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" }.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
        {
            try
            {
                using var buffer = new MemoryStream(file.Bytes);
                using var source = Image.FromStream(buffer, false, true);
                if ((long)source.Width * source.Height > 25_000_000) throw new ArgumentException("Ảnh quá lớn để xem trước.");
                preview = new Bitmap(source); content = new PictureBox { Image = preview, SizeMode = PictureBoxSizeMode.Zoom };
            }
            catch (Exception ex) when (ex is ArgumentException or OutOfMemoryException) { content = Theme.Label("Không thể xem trước ảnh này. Bạn vẫn có thể lưu tệp."); }
        }
        content.Dock = DockStyle.Fill; Controls.Add(content); Controls.Add(name); Controls.Add(save);
        FormClosed += (_, _) => preview?.Dispose();
    }
}

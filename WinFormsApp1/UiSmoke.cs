using System.Net;
using UitChat.Core;
using UitChat.Server;

namespace WinFormsApp1;

// Explicit test mode only. Uses a temporary server and real TCP clients, never production data.
internal static class UiSmoke
{
    public static void Run()
    {
        string output = Path.GetFullPath(Path.Combine("artifacts", "ui")); Directory.CreateDirectory(output);
        var runner = new Form { ShowInTaskbar = false, Opacity = 0, Size = new Size(1, 1) };
        runner.Shown += async (_, _) =>
        {
            try { await ExerciseAsync(output); File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: real TCP UI scenario, screenshots and composer send."); }
            catch (Exception ex) { Environment.ExitCode = 1; File.WriteAllText(Path.Combine(output, "result.txt"), ex.ToString()); }
            finally { runner.Close(); }
        };
        Application.Run(runner);
    }
    private static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void Capture(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height); form.DrawToBitmap(bitmap, form.ClientRectangle with { Width = form.Width, Height = form.Height }); bitmap.Save(path);
    }
    private static void ShowForTest(Form form)
    {
        form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-6000, -6000); form.ShowInTaskbar = false; form.Show();
    }
    private static async Task ExerciseAsync(string output)
    {
        string directory = Path.Combine(Path.GetTempPath(), "UITChat-ui-" + Guid.NewGuid().ToString("N"));
        await using var server = new ChatServer(directory, IPAddress.Loopback, 0); server.Start();
        await using var alice = new ChatClient(); await using var bob = new ChatClient(); await using var chi = new ChatClient();
        var clients = new[] { (alice, "minh_anh"), (bob, "hoang_nam"), (chi, "bao_chi") };
        foreach (var (client, user) in clients)
        {
            await client.ConnectAsync("127.0.0.1", server.Port, server.Fingerprint);
            await client.RequestAsync<Notice>("register", new Credentials(user, "DemoChat2026!"));
            await client.RequestAsync<Snapshot>("login", new Credentials(user, "DemoChat2026!"));
        }
        await alice.RequestAsync<Notice>("friend.request", new UserTarget("hoang_nam"));
        await bob.RequestAsync<Notice>("friend.respond", new FriendDecision("minh_anh", true));
        string group = (await alice.RequestAsync<ConversationReply>("group.create", new GroupCreate("Nhóm đồ án NT106", ["hoang_nam", "bao_chi"]))).Id;
        string direct = (await alice.RequestAsync<ConversationReply>("direct.open", new UserTarget("hoang_nam"))).Id;
        await bob.RequestAsync<MessageView>("message.send", new SendMessage(direct, "Chiều nay mình cùng test kết nối LAN nhé?", Guid.NewGuid().ToString("N")));
        var samples = new[] { (bob, "Chào mọi người! Mình vừa hoàn thành phần TCP server 🎉"), (chi, "Tuyệt! Mình sẽ kiểm thử phần chat nhóm và trạng thái online."), (alice, "Mình đã cập nhật giao diện. Chiều nay nhóm mình demo cùng nhau nhé 😊"), (bob, "OK, mình chuẩn bị luôn kịch bản cho báo cáo giữa kỳ."), (alice, "Nhớ kiểm tra cả trường hợp mất kết nối nữa nha!") };
        MessageView? last = null;
        foreach (var (c, text) in samples) last = await c.RequestAsync<MessageView>("message.send", new SendMessage(group, text, Guid.NewGuid().ToString("N")));
        await bob.RequestAsync<Notice>("read", new ReadRequest(group, last!.Id));
        var initial = await alice.RequestAsync<Snapshot>("sync", new { });
        using var chat = new ChatForm(alice, initial); ShowForTest(chat); await Task.Delay(400); await chat.SelectConversationAsync(group); await Task.Delay(250);
        Capture(chat, Path.Combine(output, "chat.png"));
        var composer = Descendants(chat).OfType<TextBox>().Single(t => t.Multiline && !t.ReadOnly);
        composer.Text = "UI smoke: gửi bằng nút WinForms";
        Descendants(chat).OfType<Button>().Single(b => b.Text == "Gửi  →").PerformClick();
        bool sent = false;
        for (int i = 0; i < 40; i++)
        {
            await Task.Delay(100);
            if ((await bob.RequestAsync<HistoryPage>("history", new HistoryQuery(group, Search: "UI smoke"))).Messages.Count == 1) { sent = true; break; }
        }
        if (!sent) throw new Exception("WinForms send button did not deliver a message.");
        using var login = new LoginForm(); ShowForTest(login); await Task.Delay(100); Capture(login, Path.Combine(output, "login.png"));
        Descendants(login).OfType<Button>().Single(b => b.Text == "Chưa có tài khoản? Đăng ký").PerformClick(); await Task.Delay(100); Capture(login, Path.Combine(output, "register.png"));
        using var groupDialog = new GroupForm(initial); ShowForTest(groupDialog); Capture(groupDialog, Path.Combine(output, "group.png"));
        using var serverForm = new ServerForm(); ShowForTest(serverForm); Capture(serverForm, Path.Combine(output, "server.png"));
        groupDialog.Close(); serverForm.Close(); login.Close(); chat.Close();
    }
}

using System.Text.Json;
using UitChat.Core;

namespace WinFormsApp1;

internal sealed record ClientSettings(string Host, int Port, string Fingerprint, string Username);
internal sealed class LoginForm : Form
{
    private readonly TextBox host = Theme.Input("127.0.0.1");
    private readonly NumericUpDown port = new() { Minimum = 1, Maximum = 65535, Value = Limits.Port, Width = 340 };
    private readonly TextBox pin = Theme.Input("SHA-256 từ server");
    private readonly TextBox username = Theme.Input("Ví dụ: minh_anh");
    private readonly TextBox password = Theme.Input("Ít nhất 8 ký tự", true);
    private readonly TextBox confirm = Theme.Input("Nhập lại mật khẩu", true);
    private readonly Label status = Theme.Label("", 9, Theme.Muted);
    private readonly Button submit = Theme.Button("Đăng nhập  →", true, 340);
    private readonly Button switchMode = Theme.Button("Chưa có tài khoản? Đăng ký", false, 340);
    private readonly Label confirmLabel = Theme.Label("Xác nhận mật khẩu", 9, Theme.Muted, true);
    private readonly Label heading = Theme.Label("Chào mừng trở lại", 23, bold: true);
    private readonly Panel confirmPanel = new() { Width = 340, Height = 65, Visible = false, Margin = Padding.Empty };
    private bool registering;
    private bool busy;
    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UITChat", "client.json");

    public LoginForm()
    {
        Theme.Setup(this, "UIT Chat • Kết nối", new Size(850, 770));
        MinimumSize = new Size(870, 740); MaximizeBox = false;
        var brand = new Panel { Dock = DockStyle.Left, Width = 370, BackColor = Theme.Teal, Padding = new Padding(35) };
        var logo = Theme.Label("uit / chat", 25, Color.White, true); logo.Location = new Point(35, 38);
        var slogan = Theme.Label("Kết nối bạn bè.\nChia sẻ mỗi ngày.", 22, Color.White, true); slogan.Location = new Point(35, 195);
        var desc = Theme.Label("Gặp bạn bè, chia sẻ ý tưởng\nvà học cùng nhau mỗi ngày.", 12, Color.FromArgb(201, 233, 222)); desc.Location = new Point(37, 335);
        var badge = Theme.Label("●  TCP SOCKET  /  KẾT NỐI TLS", 9, Color.FromArgb(201, 233, 222), true); badge.Location = new Point(37, 600);
        var course = Theme.Label("NT106 · Lập trình mạng căn bản", 9, Color.FromArgb(201, 233, 222)); course.Location = new Point(37, 628);
        brand.Controls.AddRange([logo, slogan, desc, badge, course]);
        var right = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var form = Theme.Flow(); form.Location = new Point(40, 24); form.Width = 360;
        form.Controls.Add(heading); form.Controls.Add(Theme.Label("Kết nối với server của nhóm bạn.", 10, Theme.Muted));
        form.Controls.Add(Theme.Label("Địa chỉ server", 9, Theme.Muted, true)); form.Controls.Add(host);
        form.Controls.Add(Theme.Label("Cổng TCP", 9, Theme.Muted, true)); form.Controls.Add(port);
        form.Controls.Add(Theme.Label("Dấu vân tay chứng chỉ (SHA-256)", 9, Theme.Muted, true)); form.Controls.Add(pin);
        var local = Theme.Button("Lấy mã server trên máy này", false, 340); local.Height = 30;
        local.Click += async (_, _) => await Theme.Guard(this, () => { LoadLocalPin(); return Task.CompletedTask; }); form.Controls.Add(local);
        form.Controls.Add(Theme.Label("Tên đăng nhập", 9, Theme.Muted, true)); form.Controls.Add(username);
        form.Controls.Add(Theme.Label("Mật khẩu", 9, Theme.Muted, true)); form.Controls.Add(password);
        confirmLabel.Location = new Point(0, 0); confirm.Location = new Point(0, 24);
        confirmPanel.Controls.AddRange([confirmLabel, confirm]); form.Controls.Add(confirmPanel);
        form.Controls.Add(submit); form.Controls.Add(switchMode);
        status.MaximumSize = new Size(350, 0); form.Controls.Add(status);
        right.Controls.Add(form); Controls.Add(right); Controls.Add(brand);
        submit.Click += async (_, _) => await SubmitAsync();
        switchMode.Click += (_, _) =>
        {
            registering = !registering; confirmPanel.Visible = registering;
            heading.Text = registering ? "Tạo tài khoản" : "Chào mừng trở lại";
            submit.Text = registering ? "Tạo tài khoản  →" : "Đăng nhập  →";
            switchMode.Text = registering ? "Đã có tài khoản? Đăng nhập" : "Chưa có tài khoản? Đăng ký";
            status.Text = "";
        };
        AcceptButton = submit;
        host.Text = "127.0.0.1";
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<ClientSettings>(File.ReadAllText(SettingsPath));
                if (settings is not null) { host.Text = settings.Host; port.Value = Math.Clamp(settings.Port, 1, 65535); pin.Text = settings.Fingerprint; username.Text = settings.Username; }
            }
            if (host.Text is "127.0.0.1" or "localhost") LoadLocalPin();
        }
        catch (Exception ex) when (ex is IOException or JsonException) { status.Text = "Nhập thông tin kết nối của server để bắt đầu."; }
    }
    private void LoadLocalPin()
    {
        if (host.Text.Trim() is not ("127.0.0.1" or "localhost")) throw new InvalidOperationException("Với server LAN, hãy chép mã trực tiếp từ cửa sổ server đó.");
        string path = Path.Combine(Limits.DefaultDataDirectory, "fingerprint.txt");
        if (!File.Exists(path)) throw new IOException("Hãy mở và khởi động server trên máy này trước.");
        pin.Text = File.ReadAllText(path).Trim();
    }
    private async Task SubmitAsync()
    {
        if (busy) return;
        busy = true; submit.Enabled = switchMode.Enabled = false;
        ChatClient? client = null;
        try
        {
            if (registering && password.Text != confirm.Text) throw new ArgumentException("Hai mật khẩu chưa khớp.");
            status.Text = "Đang kết nối…";
            client = new ChatClient();
            await client.ConnectAsync(host.Text.Trim(), (int)port.Value, pin.Text.Trim());
            var credentials = new Credentials(username.Text.Trim(), password.Text);
            if (registering)
            {
                var result = await client.RequestAsync<Notice>("register", credentials);
                status.Text = result.Text;
                registering = false; confirmPanel.Visible = false; heading.Text = "Chào mừng trở lại";
                submit.Text = "Đăng nhập  →"; switchMode.Text = "Chưa có tài khoản? Đăng ký";
            }
            else
            {
                var snapshot = await client.RequestAsync<Snapshot>("login", credentials);
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new ClientSettings(host.Text.Trim(), (int)port.Value, pin.Text.Trim(), username.Text.Trim())));
                password.Clear(); confirm.Clear();
                var chat = new ChatForm(client, snapshot); client = null;
                chat.FormClosed += (_, _) => { if (!IsDisposed) { Show(); status.Text = "Đã kết thúc phiên. Đăng nhập để tiếp tục."; } };
                Hide(); chat.Show();
            }
        }
        catch (Exception ex) { status.Text = ex.Message; }
        finally
        {
            if (client is not null) await client.DisposeAsync();
            if (!IsDisposed) submit.Enabled = switchMode.Enabled = true;
            busy = false;
        }
    }
}

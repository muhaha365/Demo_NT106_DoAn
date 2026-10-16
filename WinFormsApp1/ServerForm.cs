using System.Net;
using UitChat.Core;
using UitChat.Server;

namespace WinFormsApp1;

internal sealed class ServerForm : Form
{
    private ChatServer? server;
    private readonly TextBox address = Theme.Input("127.0.0.1");
    private readonly NumericUpDown port = new() { Minimum = 1, Maximum = 65535, Value = Limits.Port, Width = 130 };
    private readonly TextBox fingerprint = new() { ReadOnly = true, Width = 630, Font = Theme.Font(10) };
    private readonly TextBox log = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = Color.White, BorderStyle = BorderStyle.None, Font = Theme.Font(10) };
    private readonly Label state = Theme.Label("●  Server đang dừng", 12, Theme.Muted, true);
    private readonly Button start = Theme.Button("Khởi động", true);
    private readonly Button stop = Theme.Button("Dừng server");
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 2000 };
    private bool closing;

    public ServerForm()
    {
        Theme.Setup(this, "UIT Chat • Server", new Size(740, 650)); MinimumSize = new Size(760, 690); Padding = new Padding(30);
        var top = Theme.Flow(); top.Dock = DockStyle.Top;
        top.Controls.Add(Theme.Label("Trạm kết nối", 25, bold: true));
        top.Controls.Add(Theme.Label("Một server trung tâm · Nhiều client đồng thời", 10, Theme.Muted));
        top.Controls.Add(state);
        top.Controls.Add(Theme.Label("IP lắng nghe • 127.0.0.1 cho máy này, 0.0.0.0 cho LAN", 9));
        address.Text = "127.0.0.1";
        var row = Theme.Flow(false); row.Controls.Add(address); row.Controls.Add(port); top.Controls.Add(row);
        var actions = Theme.Flow(false); actions.Controls.AddRange([start, stop]); top.Controls.Add(actions); stop.Enabled = false;
        top.Controls.Add(Theme.Label("Mã SHA-256 • Chia sẻ mã này cho các client trong nhóm", 9)); top.Controls.Add(fingerprint);
        var copy = Theme.Button("Sao chép mã", false, 150); copy.Click += (_, _) => { if (fingerprint.Text.Length > 0) Clipboard.SetText(fingerprint.Text); }; top.Controls.Add(copy);
        var path = Theme.Label("Dữ liệu: " + Limits.DefaultDataDirectory, 8, Theme.Muted); path.MaximumSize = new Size(640, 0); top.Controls.Add(path);
        top.Controls.Add(Theme.Label("NHẬT KÝ HOẠT ĐỘNG", 9, Theme.Muted, true));
        Controls.Add(log); Controls.Add(top);
        start.Click += async (_, _) => await Theme.Guard(this, StartAsync);
        stop.Click += async (_, _) => await Theme.Guard(this, StopAsync);
        timer.Tick += (_, _) => { if (server is not null) state.Text = $"●  Đang hoạt động · {server.OnlineCount} người online · Cổng {server.Port}"; }; timer.Start();
        FormClosing += async (_, e) =>
        {
            if (closing || server is null) return;
            e.Cancel = true;
            if (MessageBox.Show(this, "Dừng server sẽ ngắt kết nối mọi client. Tiếp tục?", "Dừng server", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            await StopAsync(); closing = true; Close();
        };
        FormClosed += (_, _) => timer.Dispose();
    }
    private Task StartAsync()
    {
        if (server is not null) return Task.CompletedTask;
        var candidate = new ChatServer(Limits.DefaultDataDirectory, IPAddress.Parse(address.Text.Trim()), (int)port.Value);
        candidate.Log += line => Theme.Ui(this, () =>
        {
            if (log.TextLength > 30000) log.Clear();
            log.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
        });
        try { candidate.Start(); }
        catch { candidate.DisposeAsync().AsTask().GetAwaiter().GetResult(); throw; }
        server = candidate; fingerprint.Text = server.Fingerprint;
        state.Text = "●  Server sẵn sàng"; state.ForeColor = Theme.Teal;
        start.Enabled = address.Enabled = port.Enabled = false; stop.Enabled = true;
        return Task.CompletedTask;
    }
    private async Task StopAsync()
    {
        stop.Enabled = false;
        if (server is not null) { var old = server; server = null; await old.DisposeAsync(); }
        state.Text = "●  Server đang dừng"; state.ForeColor = Theme.Muted;
        start.Enabled = address.Enabled = port.Enabled = true;
    }
}

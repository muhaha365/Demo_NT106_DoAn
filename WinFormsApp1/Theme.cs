using System.Drawing.Drawing2D;

namespace WinFormsApp1;

internal static class Theme
{
    public static readonly Color Ink = Color.FromArgb(24, 45, 48);
    public static readonly Color Muted = Color.FromArgb(107, 125, 128);
    public static readonly Color Teal = Color.FromArgb(15, 118, 110);
    public static readonly Color Pale = Color.FromArgb(229, 243, 237);
    public static readonly Color Canvas = Color.FromArgb(246, 248, 247);
    public static readonly Color Line = Color.FromArgb(225, 233, 229);
    public static Font Font(float size = 10, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style);
    public static void Setup(Form form, string title, Size size)
    {
        form.Text = title; form.Font = Font(); form.ForeColor = Ink; form.BackColor = Canvas;
        form.ClientSize = size; form.StartPosition = FormStartPosition.CenterScreen;
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.Icon = SystemIcons.Application;
    }
    public static Label Label(string text, float size = 10, Color? color = null, bool bold = false) => new()
    { Text = text, AutoSize = true, Font = Font(size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color ?? Ink, BackColor = Color.Transparent, Margin = new Padding(0, 4, 0, 8) };
    public static Button Button(string text, bool primary = false, int width = 130) => new()
    {
        Text = text, Width = width, Height = 38, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        BackColor = primary ? Teal : Color.White, ForeColor = primary ? Color.White : Ink,
        Font = Font(10, FontStyle.Bold), Margin = new Padding(0, 4, 8, 4),
        FlatAppearance = { BorderSize = primary ? 0 : 1, BorderColor = Line }
    };
    public static TextBox Input(string placeholder, bool password = false) => new()
    { PlaceholderText = placeholder, UseSystemPasswordChar = password, Font = Font(11), Width = 340, Margin = new Padding(0, 0, 0, 14) };
    public static FlowLayoutPanel Flow(bool vertical = true) => new()
    { FlowDirection = vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
    public static void Error(IWin32Window owner, Exception ex) => MessageBox.Show(owner, ex.Message, "UIT Chat", MessageBoxButtons.OK, MessageBoxIcon.Information);
    public static async Task Guard(Form owner, Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { if (!owner.IsDisposed) Error(owner, ex); }
    }
    public static void Ui(Control control, Action action)
    {
        if (control.IsDisposed || !control.IsHandleCreated) return;
        try { control.BeginInvoke(() => { if (!control.IsDisposed) action(); }); }
        catch (InvalidOperationException) { }
    }
}

internal sealed class Avatar : Control
{
    [System.ComponentModel.DefaultValue("U")]
    public string Caption { get; set; } = "U";
    [System.ComponentModel.DefaultValue(false)]
    public bool Online { get; set; }
    public Avatar() { Size = new Size(44, 44); DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(Theme.Pale);
        e.Graphics.FillEllipse(fill, 0, 0, Width - 4, Height - 4);
        using var font = Theme.Font(14, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, Caption.Length > 0 ? Caption[..1].ToUpperInvariant() : "#", font,
            new Rectangle(0, 0, Width - 4, Height - 4), Theme.Teal, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (Online)
        {
            using var green = new SolidBrush(Color.FromArgb(55, 179, 126));
            using var outline = new Pen(Color.White, 2);
            e.Graphics.FillEllipse(green, Width - 15, Height - 15, 11, 11);
            e.Graphics.DrawEllipse(outline, Width - 15, Height - 15, 11, 11);
        }
    }
}

internal sealed class Toast : Form
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 4500 };
    protected override bool ShowWithoutActivation => true;
    public Toast(string title, string message)
    {
        Theme.Setup(this, title, new Size(340, 106));
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; BackColor = Theme.Teal;
        var a = Theme.Label(title, 11, Color.White, true); a.Location = new Point(18, 15);
        var b = Theme.Label(message, 10, Color.White); b.AutoSize = false; b.SetBounds(18, 44, 304, 52);
        Controls.AddRange([a, b]);
        StartPosition = FormStartPosition.Manual;
        var area = Screen.PrimaryScreen!.WorkingArea; Location = new Point(area.Right - Width - 20, area.Bottom - Height - 20);
        timer.Tick += (_, _) => Close(); Shown += (_, _) => timer.Start();
        Click += (_, _) => Close(); FormClosed += (_, _) => timer.Dispose();
    }
}

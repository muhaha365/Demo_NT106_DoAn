using System.Drawing.Drawing2D;
using UitChat.Core;

namespace WinFormsApp1;

internal sealed class MessageBubble : Control
{
    private readonly MessageView message;
    private readonly bool mine, group;
    private int bubbleWidth, bodyHeight;
    private readonly string body;
    public MessageBubble(MessageView message, bool mine, bool group)
    {
        this.message = message; this.mine = mine; this.group = group;
        body = message.FileName is null ? message.Text : $"↗  {message.FileName}\n{message.FileSize / 1024.0:N1} KB · Bấm để xem / lưu" + (message.Text.Length > 0 ? "\n" + message.Text : "");
        DoubleBuffered = true; Margin = new Padding(0, 0, 0, 12); BackColor = Theme.Canvas;
        if (message.FileName is not null) Cursor = Cursors.Hand;
        AccessibleName = $"{message.Sender}: {body}";
    }
    public void SetRowWidth(int width)
    {
        Width = width;
        using var font = Theme.Font(11);
        int max = Math.Min(540, (int)(width * .82));
        var size = TextRenderer.MeasureText(body, font, new Size(max - 32, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        bubbleWidth = Math.Clamp(size.Width + 34, 155, max);
        bodyHeight = TextRenderer.MeasureText(body, font, new Size(bubbleWidth - 30, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
        Height = bodyHeight + (group && !mine ? 26 : 0) + 52; Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int x = mine ? Width - bubbleWidth : 0, y = group && !mine ? 23 : 0;
        if (y > 0) { using var nameFont = Theme.Font(9, FontStyle.Bold); TextRenderer.DrawText(e.Graphics, message.Sender, nameFont, new Point(x + 4, 0), Theme.Teal); }
        var rect = new Rectangle(x, y, bubbleWidth, bodyHeight + 24);
        using var path = new GraphicsPath(); const int radius = 18;
        path.AddArc(rect.Left, rect.Top, radius, radius, 180, 90); path.AddArc(rect.Right - radius, rect.Top, radius, radius, 270, 90);
        path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90); path.AddArc(rect.Left, rect.Bottom - radius, radius, radius, 90, 90); path.CloseFigure();
        using var fill = new SolidBrush(mine ? Theme.Teal : Color.White); e.Graphics.FillPath(fill, path);
        using var font = Theme.Font(11);
        TextRenderer.DrawText(e.Graphics, body, font, new Rectangle(x + 15, y + 11, bubbleWidth - 30, bodyHeight + 4), mine ? Color.White : Theme.Ink, TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        int seen = message.SeenBy.Count(u => u != message.Sender);
        string status = message.SentAt.ToLocalTime().ToString("HH:mm") + (mine ? seen > 0 ? $"   ✓✓ Đã xem{(group ? $" ({seen})" : "")}" : "   ✓ Đã gửi" : "");
        using var metaFont = Theme.Font(8);
        TextRenderer.DrawText(e.Graphics, status, metaFont, new Rectangle(x, rect.Bottom + 5, bubbleWidth, 20), Theme.Muted, mine ? TextFormatFlags.Right : TextFormatFlags.Left);
    }
}

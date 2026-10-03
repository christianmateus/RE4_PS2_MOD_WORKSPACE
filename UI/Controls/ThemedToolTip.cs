using System.Drawing.Drawing2D;

namespace RE4_PS2_MOD_WORKSPACE;

/// <summary>Compact tooltip styled like the application's dark panels.</summary>
public sealed class ThemedToolTip : IDisposable
{
    private readonly ToolTip tip = new()
    {
        OwnerDraw = true,
        ShowAlways = true,
        InitialDelay = 550,
        ReshowDelay = 120,
        AutoPopDelay = 9000
    };
    private readonly Font font = new("Segoe UI", 8.5f);

    public ThemedToolTip()
    {
        tip.Popup += (_, e) =>
        {
            if (e.AssociatedControl == null) return;
            string message = tip.GetToolTip(e.AssociatedControl) ?? "";
            Size measured = TextRenderer.MeasureText(message, font, new Size(270, int.MaxValue), TextFormatFlags.WordBreak);
            e.ToolTipSize = new Size(Math.Min(300, Math.Max(80, measured.Width + 22)), measured.Height + 18);
        };
        tip.Draw += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new(0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);
            using var path = RoundedRectangle(bounds, 7);
            using var background = new SolidBrush(Color.FromArgb(35, 39, 47));
            using var border = new Pen(Color.FromArgb(83, 94, 110));
            e.Graphics.FillPath(background, path);
            e.Graphics.DrawPath(border, path);
            TextRenderer.DrawText(e.Graphics, e.ToolTipText, font,
                new Rectangle(10, 8, e.Bounds.Width - 20, e.Bounds.Height - 16),
                Color.FromArgb(229, 233, 239), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        };
    }

    public void SetToolTip(Control control, string message) => tip.SetToolTip(control, message);

    public void Dispose()
    {
        tip.Dispose();
        font.Dispose();
    }

    private static GraphicsPath RoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

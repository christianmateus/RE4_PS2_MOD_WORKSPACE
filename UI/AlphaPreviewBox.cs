using System.Drawing.Drawing2D;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class AlphaPreviewBox : Control
{
    private Image? image;
    private float zoom = 1f;

    public Image? Image
    {
        get => image;
        set { if (ReferenceEquals(image, value)) return; image = value; Invalidate(); }
    }

    public float Zoom
    {
        get => zoom;
        set { zoom = Math.Clamp(value, 0.25f, 8f); Invalidate(); }
    }

    public AlphaPreviewBox()
    {
        DoubleBuffered = true;
        TabStop = true;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        BackColor = Color.FromArgb(24, 26, 31);
        MouseWheel += (_, e) => { Zoom *= e.Delta > 0 ? 1.15f : 1f / 1.15f; };
        MouseEnter += (_, _) => Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        DrawCheckerboard(e.Graphics, ClientRectangle);
        if (image == null || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        Rectangle inner = Rectangle.Inflate(ClientRectangle, -8, -8);
        float fit = Math.Min((float)inner.Width / image.Width, (float)inner.Height / image.Height);
        float scale = fit * zoom;
        int width = Math.Max(1, (int)Math.Round(image.Width * scale));
        int height = Math.Max(1, (int)Math.Round(image.Height * scale));
        int x = inner.Left + (inner.Width - width) / 2;
        int y = inner.Top + (inner.Height - height) / 2;
        e.Graphics.InterpolationMode = zoom >= 1f ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.CompositingMode = CompositingMode.SourceOver;
        e.Graphics.DrawImage(image, new Rectangle(x, y, width, height));
    }

    private static void DrawCheckerboard(Graphics graphics, Rectangle bounds)
    {
        const int cell = 16;
        using var a = new SolidBrush(Color.FromArgb(255, 72, 76, 84));
        using var b = new SolidBrush(Color.FromArgb(255, 48, 52, 59));
        for (int y = bounds.Top; y < bounds.Bottom; y += cell)
            for (int x = bounds.Left; x < bounds.Right; x += cell)
            {
                int column = (x - bounds.Left) / cell;
                int row = (y - bounds.Top) / cell;
                graphics.FillRectangle(((column + row) & 1) == 0 ? a : b, x, y, Math.Min(cell, bounds.Right - x), Math.Min(cell, bounds.Bottom - y));
            }
    }
}

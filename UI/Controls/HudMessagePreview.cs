using RE4_PS2_MOD_WORKSPACE.Core.Messages;
using System.Drawing.Imaging;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class HudMessagePreview : Control
{
    private static readonly bool[] HasArgument = { false, false, true, false, false, true, true, false, false, true, false, true, true, true, false, true, false, true, true, false };
    private readonly List<List<ushort>> pages = new() { new() };
    private FntFont? fontAtlas;
    private int pageIndex;
    private readonly Button extractFontButton;

    public int PageCount => pages.Count;
    public int PageIndex => pageIndex;
    public event EventHandler? PageChanged;
    public event EventHandler? ExtractFontRequested;

    public HudMessagePreview()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(8, 10, 13);
        SetStyle(ControlStyles.ResizeRedraw, true);
        extractFontButton = new Button
        {
            Text = "EXTRAIR DA ISO",
            Size = new Size(142, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(42, 91, 157),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 8.5F),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        extractFontButton.FlatAppearance.BorderSize = 0;
        extractFontButton.Click += (_, _) => ExtractFontRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(extractFontButton);
        PositionExtractButton();
    }

    public void SetFont(FntFont? font)
    {
        fontAtlas = font;
        extractFontButton.Visible = font == null;
        Invalidate();
    }

    public void SetExtractionBusy(bool busy)
    {
        extractFontButton.Enabled = !busy;
        extractFontButton.Text = busy ? "EXTRAINDO..." : "EXTRAIR DA ISO";
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionExtractButton();
    }

    private void PositionExtractButton()
    {
        extractFontButton.Left = Math.Max(0, (ClientSize.Width - extractFontButton.Width) / 2);
        extractFontButton.Top = Math.Max(32, (ClientSize.Height - extractFontButton.Height) / 2 + 20);
    }

    public void SetMessage(string raw)
    {
        pages.Clear(); pages.Add(new List<ushort>()); pageIndex = 0;
        try
        {
            ushort[] units = MdtCodec.Encode(raw);
            for (int i = 0; i < units.Length; i++)
            {
                ushort value = units[i];
                if (value == 4) { pages.Add(new List<ushort>()); continue; }
                pages[^1].Add(value);
                if (value < HasArgument.Length && HasArgument[value] && i + 1 < units.Length) pages[^1].Add(units[++i]);
            }
        }
        catch { pages.Clear(); pages.Add(new List<ushort>()); }
        PageChanged?.Invoke(this, EventArgs.Empty); Invalidate();
    }

    public void MovePage(int delta)
    {
        int next = Math.Clamp(pageIndex + delta, 0, Math.Max(0, pages.Count - 1));
        if (next == pageIndex) return;
        pageIndex = next; PageChanged?.Invoke(this, EventArgs.Empty); Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        Rectangle screen = GetPreviewScreen();
        using (var screenBrush = new SolidBrush(Color.FromArgb(20, 22, 25))) e.Graphics.FillRectangle(screenBrush, screen);
        using (var border = new Pen(Color.FromArgb(62, 67, 76))) e.Graphics.DrawRectangle(border, screen);

        Rectangle box = Rectangle.Inflate(screen, -6, -5);

        if (fontAtlas == null)
        {
            var messageBox = new Rectangle(box.Left, box.Top, box.Width, Math.Max(20, box.Height / 2 + 4));
            TextRenderer.DrawText(e.Graphics, "common_p.fnt não encontrado", Font, messageBox, Color.FromArgb(180, 185, 194), TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom);
            return;
        }

        DrawPage(e.Graphics, box);
    }

    private Rectangle GetPreviewScreen()
    {
        int availableW = Math.Max(1, ClientSize.Width - 16), availableH = Math.Max(1, ClientSize.Height - 10);
        const float hudAspect = 4.45f;
        int width = availableW;
        int height = Math.Min(availableH, Math.Max(80, (int)(width / hudAspect)));
        if (height == availableH) width = Math.Min(availableW, (int)(height * hudAspect));
        return new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2, width, height);
    }

    private void DrawPage(Graphics graphics, Rectangle box)
    {
        List<ushort> units = pages[Math.Clamp(pageIndex, 0, pages.Count - 1)];
        float scale = Math.Clamp(box.Height / 125f, 0.75f, 1.75f);
        float cell = 32f * scale, glyphWidth = cell * 1.08f, space = cell * 0.36f, lineHeight = cell * 0.96f;
        float left = box.Left + 8f, right = box.Right - 8f, x = left, y = box.Top + 8f;
        Color color = Color.White;

        for (int i = 0; i < units.Count; i++)
        {
            ushort value = units[i];
            if (value < 0x14)
            {
                ushort argument = 0;
                if (HasArgument[value] && i + 1 < units.Count) argument = units[++i];
                if (value == 3) { x = left; y += lineHeight; }
                else if (value == 6) color = ResolveColor(argument);
                continue;
            }
            if (value == 0x80) { x += space; continue; }
            Rectangle source = fontAtlas!.GetGlyphRectangle(value);
            if (source.IsEmpty) continue;
            if (x + glyphWidth > right) { x = left; y += lineHeight; }
            if (y + cell > box.Bottom - 5) break;
            RectangleF glyphDestination = new(x, y, glyphWidth, cell);
            DrawTintedGlyph(graphics, fontAtlas.Atlas, source, new RectangleF(x + scale, y + scale, glyphWidth, cell), Color.Black);
            DrawTintedGlyph(graphics, fontAtlas.Atlas, source, glyphDestination, color);
            x += GetAdvance(value, cell);
        }

        if (pageIndex + 1 < pages.Count)
        {
            using var brush = new SolidBrush(Color.FromArgb(220, 230, 230, 230));
            PointF[] arrow = { new(box.Right - 22, box.Bottom - 18), new(box.Right - 10, box.Bottom - 18), new(box.Right - 16, box.Bottom - 10) };
            graphics.FillPolygon(brush, arrow);
        }
    }

    private static float GetAdvance(ushort value, float cell)
    {
        if (value is >= 0x83 and <= 0x8C) return cell * 0.58f;
        if (value is 0x94 or 0x95 or 0x9A or 0x9B or 0x123) return cell * 0.38f;
        return cell * 0.64f;
    }

    private static Color ResolveColor(ushort id) => id switch
    {
        1 => Color.FromArgb(120, 210, 255),
        2 => Color.FromArgb(255, 105, 95),
        3 => Color.FromArgb(120, 235, 145),
        4 => Color.FromArgb(245, 210, 90),
        _ => Color.White
    };

    private static void DrawTintedGlyph(Graphics graphics, Image atlas, Rectangle source, RectangleF destination, Color color)
    {
        float r = color.R / 255f, g = color.G / 255f, b = color.B / 255f;
        var matrix = new ColorMatrix(new[]
        {
            new[] { r, 0f, 0f, 0f, 0f }, new[] { 0f, g, 0f, 0f, 0f }, new[] { 0f, 0f, b, 0f, 0f },
            new[] { 0f, 0f, 0f, 1f, 0f }, new[] { 0f, 0f, 0f, 0f, 1f }
        });
        using var attributes = new ImageAttributes(); attributes.SetColorMatrix(matrix);
        graphics.DrawImage(atlas, Rectangle.Round(destination), source.X, source.Y, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
    }
}

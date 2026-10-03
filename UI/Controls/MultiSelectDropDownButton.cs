using System.Drawing.Drawing2D;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class MultiSelectDropDownButton : Button
{
    private sealed class Option(string key, string text, bool isChecked)
    {
        public string Key { get; } = key;
        public string Text { get; } = text;
        public bool Checked { get; set; } = isChecked;
    }

    private sealed class OptionsPanel : Control
    {
        private readonly List<Option> options;
        private int hotIndex = -1;
        public event Action<Option>? OptionClicked;

        public OptionsPanel(List<Option> options)
        {
            this.options = options; DoubleBuffered = true; Cursor = Cursors.Hand;
            BackColor = Color.FromArgb(25, 28, 34); ForeColor = Color.FromArgb(232, 235, 241);
            Font = new Font("Segoe UI Semibold", 9F); Size = new Size(188, Math.Max(1, options.Count) * 34);
        }

        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); int next = e.Y / 34; if (next >= options.Count) next = -1; if (next != hotIndex) { hotIndex = next; Invalidate(); } }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hotIndex = -1; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); int index = e.Y / 34; if (e.Button != MouseButtons.Left || index < 0 || index >= options.Count) return;
            options[index].Checked = !options[index].Checked; OptionClicked?.Invoke(options[index]); Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var hover = new SolidBrush(Color.FromArgb(38, 43, 52));
            using var border = new Pen(Color.FromArgb(75, 82, 96), 1.4f);
            using var check = new Pen(Color.FromArgb(112, 170, 255), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var text = new SolidBrush(ForeColor);
            for (int i = 0; i < options.Count; i++)
            {
                int y = i * 34; if (i == hotIndex) e.Graphics.FillRectangle(hover, 0, y, Width, 34);
                var box = new Rectangle(12, y + 9, 16, 16); e.Graphics.DrawRectangle(border, box);
                if (options[i].Checked) e.Graphics.DrawLines(check, [new PointF(15, y + 17), new PointF(19, y + 21), new PointF(26, y + 13)]);
                e.Graphics.DrawString(options[i].Text, Font, text, 39, y + 8);
            }
        }
    }

    private readonly List<Option> options = new();
    private ToolStripDropDown? dropDown;
    public event Action<string, bool>? OptionChanged;

    public void AddOption(string key, string text, bool isChecked = false) => options.Add(new Option(key, text, isChecked));
    public bool IsChecked(string key) => options.FirstOrDefault(x => x.Key == key)?.Checked == true;
    public void SetChecked(string key, bool value)
    {
        Option? option = options.FirstOrDefault(x => x.Key == key); if (option == null || option.Checked == value) return;
        option.Checked = value; dropDown?.Invalidate(true);
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (dropDown?.Visible == true) { dropDown.Close(); return; }
        var panel = new OptionsPanel(options); panel.OptionClicked += option => OptionChanged?.Invoke(option.Key, option.Checked);
        var host = new ToolStripControlHost(panel) { AutoSize = false, Margin = Padding.Empty, Padding = Padding.Empty, Size = panel.Size };
        dropDown = new ToolStripDropDown { AutoClose = true, AutoSize = false, Padding = new Padding(1), BackColor = Color.FromArgb(67, 73, 86), Size = new Size(panel.Width + 2, panel.Height + 2) };
        dropDown.Items.Add(host); dropDown.Closed += (_, _) => dropDown = null;
        dropDown.Show(this, new Point(0, Height + 2));
    }
}

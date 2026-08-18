namespace RE4_PS2_MOD_WORKSPACE;

public enum TextureResizeResampling
{
    NearestNeighbor,
    Bilinear,
    Bicubic
}

public sealed class TextureResizeDialog : Form
{
    private readonly NumericUpDown width = new() { Minimum = 1, Maximum = 4096, Width = 110 };
    private readonly NumericUpDown height = new() { Minimum = 1, Maximum = 4096, Width = 110 };
    private readonly ComboBox resampling = new() { Width = 172, DropDownStyle = ComboBoxStyle.DropDownList };

    public int TargetWidth => (int)width.Value;
    public int TargetHeight => (int)height.Value;
    public TextureResizeResampling ResamplingMode => resampling.SelectedIndex switch
    {
        0 => TextureResizeResampling.NearestNeighbor,
        1 => TextureResizeResampling.Bilinear,
        _ => TextureResizeResampling.Bicubic
    };

    public TextureResizeDialog(int currentWidth, int currentHeight)
    {
        Text = "Resize Texture";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(330, 198);
        BackColor = Color.FromArgb(22, 25, 30);
        ForeColor = Color.Gainsboro;

        width.Value = currentWidth;
        height.Value = currentHeight;
        resampling.Items.AddRange(new object[] { "Nearest Neighbor", "Bilinear", "Bicubic" });
        resampling.SelectedIndex = 2;
        resampling.BackColor = Color.FromArgb(34, 38, 45);
        resampling.ForeColor = Color.Gainsboro;
        resampling.FlatStyle = FlatStyle.Flat;

        Controls.Add(new Label { Text = "Width", Left = 18, Top = 22, Width = 70 });
        width.Left = 104; width.Top = 18; Controls.Add(width);
        Controls.Add(new Label { Text = "Height", Left = 18, Top = 58, Width = 70 });
        height.Left = 104; height.Top = 54; Controls.Add(height);
        Controls.Add(new Label { Text = "Resampling", Left = 18, Top = 96, Width = 78 });
        resampling.Left = 104; resampling.Top = 91; Controls.Add(resampling);

        var ok = new Button { Text = "OK", Left = 132, Top = 146, Width = 82, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Left = 222, Top = 146, Width = 82, DialogResult = DialogResult.Cancel };
        Controls.Add(ok); Controls.Add(cancel); AcceptButton = ok; CancelButton = cancel;
    }
}

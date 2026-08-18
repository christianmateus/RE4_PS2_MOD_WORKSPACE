using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class TextureMipmapDialog : Form
{
    private readonly string path;
    private readonly int index;
    private readonly TextureWorkspaceService service;
    private readonly AlphaPreviewBox main = new(), mip1 = new(), mip2 = new();
    private readonly Label mainInfo = new(), mip1Info = new(), mip2Info = new(), shared = new();
    private readonly Button replaceMain = new(), replaceMip1 = new(), replaceMip2 = new(), regenerate = new(), add = new(), remove = new();
    public bool Modified { get; private set; }

    public TextureMipmapDialog(string path, int index, TextureWorkspaceService service)
    {
        this.path = path; this.index = index; this.service = service;
        Text = $"Texture #{index:D3} & Mipmaps";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1040, 650);
        MinimumSize = new Size(900, 560);
        BackColor = Color.FromArgb(13, 15, 18);
        ForeColor = Color.FromArgb(238, 240, 244);
        BuildUi(); LoadFamily();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); Controls.Add(root);
        var previews = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f)); previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f)); previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334f)); root.Controls.Add(previews, 0, 0);
        previews.Controls.Add(BuildCard("MAIN", main, mainInfo, replaceMain), 0, 0); previews.Controls.Add(BuildCard("MIP 1", mip1, mip1Info, replaceMip1), 1, 0); previews.Controls.Add(BuildCard("MIP 2", mip2, mip2Info, replaceMip2), 2, 0);
        shared.Dock = DockStyle.Fill; shared.Padding = new Padding(6, 0, 0, 0); shared.TextAlign = ContentAlignment.MiddleLeft; root.Controls.Add(shared, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 6, 0, 0) };
        regenerate.Text = "Regenerate Mipmaps"; add.Text = "Add Mipmaps"; remove.Text = "Remove Mipmaps"; var close = new Button { Text = "Close", AutoSize = true };
        foreach (var b in new[] { close, remove, add, regenerate }) { b.AutoSize = true; actions.Controls.Add(b); }
        root.Controls.Add(actions, 0, 2);
        replaceMain.Text = "Replace Main..."; replaceMip1.Text = "Replace Mip 1..."; replaceMip2.Text = "Replace Mip 2...";
        replaceMain.Click += (_, _) => ReplaceMainImage(); replaceMip1.Click += (_, _) => ReplaceMipImage(0); replaceMip2.Click += (_, _) => ReplaceMipImage(1);
        regenerate.Click += (_, _) => Run(() => service.RegenerateMipmaps(path, index)); add.Click += (_, _) => Run(() => service.AddMipmaps(path, index));
        remove.Click += (_, _) => { if (MessageBox.Show(this, "Remove mipmaps from this texture?", "Mipmaps", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Run(() => service.RemoveMipmaps(path, index)); };
        close.Click += (_, _) => Close();
    }

    private Control BuildCard(string title, AlphaPreviewBox preview, Label info, Button button)
    {
        var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(8), ForeColor = ForeColor };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        preview.Dock = DockStyle.Fill; info.Dock = DockStyle.Fill; info.TextAlign = ContentAlignment.MiddleCenter; button.Dock = DockStyle.Fill;
        layout.Controls.Add(preview, 0, 0); layout.Controls.Add(info, 0, 1); layout.Controls.Add(button, 0, 2); group.Controls.Add(layout); return group;
    }

    private void LoadFamily()
    {
        SetImage(main, service.Decode(path, index));
        TextureInfo info = service.ReadInfo(path, index); mainInfo.Text = $"{info.Width}×{info.Height} • {info.BitDepthName} • {info.InterlaceName}";
        if (info.MipmapCount > 0) { SetImage(mip1, service.DecodeMip(path, index, 0)); mip1Info.Text = $"{mip1.Image!.Width}×{mip1.Image.Height}"; } else { SetImage(mip1, null); mip1Info.Text = "No mipmap"; }
        if (info.MipmapCount > 1) { SetImage(mip2, service.DecodeMip(path, index, 1)); mip2Info.Text = $"{mip2.Image!.Width}×{mip2.Image.Height}"; } else { SetImage(mip2, null); mip2Info.Text = "No mipmap"; }
        replaceMip1.Enabled = info.MipmapCount > 0; replaceMip2.Enabled = info.MipmapCount > 1; regenerate.Enabled = info.MipmapCount > 0; add.Enabled = info.MipmapCount == 0; remove.Enabled = info.MipmapCount > 0;
        shared.Text = info.BitDepth == 0x08 || info.BitDepth == 0x09 ? $"Shared CLUT: Main + mipmaps use the same {(info.BitDepth == 0x08 ? "16" : "256")}-color palette." : "This texture format does not use an indexed CLUT.";
    }

    private void ReplaceMainImage()
    {
        string? file = PickPng(); if (file == null) return;
        using var image = new Bitmap(file); Run(() => service.ReplaceMainAndRegenerate(path, index, image));
    }

    private void ReplaceMipImage(int mipIndex)
    {
        string? file = PickPng(); if (file == null) return;
        using var image = new Bitmap(file); Run(() => service.ReplaceMip(path, index, mipIndex, image));
    }

    private void Run(Action action)
    {
        try { Cursor = Cursors.WaitCursor; action(); Modified = true; LoadFamily(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Mipmaps", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { Cursor = Cursors.Default; }
    }

    private string? PickPng()
    {
        using var dialog = new OpenFileDialog { Filter = "PNG (*.png)|*.png", CheckFileExists = true };
        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    private static void SetImage(AlphaPreviewBox box, Image? image)
    {
        Image? old = box.Image; box.Image = image; old?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { SetImage(main, null); SetImage(mip1, null); SetImage(mip2, null); }
        base.Dispose(disposing);
    }
}

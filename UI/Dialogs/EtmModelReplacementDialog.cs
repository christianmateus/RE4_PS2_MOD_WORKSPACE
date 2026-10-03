using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class EtmModelReplacementDialog : AppForm
{
    private static readonly Color Bg = Color.FromArgb(13, 15, 18), Surface = Color.FromArgb(22, 25, 30), Surface2 = Color.FromArgb(28, 31, 37);
    private static readonly Color Muted = Color.FromArgb(145, 151, 163), Text = Color.FromArgb(238, 240, 244), Accent = Color.FromArgb(196, 56, 56);
    private readonly ListView resources = new();
    private readonly Label detail = new();
    private readonly Label selectedCount = new();
    private readonly NumericUpDown sourceTextureIndex = new();
    private readonly Button browse = new(), clear = new(), apply = new();
    private readonly List<Slot> slots = new();
    public IReadOnlyDictionary<int, byte[]> Replacements { get; private set; } = new Dictionary<int, byte[]>();

    private sealed class Slot(EtmResource resource, int? embeddedIndex)
    {
        public EtmResource Resource { get; } = resource;
        public int? EmbeddedIndex { get; } = embeddedIndex;
        public string? SourcePath { get; set; }
        public int SourceTextureIndex { get; set; }
        public string Label => EmbeddedIndex is int index ? $"  ↳ Textura EFF #{index:D2}" : resource.Name;
        public string Type => EmbeddedIndex != null ? "TPL no EFF" : Path.GetExtension(resource.Name).TrimStart('.').ToUpperInvariant();
    }

    public EtmModelReplacementDialog(EtmObjectDefinition definition, int instances)
    {
        base.Text = "Alterar modelo ETM";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(730, 520);
        BackColor = Bg;
        ForeColor = Text;
        Font = new Font("Segoe UI", 9F);

        Controls.Add(new Label { Text = $"ALTERAR MODELO • {definition.DisplayName}", Left = 23, Top = 18, Width = 680, Height = 31, Font = new Font("Segoe UI Semibold", 16F), ForeColor = Text });
        Controls.Add(new Label { Text = $"Tipo 0x{definition.Id:X2} • {instances} instância(s) no cenário usarão os recursos alterados.", Left = 25, Top = 54, Width = 670, Height = 20, ForeColor = Muted });
        Controls.Add(new Label { Text = "Selecione um recurso e escolha o arquivo que irá substituí-lo. Os demais permanecem intactos.", Left = 25, Top = 77, Width = 680, Height = 21, ForeColor = Muted });

        resources.SetBounds(24, 111, 682, 252);
        resources.View = View.Details;
        resources.FullRowSelect = true;
        resources.HideSelection = false;
        resources.MultiSelect = false;
        resources.BorderStyle = BorderStyle.None;
        resources.BackColor = Surface;
        resources.ForeColor = Text;
        resources.Columns.Add("Recurso no ETM", 245);
        resources.Columns.Add("Tipo", 90);
        resources.Columns.Add("Arquivo escolhido", 325);
        resources.SelectedIndexChanged += (_, _) => UpdateSelection();
        Controls.Add(resources);

        var effReader = new EffTextureReader();
        foreach (EtmResource resource in definition.Resources.Where(IsReplaceable))
        {
            AddSlot(new Slot(resource, null));
            if (!resource.Name.EndsWith(".eff", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                int count = effReader.ReadTextures(resource.Data).Count;
                for (int index = 0; index < count; index++) AddSlot(new Slot(resource, index));
            }
            catch (InvalidDataException) { }
        }

        detail.SetBounds(25, 375, 680, 23);
        detail.ForeColor = Muted;
        detail.Text = "Selecione um BIN, TPL, EFF ou uma textura dentro do EFF.";
        detail.AutoEllipsis = true;
        Controls.Add(detail);
        StyleButton(browse, "ESCOLHER ARQUIVO", Surface2, 24, 405, 154);
        browse.Click += (_, _) => Browse();
        Controls.Add(browse);
        StyleButton(clear, "LIMPAR", Surface2, 188, 405, 82);
        clear.Click += (_, _) => { if (SelectedSlot() is Slot slot) { slot.SourcePath = null; slot.SourceTextureIndex = 0; RefreshRow(slot); UpdateSelection(); UpdateCount(); } };
        Controls.Add(clear);
        Controls.Add(new Label { Text = "Textura de origem #", Left = 291, Top = 411, Width = 133, Height = 21, ForeColor = Muted });
        sourceTextureIndex.SetBounds(424, 405, 64, 29);
        sourceTextureIndex.Minimum = 0;
        sourceTextureIndex.Maximum = 0;
        sourceTextureIndex.BackColor = Surface2;
        sourceTextureIndex.ForeColor = Text;
        sourceTextureIndex.ValueChanged += (_, _) => { if (SelectedSlot() is Slot slot) slot.SourceTextureIndex = (int)sourceTextureIndex.Value; };
        Controls.Add(sourceTextureIndex);

        selectedCount.SetBounds(25, 457, 435, 24);
        selectedCount.ForeColor = Muted;
        Controls.Add(selectedCount);
        StyleButton(apply, "APLICAR", Accent, 489, 455, 104);
        apply.Click += (_, _) => Accept();
        Controls.Add(apply);
        var cancel = new Button();
        StyleButton(cancel, "CANCELAR", Surface2, 602, 455, 104);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(cancel);
        CancelButton = cancel;
        UpdateSelection();
        UpdateCount();
    }

    private static bool IsReplaceable(EtmResource resource) =>
        new[] { ".bin", ".tpl", ".eff" }.Any(extension => resource.Name.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

    private void AddSlot(Slot slot)
    {
        slots.Add(slot);
        var row = new ListViewItem(slot.Label) { Tag = slot };
        row.SubItems.Add(slot.Type);
        row.SubItems.Add("—");
        resources.Items.Add(row);
    }

    private Slot? SelectedSlot() => resources.SelectedItems.Count == 1 ? resources.SelectedItems[0].Tag as Slot : null;

    private void UpdateSelection()
    {
        Slot? slot = SelectedSlot();
        browse.Enabled = clear.Enabled = slot != null;
        sourceTextureIndex.Enabled = slot?.EmbeddedIndex != null && slot.SourcePath != null;
        sourceTextureIndex.Maximum = slot?.EmbeddedIndex != null && slot.SourcePath != null
            ? ReadTplCount(slot.SourcePath) - 1 : 0;
        sourceTextureIndex.Value = slot?.SourceTextureIndex ?? 0;
        detail.Text = slot == null ? "Selecione um recurso para escolher o arquivo." :
            slot.EmbeddedIndex != null ? "TPL no EFF: dimensões, profundidade e interlace devem coincidir; mipmaps não são aceitos." :
            $"Destino: {slot.Resource.Name} • {slot.Resource.Data.Length:N0} bytes";
    }

    private void Browse()
    {
        Slot? slot = SelectedSlot();
        if (slot == null) return;
        string extension = slot.EmbeddedIndex != null ? "tpl" : slot.Type.ToLowerInvariant();
        using var dialog = new OpenFileDialog { Title = $"Substituir {slot.Label}", Filter = $"Arquivo {extension.ToUpperInvariant()} (*.{extension})|*.{extension}", CheckFileExists = true };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        slot.SourcePath = dialog.FileName;
        slot.SourceTextureIndex = 0;
        if (slot.EmbeddedIndex != null)
        {
            try
            {
                _ = ReadTplCount(dialog.FileName);
            }
            catch (Exception ex) { slot.SourcePath = null; MessageBox.Show(this, ex.Message, "TPL inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        RefreshRow(slot);
        UpdateSelection();
        UpdateCount();
    }

    private static int ReadTplCount(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length < 8) throw new InvalidDataException("TPL truncado.");
        using var reader = new BinaryReader(stream);
        stream.Position = 4;
        uint count = reader.ReadUInt32();
        if (count == 0 || count > 4096) throw new InvalidDataException("TPL sem texturas válidas.");
        return checked((int)count);
    }

    private void RefreshRow(Slot slot)
    {
        foreach (ListViewItem row in resources.Items)
            if (ReferenceEquals(row.Tag, slot))
            {
                row.SubItems[2].Text = slot.SourcePath == null ? "—" : Path.GetFileName(slot.SourcePath);
                row.ForeColor = slot.SourcePath == null ? Text : Accent;
                break;
            }
    }

    private void UpdateCount()
    {
        int count = slots.Count(slot => slot.SourcePath != null);
        selectedCount.Text = $"{count} recurso(s) preparado(s) para substituição";
        apply.Enabled = count > 0;
    }

    private void Accept()
    {
        var requests = slots.Where(slot => slot.SourcePath != null)
            .Select(slot => new EtmReplacementRequest(slot.Resource, slot.EmbeddedIndex, slot.SourcePath!, slot.SourceTextureIndex)).ToArray();
        try
        {
            Replacements = EtmModelReplacement.Prepare(requests);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Arquivo incompatível", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private static void StyleButton(Button button, string text, Color color, int x, int y, int width)
    {
        button.Text = text;
        button.SetBounds(x, y, width, 34);
        button.BackColor = color;
        button.ForeColor = Text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI Semibold", 8.8F);
    }
}

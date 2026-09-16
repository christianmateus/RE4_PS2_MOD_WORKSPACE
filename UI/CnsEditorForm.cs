using RE4_PS2_MOD_WORKSPACE.Core.Cns;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class CnsEditorForm : AppForm
{
    private static readonly Color Bg = Color.FromArgb(18, 20, 25);
    private static readonly Color Surface = Color.FromArgb(28, 31, 38);
    private static readonly Color Surface2 = Color.FromArgb(38, 42, 51);
    private static readonly Color Border = Color.FromArgb(57, 62, 74);
    private static readonly Color TextPrimary = Color.FromArgb(232, 235, 241);
    private static readonly Color TextMuted = Color.FromArgb(157, 164, 178);
    private static readonly Color Accent = Color.FromArgb(201, 73, 73);

    private readonly string path;
    private readonly Ps2CnsFile file;
    private readonly CheckBox[] enabledChecks = new CheckBox[Ps2CnsFile.KnownValueCount];
    private readonly NumericUpDown[] valueEditors = new NumericUpDown[Ps2CnsFile.KnownValueCount];
    private readonly Label statusLabel;
    private readonly Button saveButton;

    public bool WasSaved { get; private set; }

    public CnsEditorForm(string path)
    {
        this.path = path;
        file = Ps2CnsFile.Read(path);

        Text = "Editor CNS — " + Path.GetFileName(path);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(680, 590);
        Size = new Size(780, 720);
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;

        var windowLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Bg
        };
        windowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        windowLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        windowLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        windowLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        Controls.Add(windowLayout);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(18, 10, 18, 7), Margin = Padding.Empty };
        header.Controls.Add(new Label { Text = "CONFIGURAÇÕES DO CENÁRIO", Dock = DockStyle.Top, Height = 24, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 12F) });
        header.Controls.Add(new Label { Text = $"{Path.GetFileName(path)}  •  {file.OriginalLength} bytes  •  {file.Count} valores declarados", Dock = DockStyle.Bottom, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5F) });
        windowLayout.Controls.Add(header, 0, 0);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(14, 9, 14, 9), Margin = Padding.Empty };
        statusLabel = new Label { Dock = DockStyle.Fill, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        var cancel = MakeButton("FECHAR", Surface2, 104); cancel.Dock = DockStyle.Right; cancel.DialogResult = DialogResult.Cancel;
        saveButton = MakeButton("SALVAR CNS", Accent, 132); saveButton.Dock = DockStyle.Right; saveButton.Margin = new Padding(10, 0, 0, 0); saveButton.Click += SaveButton_Click;
        footer.Controls.Add(statusLabel); footer.Controls.Add(cancel); footer.Controls.Add(saveButton);
        windowLayout.Controls.Add(footer, 0, 2);
        CancelButton = cancel;

        var scroll = new Panel { Name = "CnsScrollPanel", Dock = DockStyle.Fill, AutoScroll = true, TabStop = false, Padding = new Padding(14, 10, 14, 10), Margin = Padding.Empty };
        windowLayout.Controls.Add(scroll, 0, 1);

        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 4, BackColor = Bg, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        AddHeader(table, "USAR", 0); AddHeader(table, "LIMITE", 1); AddHeader(table, "VALOR", 2); AddHeader(table, "OFFSET", 3);

        var tip = new ToolTip { AutoPopDelay = 9000, InitialDelay = 300, ReshowDelay = 100 };
        for (int i = 0; i < Ps2CnsFile.KnownValueCount; i++)
        {
            bool available = file.IsAvailable(i);
            var check = new CheckBox { Dock = DockStyle.Fill, Checked = available && file.IsEnabled(i), Enabled = available, TextAlign = ContentAlignment.MiddleCenter, BackColor = Surface };
            var name = new Label { Dock = DockStyle.Fill, Text = Ps2CnsFile.KnownNames[i] + "  •  " + Ps2CnsFile.KnownCodes[i], ForeColor = available ? TextPrimary : TextMuted, BackColor = Surface, Padding = new Padding(8, 0, 4, 0), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Font = new Font("Segoe UI", 8.5F) };
            var value = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = uint.MaxValue, ThousandsSeparator = true, Value = available ? file.Values[i] : 0, Enabled = available, BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(7, 7, 7, 7), Font = new Font("Segoe UI", 8.5F) };
            var offset = new Label { Dock = DockStyle.Fill, Text = $"0x{8 + i * 4:X2}", TextAlign = ContentAlignment.MiddleCenter, ForeColor = TextMuted, BackColor = Surface };
            if (!available) tip.SetToolTip(name, $"Este campo não existe neste arquivo (count = {file.Count}).");
            else tip.SetToolTip(check, "Marcado: o cenário solicita que este valor substitua o padrão do executável.");
            enabledChecks[i] = check; valueEditors[i] = value;
            int row = table.RowCount++; table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.Controls.Add(check, 0, row); table.Controls.Add(name, 1, row); table.Controls.Add(value, 2, row); table.Controls.Add(offset, 3, row);
        }

        var advanced = new Panel { Dock = DockStyle.Top, Height = file.Count > 12 ? 76 : 60, BackColor = Surface, Padding = new Padding(10), Margin = new Padding(0, 10, 0, 0) };
        string extra = file.Count > 12
            ? string.Join("   ", file.Values.Skip(12).Select((v, i) => $"Value {i + 13}: {v}"))
            : "Nenhum valor adicional declarado.";
        advanced.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = TextMuted, Text = $"DADOS AVANÇADOS   •   Flags: 0x{file.Flags:X8}   •   Bits desconhecidos: 0x{file.UnknownFlags:X8}\r\n{extra}", Padding = new Padding(0, 2, 0, 0), Font = new Font("Segoe UI", 8.25F) });
        scroll.Controls.Add(advanced);
        scroll.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10, BackColor = Bg });
        scroll.Controls.Add(table);
        statusLabel.Text = "Somente valores disponíveis serão alterados; estrutura e bytes desconhecidos serão preservados.";
    }

    private static void AddHeader(TableLayoutPanel table, string text, int column)
    {
        if (table.RowCount == 0) { table.RowCount = 1; table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); }
        table.Controls.Add(new Label { Dock = DockStyle.Fill, Text = text, TextAlign = ContentAlignment.MiddleCenter, ForeColor = TextMuted, BackColor = Surface2, Font = new Font("Segoe UI Semibold", 8F) }, column, 0);
    }

    private static Button MakeButton(string text, Color color, int width) => new()
    {
        Text = text, Width = width, Height = 36, BackColor = color, ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand
    };

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // NumericUpDown/CheckBox podem solicitar foco durante a criação e fazer o
        // AutoScroll abrir no meio da tabela. O rodapé recebe o foco e o painel
        // volta explicitamente ao começo após o primeiro layout completo.
        ActiveControl = saveButton;
        BeginInvoke(() =>
        {
            Control? content = Controls.Find("CnsScrollPanel", true).FirstOrDefault();
            if (content is Panel panel) panel.AutoScrollPosition = Point.Empty;
        });
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        try
        {
            for (int i = 0; i < Ps2CnsFile.KnownValueCount && i < file.Values.Length; i++)
            {
                file.SetEnabled(i, enabledChecks[i].Checked);
                file.Values[i] = decimal.ToUInt32(valueEditors[i].Value);
            }
            file.Save(path);
            WasSaved = true;
            statusLabel.ForeColor = Color.FromArgb(106, 196, 133);
            statusLabel.Text = "CNS salvo e validado. O backup inicial foi preservado como .CNS.bak.";
            saveButton.Text = "SALVO ✓";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Salvar CNS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.ForeColor = Accent;
            statusLabel.Text = "Não foi possível salvar o arquivo.";
        }
    }
}

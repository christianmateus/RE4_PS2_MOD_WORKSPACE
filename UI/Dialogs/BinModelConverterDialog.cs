using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class BinModelConverterDialog : AppForm
{
    private static readonly Color Bg = Color.FromArgb(13, 15, 18), Surface = Color.FromArgb(22, 25, 30), Surface2 = Color.FromArgb(28, 31, 37);
    private static readonly Color TextPrimary = Color.FromArgb(238, 240, 244), Muted = Color.FromArgb(145, 151, 163), Accent = Color.FromArgb(196, 56, 56);
    private readonly TextBox binPath = new(), tplPath = new();
    private readonly RadioButton obj = new(), smd = new();
    private readonly Label details = new(), status = new();
    private readonly Button convert = new();

    public BinModelConverterDialog()
    {
        Text = "Conversor de Modelos";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(630, 386);
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9F);
        Controls.Add(new Label { Text = "CONVERSOR DE MODELOS", Left = 24, Top = 18, Width = 560, Height = 32, Font = new Font("Segoe UI Semibold", 16F), ForeColor = TextPrimary });
        Controls.Add(new Label { Text = "Converta um BIN do PS2 para OBJ com texturas ou SMD de referência.", Left = 26, Top = 53, Width = 570, Height = 22, ForeColor = Muted });

        var card = new Panel { Left = 24, Top = 87, Width = 582, Height = 214, BackColor = Surface };
        Controls.Add(card);
        AddFileRow(card, "MODELO BIN", 14, binPath, "Modelo BIN do PS2 (*.bin)|*.bin", () =>
        {
            try
            {
                var triangles = Ps2ScenarioReader.ReadStandaloneBin(File.ReadAllBytes(binPath.Text));
                if (triangles.Count == 0) throw new InvalidDataException("O BIN não contém faces renderizáveis.");
                int materials = triangles.Select(t => t.TextureIndex).Distinct().Count();
                details.Text = $"{triangles.Count:N0} faces  •  {materials:N0} material(is)  •  {new FileInfo(binPath.Text).Length / 1024d:0.0} KB";
                UpdateButtons();
            }
            catch (Exception ex) { binPath.Clear(); details.Text = "BIN inválido: " + ex.Message; UpdateButtons(); }
        });
        AddFileRow(card, "TEXTURAS TPL  •  necessário para OBJ + texturas", 84, tplPath, "Pacote de texturas TPL (*.tpl)|*.tpl", UpdateButtons);
        details.SetBounds(17, 155, 548, 22);
        details.ForeColor = Muted;
        details.Text = "Selecione um BIN para analisar o modelo.";
        card.Controls.Add(details);
        obj.Text = "OBJ + MTL + PNG"; obj.SetBounds(17, 179, 165, 26); obj.ForeColor = TextPrimary; obj.Checked = true;
        smd.Text = "SMD"; smd.SetBounds(203, 179, 100, 26); smd.ForeColor = TextPrimary;
        obj.CheckedChanged += (_, _) => UpdateButtons();
        card.Controls.Add(obj); card.Controls.Add(smd);

        status.SetBounds(25, 317, 360, 24);
        status.ForeColor = Muted;
        status.Text = "O arquivo BIN original não será alterado.";
        Controls.Add(status);
        StyleButton(convert, "CONVERTER", Accent, 392, 315, 103);
        convert.Click += async (_, _) => await ConvertAsync();
        Controls.Add(convert);
        var close = new Button(); StyleButton(close, "FECHAR", Surface2, 505, 315, 101);
        close.Click += (_, _) => Close(); Controls.Add(close);
        CancelButton = close;
        UpdateButtons();
    }

    private static void AddFileRow(Panel parent, string title, int top, TextBox box, string filter, Action changed)
    {
        parent.Controls.Add(new Label { Text = title, Left = 17, Top = top, Width = 530, Height = 19, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 8F) });
        box.SetBounds(17, top + 22, 451, 29);
        box.ReadOnly = true;
        box.BackColor = Surface2;
        box.ForeColor = TextPrimary;
        box.BorderStyle = BorderStyle.FixedSingle;
        parent.Controls.Add(box);
        var browse = new Button(); StyleButton(browse, "ESCOLHER", Surface2, 477, top + 20, 88);
        browse.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
            if (dialog.ShowLocalizedDialog(parent.FindForm()) != DialogResult.OK) return;
            box.Text = dialog.FileName;
            changed();
        };
        parent.Controls.Add(browse);
    }

    private void UpdateButtons()
    {
        convert.Enabled = File.Exists(binPath.Text) && (!obj.Checked || File.Exists(tplPath.Text));
        if (binPath.TextLength > 0 && obj.Checked && tplPath.TextLength == 0)
            status.Text = "Escolha o TPL para exportar o OBJ com texturas.";
        else if (binPath.TextLength > 0) status.Text = "Pronto para converter.";
    }

    private async Task ConvertAsync()
    {
        string extension = obj.Checked ? "obj" : "smd";
        using var dialog = new SaveFileDialog
        {
            Title = "Salvar modelo convertido", Filter = extension == "obj" ? "Wavefront OBJ (*.obj)|*.obj" : "Source Model Data (*.smd)|*.smd",
            FileName = Path.GetFileNameWithoutExtension(binPath.Text) + "." + extension,
            InitialDirectory = Path.GetDirectoryName(binPath.Text), OverwritePrompt = true
        };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        convert.Enabled = false;
        status.Text = "Convertendo modelo...";
        try
        {
            string bin = binPath.Text, tpl = tplPath.Text, output = dialog.FileName;
            bool exportObj = obj.Checked;
            ExternalModelStats result = await Task.Run(() =>
            {
                if (exportObj) SmdModelExporter.ExportStandaloneObj(bin, tpl, output);
                else SmdModelExporter.ExportStandaloneSmd(bin, File.Exists(tpl) ? tpl : null, output);
                return Ps2ModelConversionService.Analyze(output);
            });
            status.Text = $"Concluído • {result.Faces:N0} faces.";
            MessageBox.Show(this, $"Modelo exportado com {result.Faces:N0} faces para:\n{dialog.FileName}", "Conversão concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { status.Text = "Falha na conversão."; MessageBox.Show(this, ex.Message, "Conversor de Modelos", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { convert.Enabled = File.Exists(binPath.Text) && (!obj.Checked || File.Exists(tplPath.Text)); }
    }

    private static void StyleButton(Button button, string title, Color color, int x, int y, int width)
    {
        button.Text = title;
        button.SetBounds(x, y, width, 34);
        button.BackColor = color;
        button.ForeColor = TextPrimary;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI Semibold", 8.5F);
    }
}

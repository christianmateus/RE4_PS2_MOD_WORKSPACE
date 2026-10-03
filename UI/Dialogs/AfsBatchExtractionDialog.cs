using RE4_PS2_MOD_WORKSPACE.Core.Afs;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class AfsBatchExtractionDialog : AppForm
{
    private static readonly Color Bg = Color.FromArgb(13, 15, 18);
    private static readonly Color Surface = Color.FromArgb(22, 25, 30);
    private static readonly Color Surface2 = Color.FromArgb(28, 31, 37);
    private static readonly Color Border = Color.FromArgb(47, 51, 60);
    private static readonly Color TextPrimary = Color.FromArgb(238, 240, 244);
    private static readonly Color TextMuted = Color.FromArgb(145, 151, 163);
    private static readonly Color Accent = Color.FromArgb(196, 56, 56);

    private readonly AfsImage image;
    private readonly string destinationRoot;
    private readonly IReadOnlyList<AfsBatchFile> files;
    private readonly Dictionary<(string Format, string Group), CheckBox> choices = new();
    private readonly Label summary = new();
    private readonly Label status = new();
    private readonly ProgressBar progress = new();
    private readonly CheckBox overwrite = new();
    private readonly Button extract = new();
    private readonly Button cancel = new();
    private CancellationTokenSource? cancellation;
    private bool running;

    public Action<string>? Log { get; set; }

    public AfsBatchExtractionDialog(AfsImage image, string workspaceRoot)
    {
        this.image = image;
        files = AfsBatchExtraction.FindFiles(image);
        string afsName = Path.GetFileNameWithoutExtension(image.IsoAfsEntry.Name);
        destinationRoot = Path.Combine(workspaceRoot, "Extracted", "_AFS", afsName);

        Text = "Extração em lote";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(744, 548);
        BackColor = Bg;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9F);

        Controls.Add(new Label { Text = "EXTRAIR LOTE", Left = 24, Top = 18, Width = 550, Height = 31, Font = new Font("Segoe UI Semibold", 17F), ForeColor = TextPrimary });
        Controls.Add(new Label { Text = $"Origem: {image.IsoAfsEntry.Name}  •  Selecione os formatos e grupos", Left = 26, Top = 54, Width = 690, Height = 20, ForeColor = TextMuted });

        var card = new Panel { Left = 24, Top = 87, Width = 696, Height = 330, BackColor = Surface };
        Controls.Add(card);
        card.Controls.Add(new Label { Text = "FORMATO", Left = 18, Top = 13, Width = 85, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) });
        card.Controls.Add(new Label { Text = "GRUPOS DISPONÍVEIS", Left = 119, Top = 13, Width = 250, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) });

        (string Format, string[] Groups)[] rows =
        {
            ("SND", new[] { "Cenários", "Personagens", "Inimigos", "Gerais" }),
            ("REL", new[] { "Cenários", "Personagens", "Inimigos", "Gerais" }),
            ("DAT", new[] { "Cenários", "Personagens", "Inimigos", "Gerais" }),
            ("ADX", new[] { "Gerais" }),
            ("FNT", new[] { "Gerais" }),
            ("ESL", new[] { "Emleon", "Emgirl", "Emlist", "Gerais" }),
            ("IDM", new[] { "IDM" })
        };
        for (int row = 0; row < rows.Length; row++)
        {
            int top = 39 + row * 40;
            if (row > 0) card.Controls.Add(new Panel { Left = 18, Top = top - 4, Width = 660, Height = 1, BackColor = Border });
            card.Controls.Add(new Label { Text = rows[row].Format, Left = 18, Top = top + 5, Width = 75, Height = 24, Font = new Font("Segoe UI Semibold", 10F), ForeColor = row == 0 ? Accent : TextPrimary });
            for (int group = 0; group < rows[row].Groups.Length; group++)
            {
                string format = rows[row].Format;
                string groupName = rows[row].Groups[group];
                int count = files.Count(file => file.Format == format && file.Group == groupName);
                var choice = new CheckBox
                {
                    Text = $"{groupName} ({count})", Left = 119 + group * 137, Top = top + 4,
                    Width = 137, Height = 25, ForeColor = count > 0 ? TextPrimary : TextMuted,
                    Enabled = count > 0, BackColor = Surface
                };
                choice.CheckedChanged += (_, _) => UpdateSummary();
                choices.Add((format, groupName), choice);
                card.Controls.Add(choice);
            }
        }

        summary.SetBounds(26, 428, 690, 21);
        summary.ForeColor = TextMuted;
        Controls.Add(summary);
        overwrite.Text = "Sobrescrever arquivos já extraídos";
        overwrite.SetBounds(26, 452, 300, 24);
        overwrite.ForeColor = TextPrimary;
        Controls.Add(overwrite);
        status.SetBounds(26, 482, 440, 20);
        status.ForeColor = TextMuted;
        status.AutoEllipsis = true;
        Controls.Add(status);
        progress.SetBounds(26, 510, 440, 10);
        progress.Style = ProgressBarStyle.Continuous;
        Controls.Add(progress);
        StyleButton(extract, "EXTRAIR", Accent, 481, 480, 112);
        extract.Click += async (_, _) => await ExtractAsync();
        Controls.Add(extract);
        StyleButton(cancel, "FECHAR", Surface2, 605, 480, 112);
        cancel.Click += (_, _) => { if (running) cancellation?.Cancel(); else Close(); };
        Controls.Add(cancel);
        CancelButton = cancel;
        FormClosing += (_, e) => { if (running) { cancellation?.Cancel(); e.Cancel = true; } };
        UpdateSummary();
    }

    private static void StyleButton(Button button, string text, Color color, int x, int y, int width)
    {
        button.Text = text;
        button.SetBounds(x, y, width, 35);
        button.BackColor = color;
        button.ForeColor = TextPrimary;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI Semibold", 9F);
    }

    private AfsBatchFile[] SelectedFiles() => files.Where(file => choices[(file.Format, file.Group)].Checked).ToArray();

    private void UpdateSummary()
    {
        int selected = SelectedFiles().Length;
        summary.Text = $"{selected:N0} arquivo(s) selecionado(s)  •  Destino: {Path.GetFileName(destinationRoot)}/<formato>/";
        extract.Enabled = !running && selected > 0;
        status.Text = selected == 0 ? "Marque ao menos um grupo para iniciar." : "Pronto para extrair do AFS ativo.";
    }

    private async Task ExtractAsync()
    {
        AfsBatchFile[] selected = SelectedFiles();
        if (selected.Length == 0 || running) return;
        running = true;
        extract.Enabled = false;
        overwrite.Enabled = false;
        foreach (CheckBox choice in choices.Values) choice.Enabled = false;
        cancel.Text = "CANCELAR";
        progress.Maximum = selected.Length;
        progress.Value = 0;
        bool replaceExisting = overwrite.Checked;
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var updates = new Progress<(int Done, string Name)>(item =>
        {
            progress.Value = item.Done;
            status.Text = $"{item.Done:N0}/{selected.Length:N0}  •  {item.Name}";
        });
        try
        {
            var result = await Task.Run(() =>
            {
                int extracted = 0, skipped = 0, done = 0;
                var failures = new List<string>();
                foreach (AfsBatchFile file in selected)
                {
                    if (token.IsCancellationRequested) break;
                    string destination = Path.Combine(destinationRoot, file.Format, Path.GetFileName(file.Entry.FileName));
                    try
                    {
                        if (File.Exists(destination) && !replaceExisting) skipped++;
                        else { AfsService.ExtractEntry(image, file.Entry, destination); extracted++; }
                    }
                    catch (Exception ex) { failures.Add($"{file.Entry.FileName}: {ex.Message}"); }
                    ((IProgress<(int Done, string Name)>)updates).Report((++done, file.Entry.FileName));
                }
                return (extracted, skipped, failures, cancelled: token.IsCancellationRequested);
            }, token);
            string report = $"{result.extracted:N0} extraído(s), {result.skipped:N0} já existente(s), {result.failures.Count:N0} falha(s)";
            status.Text = (result.cancelled ? "Cancelado: " : "Concluído: ") + report;
            Log?.Invoke($"Extração em lote de {image.IsoAfsEntry.Name}: {report}. Destino: {destinationRoot}");
            foreach (string failure in result.failures) Log?.Invoke("ERRO AO EXTRAIR LOTE: " + failure);
            if (result.failures.Count > 0)
                MessageBox.Show(this, $"Extração concluída com {result.failures.Count:N0} falha(s). Consulte o Console para detalhes.", "Extração em lote", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (OperationCanceledException) { status.Text = "Extração cancelada."; }
        finally
        {
            running = false;
            cancellation.Dispose();
            cancellation = null;
            overwrite.Enabled = true;
            foreach (var pair in choices) pair.Value.Enabled = files.Any(file => file.Format == pair.Key.Format && file.Group == pair.Key.Group);
            extract.Enabled = selected.Length > 0;
            cancel.Text = "FECHAR";
        }
    }
}

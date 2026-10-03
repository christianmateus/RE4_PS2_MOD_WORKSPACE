using System.Globalization;
using RE4_PS2_MOD_WORKSPACE.Core.Iso;
using RE4_PS2_MOD_WORKSPACE.Core.Rel;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class RelInspectorForm : AppForm
{
    private static readonly Color Bg = Color.FromArgb(15, 17, 21);
    private static readonly Color Surface = Color.FromArgb(24, 27, 33);
    private static readonly Color Accent = Color.FromArgb(190, 55, 57);
    private static readonly Color TextColor = Color.FromArgb(235, 237, 241);
    private static readonly Color Muted = Color.FromArgb(154, 162, 176);
    private readonly TextBox relPath = new(), elfPath = new(), catalogPath = new(), loadedPaths = new(),
        baseAddress = new(), filter = new();
    private readonly Label summary = new(), status = new();
    private readonly Button analyze = new();
    private readonly TabControl tabs = new();
    private readonly DataGridView headerGrid = NewGrid(), importsGrid = NewGrid(),
        symbolsGrid = NewGrid(), relocationsGrid = NewGrid();
    private RelLinkReport? report;
    private RelCatalog? catalog;

    public RelInspectorForm(string? suggestedDirectory, string? suggestedElf)
    {
        Text = "Inspetor de REL • Resident Evil 4 PS2";
        BackColor = Bg;
        ForeColor = TextColor;
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(780, 500);
        Size = new Size(1080, 680);
        StartPosition = FormStartPosition.CenterParent;
        if (!string.IsNullOrWhiteSpace(suggestedDirectory) && Directory.Exists(suggestedDirectory))
        {
            catalogPath.Text = suggestedDirectory;
            string? candidate = Directory.EnumerateFiles(suggestedDirectory, "em21.rel", SearchOption.AllDirectories).FirstOrDefault()
                ?? Directory.EnumerateFiles(suggestedDirectory, "*.rel", SearchOption.AllDirectories).FirstOrDefault();
            if (candidate is not null) relPath.Text = candidate;
        }
        if (!string.IsNullOrWhiteSpace(suggestedElf) && File.Exists(suggestedElf)) elfPath.Text = suggestedElf;
        baseAddress.Text = "0x00500000";
        BuildUi();
    }

    private void BuildUi()
    {
        var head = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 218, BackColor = Surface,
            Padding = new Padding(14, 10, 14, 8), ColumnCount = 3, RowCount = 6,
            Margin = Padding.Empty
        };
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122));
        foreach (int height in new[] { 30, 25, 34, 34, 34, 40 })
            head.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        var title = new Label { Text = "INSPEÇÃO DE REL SNR2", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, ForeColor = TextColor,
            Font = new Font("Segoe UI Semibold", 14F) };
        head.Controls.Add(title, 0, 0);
        head.SetColumnSpan(title, 3);
        var hint = new Label { Text = "Somente leitura. Informe a base EE e os RELs anteriores na ordem de carregamento.",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true, ForeColor = Muted };
        head.Controls.Add(hint, 0, 1);
        head.SetColumnSpan(hint, 3);
        AddPathRow(head, 2, "REL", relPath, "Arquivo REL (*.rel)|*.rel|Todos os arquivos|*.*", false);
        AddPathRow(head, 3, "ELF ou ISO", elfPath,
            "Executável PS2 ou ISO (SLES_*;SLUS_*;SLPS_*;*.elf;*.iso)|SLES_*;SLUS_*;SLPS_*;*.elf;*.iso|Todos os arquivos|*.*", false);
        AddPathRow(head, 4, "Pasta REL", catalogPath, "", true);
        var options = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Margin = Padding.Empty };
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 122));
        options.Controls.Add(new Label { Text = "Base EE", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted }, 0, 0);
        baseAddress.Dock = DockStyle.Fill;
        baseAddress.Margin = new Padding(2, 5, 9, 5);
        StyleTextBox(baseAddress);
        options.Controls.Add(baseAddress, 1, 0);
        options.Controls.Add(new Label { Text = "Carregados", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted }, 2, 0);
        loadedPaths.Dock = DockStyle.Fill;
        loadedPaths.Margin = new Padding(2, 5, 8, 5);
        loadedPaths.PlaceholderText = "wep02.rel@0x00510000; outro.rel@0x00520000";
        StyleTextBox(loadedPaths);
        options.Controls.Add(loadedPaths, 3, 0);
        analyze.Text = "ANALISAR";
        analyze.Dock = DockStyle.Fill;
        analyze.Margin = new Padding(4, 3, 0, 3);
        analyze.BackColor = Accent;
        analyze.ForeColor = Color.White;
        analyze.FlatStyle = FlatStyle.Flat;
        analyze.FlatAppearance.BorderSize = 0;
        analyze.Click += async (_, _) => await AnalyzeAsync();
        options.Controls.Add(analyze, 4, 0);
        head.Controls.Add(options, 0, 5);
        head.SetColumnSpan(options, 3);

        var info = new Panel { Dock = DockStyle.Top, Height = 73, BackColor = Bg, Padding = new Padding(18, 7, 18, 4) };
        summary.Dock = DockStyle.Top;
        summary.Height = 35;
        summary.ForeColor = TextColor;
        summary.Text = "Selecione um REL e o ELF principal para analisar.";
        info.Controls.Add(summary);
        var filterLabel = new Label { Text = "Filtrar", Left = 2, Top = 42, Width = 45, ForeColor = Muted };
        info.Controls.Add(filterLabel);
        filter.SetBounds(54, 39, 430, 27);
        StyleTextBox(filter);
        filter.PlaceholderText = "nome, tipo, endereço ou origem";
        filter.TextChanged += (_, _) => RefreshGrids();
        info.Controls.Add(filter);

        tabs.Dock = DockStyle.Fill;
        tabs.TabPages.Add(new TabPage("CABEÇALHO") { BackColor = Bg, Controls = { headerGrid } });
        tabs.TabPages.Add(new TabPage("IMPORTAÇÕES") { BackColor = Bg, Controls = { importsGrid } });
        tabs.TabPages.Add(new TabPage("SÍMBOLOS") { BackColor = Bg, Controls = { symbolsGrid } });
        tabs.TabPages.Add(new TabPage("REALOCAÇÕES") { BackColor = Bg, Controls = { relocationsGrid } });
        status.Dock = DockStyle.Bottom;
        status.Height = 28;
        status.Padding = new Padding(17, 4, 0, 0);
        status.ForeColor = Muted;
        status.Text = "Nenhum arquivo modificado.";
        Controls.Add(tabs);
        Controls.Add(status);
        Controls.Add(info);
        Controls.Add(head);
    }

    private static void StyleTextBox(TextBox box)
    {
        box.BackColor = Color.FromArgb(31, 35, 43);
        box.ForeColor = TextColor;
        box.BorderStyle = BorderStyle.FixedSingle;
    }

    private static DataGridView NewGrid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = Bg, BorderStyle = BorderStyle.None,
        RowHeadersVisible = false, EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 32, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        GridColor = Color.FromArgb(51, 57, 68),
        DefaultCellStyle = new DataGridViewCellStyle { BackColor = Surface, ForeColor = TextColor,
            SelectionBackColor = Color.FromArgb(70, 39, 43), SelectionForeColor = TextColor },
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(31, 35, 43), ForeColor = TextColor }
    };

    private static void AddPathRow(TableLayoutPanel parent, int row, string title, TextBox field,
        string fileFilter, bool folder)
    {
        parent.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted }, 0, row);
        field.Dock = DockStyle.Fill;
        field.Margin = new Padding(2, 4, 8, 4);
        StyleTextBox(field);
        parent.Controls.Add(field, 1, row);
        var browse = new Button { Text = "PROCURAR", Dock = DockStyle.Fill,
            Margin = new Padding(4, 2, 0, 2), BackColor = Color.FromArgb(40, 44, 52),
            ForeColor = TextColor, FlatStyle = FlatStyle.Flat };
        browse.FlatAppearance.BorderSize = 0;
        browse.Click += (_, _) =>
        {
            if (folder)
            {
                using var dialog = new FolderBrowserDialog { Description = "Selecione a pasta dos RELs para identificar provedores" };
                if (dialog.ShowDialog(parent) == DialogResult.OK) field.Text = dialog.SelectedPath;
            }
            else
            {
                using var dialog = new OpenFileDialog { Filter = fileFilter, Title = $"Selecione {title}" };
                if (dialog.ShowDialog(parent) == DialogResult.OK) field.Text = dialog.FileName;
            }
        };
        parent.Controls.Add(browse, 2, row);
    }

    private async Task AnalyzeAsync()
    {
        if (!File.Exists(relPath.Text) || !File.Exists(elfPath.Text))
        {
            MessageBox.Show(this, "Selecione um REL e um ELF principal existentes.", "Arquivos necessários",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!TryAddress(baseAddress.Text, out uint guestBase))
        {
            MessageBox.Show(this, "Informe a base EE em hexadecimal (por exemplo, 0x00500000).", "Base inválida",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        analyze.Enabled = false;
        report = null;
        catalog = null;
        headerGrid.DataSource = null;
        importsGrid.DataSource = null;
        symbolsGrid.DataSource = null;
        relocationsGrid.DataSource = null;
        summary.Text = "Analisando...";
        status.Text = "Lendo REL, ELF e provedores...";
        try
        {
            string relFile = relPath.Text, elfFile = elfPath.Text, folder = catalogPath.Text,
                loaded = loadedPaths.Text;
            var result = await Task.Run(() =>
            {
                RelModule module = RelModule.Open(relFile);
                RelMainSymbols main = ReadMainSymbols(elfFile);
                RelCatalog? available = Directory.Exists(folder) ? RelCatalog.Scan(folder, relFile) : null;
                var loadedModules = new List<RelLoadedModule>();
                foreach (string spec in loaded.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    int at = spec.LastIndexOf('@');
                    if (at <= 0 || !TryAddress(spec[(at + 1)..], out uint address))
                        throw new InvalidDataException($"REL carregado inválido: {spec}. Use caminho@0xBASE.");
                    RelModule prior = RelModule.Open(spec[..at].Trim().Trim('"'));
                    if ((address & (prior.Header.Alignment - 1)) != 0)
                        throw new InvalidDataException($"{prior.SourcePath}: base não respeita o alinhamento do REL.");
                    loadedModules.Add(new RelLoadedModule(prior, address));
                }
                return (RelLinkAnalysis.Analyze(module, main, guestBase, loadedModules, available), available);
            });
            report = result.Item1;
            catalog = result.available;
            RefreshGrids();
            int direct = report.Imports.Count(x => x.Status == RelImportStatus.MainExecutable);
            int loadedCount = report.Imports.Count(x => x.Status == RelImportStatus.LoadedRel);
            int candidate = report.Imports.Count(x => x.Status == RelImportStatus.ProviderAvailable);
            int missing = report.Imports.Count(x => x.Status == RelImportStatus.Missing);
            summary.Text = $"{Path.GetFileName(report.Module.SourcePath)} • {report.Module.ImageName} • " +
                $"{report.Module.Symbols.Count} símbolos • {report.PatchedCount}/{report.Relocations.Count} realocações simuladas";
            status.Text = $"Importações: {direct} ELF, {loadedCount} REL carregado, {candidate} provedor possível, {missing} ausentes. " +
                $"Catálogo: {catalog?.Modules.Count ?? 0} RELs; {catalog?.Issues.Count ?? 0} arquivos ignorados. Somente leitura.";
        }
        catch (Exception ex)
        {
            status.Text = "Falha na análise. Nenhum arquivo foi modificado.";
            MessageBox.Show(this, ex.Message, "Não foi possível analisar o REL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { analyze.Enabled = true; }
    }

    private void RefreshGrids()
    {
        if (report is null) return;
        string query = filter.Text.Trim();
        bool Match(params string[] values) => query.Length == 0 ||
            values.Any(x => x.Contains(query, StringComparison.OrdinalIgnoreCase));
        RelHeader h = report.Module.Header;
        headerGrid.DataSource = new (string Campo, string Valor)[]
        {
            ("Assinatura", "SNR2"), ("Imagem", report.Module.ImageName),
            ("Tamanho declarado", $"0x{h.DeclaredSize:X} ({h.DeclaredSize:N0} bytes)"),
            ("Alinhamento", $"0x{h.Alignment:X}"),
            ("Base simulada", $"0x{report.Base:X8}"),
            ("Tabela de realocações", $"+0x{h.RelocationOffset:X} • {h.RelocationCount} registros"),
            ("Tabela de símbolos", $"+0x{h.SymbolOffset:X} • {h.SymbolCount} registros"),
            ("Nome da imagem", $"+0x{h.ImageNameOffset:X}"),
            ("Construtores", $"+0x{h.ConstructorsOffset:X}"),
            ("Destrutores", $"+0x{h.DestructorsOffset:X}"),
            ("Tabela de exports", $"+0x{h.ExportOffset:X} • {h.ExportCount} entradas"),
            ("Exports", string.Join(", ", report.Module.Exports.Select((x, i) => $"[{i}]=+0x{x:X}"))),
            ("Próximo módulo", $"0x{h.NextModule:X8}"),
            ("Prolog", $"+0x{h.Prolog:X}"), ("Epilog", $"+0x{h.Epilog:X}")
        }.Where(x => Match(x.Campo, x.Valor)).Select(x => new { x.Campo, x.Valor }).ToList();
        importsGrid.DataSource = report.Imports.Where(x => Match(x.Symbol.Name, x.Status.ToString(), x.Origin,
                string.Join("; ", x.ProviderCandidates)))
            .Select(x => new { x.Symbol.Index, x.Symbol.Name, Metadata = $"0x{x.Symbol.Metadata:X4}",
                Usos = x.RelocationUses, Estado = x.Status.ToString(),
                Endereco = x.Address is uint a ? $"0x{a:X8}" : "—", x.Origin,
                Provedores = string.Join("; ", x.ProviderCandidates.Select(Path.GetFileName)) }).ToList();
        var useCounts = report.Module.Relocations.GroupBy(r => r.SymbolIndex).ToDictionary(g => g.Key, g => g.Count());
        symbolsGrid.DataSource = report.Module.Symbols.Where(x => Match(x.Name, x.IsImport ? "import" : "local",
                $"0x{x.Address:X}"))
            .Select(x => new { x.Index, x.Name, Endereco = x.IsImport ? "import" : $"+0x{x.Address:X}",
                Metadata = $"0x{x.Metadata:X4}", x.Kind, x.Flags, Usos = useCounts.GetValueOrDefault(x.Index) }).ToList();
        relocationsGrid.DataSource = report.Relocations.Where(x => Match(x.SymbolName, x.Relocation.TypeName,
                x.Origin, x.Error ?? "", $"0x{x.Relocation.Offset:X}"))
            .Select(x => new { x.Relocation.Index, Offset = $"0x{x.Relocation.Offset:X}",
                Tipo = x.Relocation.TypeName, Simbolo = x.SymbolName, Addend = x.Relocation.Addend,
                Original = $"0x{x.OriginalWord:X8}",
                Alvo = x.Target is uint a ? $"0x{a:X8}" : "—",
                Simulado = x.PatchedWord is uint p ? $"0x{p:X8}" : "—",
                x.Origin, Erro = x.Error ?? "" }).ToList();
    }

    private static bool TryAddress(string value, out uint address)
    {
        string text = value.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text[2..];
        return uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address);
    }

    private static RelMainSymbols ReadMainSymbols(string path)
    {
        if (!Path.GetExtension(path).Equals(".iso", StringComparison.OrdinalIgnoreCase))
            return RelMainSymbols.Open(path);
        IsoFileEntry entry = Iso9660Reader.ReadAllFiles(path)
            .Where(x => !x.IsDirectory &&
                (x.Name.StartsWith("SLES_", StringComparison.OrdinalIgnoreCase) ||
                 x.Name.StartsWith("SLUS_", StringComparison.OrdinalIgnoreCase) ||
                 x.Name.StartsWith("SLPS_", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Name.StartsWith("SLES_", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .FirstOrDefault() ?? throw new InvalidDataException("Nenhum executável PS2 encontrado na raiz da ISO.");
        byte[] elf = new byte[checked((int)entry.Size)];
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Position = entry.DataOffset;
        stream.ReadExactly(elf);
        return RelMainSymbols.Parse($"{Path.GetFileName(path)} › {entry.Name}", elf);
    }
}

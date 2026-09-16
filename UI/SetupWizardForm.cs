using System.Text.Json;

namespace RE4_PS2_MOD_WORKSPACE;

public enum SetupPreparation { FirstScenario, AllScenarios, None }

public sealed record SetupWizardResult(string IsoPath, string WorkspacePath, string Pcsx2Path, string? TplManagerPath, SetupPreparation Preparation, bool ShowAgain);

public sealed class SetupWizardForm : AppForm
{
    private static readonly Color Bg = Color.FromArgb(13, 15, 18), Surface = Color.FromArgb(22, 25, 30), Surface2 = Color.FromArgb(28, 31, 37), Border = Color.FromArgb(47, 51, 60), TextPrimary = Color.FromArgb(238, 240, 244), TextMuted = Color.FromArgb(145, 151, 163), Accent = Color.FromArgb(196, 56, 56), Success = Color.FromArgb(113, 190, 137);
    private readonly Panel pageHost = new(), progressLine = new();
    private readonly Label stepTitle = new(), stepSubtitle = new(), progressStatus = new();
    private readonly Button back = new(), next = new(), later = new();
    private readonly ProgressBar progress = new();
    private readonly Panel[] pages = new Panel[5];
    private readonly Label[] stepLabels = new Label[5];
    private readonly TextBox iso = new(), workspace = new(), pcsx2 = new(), tpl = new();
    private readonly Label isoInfo = new(), workspaceInfo = new();
    private readonly RadioButton prepFirst = new(), prepAll = new(), prepNone = new();
    private readonly CheckBox showAgain = new();
    private readonly Action<SetupWizardResult>? saveDraft;
    private readonly Func<SetupWizardResult, IProgress<string>, Task> complete;
    private int step;

    public SetupWizardForm(SetupWizardResult initial, Action<SetupWizardResult>? saveDraft, Func<SetupWizardResult, IProgress<string>, Task> complete)
    {
        this.saveDraft = saveDraft; this.complete = complete;
        Text = "Configuração inicial • RE4 PS2 Mod Workspace"; ClientSize = new Size(820, 570); MinimumSize = MaximumSize = new Size(836, 609); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; BackColor = Bg; ForeColor = TextPrimary; Font = new Font("Segoe UI", 9F); ShowInTaskbar = false;
        BuildChrome(); BuildPages();
        iso.Text = initial.IsoPath; workspace.Text = initial.WorkspacePath; pcsx2.Text = initial.Pcsx2Path; tpl.Text = initial.TplManagerPath ?? ""; showAgain.Checked = initial.ShowAgain;
        prepFirst.Checked = initial.Preparation == SetupPreparation.FirstScenario; prepAll.Checked = initial.Preparation == SetupPreparation.AllScenarios; prepNone.Checked = initial.Preparation == SetupPreparation.None;
        ShowStep(0);
    }

    private void BuildChrome()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Bg,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 122F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Margin = Padding.Empty };
        header.Controls.Add(new Label { Text = "RE4 PS2", Left = 28, Top = 20, Width = 160, Height = 28, Font = new Font("Segoe UI Semibold", 17F), ForeColor = TextPrimary });
        header.Controls.Add(new Label { Text = "CONFIGURAÇÃO INICIAL", Left = 29, Top = 50, Width = 220, Height = 20, Font = new Font("Segoe UI Semibold", 8F), ForeColor = Accent });
        string[] names = { "INÍCIO", "ISO", "WORKSPACE", "FERRAMENTAS", "PREPARAR" };
        for (int i = 0; i < names.Length; i++) { int index = i; stepLabels[i] = new Label { Text = $"{i + 1}\n{names[i]}", Left = 280 + i * 101, Top = 23, Width = 88, Height = 52, TextAlign = ContentAlignment.MiddleCenter, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) }; header.Controls.Add(stepLabels[i]); }
        progressLine.SetBounds(280, 88, 492, 3); progressLine.BackColor = Accent; header.Controls.Add(progressLine);
        pageHost.Dock = DockStyle.Fill; pageHost.BackColor = Bg; pageHost.Margin = Padding.Empty;
        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(24, 17, 24, 17), Margin = Padding.Empty };
        StyleButton(later, "CONFIGURAR DEPOIS", Surface2, 150); later.Dock = DockStyle.Left; later.DialogResult = DialogResult.Cancel;
        StyleButton(next, "CONTINUAR  ›", Accent, 142); next.Dock = DockStyle.Right; next.Click += Next_Click;
        StyleButton(back, "‹  VOLTAR", Surface2, 112); back.Dock = DockStyle.Right; back.Margin = new Padding(0, 0, 10, 0); back.Click += (_, _) => ShowStep(step - 1);
        footer.Controls.Add(next); footer.Controls.Add(back); footer.Controls.Add(later);
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(pageHost, 0, 1);
        layout.Controls.Add(footer, 0, 2);
        Controls.Add(layout);
        CancelButton = later;
    }

    private void BuildPages()
    {
        for (int i = 0; i < pages.Length; i++) { pages[i] = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Padding = new Padding(34, 24, 34, 18), Visible = false }; pageHost.Controls.Add(pages[i]); }
        AddHeading(pages[0], "Bem-vindo ao Mod Workspace", "Vamos preparar tudo para você começar a editar com segurança.");
        pages[0].Controls.Add(new Label { Text = "Este assistente configura sua ISO original, cria uma pasta de trabalho organizada e conecta o PCSX2. A ISO original nunca será modificada: builds e testes usam uma cópia separada.", Left = 34, Top = 105, Width = 730, Height = 72, ForeColor = TextMuted, Font = new Font("Segoe UI", 10F) });
        var safety = Card(34, 194, 730, 92); safety.Controls.Add(new Label { Text = "✓  SUA ISO ORIGINAL FICA PROTEGIDA", Left = 18, Top = 15, Width = 430, Height = 22, ForeColor = Success, Font = new Font("Segoe UI Semibold", 10F) }); safety.Controls.Add(new Label { Text = "O Workspace cria pastas separadas para extração, modificações, arquivos temporários e builds.", Left = 18, Top = 48, Width = 680, Height = 24, ForeColor = TextMuted }); pages[0].Controls.Add(safety);
        var existing = new Button(); StyleButton(existing, "USAR PROJETO EXISTENTE", Surface2, 210); existing.SetBounds(34, 310, 210, 38); existing.Click += UseExisting_Click; pages[0].Controls.Add(existing);

        AddHeading(pages[1], "Selecione a ISO original", "A ferramenta verificará se existe uma estrutura AFS compatível."); AddPathRow(pages[1], iso, "ARQUIVO ISO", 116, "SELECIONAR ISO", BrowseIso); isoInfo.SetBounds(34, 191, 730, 48); isoInfo.ForeColor = TextMuted; pages[1].Controls.Add(isoInfo); iso.TextChanged += (_, _) => ValidateIsoVisual();
        var isoCard = Card(34, 260, 730, 86); isoCard.Controls.Add(new Label { Text = "A ISO selecionada será apenas a fonte original. Nenhuma operação de edição ou build escreverá nela.", Left = 18, Top = 20, Width = 680, Height = 42, ForeColor = TextMuted }); pages[1].Controls.Add(isoCard);

        AddHeading(pages[2], "Escolha o workspace", "Use uma pasta dedicada para manter os arquivos do mod organizados."); AddPathRow(pages[2], workspace, "PASTA DO PROJETO", 116, "SELECIONAR PASTA", BrowseWorkspace); workspaceInfo.SetBounds(34, 191, 730, 45); workspaceInfo.ForeColor = TextMuted; pages[2].Controls.Add(workspaceInfo); workspace.TextChanged += (_, _) => ValidateWorkspaceVisual();
        var folders = Card(34, 252, 730, 108); folders.Controls.Add(new Label { Text = "ESTRUTURA CRIADA", Left = 18, Top = 13, Width = 180, Height = 20, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 9F) }); folders.Controls.Add(new Label { Text = "Original     Extracted     Mods     Build     Temp", Left = 18, Top = 48, Width = 680, Height = 28, ForeColor = TextMuted, Font = new Font("Consolas", 10F) }); pages[2].Controls.Add(folders);

        AddHeading(pages[3], "Configure as ferramentas", "O PCSX2 é necessário para testar a ISO; o TPL Manager é opcional."); AddPathRow(pages[3], pcsx2, "PCSX2 2.7+  •  OBRIGATÓRIO", 108, "SELECIONAR", () => BrowseExe(pcsx2, "Selecione o PCSX2")); AddPathRow(pages[3], tpl, "TPL MANAGER  •  OPCIONAL", 210, "SELECIONAR", () => BrowseExe(tpl, "Selecione o TPL Manager")); pages[3].Controls.Add(new Label { Text = "Você pode alterar essas ferramentas depois em Configurações/Ferramentas.", Left = 34, Top = 300, Width = 700, Height = 24, ForeColor = TextMuted });

        AddHeading(pages[4], "Prepare o primeiro uso", "Escolha quanto conteúdo deseja extrair agora."); SetupRadio(prepFirst, "Extrair o primeiro cenário", "Mais rápido. Abre um cenário inicial para conhecer o Editor Visual.", 108, true); SetupRadio(prepAll, "Extrair todos os cenários", "Prepara r100 até o último cenário disponível. Pode levar alguns minutos.", 181, false); SetupRadio(prepNone, "Não extrair agora", "Finaliza a configuração e deixa a extração para o módulo Arquivos.", 254, false);
        showAgain.Text = "Mostrar este assistente novamente ao iniciar"; showAgain.SetBounds(34, 340, 430, 26); showAgain.ForeColor = TextMuted; showAgain.BackColor = Bg; pages[4].Controls.Add(showAgain);
        progressStatus.SetBounds(34, 374, 730, 24); progressStatus.ForeColor = TextMuted; progressStatus.Visible = false; pages[4].Controls.Add(progressStatus); progress.SetBounds(34, 405, 730, 12); progress.Style = ProgressBarStyle.Marquee; progress.MarqueeAnimationSpeed = 25; progress.Visible = false; pages[4].Controls.Add(progress);
    }

    private async void Next_Click(object? sender, EventArgs e)
    {
        if (!ValidateStep()) return;
        SaveDraft();
        if (step < 4) { ShowStep(step + 1); return; }
        ToggleBusy(true); try { await complete(Result(), new Progress<string>(x => { progressStatus.Text = x; progressStatus.Refresh(); })); DialogResult = DialogResult.OK; Close(); } catch (Exception ex) { ToggleBusy(false); MessageBox.Show(this, ex.Message, "Não foi possível concluir", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private bool ValidateStep()
    {
        if (step == 1 && (!File.Exists(iso.Text.Trim()) || !Path.GetExtension(iso.Text.Trim()).Equals(".iso", StringComparison.OrdinalIgnoreCase))) return Warn("Selecione um arquivo ISO válido.");
        if (step == 2 && string.IsNullOrWhiteSpace(workspace.Text)) return Warn("Selecione a pasta do workspace.");
        if (step == 3 && (!File.Exists(pcsx2.Text.Trim()) || !Path.GetExtension(pcsx2.Text.Trim()).Equals(".exe", StringComparison.OrdinalIgnoreCase))) return Warn("Selecione o executável do PCSX2.");
        if (step == 3 && !string.IsNullOrWhiteSpace(tpl.Text) && !File.Exists(tpl.Text.Trim())) return Warn("O TPL Manager informado não foi encontrado. Remova o caminho ou selecione um executável válido.");
        return true;
    }

    private bool Warn(string text) { MessageBox.Show(this, text, "Configuração inicial", MessageBoxButtons.OK, MessageBoxIcon.Information); return false; }
    private SetupWizardResult Result() => new(iso.Text.Trim(), workspace.Text.Trim(), pcsx2.Text.Trim(), string.IsNullOrWhiteSpace(tpl.Text) ? null : tpl.Text.Trim(), prepAll.Checked ? SetupPreparation.AllScenarios : prepNone.Checked ? SetupPreparation.None : SetupPreparation.FirstScenario, showAgain.Checked);
    private void SaveDraft() => saveDraft?.Invoke(Result());
    private void ShowStep(int value) { step = Math.Clamp(value, 0, 4); for (int i = 0; i < pages.Length; i++) { pages[i].Visible = i == step; stepLabels[i].ForeColor = i <= step ? (i == step ? TextPrimary : Accent) : TextMuted; } progressLine.Width = Math.Max(8, (int)(492 * ((step + 1) / 5f))); back.Visible = step > 0; next.Text = step == 4 ? "CONCLUIR" : "CONTINUAR  ›"; pages[step].BringToFront(); }
    private void ToggleBusy(bool busy) { back.Enabled = next.Enabled = later.Enabled = !busy; pageHost.Enabled = !busy; progress.Visible = progressStatus.Visible = busy; if (busy) progressStatus.Text = "Validando configuração..."; }
    private void ValidateIsoVisual() { if (!File.Exists(iso.Text.Trim())) { isoInfo.Text = "Selecione uma ISO do jogo para continuar."; isoInfo.ForeColor = TextMuted; return; } var f = new FileInfo(iso.Text.Trim()); isoInfo.Text = $"✓ Arquivo encontrado  •  {f.Length / 1024d / 1024d / 1024d:N2} GB"; isoInfo.ForeColor = Success; }
    private void ValidateWorkspaceVisual() { try { string path = workspace.Text.Trim(); if (path.Length == 0) throw new Exception(); string root = Path.GetPathRoot(Path.GetFullPath(path))!; var drive = new DriveInfo(root); workspaceInfo.Text = $"Espaço disponível: {drive.AvailableFreeSpace / 1024d / 1024d / 1024d:N1} GB  •  {(Directory.Exists(path) ? "Pasta existente" : "A pasta será criada")}"; workspaceInfo.ForeColor = Success; } catch { workspaceInfo.Text = "Selecione uma pasta válida."; workspaceInfo.ForeColor = TextMuted; } }
    private void BrowseIso() { using var d = new OpenFileDialog { Filter = "ISO do PlayStation 2 (*.iso)|*.iso", Title = "Selecione a ISO original" }; if (d.ShowLocalizedDialog(this) == DialogResult.OK) iso.Text = d.FileName; }
    private void BrowseWorkspace() { using var d = new FolderBrowserDialog { Description = "Escolha a pasta do workspace", UseDescriptionForTitle = true, ShowNewFolderButton = true }; if (d.ShowLocalizedDialog(this) == DialogResult.OK) workspace.Text = d.SelectedPath; }
    private void BrowseExe(TextBox target, string title) { using var d = new OpenFileDialog { Filter = "Executável (*.exe)|*.exe", Title = title }; if (d.ShowLocalizedDialog(this) == DialogResult.OK) target.Text = d.FileName; }
    private void UseExisting_Click(object? sender, EventArgs e) { using var d = new FolderBrowserDialog { Description = "Selecione um workspace existente", UseDescriptionForTitle = true }; if (d.ShowLocalizedDialog(this) != DialogResult.OK) return; workspace.Text = d.SelectedPath; try { string file = Path.Combine(d.SelectedPath, ".re4workspace.json"); if (File.Exists(file)) { using JsonDocument json = JsonDocument.Parse(File.ReadAllText(file)); if (json.RootElement.TryGetProperty("IsoPath", out JsonElement value) && value.ValueKind == JsonValueKind.String) iso.Text = value.GetString() ?? iso.Text; } } catch { } ShowStep(3); }
    private void SetupRadio(RadioButton radio, string title, string detail, int top, bool check) { radio.Text = title + "\r\n" + detail; radio.SetBounds(34, top, 730, 58); radio.Appearance = Appearance.Button; radio.FlatStyle = FlatStyle.Flat; radio.FlatAppearance.BorderColor = Border; radio.FlatAppearance.CheckedBackColor = Color.FromArgb(55, 38, 41); radio.BackColor = Surface; radio.ForeColor = TextPrimary; radio.Padding = new Padding(14, 4, 8, 4); radio.TextAlign = ContentAlignment.MiddleLeft; radio.Checked = check; pages[4].Controls.Add(radio); }
    private static Panel Card(int left, int top, int width, int height) => new() { Left = left, Top = top, Width = width, Height = height, BackColor = Surface2, BorderStyle = BorderStyle.FixedSingle };
    private static void AddHeading(Control page, string title, string subtitle) { page.Controls.Add(new Label { Text = title, Left = 34, Top = 25, Width = 730, Height = 35, ForeColor = TextPrimary, Font = new Font("Segoe UI Semibold", 18F) }); page.Controls.Add(new Label { Text = subtitle, Left = 35, Top = 67, Width = 720, Height = 24, ForeColor = TextMuted }); }
    private static void AddPathRow(Control page, TextBox field, string label, int top, string buttonText, Action browse) { page.Controls.Add(new Label { Text = label, Left = 34, Top = top, Width = 400, Height = 19, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) }); field.SetBounds(34, top + 24, 548, 30); field.BackColor = Surface2; field.ForeColor = TextPrimary; field.BorderStyle = BorderStyle.FixedSingle; page.Controls.Add(field); var button = new Button(); StyleButton(button, buttonText, Accent, 170); button.SetBounds(594, top + 22, 170, 34); button.Click += (_, _) => browse(); page.Controls.Add(button); }
    private static void StyleButton(Button button, string text, Color color, int width) { button.Text = text; button.Width = width; button.Height = 36; button.BackColor = color; button.ForeColor = TextPrimary; button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 0; button.Cursor = Cursors.Hand; button.Font = new Font("Segoe UI Semibold", 8.5F); button.UseMnemonic = false; }
}

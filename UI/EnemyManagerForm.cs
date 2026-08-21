using RE4_PS2_MOD_WORKSPACE.Core.Afs;
using RE4_PS2_MOD_WORKSPACE.Core.Visual;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed class EnemyManagerForm : Form
{
    private readonly AfsImage afs;
    private readonly string workspaceRoot;
    private readonly Action<string> log;
    private readonly Action<EslScene?> sceneLoaded;
    private readonly ComboBox cmbFiles = new();
    private readonly ListBox lstEntries = new();
    private readonly PropertyGrid props = new();
    private readonly CheckBox chkActiveOnly = new();
    private readonly Label lblFileInfo = new();
    private readonly Label lblEntryCount = new();
    private readonly Button btnOpen = new();
    private readonly Button btnSave = new();
    private readonly Button btnReextract = new();
    private EslScene? scene;
    private string? currentPath;

    public EnemyManagerForm(AfsImage afs, string workspaceRoot, Action<string> log, Action<EslScene?> sceneLoaded)
    {
        this.afs = afs;
        this.workspaceRoot = workspaceRoot;
        this.log = log;
        this.sceneLoaded = sceneLoaded;

        Text = "Enemy Manager • ESL";
        Width = 1040;
        Height = 720;
        MinimumSize = new Size(820, 560);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(13, 15, 18);
        ForeColor = Color.FromArgb(238, 240, 244);
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        RefreshFiles();
    }

    private void BuildUi()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(16, 12, 16, 10), BackColor = Color.FromArgb(22, 25, 30) };
        var title = new Label { Text = "Enemy Manager", Left = 16, Top = 10, Width = 250, Height = 28, Font = new Font("Segoe UI Semibold", 17F), ForeColor = Color.FromArgb(238, 240, 244) };
        var sub = new Label { Text = "Selecione um emleon*.ESL diretamente da lista do AFS ativo.", Left = 18, Top = 40, Width = 650, Height = 20, ForeColor = Color.FromArgb(145, 151, 163) };
        cmbFiles.Left = 16; cmbFiles.Top = 70; cmbFiles.Width = 310; cmbFiles.Height = 30; cmbFiles.DropDownStyle = ComboBoxStyle.DropDownList; cmbFiles.BackColor = Color.FromArgb(28, 31, 37); cmbFiles.ForeColor = Color.FromArgb(238, 240, 244); cmbFiles.FlatStyle = FlatStyle.Flat; cmbFiles.SelectedIndexChanged += (_, _) => UpdateFileInfo();
        btnOpen.SetBounds(338, 68, 110, 32); StyleButton(btnOpen, "ABRIR ESL", true); btnOpen.Click += (_, _) => OpenSelected(false);
        btnReextract.SetBounds(456, 68, 118, 32); StyleButton(btnReextract, "RE-EXTRAIR", false); btnReextract.Click += (_, _) => OpenSelected(true);
        btnSave.SetBounds(582, 68, 105, 32); StyleButton(btnSave, "SALVAR", true); btnSave.Enabled = false; btnSave.Click += (_, _) => SaveCurrent();
        lblFileInfo.SetBounds(700, 66, 310, 38); lblFileInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; lblFileInfo.ForeColor = Color.FromArgb(145, 151, 163); lblFileInfo.TextAlign = ContentAlignment.MiddleRight; lblFileInfo.AutoEllipsis = true;
        header.Controls.AddRange(new Control[] { title, sub, cmbFiles, btnOpen, btnReextract, btnSave, lblFileInfo });

        var filter = new Panel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(12, 8, 12, 6), BackColor = Color.FromArgb(18, 20, 24) };
        chkActiveOnly.Text = "Somente ativos"; chkActiveOnly.Checked = true; chkActiveOnly.AutoSize = true; chkActiveOnly.Left = 12; chkActiveOnly.Top = 11; chkActiveOnly.ForeColor = Color.FromArgb(200, 205, 212); chkActiveOnly.CheckedChanged += (_, _) => RefreshEntries();
        lblEntryCount.Left = 180; lblEntryCount.Top = 11; lblEntryCount.Width = 330; lblEntryCount.Height = 20; lblEntryCount.ForeColor = Color.FromArgb(145, 151, 163);
        filter.Controls.Add(chkActiveOnly); filter.Controls.Add(lblEntryCount);

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 365, BackColor = Color.FromArgb(13, 15, 18), Panel1MinSize = 250, Panel2MinSize = 350 };
        lstEntries.Dock = DockStyle.Fill; lstEntries.BorderStyle = BorderStyle.None; lstEntries.BackColor = Color.FromArgb(22, 25, 30); lstEntries.ForeColor = Color.FromArgb(238, 240, 244); lstEntries.Font = new Font("Consolas", 9.5F); lstEntries.SelectedIndexChanged += (_, _) => props.SelectedObject = lstEntries.SelectedItem as EslEnemyEntry;
        props.Dock = DockStyle.Fill; props.HelpVisible = true; props.ToolbarVisible = false; props.BackColor = Color.FromArgb(22, 25, 30); props.PropertyValueChanged += (_, _) => { lstEntries.Refresh(); };
        split.Panel1.Padding = new Padding(12); split.Panel2.Padding = new Padding(12); split.Panel1.Controls.Add(lstEntries); split.Panel2.Controls.Add(props);

        Controls.Add(split); Controls.Add(filter); Controls.Add(header);
    }

    private static void StyleButton(Button button, string text, bool accent)
    {
        button.Text = text; button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = accent ? 0 : 1; button.FlatAppearance.BorderColor = Color.FromArgb(58, 63, 72); button.BackColor = accent ? Color.FromArgb(71, 108, 255) : Color.FromArgb(28, 31, 37); button.ForeColor = Color.White; button.Cursor = Cursors.Hand; button.Font = new Font("Segoe UI Semibold", 8.5F);
    }

    private void RefreshFiles()
    {
        AfsEntry[] entries = AfsService.GetEmleonEslEntries(afs).ToArray();
        cmbFiles.BeginUpdate(); cmbFiles.Items.Clear(); cmbFiles.Items.AddRange(entries.Cast<object>().ToArray()); cmbFiles.EndUpdate();
        if (cmbFiles.Items.Count > 0) cmbFiles.SelectedIndex = 0;
        btnOpen.Enabled = cmbFiles.Items.Count > 0; btnReextract.Enabled = cmbFiles.Items.Count > 0;
        if (entries.Length == 0) lblFileInfo.Text = "Nenhum emleon*.ESL encontrado neste AFS.";
        log($"Enemy Manager: {entries.Length:N0} arquivo(s) emleon*.ESL encontrado(s) em {afs.IsoAfsEntry.FullPath}.");
    }

    private void UpdateFileInfo()
    {
        if (cmbFiles.SelectedItem is not AfsEntry entry) { lblFileInfo.Text = "Nenhum ESL selecionado"; return; }
        lblFileInfo.Text = $"AFS #{entry.Index:D4} • {FormatBytes(entry.CurrentSize)} • reservado {FormatBytes(entry.AllocatedSize)}";
    }

    private void OpenSelected(bool forceExtract)
    {
        if (cmbFiles.SelectedItem is not AfsEntry entry) return;
        try
        {
            string dir = Path.Combine(workspaceRoot, "Extracted", "ESL");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, entry.FileName);
            if (forceExtract && File.Exists(path))
            {
                if (MessageBox.Show(this, "Isso substituirá a cópia extraída atual pelo arquivo original do AFS. Continuar?", "Re-extrair ESL", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            }
            if (forceExtract || !File.Exists(path))
            {
                AfsService.ExtractEntry(afs, entry, path);
                log($"Enemy Manager: {entry.FileName} extraído do AFS para {path}.");
            }
            else log($"Enemy Manager: usando cópia já extraída de {entry.FileName}.");

            scene = Ps2EslReader.Read(path);
            currentPath = path;
            btnSave.Enabled = true;
            Text = "Enemy Manager • " + entry.FileName;
            RefreshEntries();
            sceneLoaded(scene);
            log($"Enemy Manager: {entry.FileName} carregado • {scene.ActiveCount:N0}/{scene.Entries.Count:N0} entries ativas.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Enemy Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            log("Enemy Manager: ERRO: " + ex.Message);
        }
    }

    private void RefreshEntries()
    {
        EslEnemyEntry? selected = lstEntries.SelectedItem as EslEnemyEntry;
        lstEntries.BeginUpdate(); lstEntries.Items.Clear();
        if (scene != null)
            foreach (EslEnemyEntry entry in scene.Entries.Where(x => !chkActiveOnly.Checked || x.Active != 0)) lstEntries.Items.Add(entry);
        lstEntries.EndUpdate();
        if (selected != null && lstEntries.Items.Contains(selected)) lstEntries.SelectedItem = selected;
        else if (lstEntries.Items.Count > 0) lstEntries.SelectedIndex = 0;
        lblEntryCount.Text = scene == null ? "Nenhum ESL aberto" : $"{scene.ActiveCount:N0} ativos • {scene.Entries.Count:N0} entries totais";
    }

    private void SaveCurrent()
    {
        if (scene == null || string.IsNullOrWhiteSpace(currentPath)) return;
        try
        {
            Ps2EslWriter.Save(scene);
            lstEntries.Refresh();
            sceneLoaded(scene);
            log($"Enemy Manager: ESL salvo: {Path.GetFileName(currentPath)}.");
            MessageBox.Show(this, "ESL salvo com sucesso. O backup .bak inicial foi preservado.", "Enemy Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Enemy Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            log("Enemy Manager: erro ao salvar ESL: " + ex.Message);
        }
    }

    private static string FormatBytes(long value)
    {
        if (value >= 1024 * 1024) return $"{value / 1024d / 1024d:0.00} MB";
        if (value >= 1024) return $"{value / 1024d:0.00} KB";
        return $"{value:N0} B";
    }
}

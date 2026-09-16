using RE4_PS2_MOD_WORKSPACE.Core.Audio;
using System.Media;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private SndDocument? currentSnd;
    private SoundPlayer? soundPlayer;
    private string? soundPreviewPath;
    private readonly System.Windows.Forms.Timer soundPreviewTimer = new() { Interval = 100 };
    private DateTime soundPreviewStarted;
    private TimeSpan soundPreviewDuration;
    private readonly Stack<SoundUndoEntry> soundUndo = new();
    private readonly Stack<SoundUndoEntry> soundRedo = new();
    private Dictionary<string, SoundTrackMetadata> soundMetadata = new(StringComparer.OrdinalIgnoreCase);

    private void btnNavSounds_Click(object? sender, EventArgs e)
    {
        SaveVisualCameraIfLeaving(); ShowPage(pnlSounds, btnNavSounds, "Sons"); RememberMainPage("Sounds"); RefreshSoundFiles();
    }
    private void btnSoundRefresh_Click(object? sender, EventArgs e) => RefreshSoundFiles();
    private void btnSoundBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "Resident Evil 4 SND (*.snd)|*.snd|Todos os arquivos (*.*)|*.*", Title = "Abrir banco de sons SND" };
        if (dlg.ShowLocalizedDialog(this) == DialogResult.OK) LoadSnd(dlg.FileName);
    }
    private void RefreshSoundFiles()
    {
        string? selected = (cmbSoundFiles.SelectedItem as SoundFileItem)?.Path; cmbSoundFiles.BeginUpdate(); cmbSoundFiles.Items.Clear();
        if (!string.IsNullOrWhiteSpace(project.RootPath) && Directory.Exists(project.RootPath))
        {
            try { foreach (string path in Directory.EnumerateFiles(project.RootPath, "*.snd", SearchOption.AllDirectories).OrderBy(Path.GetFileName)) cmbSoundFiles.Items.Add(new SoundFileItem(path, Path.GetRelativePath(project.RootPath, path))); } catch { }
        }
        cmbSoundFiles.EndUpdate();
        if (cmbSoundFiles.Items.Count > 0) { int i = 0; if (selected != null) for (int n = 0; n < cmbSoundFiles.Items.Count; n++) if ((cmbSoundFiles.Items[n] as SoundFileItem)?.Path == selected) { i = n; break; } cmbSoundFiles.SelectedIndex = i; }
        else if (currentSnd == null) lblSoundStatus.Text = "Nenhum SND encontrado no workspace. Use ABRIR SND.";
    }
    private void cmbSoundFiles_SelectedIndexChanged(object? sender, EventArgs e) { if (cmbSoundFiles.SelectedItem is SoundFileItem item && currentSnd?.SourcePath != item.Path) LoadSnd(item.Path); }
    private void LoadSnd(string path)
    {
        try
        {
            StopSoundPreview(); currentSnd = SndCodec.Read(path); LoadSoundMetadata(); lblSoundFile.Text = Path.GetFileName(path); int count = currentSnd.Groups.Sum(x => x.Samples.Count);
            lblSoundSummary.Text = $"{currentSnd.Groups.Count} grupo(s)    {count} áudio(s)    {new FileInfo(path).Length:N0} bytes";
            cmbSoundGroups.BeginUpdate(); cmbSoundGroups.Items.Clear(); foreach (SndGroup group in currentSnd.Groups) cmbSoundGroups.Items.Add(new SoundGroupItem(group)); cmbSoundGroups.EndUpdate();
            if (cmbSoundGroups.Items.Count > 0) cmbSoundGroups.SelectedIndex = 0; soundUndo.Clear(); soundRedo.Clear(); btnSoundSave.Enabled = false; btnSoundRestore.Enabled = true; lblSoundStatus.Text = "Banco analisado com sucesso. Selecione uma faixa para ouvir ou editar."; ExtractLog($"Sons: {Path.GetFileName(path)} | {currentSnd.Groups.Count} grupos | {count} áudios.");
        }
        catch (Exception ex) { currentSnd = null; gridSoundSamples.Rows.Clear(); cmbSoundGroups.Items.Clear(); lblSoundStatus.Text = "Não foi possível abrir este SND."; MessageBox.Show(this, ex.Message, "Sons", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void cmbSoundGroups_SelectedIndexChanged(object? sender, EventArgs e) => RefreshSoundSampleGrid();
    private void RefreshSoundSampleGrid(int selectIndex = -1)
    {
        gridSoundSamples.Rows.Clear(); if (cmbSoundGroups.SelectedItem is not SoundGroupItem item) return;
        foreach (SndSample s in item.Group.Samples)
        {
            SoundTrackMetadata metadata = GetSoundMetadata(item.Group.Index, s.Index);
            int row = gridSoundSamples.Rows.Add(metadata.Favorite ? "★" : "☆", null, $"#{s.Index + 1:000}", FormatDuration(s.Duration), $"{s.SampleRate:N0} Hz", FormatBytes(s.EffectiveData.Length), s.LoopFlag == 0 ? "Não" : "Sim", metadata.Note, s.ReplacementData == null ? "Original" : "Modificado");
            DataGridViewRow gridRow = gridSoundSamples.Rows[row]; gridRow.Tag = s.Index;
            gridRow.Cells["Favorite"].Style.ForeColor = metadata.Favorite ? Color.FromArgb(238, 190, 82) : TextMuted; gridRow.Cells["Favorite"].Tag = metadata.Favorite ? 1d : 0d;
            gridRow.Cells["Index"].Tag = (double)s.Index; gridRow.Cells["Duration"].Tag = s.Duration.TotalSeconds; gridRow.Cells["Rate"].Tag = (double)s.SampleRate; gridRow.Cells["Size"].Tag = (double)s.EffectiveData.Length; gridRow.Cells["Loop"].Tag = s.LoopFlag == 0 ? 0d : 1d;
        }
        if (gridSoundSamples.Rows.Count > 0) { DataGridViewRow selected = selectIndex >= 0 ? gridSoundSamples.Rows.Cast<DataGridViewRow>().FirstOrDefault(x => x.Tag is int index && index == selectIndex) ?? gridSoundSamples.Rows[0] : gridSoundSamples.Rows[0]; selected.Selected = true; gridSoundSamples.CurrentCell = selected.Cells[0]; }
    }
    private SndSample? SelectedSoundSample => cmbSoundGroups.SelectedItem is SoundGroupItem g && gridSoundSamples.CurrentRow?.Tag is int i && i >= 0 && i < g.Group.Samples.Count ? g.Group.Samples[i] : null;
    private void gridSoundSamples_SortCompare(object? sender, DataGridViewSortCompareEventArgs e)
    {
        object? left = gridSoundSamples.Rows[e.RowIndex1].Cells[e.Column.Index].Tag, right = gridSoundSamples.Rows[e.RowIndex2].Cells[e.Column.Index].Tag;
        if (left is double a && right is double b) { e.SortResult = a.CompareTo(b); e.Handled = true; return; }
        e.SortResult = StringComparer.CurrentCultureIgnoreCase.Compare(e.CellValue1?.ToString(), e.CellValue2?.ToString()); e.Handled = true;
    }
    private void gridSoundSamples_SelectionChanged(object? sender, EventArgs e)
    {
        StopSoundPreview(); SndSample? s = SelectedSoundSample; lblSoundSelection.Text = s == null ? "Selecione um áudio à esquerda" : $"Faixa #{s.Index + 1:000}\n{FormatDuration(s.Duration)}  •  {s.SampleRate:N0} Hz  •  {FormatBytes(s.EffectiveData.Length)}";
        bool enabled = s != null; btnSoundPlay.Enabled = btnSoundExportVag.Enabled = btnSoundExportWav.Enabled = btnSoundReplace.Enabled = enabled;
    }
    private void gridSoundSamples_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) return; DataGridView.HitTestInfo hit = gridSoundSamples.HitTest(e.X, e.Y);
        if (hit.RowIndex < 0) { gridSoundSamples.ClearSelection(); return; }
        if (!gridSoundSamples.Rows[hit.RowIndex].Selected) gridSoundSamples.ClearSelection(); gridSoundSamples.Rows[hit.RowIndex].Selected = true; gridSoundSamples.CurrentCell = gridSoundSamples.Rows[hit.RowIndex].Cells[0];
    }
    private void gridSoundSamples_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return; gridSoundSamples.CurrentCell = gridSoundSamples.Rows[e.RowIndex].Cells[e.ColumnIndex]; gridSoundSamples.Rows[e.RowIndex].Selected = true;
        if (e.ColumnIndex == gridSoundSamples.Columns["Play"].Index) btnSoundPlay_Click(null, EventArgs.Empty);
        else if (e.ColumnIndex == gridSoundSamples.Columns["Favorite"].Index) ToggleSelectedSoundFavorite();
    }
    private void gridSoundSamples_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex == gridSoundSamples.Columns["Play"].Index || e.ColumnIndex == gridSoundSamples.Columns["Favorite"].Index) return; gridSoundSamples.CurrentCell = gridSoundSamples.Rows[e.RowIndex].Cells[0]; SndSample? sample = SelectedSoundSample; if (sample == null) return;
        using var editor = new SoundEditorForm(sample); if (editor.ShowLocalizedDialog(this) != DialogResult.OK) return; SoundUndoEntry undo = CaptureSoundUndo(sample); sample.ReplacementData = editor.ResultData; sample.SampleRate = editor.ResultRate; sample.LoopFlag = editor.ResultLoop; soundUndo.Push(undo); soundRedo.Clear(); btnSoundSave.Enabled = true; RefreshSoundSampleGrid(sample.Index); lblSoundStatus.Text = "Ajustes aplicados à faixa. Ctrl+Z desfaz; salve o SND para gravar.";
    }
    private void btnSoundPlay_Click(object? sender, EventArgs e)
    {
        SndSample? s = SelectedSoundSample; if (s == null) return;
        try
        {
            StopSoundPreview(); short[] pcm = PsxAdpcm.Decode(s.EffectiveData); byte[] wav = WaveCodec.Write(pcm, checked((int)s.SampleRate)); soundPreviewPath = Path.Combine(Path.GetTempPath(), $"re4ps2-preview-{Guid.NewGuid():N}.wav"); File.WriteAllBytes(soundPreviewPath, wav); soundPlayer = new SoundPlayer(soundPreviewPath); soundPlayer.Play(); soundPreviewStarted = DateTime.UtcNow; soundPreviewDuration = TimeSpan.FromSeconds(pcm.Length / (double)Math.Max(1, s.SampleRate)); trkSoundPosition.Value = 0; soundPreviewTimer.Tick -= SoundPreviewTimer_Tick; soundPreviewTimer.Tick += SoundPreviewTimer_Tick; soundPreviewTimer.Start(); btnSoundPlay.Text = "PLAYING";
        }
        catch (Exception ex) { StopSoundPreview(); MessageBox.Show(this, ex.Message, "Prévia de áudio", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void SoundPreviewTimer_Tick(object? sender, EventArgs e) { if (soundPreviewDuration.TotalMilliseconds <= 0) return; double x = (DateTime.UtcNow - soundPreviewStarted).TotalMilliseconds / soundPreviewDuration.TotalMilliseconds; trkSoundPosition.Value = Math.Clamp((int)(x * 1000), 0, 1000); if (x >= 1) StopSoundPreview(); }
    private void btnSoundStop_Click(object? sender, EventArgs e) => StopSoundPreview();
    private void StopSoundPreview()
    {
        soundPreviewTimer.Stop(); soundPlayer?.Stop(); soundPlayer?.Dispose(); soundPlayer = null; if (btnSoundPlay != null) btnSoundPlay.Text = "PLAY"; if (trkSoundPosition != null) trkSoundPosition.Value = 0;
        if (soundPreviewPath != null) { try { File.Delete(soundPreviewPath); } catch { } soundPreviewPath = null; }
    }
    private void btnSoundExportVag_Click(object? sender, EventArgs e) => ExportSelectedSound(false);
    private void btnSoundExportWav_Click(object? sender, EventArgs e) => ExportSelectedSound(true);
    private void btnSoundExportAllWav_Click(object? sender, EventArgs e) => ExportAllSounds(true);
    private void btnSoundExportAllVag_Click(object? sender, EventArgs e) => ExportAllSounds(false);
    private void ExportSelectedSound(bool wave)
    {
        SndSample? s = SelectedSoundSample; if (s == null || currentSnd == null) return; string ext = wave ? "wav" : "vag";
        using var dlg = new SaveFileDialog { Filter = wave ? "Wave PCM (*.wav)|*.wav" : "Sony VAG (*.vag)|*.vag", FileName = $"{Path.GetFileNameWithoutExtension(currentSnd.SourcePath)}_{s.Index + 1:000}.{ext}" };
        if (dlg.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try { byte[] data = wave ? WaveCodec.Write(PsxAdpcm.Decode(s.EffectiveData), checked((int)s.SampleRate)) : SndCodec.ExportVag(s); File.WriteAllBytes(dlg.FileName, data); lblSoundStatus.Text = $"Faixa exportada como {ext.ToUpperInvariant()}."; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar áudio", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void ExportAllSounds(bool wave)
    {
        if (currentSnd == null) return; using var dlg = new FolderBrowserDialog { Description = $"Escolha a pasta para exportar todas as faixas em {(wave ? "WAV" : "VAG")}." }; if (dlg.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try { int count = 0; foreach (SndGroup group in currentSnd.Groups) foreach (SndSample sample in group.Samples) { string name = GetBatchSoundName(group.Index, sample.Index, wave ? ".wav" : ".vag"); byte[] data = wave ? WaveCodec.Write(PsxAdpcm.Decode(sample.EffectiveData), checked((int)sample.SampleRate)) : SndCodec.ExportVag(sample, Path.GetFileNameWithoutExtension(name)); File.WriteAllBytes(Path.Combine(dlg.SelectedPath, name), data); count++; } lblSoundStatus.Text = $"{count} faixas exportadas. Padrão: {GetBatchSoundName(0, 0, wave ? ".wav" : ".vag")}"; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar todas", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void btnSoundImportFolder_Click(object? sender, EventArgs e)
    {
        if (currentSnd == null) return; string exampleWav = GetBatchSoundName(0, 0, ".wav"), exampleVag = GetBatchSoundName(0, 0, ".vag");
        if (MessageBox.Show(this, $"Os arquivos devem usar exatamente este padrão:\n\n{exampleWav}\nou\n{exampleVag}\n\n'g01' é o grupo e '001' é a faixa. Arquivos ausentes serão ignorados. Continuar?", "Importar pasta", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;
        using var dlg = new FolderBrowserDialog { Description = "Selecione a pasta com os WAV/VAG nomeados conforme o padrão exibido." }; if (dlg.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            var files = Directory.EnumerateFiles(dlg.SelectedPath).Where(x => Path.GetExtension(x).Equals(".wav", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(x).Equals(".vag", StringComparison.OrdinalIgnoreCase)).ToDictionary(x => Path.GetFileName(x)!, StringComparer.OrdinalIgnoreCase); int imported = 0; var errors = new List<string>(); soundRedo.Clear();
            foreach (SndGroup group in currentSnd.Groups) foreach (SndSample sample in group.Samples)
            {
                string wav = GetBatchSoundName(group.Index, sample.Index, ".wav"), vag = GetBatchSoundName(group.Index, sample.Index, ".vag"); string? file = files.GetValueOrDefault(wav) ?? files.GetValueOrDefault(vag); if (file == null) continue;
                try { SoundUndoEntry undo = CaptureSoundUndo(group.Index, sample); byte[] data = File.ReadAllBytes(file); if (Path.GetExtension(file).Equals(".vag", StringComparison.OrdinalIgnoreCase)) SndCodec.ImportVag(sample, data); else SndCodec.ImportWave(sample, data); soundUndo.Push(undo); imported++; } catch (Exception ex) { errors.Add($"{Path.GetFileName(file)}: {ex.Message}"); }
            }
            RefreshSoundSampleGrid(); btnSoundSave.Enabled = currentSnd.IsModified; lblSoundStatus.Text = imported == 0 ? "Nenhum arquivo com o padrão esperado foi encontrado." : $"{imported} faixa(s) importada(s) da pasta. Salve o SND para gravar."; if (errors.Count > 0) MessageBox.Show(this, string.Join("\n", errors.Take(12)), "Alguns arquivos não foram importados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Importar pasta", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void btnSoundReplace_Click(object? sender, EventArgs e)
    {
        SndSample? s = SelectedSoundSample; if (s == null) return; using var dlg = new OpenFileDialog { Filter = "Áudio compatível (*.wav;*.vag)|*.wav;*.vag|Wave PCM (*.wav)|*.wav|Sony VAG (*.vag)|*.vag", Title = "Substituir faixa" }; if (dlg.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try { byte[] data = File.ReadAllBytes(dlg.FileName); SoundUndoEntry undo = CaptureSoundUndo(s); if (Path.GetExtension(dlg.FileName).Equals(".vag", StringComparison.OrdinalIgnoreCase)) SndCodec.ImportVag(s, data); else SndCodec.ImportWave(s, data); soundUndo.Push(undo); soundRedo.Clear(); btnSoundSave.Enabled = true; RefreshSoundSampleGrid(s.Index); lblSoundStatus.Text = "Faixa substituída em memória. Use SALVAR SND para gravar o banco. Ctrl+Z desfaz."; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Substituir áudio", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void soundZeroMenu_Click(object? sender, EventArgs e)
    {
        if (cmbSoundGroups.SelectedItem is not SoundGroupItem groupItem) return; int[] rows = gridSoundSamples.SelectedRows.Cast<DataGridViewRow>().Select(x => x.Tag).OfType<int>().Where(x => x >= 0 && x < groupItem.Group.Samples.Count).Distinct().OrderBy(x => x).ToArray(); if (rows.Length == 0) return;
        long previousSize = 0; soundRedo.Clear(); foreach (int row in rows) { SndSample s = groupItem.Group.Samples[row]; previousSize += s.EffectiveData.Length; soundUndo.Push(CaptureSoundUndo(groupItem.Group.Index, s)); s.ReplacementData = new byte[16]; s.ReplacementData[1] = 1; s.LoopFlag = 0; }
        btnSoundSave.Enabled = true; RefreshSoundSampleGrid(rows[0]); lblSoundStatus.Text = $"{rows.Length} faixa(s) zerada(s): {FormatBytes(previousSize)} → {FormatBytes(rows.Length * 16L)}. Índices preservados; use SALVAR SND.";
    }
    private void soundFavoriteMenu_Click(object? sender, EventArgs e) => ToggleSelectedSoundFavorite();
    private void ToggleSelectedSoundFavorite()
    {
        if (currentSnd == null || cmbSoundGroups.SelectedItem is not SoundGroupItem group || gridSoundSamples.CurrentRow?.Tag is not int sampleIndex) return; SoundTrackMetadata metadata = GetSoundMetadata(group.Group.Index, sampleIndex); metadata.Favorite = !metadata.Favorite; SaveSoundMetadata(); RefreshSoundSampleGrid(sampleIndex); lblSoundStatus.Text = metadata.Favorite ? "Faixa adicionada aos favoritos." : "Faixa removida dos favoritos.";
    }
    private void soundNoteMenu_Click(object? sender, EventArgs e)
    {
        if (currentSnd == null || cmbSoundGroups.SelectedItem is not SoundGroupItem group || gridSoundSamples.CurrentRow?.Tag is not int sampleIndex) return; SoundTrackMetadata metadata = GetSoundMetadata(group.Group.Index, sampleIndex);
        string? note = ShowSoundNoteDialog(metadata.Note, sampleIndex + 1); if (note == null) return; metadata.Note = note.Trim(); SaveSoundMetadata(); RefreshSoundSampleGrid(sampleIndex); lblSoundStatus.Text = metadata.Note.Length == 0 ? "Anotação removida." : "Anotação salva.";
    }
    private SoundTrackMetadata GetSoundMetadata(int groupIndex, int sampleIndex)
    {
        string key = GetSoundMetadataKey(groupIndex, sampleIndex); if (!soundMetadata.TryGetValue(key, out SoundTrackMetadata? value)) soundMetadata[key] = value = new SoundTrackMetadata(); return value;
    }
    private string GetSoundMetadataKey(int groupIndex, int sampleIndex)
    {
        string source = currentSnd?.SourcePath ?? "unknown"; string identity = source;
        if (!string.IsNullOrWhiteSpace(project.RootPath)) { string relative = Path.GetRelativePath(project.RootPath, source); if (relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)) identity = relative; }
        return $"{identity.Replace('\\', '/')}|g{groupIndex + 1:00}|s{sampleIndex + 1:000}";
    }
    private string GetSoundMetadataPath()
    {
        if (!string.IsNullOrWhiteSpace(project.RootPath)) return Path.Combine(project.RootPath, ".workspace", "sounds", "track-metadata.json");
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RE4_PS2_MOD_WORKSPACE", "sound-track-metadata.json");
    }
    private void LoadSoundMetadata()
    {
        try { string path = GetSoundMetadataPath(); soundMetadata = File.Exists(path) ? JsonSerializer.Deserialize<Dictionary<string, SoundTrackMetadata>>(File.ReadAllText(path)) ?? new(StringComparer.OrdinalIgnoreCase) : new(StringComparer.OrdinalIgnoreCase); soundMetadata = new Dictionary<string, SoundTrackMetadata>(soundMetadata, StringComparer.OrdinalIgnoreCase); }
        catch { soundMetadata = new(StringComparer.OrdinalIgnoreCase); }
    }
    private void SaveSoundMetadata()
    {
        string? temp = null;
        try { string path = GetSoundMetadataPath(); Directory.CreateDirectory(Path.GetDirectoryName(path)!); temp = path + ".tmp"; File.WriteAllText(temp, JsonSerializer.Serialize(soundMetadata, new JsonSerializerOptions { WriteIndented = true })); File.Move(temp, path, true); }
        catch (Exception ex) { MessageBox.Show(this, "Não foi possível persistir os favoritos e anotações.\n\n" + ex.Message, "Metadados de sons", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally { if (temp != null && File.Exists(temp)) try { File.Delete(temp); } catch { } }
    }
    private string? ShowSoundNoteDialog(string current, int trackNumber)
    {
        using var dialog = new AppForm { Text = $"Anotação da faixa #{trackNumber:000}", StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(500, 250), MinimumSize = new Size(420, 220), BackColor = Bg, ForeColor = TextPrimary, Font = new Font("Segoe UI", 9F), ShowInTaskbar = false };
        var label = new Label { Text = "ANOTAÇÃO", Left = 18, Top = 16, Width = 150, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI Semibold", 8F) }; dialog.Controls.Add(label);
        var editor = new TextBox { Text = current, Left = 18, Top = 42, Width = 464, Height = 142, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, BackColor = Surface2, ForeColor = TextPrimary, BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right }; dialog.Controls.Add(editor);
        var cancel = new Button { Text = "CANCELAR", DialogResult = DialogResult.Cancel, Left = 274, Top = 198, Width = 100, Height = 34, BackColor = Surface2, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Bottom | AnchorStyles.Right }; dialog.Controls.Add(cancel);
        var save = new Button { Text = "SALVAR", DialogResult = DialogResult.OK, Left = 382, Top = 198, Width = 100, Height = 34, BackColor = Accent, ForeColor = TextPrimary, FlatStyle = FlatStyle.Flat, Anchor = AnchorStyles.Bottom | AnchorStyles.Right }; save.FlatAppearance.BorderSize = 0; dialog.Controls.Add(save); dialog.AcceptButton = save; dialog.CancelButton = cancel; dialog.Shown += (_, _) => { editor.Focus(); editor.SelectionStart = editor.TextLength; };
        return dialog.ShowLocalizedDialog(this) == DialogResult.OK ? editor.Text : null;
    }
    private SoundUndoEntry CaptureSoundUndo(SndSample sample)
    {
        return CaptureSoundUndo(cmbSoundGroups.SelectedIndex, sample);
    }
    private static SoundUndoEntry CaptureSoundUndo(int group, SndSample sample) => new(group, sample.Index, sample.ReplacementData == null ? null : (byte[])sample.ReplacementData.Clone(), sample.SampleRate, sample.LoopFlag);
    private bool UndoSoundEdit()
    {
        if (currentSnd == null || soundUndo.Count == 0) return false; StopSoundPreview(); SoundUndoEntry undo = soundUndo.Pop();
        SoundUndoEntry? current = ApplySoundHistoryEntry(undo); if (current == null) return false; soundRedo.Push(current);
        lblSoundStatus.Text = soundUndo.Count == 0 ? "Alteração desfeita. O banco voltou ao estado carregado. Ctrl+Y refaz." : $"Alteração desfeita. {soundUndo.Count} ação(ões) ainda podem ser desfeitas; Ctrl+Y refaz."; return true;
    }
    private bool RedoSoundEdit()
    {
        if (currentSnd == null || soundRedo.Count == 0) return false; StopSoundPreview(); SoundUndoEntry redo = soundRedo.Pop();
        SoundUndoEntry? current = ApplySoundHistoryEntry(redo); if (current == null) return false; soundUndo.Push(current);
        lblSoundStatus.Text = soundRedo.Count == 0 ? "Alteração refeita." : $"Alteração refeita. {soundRedo.Count} ação(ões) ainda podem ser refeitas."; return true;
    }
    private SoundUndoEntry? ApplySoundHistoryEntry(SoundUndoEntry entry)
    {
        if (currentSnd == null || entry.GroupIndex < 0 || entry.GroupIndex >= currentSnd.Groups.Count || entry.SampleIndex < 0 || entry.SampleIndex >= currentSnd.Groups[entry.GroupIndex].Samples.Count) return null;
        SndSample sample = currentSnd.Groups[entry.GroupIndex].Samples[entry.SampleIndex]; var inverse = new SoundUndoEntry(entry.GroupIndex, entry.SampleIndex, sample.ReplacementData == null ? null : (byte[])sample.ReplacementData.Clone(), sample.SampleRate, sample.LoopFlag);
        sample.ReplacementData = entry.ReplacementData == null ? null : (byte[])entry.ReplacementData.Clone(); sample.SampleRate = entry.SampleRate; sample.LoopFlag = entry.LoopFlag;
        cmbSoundGroups.SelectedIndex = entry.GroupIndex; RefreshSoundSampleGrid(entry.SampleIndex); btnSoundSave.Enabled = currentSnd.IsModified; return inverse;
    }
    private void btnSoundRestore_Click(object? sender, EventArgs e)
    {
        if (currentSnd == null) return; string path = currentSnd.SourcePath; string backup = path + ".bak"; bool hasBackup = File.Exists(backup);
        string message = hasBackup ? $"Restaurar todo o arquivo usando {Path.GetFileName(backup)}? Todas as edições atuais do SND serão descartadas." : "Ainda não existe um backup .bak. Recarregar o arquivo salvo em disco e descartar todas as alterações em memória?";
        if (MessageBox.Show(this, message, "Restaurar SND", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { StopSoundPreview(); if (hasBackup) File.Copy(backup, path, true); LoadSnd(path); lblSoundStatus.Text = hasBackup ? "Arquivo SND restaurado integralmente a partir do backup original." : "Alterações em memória descartadas; o SND foi recarregado do disco."; ExtractLog($"Sons: {Path.GetFileName(path)} restaurado{(hasBackup ? " do backup original" : " do disco")}."); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Restaurar SND", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void btnSoundSave_Click(object? sender, EventArgs e)
    {
        if (currentSnd == null || !currentSnd.IsModified) return;
        try
        {
            StopSoundPreview(); string backup = currentSnd.SourcePath + ".bak"; if (!File.Exists(backup)) File.Copy(currentSnd.SourcePath, backup); string path = currentSnd.SourcePath; SndCodec.Save(currentSnd, path); LoadSnd(path); lblSoundStatus.Text = $"SND salvo e validado. Backup: {Path.GetFileName(backup)}"; ExtractLog($"Sons: {Path.GetFileName(path)} salvo e validado; backup preservado.");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Salvar SND", MessageBoxButtons.OK, MessageBoxIcon.Error); lblSoundStatus.Text = "Falha ao salvar; o arquivo original foi preservado."; }
    }
    private static string FormatDuration(TimeSpan value) => value.TotalMinutes >= 1 ? $"{(int)value.TotalMinutes}:{value.Seconds:00}.{value.Milliseconds / 100}" : $"{value.TotalSeconds:0.0} s";
    private string GetBatchSoundName(int groupIndex, int sampleIndex, string extension) => $"{Path.GetFileNameWithoutExtension(currentSnd!.SourcePath)}_g{groupIndex + 1:00}_{sampleIndex + 1:000}{extension}";
    private sealed record SoundFileItem(string Path, string Label) { public override string ToString() => Label; }
    private sealed record SoundGroupItem(SndGroup Group) { public override string ToString() => $"Grupo {Group.Index + 1}  •  {Group.Samples.Count} faixas"; }
    private sealed record SoundUndoEntry(int GroupIndex, int SampleIndex, byte[]? ReplacementData, uint SampleRate, uint LoopFlag);
    private sealed class SoundTrackMetadata { public SoundTrackMetadata() { } public bool Favorite { get; set; } public string Note { get; set; } = ""; }
}

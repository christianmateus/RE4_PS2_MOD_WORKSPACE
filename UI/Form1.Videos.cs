using System.Diagnostics;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private SfdInfo? currentVideo;
    private string? currentVideoPath;
    private bool refreshingVideoFiles;
    private string? videoPreviewRoot;
    private readonly System.Windows.Forms.Timer videoPreviewTimer = new() { Interval = 100 };
    private bool updatingVideoPosition;
    private Dictionary<string, string> videoNotes = new(StringComparer.OrdinalIgnoreCase);

    private async void btnNavVideos_Click(object? sender, EventArgs e)
    {
        SaveVisualCameraIfLeaving(); ShowPage(pnlVideos, btnNavVideos, "Vídeos"); RememberMainPage("Videos"); await RefreshVideoFilesAsync();
    }

    private async void btnVideoRefresh_Click(object? sender, EventArgs e) => await RefreshVideoFilesAsync();

    private async Task RefreshVideoFilesAsync(string? selectPath = null)
    {
        if (refreshingVideoFiles) return;
        refreshingVideoFiles = true; btnVideoRefresh.Enabled = false;
        string? previous = selectPath ?? currentVideoPath;
        try
        {
            var items = new List<VideoFileItem>();
            if (!string.IsNullOrWhiteSpace(project.RootPath) && Directory.Exists(project.RootPath))
            {
                foreach (string path in await Task.Run(() => Directory.EnumerateFiles(project.RootPath, "*.sfd", SearchOption.AllDirectories)
                    .Where(x => !x.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray()))
                    items.Add(new VideoFileItem(Path.GetRelativePath(project.RootPath, path), path, Path.GetFileName(path), null));
            }

            string? iso = Clean(txtIsoPath.Text);
            if (!string.IsNullOrWhiteSpace(iso) && File.Exists(iso))
            {
                try
                {
                    var virtualItems = await Task.Run(() =>
                    {
                        IsoFileEntry? afsFile = AfsService.FindAfsFiles(iso).FirstOrDefault(x => x.Name.Equals("BIO4MOV.AFS", StringComparison.OrdinalIgnoreCase));
                        if (afsFile == null) return Array.Empty<VideoFileItem>();
                        AfsImage image = AfsService.OpenAfsFromIso(iso, afsFile);
                        HashSet<string> localNames = items.Select(x => x.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        return AfsService.GetUniqueValidEntries(image)
                            .Where(x => x.FileName.EndsWith(".SFD", StringComparison.OrdinalIgnoreCase) && !localNames.Contains(x.FileName))
                            .OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase)
                            .Select(x => new VideoFileItem($"{x.FileName}  •  BIO4MOV.AFS", null, x.FileName, afsFile.FullPath)).ToArray();
                    });
                    items.AddRange(virtualItems);
                }
                catch (Exception ex) { ExtractLog("Vídeos: não foi possível indexar BIO4MOV.AFS: " + ex.Message); }
            }

            refreshingVideoFiles = true; cmbVideoFiles.BeginUpdate(); cmbVideoFiles.Items.Clear(); cmbVideoFiles.Items.AddRange(items.Cast<object>().ToArray()); cmbVideoFiles.EndUpdate();
            int selected = previous == null ? -1 : items.FindIndex(x => x.Path != null && x.Path.Equals(previous, StringComparison.OrdinalIgnoreCase));
            cmbVideoFiles.SelectedIndex = selected >= 0 ? selected : (items.Count > 0 ? 0 : -1);
            lblVideoStatus.Text = items.Count == 0 ? "Nenhum SFD encontrado. Abra um arquivo ou configure uma ISO com BIO4MOV.AFS." : $"{items.Count} cutscene(s) encontrada(s).";
        }
        finally
        {
            refreshingVideoFiles = false; btnVideoRefresh.Enabled = true;
            if (cmbVideoFiles.SelectedItem is VideoFileItem item) await LoadVideoItemAsync(item);
        }
    }

    private async void cmbVideoFiles_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!refreshingVideoFiles && cmbVideoFiles.SelectedItem is VideoFileItem item) await LoadVideoItemAsync(item);
    }

    private async Task LoadVideoItemAsync(VideoFileItem item)
    {
        try
        {
            string path = item.Path ?? await ExtractVideoFromIsoAsync(item);
            await LoadVideoAsync(path);
            if (item.Path == null) await RefreshVideoFilesAsync(path);
        }
        catch (Exception ex) { ClearVideoSelection("Não foi possível abrir a cutscene."); MessageBox.Show(this, ex.Message, "Vídeos", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task<string> ExtractVideoFromIsoAsync(VideoFileItem item)
    {
        if (string.IsNullOrWhiteSpace(project.RootPath)) throw new InvalidOperationException("Abra um workspace antes de extrair vídeos da ISO.");
        string? iso = Clean(txtIsoPath.Text); if (string.IsNullOrWhiteSpace(iso) || !File.Exists(iso)) throw new FileNotFoundException("A ISO base não foi encontrada.");
        string destination = Path.Combine(project.RootPath, "Extracted", "_AFS", "BIO4MOV", item.FileName);
        lblVideoStatus.Text = $"Extraindo {item.FileName} do BIO4MOV.AFS...";
        await Task.Run(() =>
        {
            IsoFileEntry afsFile = AfsService.FindAfsFiles(iso).FirstOrDefault(x => x.FullPath.Equals(item.AfsPath, StringComparison.OrdinalIgnoreCase) || x.Name.Equals("BIO4MOV.AFS", StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException("BIO4MOV.AFS não encontrado na ISO.");
            AfsImage image = AfsService.OpenAfsFromIso(iso, afsFile);
            AfsEntry entry = AfsService.FindFirstValidEntryByName(image, item.FileName) ?? throw new FileNotFoundException($"{item.FileName} não encontrado no BIO4MOV.AFS.");
            AfsService.ExtractEntry(image, entry, destination);
        });
        ExtractLog($"Vídeos: {item.FileName} extraído do BIO4MOV.AFS."); return destination;
    }

    private async Task LoadVideoAsync(string path)
    {
        CleanupVideoPreview(); lblVideoStatus.Text = "Analisando contêiner Sofdec...";
        SfdInfo info = await Task.Run(() => SfdService.Read(path)); currentVideo = info; currentVideoPath = Path.GetFullPath(path);
        LoadVideoNotes();
        txtVideoNotes.Text = videoNotes.GetValueOrDefault(GetCurrentVideoNoteKey(), string.Empty);
        lblVideoTitle.Text = $"{Path.GetFileName(path)}\n{FormatBytes(info.FileSize)}";
        lblVideoResolution.Text = $"{info.Video.Width} × {info.Video.Height}  •  {info.Video.FramesPerSecond:0.###} fps";
        lblVideoDuration.Text = FormatDuration(info.Audio.Duration);
        lblVideoCodec.Text = $"MPEG-1  •  {info.Video.BitRate / 1_000_000d:0.00} Mbps";
        lblVideoAudio.Text = $"CRI ADX  •  {info.Audio.Channels} canais  •  {info.Audio.SampleRate / 1000d:0.#} kHz";
        lblVideoPreview.Text = $"▶\n\n{info.StreamName}\n{info.Video.Width} × {info.Video.Height}";
        lblVideoPackets.Text = $"{info.SectorCount:N0} setores  •  {info.VideoPacketCount:N0} pacotes de vídeo  •  {info.AudioPacketCount:N0} pacotes de áudio";
        lblVideoStatus.Text = "SFD válido e pronto para reprodução, exportação ou substituição.";
        SetVideoActionsEnabled(true); ExtractLog($"Vídeos: {Path.GetFileName(path)} | {info.Video.Width}x{info.Video.Height} @ {info.Video.FramesPerSecond:0.###} fps | ADX {info.Audio.SampleRate:N0} Hz | {info.Audio.Duration:mm\\:ss\\.fff}.");
        try { await EnsureVideoPreviewPreparedAsync(); videoPlayerControl.ShowFirstFrame(); lblVideoStatus.Text = "SFD válido e pronto para reprodução, exportação ou substituição."; }
        catch (Exception ex) { ExtractLog("Vídeos: não foi possível gerar a thumbnail: " + ex.Message); lblVideoStatus.Text = "SFD válido. A prévia do primeiro quadro não pôde ser gerada pelo Windows."; }
    }

    private void ClearVideoSelection(string status)
    {
        CleanupVideoPreview(); currentVideo = null; currentVideoPath = null; lblVideoResolution.Text = lblVideoDuration.Text = lblVideoCodec.Text = lblVideoAudio.Text = "—"; lblVideoTitle.Text = "Nenhum vídeo carregado"; lblVideoPreview.Text = "▶\n\nSelecione um SFD"; lblVideoPackets.Text = "Contêiner ainda não analisado"; lblVideoStatus.Text = status; txtVideoNotes.Text = ""; SetVideoActionsEnabled(false);
    }

    private void SetVideoActionsEnabled(bool enabled)
    {
        foreach (Button button in new[] { btnVideoRestore, btnVideoPlay, btnVideoPause, btnVideoStop, btnVideoExportSfd, btnVideoExportStreams, btnVideoExportWav, btnVideoExportMpegWav, btnVideoReplace, btnVideoBuildReplace, btnVideoOpenFolder }) button.Enabled = enabled;
        txtVideoNotes.Enabled = btnVideoSaveNotes.Enabled = enabled;
    }

    private string GetVideoNotesPath() => !string.IsNullOrWhiteSpace(project.RootPath)
        ? Path.Combine(project.RootPath, ".workspace", "videos", "video-notes.json")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RE4_PS2_MOD_WORKSPACE", "video-notes.json");

    private string GetCurrentVideoNoteKey()
    {
        string source = currentVideoPath ?? "unknown";
        if (!string.IsNullOrWhiteSpace(project.RootPath))
        {
            string relative = Path.GetRelativePath(project.RootPath, source);
            if (relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)) source = relative;
        }
        return source.Replace('\\', '/');
    }

    private void LoadVideoNotes()
    {
        try
        {
            string path = GetVideoNotesPath();
            var loaded = File.Exists(path) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) : null;
            videoNotes = loaded == null ? new(StringComparer.OrdinalIgnoreCase) : new(loaded, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            videoNotes = new(StringComparer.OrdinalIgnoreCase);
            ExtractLog("Vídeos: não foi possível carregar as anotações: " + ex.Message);
        }
    }

    private void btnVideoSaveNotes_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null) return;
        string key = GetCurrentVideoNoteKey(), note = txtVideoNotes.Text.Trim();
        if (note.Length == 0) videoNotes.Remove(key); else videoNotes[key] = note;
        string path = GetVideoNotesPath(), temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(videoNotes, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
            lblVideoStatus.Text = note.Length == 0 ? "Anotação removida." : "Anotação salva para esta cutscene.";
        }
        catch (Exception ex) { MessageBox.Show(this, "Não foi possível salvar a anotação.\n\n" + ex.Message, "Anotações de vídeo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally { if (File.Exists(temporary)) try { File.Delete(temporary); } catch { } }
    }

    private void txtVideoNotes_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.Control || e.KeyCode != Keys.Enter) return;
        e.SuppressKeyPress = true; e.Handled = true; btnVideoSaveNotes_Click(sender, EventArgs.Empty);
    }

    private async void btnVideoBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "CRI Sofdec (*.sfd)|*.sfd|Todos os arquivos (*.*)|*.*", Title = "Abrir cutscene SFD" };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try { await LoadVideoAsync(dialog.FileName); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Abrir SFD", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void btnVideoPlay_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null) return;
        try
        {
            await EnsureVideoPreviewPreparedAsync(); videoPlayerControl.Play(); videoPreviewTimer.Tick -= VideoPreviewTimer_Tick; videoPreviewTimer.Tick += VideoPreviewTimer_Tick; videoPreviewTimer.Start(); btnVideoPlay.Text = "PLAYING"; lblVideoStatus.Text = "Reproduzindo vídeo MPEG-1 e áudio CRI ADX sincronizados.";
        }
        catch (Exception ex) { MessageBox.Show(this, "Não foi possível iniciar a reprodução interna.\n\n" + ex.Message, "Reproduzir vídeo", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task EnsureVideoPreviewPreparedAsync()
    {
        if (currentVideoPath == null || currentVideo == null) return;
        PrepareVideoPreviewRoot(); string stem = Path.GetFileNameWithoutExtension(currentVideoPath), video = Path.Combine(videoPreviewRoot!, stem + ".m1v"), adx = Path.Combine(videoPreviewRoot!, stem + ".sfa"), wave = Path.Combine(videoPreviewRoot!, stem + ".wav");
        if (videoPlayerControl.IsLoaded && string.Equals(videoPlayerControl.SourceVideoPath, video, StringComparison.OrdinalIgnoreCase)) return;
        lblVideoStatus.Text = "Preparando reprodução interna e decodificando o áudio ADX...";
        await Task.Run(() => { SfdService.Demux(currentVideoPath, video, adx); SfdService.ExportAudioWave(currentVideoPath, wave); });
        videoPlayerControl.LoadMedia(video, wave); videoPlayerControl.Volume = trkVideoVolume.Value / 100d; videoPlayerControl.Visible = true; videoPlayerControl.BringToFront(); lblVideoPreview.Visible = false; trkVideoPosition.Enabled = true;
    }

    private void btnVideoPause_Click(object? sender, EventArgs e) { videoPlayerControl.Pause(); videoPreviewTimer.Stop(); btnVideoPlay.Text = "PLAY"; lblVideoStatus.Text = "Reprodução pausada."; }

    private void VideoPreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (currentVideo == null || currentVideo.Audio.Duration.TotalMilliseconds <= 0) return; double value = videoPlayerControl.Position.TotalMilliseconds / currentVideo.Audio.Duration.TotalMilliseconds; updatingVideoPosition = true; try { trkVideoPosition.Value = Math.Clamp((int)(value * 1000), 0, 1000); } finally { updatingVideoPosition = false; } if (value >= 0.999) StopVideoPreview();
    }

    private void trkVideoPosition_Scroll(object? sender, EventArgs e) { if (updatingVideoPosition || currentVideo == null) return; videoPlayerControl.Seek(TimeSpan.FromMilliseconds(currentVideo.Audio.Duration.TotalMilliseconds * trkVideoPosition.Value / 1000d)); }
    private void trkVideoVolume_Scroll(object? sender, EventArgs e) => videoPlayerControl.Volume = trkVideoVolume.Value / 100d;
    private void videoPlayerControl_PlaybackFailed(object? sender, string message) { videoPreviewTimer.Stop(); btnVideoPlay.Text = "PLAY"; lblVideoStatus.Text = "O decoder de vídeo do Windows recusou este MPEG-1."; MessageBox.Show(this, message, "Reprodução MPEG-1", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

    private void btnVideoStop_Click(object? sender, EventArgs e) => StopVideoPreview();
    private void StopVideoPreview()
    {
        videoPreviewTimer.Stop(); videoPlayerControl?.StopPlayback(); if (btnVideoPlay != null) btnVideoPlay.Text = "PLAY"; if (trkVideoPosition != null) trkVideoPosition.Value = 0;
    }

    private void PrepareVideoPreviewRoot()
    {
        if (videoPreviewRoot != null && Directory.Exists(videoPreviewRoot)) return; videoPreviewRoot = Path.Combine(Path.GetTempPath(), "re4ps2-sfd-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(videoPreviewRoot);
    }

    private void CleanupVideoPreview()
    {
        StopVideoPreview(); videoPlayerControl?.UnloadMedia(); if (videoPlayerControl != null) videoPlayerControl.Visible = false; if (lblVideoPreview != null) lblVideoPreview.Visible = true; if (trkVideoPosition != null) trkVideoPosition.Enabled = false; if (videoPreviewRoot == null) return; try { if (Directory.Exists(videoPreviewRoot)) Directory.Delete(videoPreviewRoot, true); } catch { } videoPreviewRoot = null;
    }

    private void btnVideoExportSfd_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null) return; using var dialog = new SaveFileDialog { Filter = "CRI Sofdec (*.sfd)|*.sfd", FileName = Path.GetFileName(currentVideoPath) }; if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return; File.Copy(currentVideoPath, dialog.FileName, true); lblVideoStatus.Text = "Contêiner SFD exportado.";
    }

    private async void btnVideoExportStreams_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null) return; using var dialog = new FolderBrowserDialog { Description = "Escolha a pasta para exportar vídeo MPEG-1 e áudio ADX." }; if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try { string stem = Path.GetFileNameWithoutExtension(currentVideoPath); await Task.Run(() => SfdService.Demux(currentVideoPath, Path.Combine(dialog.SelectedPath, stem + ".m1v"), Path.Combine(dialog.SelectedPath, stem + ".sfa"))); lblVideoStatus.Text = "Streams MPEG-1 e CRI ADX exportados."; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar streams", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void btnVideoExportWav_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null) return; using var dialog = new SaveFileDialog { Filter = "Wave PCM (*.wav)|*.wav", FileName = Path.GetFileNameWithoutExtension(currentVideoPath) + ".wav" }; if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try { await Task.Run(() => SfdService.ExportAudioWave(currentVideoPath, dialog.FileName)); lblVideoStatus.Text = "Áudio ADX decodificado e exportado como WAV estéreo."; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar WAV", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void btnVideoExportMpegWav_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null) return;
        using var dialog = new FolderBrowserDialog { Description = "Escolha a pasta para exportar o MPEG-1 e o WAV PCM." };
        if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            string stem = Path.GetFileNameWithoutExtension(currentVideoPath), mpeg = Path.Combine(dialog.SelectedPath, stem + ".m1v"), wave = Path.Combine(dialog.SelectedPath, stem + ".wav"), adx = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".sfa");
            try { await Task.Run(() => { SfdService.Demux(currentVideoPath, mpeg, adx); SfdService.ExportAudioWave(currentVideoPath, wave); }); }
            finally { try { if (File.Exists(adx)) File.Delete(adx); } catch { } }
            lblVideoStatus.Text = "Vídeo MPEG-1 e áudio WAV exportados juntos.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Exportar MPEG-1 + WAV", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void btnVideoOpenFolder_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null) return; Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{currentVideoPath}\"") { UseShellExecute = true });
    }

    private async void btnVideoReplace_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null || currentVideo == null) return; using var dialog = new OpenFileDialog { Filter = "CRI Sofdec (*.sfd)|*.sfd", Title = "Escolha a nova cutscene SFD" }; if (dialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        try
        {
            SfdInfo replacement = await Task.Run(() => SfdService.Read(dialog.FileName));
            bool compatible = replacement.Video.Width == currentVideo.Video.Width && replacement.Video.Height == currentVideo.Video.Height && Math.Abs(replacement.Video.FramesPerSecond - currentVideo.Video.FramesPerSecond) < 0.01 && replacement.Audio.SampleRate == currentVideo.Audio.SampleRate && replacement.Audio.Channels == currentVideo.Audio.Channels;
            string warning = compatible ? $"Substituir {Path.GetFileName(currentVideoPath)} pela cutscene selecionada?\n\nO original será preservado em .bak e a mudança ficará pendente para o próximo Build." : "A nova cutscene usa parâmetros diferentes da original. Isso pode não ser aceito pelo jogo.\n\nContinuar mesmo assim?";
            if (MessageBox.Show(this, warning, "Substituir cutscene", MessageBoxButtons.YesNo, compatible ? MessageBoxIcon.Question : MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            StopVideoPreview(); string backup = currentVideoPath + ".bak"; if (!File.Exists(backup)) File.Copy(currentVideoPath, backup); File.Copy(dialog.FileName, currentVideoPath, true); await LoadVideoAsync(currentVideoPath); lblVideoStatus.Text = $"Cutscene substituída e validada. Backup preservado em {Path.GetFileName(backup)}."; ExtractLog($"Vídeos: {Path.GetFileName(currentVideoPath)} substituído; reinjeção pendente no BIO4MOV.AFS."); if (pnlBuild?.Visible == true) await RefreshTrackedDatsAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Substituir cutscene", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async void btnVideoBuildReplace_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null || currentVideo == null) return;
        using var videoDialog = new OpenFileDialog { Filter = "Stream MPEG-1 elementar (*.m1v)|*.m1v|Todos os arquivos (*.*)|*.*", Title = "Escolha o vídeo MPEG-1 elementar" };
        if (videoDialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        using var waveDialog = new OpenFileDialog { Filter = "Wave PCM (*.wav)|*.wav", Title = "Escolha o áudio WAV" };
        if (waveDialog.ShowLocalizedDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show(this, $"Construir um novo {Path.GetFileName(currentVideoPath)} usando:\n\nVídeo: {Path.GetFileName(videoDialog.FileName)}\nÁudio: {Path.GetFileName(waveDialog.FileName)}\n\nO original será preservado em .bak.", "Construir SFD e substituir", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        string target = currentVideoPath, temporary = target + ".building";
        try
        {
            StopVideoPreview(); lblVideoStatus.Text = "Codificando WAV para ADX e construindo o contêiner SFD..."; btnVideoBuildReplace.Enabled = false;
            SfdBuildResult result = await Task.Run(() => SfdService.BuildFromMpegAndWave(target, videoDialog.FileName, waveDialog.FileName, temporary));
            double durationDifference = Math.Abs((result.VideoDuration - result.AudioDuration).TotalSeconds);
            if (durationDifference > 0.25 && MessageBox.Show(this, $"O vídeo e o áudio diferem em {durationDifference:0.00} segundos. Isso pode causar perda de sincronização.\n\nSubstituir mesmo assim?", "Duração diferente", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            string backup = target + ".bak"; if (!File.Exists(backup)) File.Copy(target, backup); File.Move(temporary, target, true);
            await LoadVideoAsync(target); lblVideoStatus.Text = "Novo SFD construído, validado e aplicado. Alteração pronta para o próximo Build.";
            ExtractLog($"Vídeos: {Path.GetFileName(target)} reconstruído de MPEG-1 + WAV; original preservado em {Path.GetFileName(backup)}.");
            if (pnlBuild?.Visible == true) await RefreshTrackedDatsAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Construir SFD", MessageBoxButtons.OK, MessageBoxIcon.Error); lblVideoStatus.Text = "Não foi possível construir o SFD."; }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } if (currentVideoPath != null) btnVideoBuildReplace.Enabled = true; }
    }

    private async void btnVideoRestore_Click(object? sender, EventArgs e)
    {
        if (currentVideoPath == null) return; if (MessageBox.Show(this, $"Restaurar {Path.GetFileName(currentVideoPath)} a partir da ISO original?\n\nA versão modificada será descartada.", "Restaurar cutscene", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        try
        {
            StopVideoPreview(); string target = currentVideoPath, fileName = Path.GetFileName(target); string? iso = Clean(txtIsoPath.Text);
            if (!string.IsNullOrWhiteSpace(iso) && File.Exists(iso))
            {
                await Task.Run(() => { IsoFileEntry afsFile = AfsService.FindAfsFiles(iso).FirstOrDefault(x => x.Name.Equals("BIO4MOV.AFS", StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException("BIO4MOV.AFS não encontrado na ISO original."); AfsImage image = AfsService.OpenAfsFromIso(iso, afsFile); AfsEntry entry = AfsService.FindFirstValidEntryByName(image, fileName) ?? throw new FileNotFoundException($"{fileName} não encontrado no BIO4MOV.AFS original."); AfsService.ExtractEntry(image, entry, target); });
            }
            else if (File.Exists(target + ".bak")) File.Copy(target + ".bak", target, true);
            else throw new FileNotFoundException("A ISO original não está disponível e não existe backup .bak para esta cutscene.");
            await LoadVideoAsync(target); lblVideoStatus.Text = "Cutscene restaurada a partir do BIO4MOV.AFS original."; ExtractLog($"Vídeos: {fileName} restaurado da fonte original.");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Restaurar cutscene", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private sealed record VideoFileItem(string Label, string? Path, string FileName, string? AfsPath) { public override string ToString() => Label; }
}

using RE4_PS2_MOD_WORKSPACE.Core.Animation;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private FcvAnimation? currentFcv;
    private Ps2BinSkeleton? currentAnimationSkeleton;
    private System.Windows.Forms.Timer? animationPlaybackTimer;
    private int animationPlaybackFrame;

    private void btnNavAnimations_Click(object? sender, EventArgs e)
    {
        SaveVisualCameraIfLeaving(); ShowPage(pnlAnimations, btnNavAnimations, "Animações"); RememberMainPage("Animations"); RefreshAnimationFiles();
    }

    private void btnAnimationBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "Resident Evil 4 FCV (*.fcv)|*.fcv|Todos os arquivos (*.*)|*.*", Title = "Abrir animação FCV" };
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadFcv(dlg.FileName);
    }
    private void btnAnimationRefresh_Click(object? sender, EventArgs e) => RefreshAnimationFiles();
    private void cmbAnimationFiles_SelectedIndexChanged(object? sender, EventArgs e) { if (cmbAnimationFiles.SelectedItem is AnimationFileItem item) LoadFcv(item.Path); }
    private void gridAnimationTracks_SelectionChanged(object? sender, EventArgs e) => ShowSelectedFcvTrack();
    private void tabAnimationAxis_SelectedIndexChanged(object? sender, EventArgs e) => ShowSelectedFcvTrack();

    private void RefreshAnimationFiles()
    {
        string? selected = (cmbAnimationFiles.SelectedItem as AnimationFileItem)?.Path; cmbAnimationFiles.BeginUpdate(); cmbAnimationFiles.Items.Clear();
        if (!string.IsNullOrWhiteSpace(project.RootPath) && Directory.Exists(project.RootPath))
        {
            try { foreach (string path in Directory.EnumerateFiles(project.RootPath, "*.fcv", SearchOption.AllDirectories).OrderBy(Path.GetFileName)) cmbAnimationFiles.Items.Add(new AnimationFileItem(path, Path.GetRelativePath(project.RootPath, path))); } catch { }
        }
        cmbAnimationFiles.EndUpdate();
        if (cmbAnimationFiles.Items.Count > 0)
        {
            int index = 0; if (selected != null) for (int i = 0; i < cmbAnimationFiles.Items.Count; i++) if ((cmbAnimationFiles.Items[i] as AnimationFileItem)?.Path == selected) { index = i; break; }
            cmbAnimationFiles.SelectedIndex = index; lblAnimationStatus.Text = $"{cmbAnimationFiles.Items.Count} FCV(s) encontrado(s) no workspace.";
        }
        else lblAnimationStatus.Text = "Nenhum FCV encontrado no workspace. Use ABRIR FCV para selecionar um arquivo.";
    }

    private void LoadFcv(string path)
    {
        try
        {
            StopAnimationPlayback(); currentFcv = FcvReader.Read(path); animationPlaybackFrame = 0;
            lblAnimationFile.Text = Path.GetFileName(path);
            lblAnimationSummary.Text = $"Frames: {currentFcv.FrameCount}    Tracks: {currentFcv.TrackCount}    Tamanho: {currentFcv.ActualFileSize:N0} bytes    Header size: 0x{currentFcv.DeclaredFileSize:X}";
            lblAnimationStatus.Text = currentFcv.DeclaredFileSize == currentFcv.ActualFileSize ? "FCV lido com sucesso. Tamanho do header confere com o arquivo." : $"FCV lido. Header declara {currentFcv.DeclaredFileSize:N0} bytes; arquivo possui {currentFcv.ActualFileSize:N0}.";
            gridAnimationTracks.Rows.Clear(); foreach (var t in currentFcv.Tracks) gridAnimationTracks.Rows.Add(t.Index, $"0x{t.NodeId:X2}", $"0x{t.Type:X2}", t.TypeName, $"0x{t.DataType:X2}", $"0x{t.Offset:X8}", t.PhysicalOrder, t.X.Keys.Count, t.Y.Keys.Count, t.Z.Keys.Count);
            if (gridAnimationTracks.Rows.Count > 0) { gridAnimationTracks.ClearSelection(); gridAnimationTracks.Rows[0].Selected = true; gridAnimationTracks.CurrentCell = gridAnimationTracks.Rows[0].Cells[0]; }
            trkAnimationFrame.Maximum = Math.Max(1, currentFcv.FrameCount - 1); trkAnimationFrame.Value = 0; UpdateAnimationFrameUi(); animationSkeletonViewport.SetAnimation(chkAnimationRestPose.Checked ? null : currentFcv); visualViewport?.SetEnemyAttachmentAnimation(chkAnimationRestPose.Checked ? null : currentFcv, animationPlaybackFrame);
            ShowSelectedFcvTrack(); ExtractLog($"FCV Inspector: {Path.GetFileName(path)} | {currentFcv.FrameCount} frames | {currentFcv.TrackCount} tracks.");
            TryAutoLoadAnimationBin(false);
        }
        catch (Exception ex)
        {
            currentFcv = null; gridAnimationTracks.Rows.Clear(); gridAnimationKeys.Rows.Clear(); lblAnimationStatus.Text = "Erro ao ler FCV: " + ex.Message;
            MessageBox.Show(this, ex.Message, "FCV Inspector", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowSelectedFcvTrack()
    {
        gridAnimationKeys.Rows.Clear(); if (currentFcv == null || gridAnimationTracks.CurrentRow == null) return;
        int index = Convert.ToInt32(gridAnimationTracks.CurrentRow.Cells[0].Value); if (index < 0 || index >= currentFcv.Tracks.Count) return;
        FcvTrack t = currentFcv.Tracks[index]; FcvAxis axis = tabAnimationAxis.SelectedIndex switch { 1 => t.Y, 2 => t.Z, _ => t.X };
        lblAnimationTrackDetail.Text = $"Track #{t.Index}  •  Node 0x{t.NodeId:X2}  •  {t.TypeName}  •  Data 0x{t.DataType:X2}  •  {axis.Keys.Count} key(s)";
        for (int i = 0; i < axis.Keys.Count; i++) { var k = axis.Keys[i]; gridAnimationKeys.Rows.Add(i, k.Frame, FormatFcvNumber(k.Value), FormatFcvNumber(k.TangentIn), FormatFcvNumber(k.TangentOut), FormatFcvNumber(k.Extra)); }
    }

    private void btnAnimationBinBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "Resident Evil 4 BIN (*.bin)|*.bin|Todos os arquivos (*.*)|*.*", Title = "Abrir BIN com skeleton" };
        if (currentFcv != null) dlg.InitialDirectory = Path.GetDirectoryName(currentFcv.FilePath);
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadAnimationSkeleton(dlg.FileName);
    }
    private void btnAnimationAutoBin_Click(object? sender, EventArgs e) => TryAutoLoadAnimationBin(true);
    private void btnAnimationFit_Click(object? sender, EventArgs e) { animationSkeletonViewport.Fit(); animationSkeletonViewport.Invalidate(); }

    private void TryAutoLoadAnimationBin(bool showMessage)
    {
        if (currentFcv == null) return; string? dir = Path.GetDirectoryName(currentFcv.FilePath); if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
        string stem = Path.GetFileNameWithoutExtension(currentFcv.FilePath); int underscore = stem.IndexOf('_'); string prefix = underscore > 0 ? stem[..underscore] : stem;
        string candidate = Path.Combine(dir, prefix + "_440.BIN");
        if (!File.Exists(candidate)) candidate = Directory.EnumerateFiles(dir, "*440*.bin", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? "";
        if (File.Exists(candidate)) LoadAnimationSkeleton(candidate); else if (showMessage) MessageBox.Show(this, "Não encontrei automaticamente o BIN 440 na mesma pasta do FCV. Use ABRIR BIN.", "Skeleton Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadAnimationSkeleton(string path)
    {
        try
        {
            currentAnimationSkeleton = Ps2BinSkeletonReader.Read(path); animationSkeletonViewport.SetSkeleton(currentAnimationSkeleton); animationSkeletonViewport.SetAnimation(chkAnimationRestPose.Checked ? null : currentFcv); animationSkeletonViewport.SetFrame(animationPlaybackFrame);
            int roots = currentAnimationSkeleton.Bones.Count(x => x.ParentIndex < 0); lblAnimationSkeleton.Text = $"BIN: {Path.GetFileName(path)}  •  {currentAnimationSkeleton.Bones.Count} bones  •  {roots} root(s)";
            cmbAnimationBones.BeginUpdate(); cmbAnimationBones.Items.Clear();
            foreach (var b in currentAnimationSkeleton.Bones) cmbAnimationBones.Items.Add(new AnimationBoneItem(b.Index, b.Id, b.ParentId));
            cmbAnimationBones.EndUpdate();
            int suspicious = FindMostSuspiciousAnimationBone(); if (cmbAnimationBones.Items.Count > 0) cmbAnimationBones.SelectedIndex = suspicious >= 0 ? suspicious : 0;
            ExtractLog($"FCV Skeleton Viewer: {Path.GetFileName(path)} | {currentAnimationSkeleton.Bones.Count} bones{(suspicious >= 0 ? $" | bone suspeito 0x{currentAnimationSkeleton.Bones[suspicious].Id:X2}" : "")}.");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Skeleton Viewer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void btnAnimationPlay_Click(object? sender, EventArgs e)
    {
        if (currentFcv == null || currentAnimationSkeleton == null) { MessageBox.Show(this, "Abra um FCV e carregue um BIN com skeleton primeiro.", "Skeleton Viewer", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        // Primeiro teste de playback: sempre inicia do frame atual e faz loop a ~30 FPS.
        // Se já chegou ao final, volta ao frame zero para o PLAY ser imediatamente perceptível.
        if (animationPlaybackFrame >= currentFcv.FrameCount - 1) { animationPlaybackFrame = 0; trkAnimationFrame.Value = 0; animationSkeletonViewport.SetFrame(0); }
        visualViewport?.SetEnemyAttachmentAnimation(chkAnimationRestPose.Checked ? null : currentFcv, animationPlaybackFrame); animationPlaybackTimer ??= CreateAnimationPlaybackTimer(); animationPlaybackTimer.Start(); btnAnimationPlay.Text = "PLAYING";
    }
    private System.Windows.Forms.Timer CreateAnimationPlaybackTimer()
    {
        var timer = new System.Windows.Forms.Timer { Interval = 33 }; timer.Tick += (_, _) => { if (currentFcv == null) return; animationPlaybackFrame++; if (animationPlaybackFrame >= currentFcv.FrameCount) animationPlaybackFrame = 0; trkAnimationFrame.Value = Math.Min(trkAnimationFrame.Maximum, animationPlaybackFrame); animationSkeletonViewport.SetFrame(animationPlaybackFrame); visualViewport?.SetEnemyAttachmentAnimation(chkAnimationRestPose.Checked ? null : currentFcv, animationPlaybackFrame); UpdateAnimationFrameUi(); UpdateAnimationBoneDebug(); }; return timer;
    }
    private void btnAnimationStop_Click(object? sender, EventArgs e) => StopAnimationPlayback();
    private void StopAnimationPlayback() { animationPlaybackTimer?.Stop(); if (btnAnimationPlay != null) btnAnimationPlay.Text = "PLAY"; }
    private void trkAnimationFrame_Scroll(object? sender, EventArgs e) { StopAnimationPlayback(); animationPlaybackFrame = trkAnimationFrame.Value; animationSkeletonViewport.SetFrame(animationPlaybackFrame); visualViewport?.SetEnemyAttachmentAnimation(chkAnimationRestPose.Checked ? null : currentFcv, animationPlaybackFrame); UpdateAnimationFrameUi(); UpdateAnimationBoneDebug(); }
    private void UpdateAnimationFrameUi() { lblAnimationFrame.Text = currentFcv == null ? "Frame 0 / —" : $"Frame {animationPlaybackFrame} / {Math.Max(0, currentFcv.FrameCount - 1)}"; }


    private int FindMostSuspiciousAnimationBone()
    {
        if (currentAnimationSkeleton == null) return -1; var pose = FcvSkeletonEvaluator.Evaluate(currentAnimationSkeleton, chkAnimationRestPose.Checked ? null : currentFcv, animationPlaybackFrame);
        int best = -1; float bestRatio = 4f;
        for (int i = 0; i < currentAnimationSkeleton.Bones.Count; i++)
        {
            var b = currentAnimationSkeleton.Bones[i]; if (b.ParentIndex < 0) continue; float rest = Math.Max(1f, b.LocalPosition.Length()); float now = (pose.WorldPositions[i] - pose.WorldPositions[b.ParentIndex]).Length(); float ratio = now / rest;
            if (now > rest + 500f && ratio > bestRatio) { bestRatio = ratio; best = i; }
        }
        return best;
    }

    private void chkAnimationBoneIds_CheckedChanged(object? sender, EventArgs e) { animationSkeletonViewport.ShowBoneIds = chkAnimationBoneIds.Checked; animationSkeletonViewport.Invalidate(); }
    private void chkAnimationRestPose_CheckedChanged(object? sender, EventArgs e)
    {
        StopAnimationPlayback(); animationSkeletonViewport.SetAnimation(chkAnimationRestPose.Checked ? null : currentFcv); animationSkeletonViewport.SetFrame(animationPlaybackFrame); visualViewport?.SetEnemyAttachmentAnimation(chkAnimationRestPose.Checked ? null : currentFcv, animationPlaybackFrame); UpdateAnimationBoneDebug();
    }
    private void cmbAnimationBones_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbAnimationBones.SelectedItem is AnimationBoneItem item) animationSkeletonViewport.SelectBone(item.Index);
    }
    private void animationSkeletonViewport_SelectedBoneChanged(object? sender, EventArgs e)
    {
        int index = animationSkeletonViewport.SelectedBoneIndex;
        if (index >= 0 && index < cmbAnimationBones.Items.Count && cmbAnimationBones.SelectedIndex != index) cmbAnimationBones.SelectedIndex = index;
        UpdateAnimationBoneDebug();
    }
    private void UpdateAnimationBoneDebug()
    {
        if (currentAnimationSkeleton == null || lblAnimationBoneDebug == null) return;
        int index = animationSkeletonViewport.SelectedBoneIndex; if (index < 0 || index >= currentAnimationSkeleton.Bones.Count) { lblAnimationBoneDebug.Text = "Clique em um joint ou selecione um bone acima."; return; }
        var b = currentAnimationSkeleton.Bones[index]; var pose = FcvSkeletonEvaluator.Evaluate(currentAnimationSkeleton, chkAnimationRestPose.Checked ? null : currentFcv, animationPlaybackFrame);
        var lp = pose.LocalPositions[index]; var wp = pose.WorldPositions[index]; float dist = 0f; string parent = "ROOT";
        if (b.ParentIndex >= 0) { parent = $"0x{currentAnimationSkeleton.Bones[b.ParentIndex].Id:X2}"; dist = VectorDistance(wp, pose.WorldPositions[b.ParentIndex]); }
        float restDist = b.ParentIndex >= 0 ? b.LocalPosition.Length() : 0f;
        bool suspicious = b.ParentIndex >= 0 && dist > Math.Max(restDist * 4f, restDist + 500f);
        var localEuler = QuaternionToEulerDegrees(pose.LocalRotations[index]);
        var worldEuler = QuaternionToEulerDegrees(pose.WorldRotations[index]);
        string tracks = "nenhum";
        if (currentFcv != null)
        {
            var related = currentFcv.Tracks.Where(t => t.NodeId == b.Id).ToArray();
            if (related.Length > 0)
            {
                tracks = string.Join(" | ", related.Select(t =>
                {
                    var raw = FcvSkeletonEvaluator.SampleTrackRaw(t, animationPlaybackFrame);
                    return $"{t.Type:X2}/{t.DataType:X2} raw({raw.X:0.###},{raw.Y:0.###},{raw.Z:0.###})";
                }));
            }
        }
        lblAnimationBoneDebug.Text =
            $"Bone 0x{b.Id:X2}  Parent {parent}  Seg {dist:0.##} / rest {restDist:0.##}{(suspicious ? "  ⚠ LONGO" : "")}\n" +
            $"Local Pos {lp.X:0.##}, {lp.Y:0.##}, {lp.Z:0.##}   World Pos {wp.X:0.##}, {wp.Y:0.##}, {wp.Z:0.##}\n" +
            $"Local Rot {localEuler.X:0.##}°, {localEuler.Y:0.##}°, {localEuler.Z:0.##}°   World Rot {worldEuler.X:0.##}°, {worldEuler.Y:0.##}°, {worldEuler.Z:0.##}°\n" +
            $"FCV @ frame {animationPlaybackFrame}: {tracks}";
    }

    private static System.Numerics.Vector3 QuaternionToEulerDegrees(System.Numerics.Quaternion q)
    {
        q = System.Numerics.Quaternion.Normalize(q);
        double sinrCosp = 2.0 * (q.W * q.X + q.Y * q.Z);
        double cosrCosp = 1.0 - 2.0 * (q.X * q.X + q.Y * q.Y);
        double x = Math.Atan2(sinrCosp, cosrCosp);
        double sinp = 2.0 * (q.W * q.Y - q.Z * q.X);
        double y = Math.Abs(sinp) >= 1.0 ? Math.CopySign(Math.PI / 2.0, sinp) : Math.Asin(sinp);
        double sinyCosp = 2.0 * (q.W * q.Z + q.X * q.Y);
        double cosyCosp = 1.0 - 2.0 * (q.Y * q.Y + q.Z * q.Z);
        double z = Math.Atan2(sinyCosp, cosyCosp);
        const double radToDeg = 180.0 / Math.PI;
        return new System.Numerics.Vector3((float)(x * radToDeg), (float)(y * radToDeg), (float)(z * radToDeg));
    }
    private static float VectorDistance(System.Numerics.Vector3 a, System.Numerics.Vector3 b) => (a - b).Length();

    private static string FormatFcvNumber(double value) => Math.Abs(value % 1) < 0.0000001 ? value.ToString("0") : value.ToString("0.######");
    private sealed record AnimationFileItem(string Path, string Label) { public override string ToString() => Label; }
    private sealed record AnimationBoneItem(int Index, byte Id, byte ParentId) { public override string ToString() => $"#{Index:00}  Bone 0x{Id:X2}  Parent {(ParentId == 0xFF ? "ROOT" : $"0x{ParentId:X2}")}"; }
}

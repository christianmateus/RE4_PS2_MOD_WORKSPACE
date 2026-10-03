using RE4_PS2_MOD_WORKSPACE.Core.Animation;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class CharacterCustomizerForm
{
    private readonly Panel animationTimeline = new() { Dock = DockStyle.Bottom, Height = 65, BackColor = Color.FromArgb(22, 25, 30), Padding = new Padding(8, 4, 8, 4) };
    private readonly CheckBox chkAnimationTimeline = new() { Text = "TIMELINE", AutoSize = true, Checked = true, ForeColor = Color.Gainsboro, Margin = new Padding(12, 5, 3, 0) };
    private readonly CheckBox chkCharacterLockRoot = new() { Text = "TRAVAR ROOT", Dock = DockStyle.Fill, Checked = true, ForeColor = Color.Gainsboro, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(6, 0, 0, 0) };
    private readonly CheckBox chkCharacterTPose = new() { Text = "T-POSE", Dock = DockStyle.Fill, ForeColor = Color.Gainsboro, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(4, 0, 0, 0) };
    private readonly ComboBox cmbCharacterAnimation = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro, FlatStyle = FlatStyle.Flat };
    private readonly TextBox txtCharacterAnimationName = new() { Dock = DockStyle.Fill, PlaceholderText = "Nome da animação", BackColor = Color.FromArgb(28, 31, 37), ForeColor = Color.Gainsboro };
    private readonly Button btnCharacterPlayPause = new();
    private readonly Button btnCharacterAnimationSave = new();
    private readonly TrackBar trkCharacterAnimation = new() { Dock = DockStyle.Fill, AutoSize = false, Minimum = 0, Maximum = 1, TickStyle = TickStyle.None, Margin = new Padding(0, 0, 8, 0) };
    private readonly Label lblCharacterFrame = new() { Dock = DockStyle.Fill, Text = "Frame 0 / —", ForeColor = Color.Gainsboro, TextAlign = ContentAlignment.MiddleRight };
    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 33 };
    private FcvAnimation? characterAnimation;
    private int characterAnimationFrame;
    private bool syncingAnimationChoice;
    private Dictionary<string, string> characterAnimationNames = new(StringComparer.OrdinalIgnoreCase);

    private sealed record CharacterAnimationItem(FcvCatalogEntry Entry, string? Name)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Entry.FileName : $"{Entry.FileName} • {Name}";
    }

    private void BuildAnimationTimeline()
    {
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2 };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        ConfigureTextureButton(btnCharacterPlayPause, "PLAY", (_, _) => ToggleCharacterPlayback(), 70);
        ConfigureTextureButton(btnCharacterAnimationSave, "SALVAR NOME", (_, _) => SaveCharacterAnimationName(), 105);
        btnCharacterPlayPause.Enabled = false;
        btnCharacterAnimationSave.Enabled = false;
        cmbCharacterAnimation.SelectedIndexChanged += (_, _) => SelectCharacterAnimation();
        txtCharacterAnimationName.KeyDown += (_, e) => { if (e.KeyCode != Keys.Enter) return; SaveCharacterAnimationName(); e.SuppressKeyPress = true; };
        trkCharacterAnimation.Scroll += (_, _) => { PauseCharacterPlayback(); characterAnimationFrame = trkCharacterAnimation.Value; UpdateCharacterAnimationPose(); };
        animationTimer.Tick += (_, _) =>
        {
            if (characterAnimation == null) return;
            characterAnimationFrame = (characterAnimationFrame + 1) % characterAnimation.FrameCount;
            UpdateCharacterAnimationPose();
        };
        chkAnimationTimeline.CheckedChanged += (_, _) => animationTimeline.Visible = chkAnimationTimeline.Checked;
        chkCharacterTPose.CheckedChanged += (_, _) =>
        {
            if (chkCharacterTPose.Checked) PauseCharacterPlayback();
            UpdateCharacterAnimationPose();
        };
        chkCharacterLockRoot.CheckedChanged += (_, _) =>
        {
            characterLockRootChanged?.Invoke(chkCharacterLockRoot.Checked);
            viewport.SetEnemyAnimationIgnoreRootMotion(chkCharacterLockRoot.Checked);
            UpdateCharacterAnimationPose();
        };
        rows.Controls.Add(btnCharacterPlayPause, 0, 0);
        rows.Controls.Add(chkCharacterTPose, 1, 0);
        rows.Controls.Add(cmbCharacterAnimation, 2, 0);
        rows.Controls.Add(txtCharacterAnimationName, 3, 0);
        rows.Controls.Add(btnCharacterAnimationSave, 4, 0);
        rows.Controls.Add(chkCharacterLockRoot, 5, 0);
        rows.Controls.Add(trkCharacterAnimation, 0, 1);
        rows.SetColumnSpan(trkCharacterAnimation, 5);
        rows.Controls.Add(lblCharacterFrame, 5, 1);
        animationTimeline.Controls.Add(rows);
    }

    private void RefreshCharacterAnimations()
    {
        PauseCharacterPlayback();
        characterAnimation = null;
        characterAnimationFrame = 0;
        viewport.SetEnemyAttachmentAnimation(null, 0f, true);
        syncingAnimationChoice = true;
        cmbCharacterAnimation.Items.Clear();
        txtCharacterAnimationName.Clear();
        trkCharacterAnimation.Maximum = 1;
        trkCharacterAnimation.Value = 0;
        lblCharacterFrame.Text = "Frame 0 / —";
        btnCharacterPlayPause.Enabled = false;
        btnCharacterAnimationSave.Enabled = false;
        if (datPath != null)
        {
            try
            {
                characterAnimationNames = FcvNameCatalog.Load(characterWorkspace.MetadataPath(datPath), animationCatalogRoot);
                foreach (FcvCatalogEntry entry in characterWorkspace.Animations(datPath) ?? FcvCatalog.List(datPath))
                {
                    characterAnimationNames.TryGetValue(FcvNameCatalog.Key(datPath, entry.FileName), out string? name);
                    cmbCharacterAnimation.Items.Add(new CharacterAnimationItem(entry, name));
                }
            }
            catch (Exception ex) { status.Text = "Falha ao listar FCVs: " + ex.Message; }
        }
        syncingAnimationChoice = false;
        if (cmbCharacterAnimation.Items.Count > 0) cmbCharacterAnimation.SelectedIndex = 0;
    }

    private void SelectCharacterAnimation()
    {
        if (syncingAnimationChoice) return;
        PauseCharacterPlayback();
        characterAnimation = null;
        characterAnimationFrame = 0;
        viewport.SetEnemyAttachmentAnimation(null, 0f, true);
        if (cmbCharacterAnimation.SelectedItem is not CharacterAnimationItem item) return;
        txtCharacterAnimationName.Text = item.Name ?? "";
        btnCharacterAnimationSave.Enabled = true;
        try
        {
            characterAnimation = item.Entry.Read();
            if (characterAnimation.FrameCount == 0) throw new InvalidDataException("FCV sem frames.");
            trkCharacterAnimation.Maximum = Math.Max(1, characterAnimation.FrameCount - 1);
            trkCharacterAnimation.Value = 0;
            btnCharacterPlayPause.Enabled = model?.Skeleton != null;
            UpdateCharacterAnimationPose();
        }
        catch (Exception ex)
        {
            btnCharacterPlayPause.Enabled = false;
            lblCharacterFrame.Text = "FCV inválido";
            status.Text = $"Não foi possível ler {item.Entry.FileName}: {ex.Message}";
        }
    }

    private void ToggleCharacterPlayback()
    {
        if (animationTimer.Enabled) { PauseCharacterPlayback(); return; }
        if (characterAnimation == null || model?.Skeleton == null) return;
        if (chkCharacterTPose.Checked) chkCharacterTPose.Checked = false;
        if (chkFaceEdit.Checked) chkFaceEdit.Checked = false;
        animationTimer.Start();
        btnCharacterPlayPause.Text = "PAUSAR";
        UpdateCharacterPlaybackEditingState();
    }

    private void PauseCharacterPlayback()
    {
        animationTimer.Stop();
        btnCharacterPlayPause.Text = "PLAY";
        UpdateCharacterPlaybackEditingState();
    }

    private void UpdateCharacterPlaybackEditingState()
    {
        bool editable = !animationTimer.Enabled;
        chkFaceEdit.Enabled = editable;
        btnMoveMesh.Enabled = editable;
        btnRotateMesh.Enabled = editable && !chkFaceEdit.Checked;
        btnDeleteFaces.Enabled = editable && chkFaceEdit.Checked && selectedFaceFlags.Count > 0;
        if (meshTransformGroup != null) meshTransformGroup.Enabled = editable;
        viewport.EnemyModelGizmoEnabled = editable;
        viewport.EnemyModelPartPickingEnabled = editable && !chkFaceEdit.Checked;
        viewport.Invalidate();
    }

    private void UpdateCharacterAnimationPose()
    {
        if (characterAnimation == null) return;
        trkCharacterAnimation.Value = Math.Min(trkCharacterAnimation.Maximum, characterAnimationFrame);
        lblCharacterFrame.Text = $"Frame {characterAnimationFrame} / {characterAnimation.FrameCount - 1}";
        viewport.SetEnemyAnimationIgnoreRootMotion(chkCharacterLockRoot.Checked);
        viewport.SetEnemyAttachmentAnimation(chkCharacterTPose.Checked ? null : characterAnimation, characterAnimationFrame, true);
    }

    private void SaveCharacterAnimationName()
    {
        if (datPath == null || cmbCharacterAnimation.SelectedItem is not CharacterAnimationItem item) return;
        string name = txtCharacterAnimationName.Text.Trim();
        string key = FcvNameCatalog.Key(datPath, item.Entry.FileName);
        characterAnimationNames[key] = name;
        try
        {
            FcvNameCatalog.Save(characterWorkspace.MetadataPath(datPath), animationCatalogRoot, characterAnimationNames);
            int index = cmbCharacterAnimation.SelectedIndex;
            syncingAnimationChoice = true;
            cmbCharacterAnimation.Items[index] = new CharacterAnimationItem(item.Entry, name);
            cmbCharacterAnimation.SelectedIndex = index;
            syncingAnimationChoice = false;
            status.Text = name.Length == 0 ? "Nome da animação removido." : $"Nome da animação salvo: {name}";
        }
        catch (Exception ex) { syncingAnimationChoice = false; MessageBox.Show(this, ex.Message, "Não foi possível salvar o nome", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}


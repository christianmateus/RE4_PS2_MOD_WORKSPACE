using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using System.ComponentModel;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private bool syncingVisualDat;
    private bool loadingVisualEditor;
    private bool visualAevModified;
    private bool syncingVisualAevFilter;

    private sealed record VisualAevTypeFilterItem(byte? Type, string Name)
    {
        public override string ToString() => Name;
    }
    private string? visualSmdPath;
    private string? visualAevPath;

    private void SaveVisualCameraStateForActiveDat()
    {
        if (visualViewport == null || string.IsNullOrWhiteSpace(project.ActiveDatName)) return;
        DatProjectState? state = GetDatState(project.ActiveDatName, false);
        if (state == null) return;

        ScenarioCameraState camera = visualViewport.GetCameraState();
        state.HasVisualCamera = true;
        state.VisualCameraX = camera.X;
        state.VisualCameraY = camera.Y;
        state.VisualCameraZ = camera.Z;
        state.VisualCameraYaw = camera.Yaw;
        state.VisualCameraPitch = camera.Pitch;
        if (trkVisualMoveSpeed != null) state.VisualMoveSpeedSlider = trkVisualMoveSpeed.Value;
        if (trkVisualLookSpeed != null) state.VisualLookSpeedSlider = trkVisualLookSpeed.Value;

        if (!restoringSession) SaveProject();
    }

    private void RestoreVisualCameraState(string datName)
    {
        DatProjectState? state = GetDatState(datName, false);
        if (state == null) return;

        if (trkVisualMoveSpeed != null)
        {
            trkVisualMoveSpeed.Value = Math.Clamp(state.VisualMoveSpeedSlider <= 0 ? 100 : state.VisualMoveSpeedSlider,
                trkVisualMoveSpeed.Minimum, trkVisualMoveSpeed.Maximum);
            visualMoveSpeed_Scroll(null, EventArgs.Empty);
        }

        if (trkVisualLookSpeed != null)
        {
            trkVisualLookSpeed.Value = Math.Clamp(state.VisualLookSpeedSlider <= 0 ? 100 : state.VisualLookSpeedSlider,
                trkVisualLookSpeed.Minimum, trkVisualLookSpeed.Maximum);
            visualLookSpeed_Scroll(null, EventArgs.Empty);
        }

        if (state.HasVisualCamera)
        {
            visualViewport.SetCameraState(new ScenarioCameraState(
                state.VisualCameraX, state.VisualCameraY, state.VisualCameraZ,
                state.VisualCameraYaw, state.VisualCameraPitch));
        }
    }

    private void RefreshVisualDatList()
    {
        if (cmbVisualDat == null || cmbVisualDat.IsDisposed) return;

        string? active = project.ActiveDatName;
        syncingVisualDat = true;
        cmbVisualDat.BeginUpdate();
        try
        {
            cmbVisualDat.Items.Clear();
            foreach (DatProjectState state in project.DatStates
                .Where(x => !string.IsNullOrWhiteSpace(x.ContentPath) && Directory.Exists(x.ContentPath))
                .OrderBy(x => x.DatName, StringComparer.OrdinalIgnoreCase))
            {
                cmbVisualDat.Items.Add(new TextureDatItem(state.DatName, state.ContentPath!));
            }

            int selected = -1;
            if (!string.IsNullOrWhiteSpace(active))
            {
                for (int i = 0; i < cmbVisualDat.Items.Count; i++)
                {
                    if (cmbVisualDat.Items[i] is TextureDatItem item &&
                        item.DatName.Equals(active, StringComparison.OrdinalIgnoreCase))
                    {
                        selected = i;
                        break;
                    }
                }
            }

            if (cmbVisualDat.Items.Count > 0)
                cmbVisualDat.SelectedIndex = selected >= 0 ? selected : 0;
        }
        finally
        {
            cmbVisualDat.EndUpdate();
            syncingVisualDat = false;
        }
    }

    private async void cmbVisualDat_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (syncingVisualDat || loadingVisualEditor || cmbVisualDat.SelectedItem is not TextureDatItem item) return;

        if (visualAevModified)
        {
            DialogResult answer = MessageBox.Show(
                "O AEV atual possui alterações não salvas.\n\nTrocar de DAT e descartar essas alterações?",
                "Visual Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
            {
                RefreshVisualDatList();
                return;
            }
        }

        if (!item.DatName.Equals(project.ActiveDatName, StringComparison.OrdinalIgnoreCase))
        {
            SaveVisualCameraStateForActiveDat();
            ActivateTextureDat(item);
        }

        await LoadVisualDatAsync(item);
    }

    public async Task RefreshAndLoadVisualEditorAsync()
    {
        RefreshVisualDatList();
        if (cmbVisualDat.SelectedItem is TextureDatItem item)
            await LoadVisualDatAsync(item);
        else
            ClearVisualEditor("Nenhum DAT extraído disponível.");
    }

    private async Task LoadVisualDatAsync(TextureDatItem item)
    {
        if (loadingVisualEditor) return;
        loadingVisualEditor = true;
        UseWaitCursor = true;
        btnVisualFit.Enabled = false;
        btnVisualSaveAev.Enabled = false;

        try
        {
            string content = item.ContentPath;
            if (!Directory.Exists(content))
            {
                ClearVisualEditor("Content do DAT não encontrado.");
                return;
            }

            (string? smd, string? aev) = FindVisualFiles(content, item.DatName);
            visualSmdPath = smd;
            visualAevPath = aev;
            visualAevModified = false;

            if (smd == null)
            {
                visualViewport.SetScene(null);
                visualViewport.SetTextureSource(null);
                lblVisualStage.Text = item.DatName + " • sem SMD";
            }
            else
            {
                lblVisualStage.Text = "Carregando " + Path.GetFileName(smd) + "...";
                lblVisualStatus.Text = "Lendo SMD/BIN...";
                ExtractLog($"Visual Editor: DAT {item.DatName} -> {Path.GetFileName(smd)}");

                ScenarioScene scene = await Task.Run(() => Ps2ScenarioReader.Read(smd));
                visualViewport.SetScene(scene);

                string visualTplPath = GetTplWorkPath(smd, item.DatName);
                if (!File.Exists(visualTplPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(visualTplPath)!);
                    await Task.Run(() => SmdTextureService.ExtractTpl(smd, visualTplPath));
                }
                visualViewport.SetTextureSource(visualTplPath);

                ExtractLog($"Visual Editor: cenário automático carregado. Entries: {scene.EntryCount:N0}; BINs: {scene.LoadedBinCount:N0}/{scene.BinCount:N0}; Triangles: {scene.Triangles.Count:N0}.");
            }

            if (aev != null)
            {
                AevScene aevScene = await Task.Run(() => Ps2AevReader.Read(aev));
                visualViewport.SetAevScene(aevScene);
                WireVisualAevEvents();

                PopulateVisualAevTypeFilter(aevScene);
                RefreshVisualAevEntryList();

                pgVisualProperties.SelectedObject = null;
                clbVisualLayers.SetItemChecked(1, true);
                visualViewport.AevVisible = true;
                btnVisualSaveAev.Enabled = true;
                ExtractLog($"Visual Editor: AEV automático carregado: {Path.GetFileName(aev)} • {aevScene.Count:N0} entries.");
            }
            else
            {
                visualViewport.SetAevScene(null);
                lstVisualAevEntries.Items.Clear();
                pgVisualProperties.SelectedObject = null;
                btnVisualSaveAev.Enabled = false;
            }

            RestoreVisualCameraState(item.DatName);
            await UpdateVisualModifiedStateAsync();
            UpdateVisualStatus();
        }
        catch (Exception ex)
        {
            ExtractLog("Visual Editor: ERRO: " + ex.Message);
            ClearVisualEditor("Falha ao carregar DAT.");
            MessageBox.Show(this, ex.Message, "Visual Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            btnVisualFit.Enabled = true;
            loadingVisualEditor = false;
        }
    }

    private void WireVisualAevEvents()
    {
        visualViewport.AevEntryClicked -= visualViewport_AevEntryClicked;
        visualViewport.AevEntryClicked += visualViewport_AevEntryClicked;
        visualViewport.AevEntryEdited -= visualViewport_AevEntryEdited;
        visualViewport.AevEntryEdited += visualViewport_AevEntryEdited;
        visualViewport.DuplicateAevRequested -= visualViewport_DuplicateAevRequested;
        visualViewport.DuplicateAevRequested += visualViewport_DuplicateAevRequested;
        visualViewport.DeleteAevRequested -= visualViewport_DeleteAevRequested;
        visualViewport.DeleteAevRequested += visualViewport_DeleteAevRequested;
    }

    private static (string? Smd, string? Aev) FindVisualFiles(string content, string datName)
    {
        string datBase = Path.GetFileNameWithoutExtension(datName);
        string[] smds = Directory.GetFiles(content, "*.SMD", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        string[] aevs = Directory.GetFiles(content, "*.AEV", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

        string? smd = smds.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(datBase, StringComparison.OrdinalIgnoreCase))
            ?? smds.FirstOrDefault();

        string? preferredBase = smd == null ? datBase : Path.GetFileNameWithoutExtension(smd);
        string? aev = aevs.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(preferredBase, StringComparison.OrdinalIgnoreCase))
            ?? aevs.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(datBase, StringComparison.OrdinalIgnoreCase))
            ?? aevs.FirstOrDefault();

        return (smd, aev);
    }

    private void ClearVisualEditor(string status)
    {
        visualSmdPath = null;
        visualAevPath = null;
        visualAevModified = false;
        visualViewport?.SetScene(null);
        visualViewport?.SetAevScene(null);
        visualViewport?.SetTextureSource(null);
        lstVisualAevEntries?.Items.Clear();
        if (cmbVisualAevTypeFilter != null) cmbVisualAevTypeFilter.Items.Clear();
        if (pgVisualProperties != null) pgVisualProperties.SelectedObject = null;
        if (btnVisualSaveAev != null) btnVisualSaveAev.Enabled = false;
        lblVisualStage.Text = status;
        UpdateVisualStatus();
    }

    private async void btnVisualSaveAev_Click(object? sender, EventArgs e)
    {
        AevScene? scene = visualViewport?.AevScene;
        if (scene == null || string.IsNullOrWhiteSpace(visualAevPath)) return;

        btnVisualSaveAev.Enabled = false;
        try
        {
            string backup = GetVisualAevBackupPath(visualAevPath);
            bool backupCreated = await Task.Run(() => Ps2AevWriter.Save(scene, backup));
            visualAevModified = Ps2AevWriter.HasEditableChanges(scene);

            ExtractLog($"Visual Editor: AEV salvo: {Path.GetFileName(visualAevPath)}.");
            if (backupCreated) ExtractLog($"Visual Editor: backup inicial criado: {backup}");

            await RefreshChangeStatusAsync();
            _ = RefreshTrackedDatsAsync();
            await UpdateVisualModifiedStateAsync();
            UpdateVisualStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Salvar AEV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ExtractLog("Visual Editor: erro ao salvar AEV: " + ex.Message);
        }
        finally
        {
            btnVisualSaveAev.Enabled = visualViewport?.AevScene != null;
        }
    }

    private string GetVisualAevBackupPath(string aevPath)
    {
        string dat = string.IsNullOrWhiteSpace(project.ActiveDatName) ? "active" : Path.GetFileNameWithoutExtension(project.ActiveDatName);
        string root = project.RootPath ?? AppContext.BaseDirectory;
        return Path.Combine(root, ".workspace", "backups", dat, Path.GetFileName(aevPath) + ".bak");
    }

    private async Task UpdateVisualModifiedStateAsync()
    {
        string datName = project.ActiveDatName ?? "";
        string? content = GetActiveContentPath();

        bool smdModified = false;
        if (!string.IsNullOrWhiteSpace(visualSmdPath) && !string.IsNullOrWhiteSpace(content) && Directory.Exists(content) && !string.IsNullOrWhiteSpace(datName))
        {
            try
            {
                var state = await GetChangeStateAsync(datName, content);
                string rel = Path.GetRelativePath(content, visualSmdPath).Replace('\\', '/');
                smdModified = state.Diff.Changed.Concat(state.Diff.Added)
                    .Any(x => x.Replace('\\', '/').Equals(rel, StringComparison.OrdinalIgnoreCase));
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(visualSmdPath))
            lblVisualStage.Text = Path.GetFileName(visualSmdPath) + (smdModified ? " • SMD Modified" : "");
        else if (!string.IsNullOrWhiteSpace(project.ActiveDatName))
            lblVisualStage.Text = project.ActiveDatName + " • sem SMD";

        visualAevModified = visualViewport?.AevScene != null && Ps2AevWriter.HasEditableChanges(visualViewport.AevScene);
        btnVisualSaveAev.Text = visualAevModified ? "SAVE AEV *" : "SAVE AEV";
    }

    private void visualViewport_DuplicateAevRequested() => DuplicateSelectedAev();
    private void visualViewport_DeleteAevRequested() => DeleteSelectedAev();

    private void lstVisualAevEntries_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.D)
        {
            DuplicateSelectedAev();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            DeleteSelectedAev();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private AevEntry? GetSelectedAevEntryFromUi()
    {
        if (lstVisualAevEntries.SelectedItem is AevEntry listEntry) return listEntry;
        if (pgVisualProperties.SelectedObject is AevPropertyView view) return view.Entry;
        return null;
    }

    private void SetAevPropertiesObject(AevEntry? entry)
    {
        pgVisualProperties.SelectedObject = entry == null ? null : new AevPropertyView(entry);
    }

    private void DuplicateSelectedAev()
    {
        AevScene? scene = visualViewport?.AevScene;
        AevEntry? source = GetSelectedAevEntryFromUi();
        if (scene == null || source == null) return;

        int maxIndex = scene.Entries.Count == 0 ? -1 : scene.Entries.Max(x => x.Index);
        if (maxIndex >= 255)
        {
            MessageBox.Show(this, "Não há Index livre acima de 0xFF.", "Duplicar AEV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        AevEntry clone = CloneAevEntry(source);
        clone.Index = maxIndex + 1;
        clone.FileOrder = scene.Entries.Count;
        scene.Entries.Add(clone);
        RefreshAevFileOrder(scene);

        visualViewport.RegisterAevUndo(() =>
        {
            scene.Entries.Remove(clone);
            RefreshAevFileOrder(scene);
            RefreshVisualAevEntryList(source.FileOrder);
            visualViewport.RefreshAevSceneGeometry(source);
            SetAevPropertiesObject(source);
            MarkAevStructuralModified();
        });

        RefreshVisualAevEntryList(clone.FileOrder);
        visualViewport.RefreshAevSceneGeometry(clone);
        SetAevPropertiesObject(clone);
        MarkAevStructuralModified();
        ExtractLog($"Visual Editor: AEV duplicado no final com Index 0x{clone.Index:X2}. Índices existentes preservados.");
    }

    private void DeleteSelectedAev()
    {
        AevScene? scene = visualViewport?.AevScene;
        AevEntry? entry = GetSelectedAevEntryFromUi();
        if (scene == null || entry == null) return;

        int removeAt = scene.Entries.IndexOf(entry);
        if (removeAt < 0) return;

        scene.Entries.RemoveAt(removeAt);
        RefreshAevFileOrder(scene);
        AevEntry? next = scene.Entries.Count == 0 ? null : scene.Entries[Math.Clamp(removeAt, 0, scene.Entries.Count - 1)];

        visualViewport.RegisterAevUndo(() =>
        {
            scene.Entries.Insert(Math.Clamp(removeAt, 0, scene.Entries.Count), entry);
            RefreshAevFileOrder(scene);
            RefreshVisualAevEntryList(entry.FileOrder);
            visualViewport.RefreshAevSceneGeometry(entry);
            SetAevPropertiesObject(entry);
            MarkAevStructuralModified();
        });

        RefreshVisualAevEntryList(next?.FileOrder ?? -1);
        visualViewport.RefreshAevSceneGeometry(next);
        SetAevPropertiesObject(next);
        MarkAevStructuralModified();
        ExtractLog($"Visual Editor: AEV Index 0x{entry.Index:X2} removido sem reordenar os demais índices.");
    }

    private void MarkAevStructuralModified()
    {
        visualAevModified = true;
        btnVisualSaveAev.Text = "SAVE AEV *";
        lstVisualAevEntries.Refresh();
        UpdateVisualStatus();
    }

    private static void RefreshAevFileOrder(AevScene scene)
    {
        for (int i = 0; i < scene.Entries.Count; i++) scene.Entries[i].FileOrder = i;
    }

    private static AevEntry CloneAevEntry(AevEntry source)
    {
        return new AevEntry
        {
            FileOrder = source.FileOrder,
            RawData = (byte[])source.RawData.Clone(),
            ParameterBuffer = (byte[])source.ParameterBuffer.Clone(),
            HasExplicitRadius = source.HasExplicitRadius,
            IsPs2Layout = source.IsPs2Layout,
            Index = source.Index,
            Type = source.Type,
            Active = source.Active,
            Priority = source.Priority,
            DefinitionByte2 = source.DefinitionByte2,
            DefinitionByte3 = source.DefinitionByte3,
            DefinitionByte4 = source.DefinitionByte4,
            FunctionPointer = source.FunctionPointer,
            AreaHitType = source.AreaHitType,
            HitType = source.HitType,
            TriggerType = source.TriggerType,
            TargetType = source.TargetType,
            HitAngle = source.HitAngle,
            OpenAngle = source.OpenAngle,
            ActionType = source.ActionType,
            Y = source.Y,
            Height = source.Height,
            CircleRadius = source.CircleRadius,
            Position1 = source.Position1,
            Position2 = source.Position2,
            Position3 = source.Position3,
            Position4 = source.Position4
        };
    }

    private void PopulateVisualAevTypeFilter(AevScene scene)
    {
        syncingVisualAevFilter = true;
        cmbVisualAevTypeFilter.BeginUpdate();
        try
        {
            cmbVisualAevTypeFilter.Items.Clear();
            cmbVisualAevTypeFilter.Items.Add(new VisualAevTypeFilterItem(null, "All Event Types"));

            foreach (byte type in scene.Entries.Select(x => x.Type).Distinct().OrderBy(x => x))
                cmbVisualAevTypeFilter.Items.Add(new VisualAevTypeFilterItem(type, AevNames.EventTypeName(type)));

            cmbVisualAevTypeFilter.SelectedIndex = 0;
        }
        finally
        {
            cmbVisualAevTypeFilter.EndUpdate();
            syncingVisualAevFilter = false;
        }
        visualViewport.SetAevTypeFilter(null);
    }

    private void RefreshVisualAevEntryList(int preserveFileOrder = -1)
    {
        AevScene? scene = visualViewport?.AevScene;
        if (scene == null) return;

        byte? filter = (cmbVisualAevTypeFilter.SelectedItem as VisualAevTypeFilterItem)?.Type;

        lstVisualAevEntries.BeginUpdate();
        try
        {
            lstVisualAevEntries.Items.Clear();
            foreach (AevEntry entry in scene.Entries)
                if (!filter.HasValue || entry.Type == filter.Value)
                    lstVisualAevEntries.Items.Add(entry);
        }
        finally { lstVisualAevEntries.EndUpdate(); }

        if (preserveFileOrder >= 0)
        {
            for (int i = 0; i < lstVisualAevEntries.Items.Count; i++)
                if (lstVisualAevEntries.Items[i] is AevEntry entry && entry.FileOrder == preserveFileOrder)
                {
                    lstVisualAevEntries.SelectedIndex = i;
                    break;
                }
        }
    }

    private void cmbVisualAevTypeFilter_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (syncingVisualAevFilter || visualViewport == null) return;
        byte? filter = (cmbVisualAevTypeFilter.SelectedItem as VisualAevTypeFilterItem)?.Type;
        visualViewport.SetAevTypeFilter(filter);
        pgVisualProperties.SelectedObject = null;
        RefreshVisualAevEntryList();
    }

    private void pgVisualProperties_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        if (pgVisualProperties.SelectedObject is not AevPropertyView view) return;
        AevEntry entry = view.Entry;
        PropertyDescriptor? descriptor = e.ChangedItem.PropertyDescriptor;
        if (descriptor == null) return;
        object? oldValue = e.OldValue;
        string propertyName = descriptor.Name;

        visualViewport.RegisterAevUndo(() =>
        {
            try
            {
                descriptor.SetValue(view, oldValue);
                visualViewport.RefreshAevSceneGeometry(entry);
                SetAevPropertiesObject(entry);
                RefreshVisualAevEntryList(entry.FileOrder);
                MarkAevStructuralModified();
            }
            catch { }
        });

        if (propertyName == nameof(AevPropertyView.Type))
        {
            PopulateVisualAevTypeFilter(visualViewport.AevScene!);
            RefreshVisualAevEntryList(entry.FileOrder);
        }
        else lstVisualAevEntries.Refresh();

        visualViewport.RefreshAevSceneGeometry(entry);
        SetAevPropertiesObject(entry);
        visualAevModified = visualViewport.AevScene != null && Ps2AevWriter.HasEditableChanges(visualViewport.AevScene);
        btnVisualSaveAev.Text = visualAevModified ? "SAVE AEV *" : "SAVE AEV";
        UpdateVisualStatus();
    }

    private void visualViewport_AevEntryEdited(AevEntry entry)
    {
        visualAevModified = visualViewport?.AevScene != null && Ps2AevWriter.HasEditableChanges(visualViewport.AevScene);
        btnVisualSaveAev.Text = visualAevModified ? "SAVE AEV *" : "SAVE AEV";
        if (pgVisualProperties.SelectedObject is AevPropertyView view && ReferenceEquals(view.Entry, entry))
            pgVisualProperties.Refresh();
        lstVisualAevEntries.Refresh();
        visualViewport.Invalidate();
        UpdateVisualStatus();
    }

    private void visualViewport_AevEntryClicked(AevEntry? entry)
    {
        if (entry == null)
        {
            lstVisualAevEntries.ClearSelected();
            pgVisualProperties.SelectedObject = null;
            return;
        }

        bool found = false;
        for (int i = 0; i < lstVisualAevEntries.Items.Count; i++)
        {
            if (lstVisualAevEntries.Items[i] is AevEntry item && item.FileOrder == entry.FileOrder)
            {
                lstVisualAevEntries.SelectedIndex = i;
                lstVisualAevEntries.TopIndex = Math.Max(0, i - 4);
                found = true;
                break;
            }
        }

        if (!found && cmbVisualAevTypeFilter.Items.Count > 0)
        {
            cmbVisualAevTypeFilter.SelectedIndex = 0;
            RefreshVisualAevEntryList(entry.FileOrder);
        }

        SetAevPropertiesObject(entry);
    }

    private void lstVisualAevEntries_SelectedIndexChanged(object? sender, EventArgs e)
    {
        AevEntry? entry = lstVisualAevEntries.SelectedItem as AevEntry;
        SetAevPropertiesObject(entry);
        visualViewport.SelectAevEntry(entry);
    }

    private void visualMoveSpeed_Scroll(object? sender, EventArgs e)
    {
        if (trkVisualMoveSpeed == null) return;
        float scale = trkVisualMoveSpeed.Value / 100f;
        if (visualViewport != null) visualViewport.MovementSpeedMultiplier = scale;
        if (lblVisualMoveSpeed != null) lblVisualMoveSpeed.Text = $"MOVE {scale:0.00}×";
        if (!loadingVisualEditor && !restoringSession) SaveVisualCameraStateForActiveDat();
    }

    private void visualLookSpeed_Scroll(object? sender, EventArgs e)
    {
        if (trkVisualLookSpeed == null) return;
        float scale = trkVisualLookSpeed.Value / 100f;
        if (visualViewport != null) visualViewport.LookSensitivity = 0.0032f * scale;
        if (lblVisualLookSpeed != null) lblVisualLookSpeed.Text = $"LOOK {scale:0.00}×";
        if (!loadingVisualEditor && !restoringSession) SaveVisualCameraStateForActiveDat();
    }

    private void UpdateVisualStatus()
    {
        ScenarioScene? scene = visualViewport?.Scene;
        AevScene? aev = visualViewport?.AevScene;
        string modified = visualAevModified ? " • AEV Modified" : "";

        if (scene != null && aev != null) lblVisualStatus.Text = $"{scene.Triangles.Count:N0} tris • {aev.Count:N0} AEV{modified}";
        else if (scene != null) lblVisualStatus.Text = $"{scene.Triangles.Count:N0} tris • {visualViewport.LoadedTextureCount:N0} tex";
        else if (aev != null) lblVisualStatus.Text = $"{aev.Count:N0} AEV{modified}";
        else lblVisualStatus.Text = "v0.3.5 • AEV Event Editor";
    }

    private void RefreshVisualEditorTexturesFromTextureManager()
    {
        if (visualViewport?.Scene == null ||
            string.IsNullOrWhiteSpace(activeTextureSmdPath) ||
            string.IsNullOrWhiteSpace(activeTextureTplPath) ||
            !File.Exists(activeTextureTplPath))
            return;

        if (!Path.GetFullPath(visualViewport.Scene.SourcePath)
            .Equals(Path.GetFullPath(activeTextureSmdPath), StringComparison.OrdinalIgnoreCase))
            return;

        visualViewport.ReloadTextures(activeTextureTplPath);
        _ = UpdateVisualModifiedStateAsync();
        UpdateVisualStatus();
    }

    private void clbVisualLayers_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (e.Index > 1) return;
        BeginInvoke(new Action(() =>
        {
            if (visualViewport == null || visualViewport.IsDisposed) return;
            visualViewport.ScenarioVisible = clbVisualLayers.GetItemChecked(0);
            visualViewport.AevVisible = clbVisualLayers.GetItemChecked(1);
            visualViewport.Invalidate();
        }));
    }

    private void cmbVisualRenderMode_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (visualViewport == null || visualViewport.IsDisposed) return;
        ScenarioRenderMode mode = cmbVisualRenderMode.SelectedIndex switch
        {
            1 => ScenarioRenderMode.SolidWireframe,
            2 => ScenarioRenderMode.Wireframe,
            _ => ScenarioRenderMode.Solid
        };
        visualViewport.SetRenderMode(mode);
    }
}

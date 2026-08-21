using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using System.ComponentModel;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private bool syncingVisualDat;
    private bool loadingVisualEditor;
    private bool visualAevModified;
    private bool syncingVisualAevFilter;
    private bool syncingVisualEnemyFilter;
    private bool syncingVisualEnemyModelParts;
    private bool syncingVisualEnemyAttachment;
    private System.Windows.Forms.Timer? visualEnemyIdleTimer;
    private float visualEnemyIdleFrame;

    private sealed class VisualEnemyModelPartItem
    {
        public EnemyModelPart Part { get; }
        public VisualEnemyModelPartItem(EnemyModelPart part) => Part = part;
        public override string ToString()
        {
            string? known = EnemyModelPartCatalog.GetKnownPartName(Part.DatEntryIndex);
            string name = known == null ? $"DAT #{Part.DatEntryIndex:D3}" : $"#{Part.DatEntryIndex:D3} {known}";
            string tpl = Part.TplEntryIndex < 0 ? "TPL --" : $"TPL #{Part.TplEntryIndex:D3}";
            string how = Part.TplResolution switch { EnemyTplResolutionKind.DirectNext => "direct", EnemyTplResolutionKind.SharedPrevious => "shared", _ => "none" };
            string maps = Part.DiffuseMaps.Count == 0 ? "--" : string.Join(",", Part.DiffuseMaps.Select(x => x < 0 ? "none" : x.ToString()));
            return $"BIN {Part.BinIndex:D2} • {name} • {tpl} {how} • diffuse/tex [{maps}]";
        }
    }

    private sealed record VisualAevTypeFilterItem(byte? Type, string Name)
    {
        public override string ToString() => Name;
    }
    private sealed record VisualEnemyLocationFilterItem(byte? StageId, byte? RoomId, string Name)
    {
        public override string ToString() => Name;
    }
    private sealed record VisualEnemyAttachmentBoneItem(int Index, byte Id, byte ParentId)
    {
        public override string ToString() => $"#{Index:00} • Bone 0x{Id:X2} • Parent {(ParentId==0xFF?"ROOT":$"0x{ParentId:X2}")}";
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
        await EnsureDefaultVisualEnemyEslAsync();
        RefreshVisualDatList();
        if (cmbVisualDat.SelectedItem is TextureDatItem item)
            await LoadVisualDatAsync(item);
        else
            ClearVisualEditor("Nenhum DAT extraído disponível.");
    }

    private static bool TryParseScenarioLocation(string? datName, out byte stageId, out byte roomId)
    {
        stageId = roomId = 0;
        string name = Path.GetFileNameWithoutExtension(datName ?? string.Empty);
        if (name.Length < 4 || (name[0] != 'r' && name[0] != 'R')) return false;
        return byte.TryParse(name.Substring(1,1), System.Globalization.NumberStyles.HexNumber, null, out stageId)
            && byte.TryParse(name.Substring(2,2), System.Globalization.NumberStyles.HexNumber, null, out roomId);
    }

    private void PopulateVisualEnemyLocationFilter(string? preferredDatName = null)
    {
        if (cmbVisualEnemyLocationFilter == null) return;
        syncingVisualEnemyFilter = true;
        try
        {
            cmbVisualEnemyLocationFilter.BeginUpdate();
            cmbVisualEnemyLocationFilter.Items.Clear();
            cmbVisualEnemyLocationFilter.Items.Add(new VisualEnemyLocationFilterItem(null, null, "Todas as fases"));
            if (selectedEnemyScene != null)
                foreach (var x in selectedEnemyScene.Entries.Select(e => (e.StageID, e.RoomID)).Distinct().OrderBy(x => x.StageID).ThenBy(x => x.RoomID))
                    cmbVisualEnemyLocationFilter.Items.Add(new VisualEnemyLocationFilterItem(x.StageID, x.RoomID, $"r{x.StageID:X1}{x.RoomID:X2}"));
            int select = 0;
            if (TryParseScenarioLocation(preferredDatName ?? project.ActiveDatName, out byte stage, out byte room))
                for (int i=1;i<cmbVisualEnemyLocationFilter.Items.Count;i++) if (cmbVisualEnemyLocationFilter.Items[i] is VisualEnemyLocationFilterItem f && f.StageId==stage && f.RoomId==room) { select=i; break; }
            cmbVisualEnemyLocationFilter.SelectedIndex = select;
            cmbVisualEnemyLocationFilter.EndUpdate();
            ApplyVisualEnemyLocationFilter();
        }
        finally { syncingVisualEnemyFilter = false; }
    }

    private void cmbVisualEnemyLocationFilter_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (syncingVisualEnemyFilter) return;
        ApplyVisualEnemyLocationFilter();
    }

    private void ApplyVisualEnemyLocationFilter()
    {
        if (visualViewport == null) return;
        if (cmbVisualEnemyLocationFilter?.SelectedItem is VisualEnemyLocationFilterItem f) visualViewport.SetEnemyLocationFilter(f.StageId, f.RoomId);
        else visualViewport.SetEnemyLocationFilter(null, null);
        RefreshVisualEnemyEntryList();
        _ = EnsureVisualEnemyModelsAsync();
    }

    private bool VisualEnemyPassesFilter(EslEnemyEntry e)
    {
        if (!chkVisualEnemyInactive.Checked && e.Active == 0) return false;
        if (cmbVisualEnemyLocationFilter.SelectedItem is VisualEnemyLocationFilterItem f && f.StageId.HasValue && f.RoomId.HasValue)
            return e.StageID == f.StageId.Value && e.RoomID == f.RoomId.Value;
        return true;
    }

    private void RefreshVisualEnemyEntryList(IEnumerable<int>? preserve = null)
    {
        if (lstVisualEnemyEntries == null) return;
        HashSet<int> selected = preserve != null ? preserve.ToHashSet() : lstVisualEnemyEntries.SelectedItems.Cast<object>().OfType<EslEnemyEntry>().Select(x => x.Index).ToHashSet();
        lstVisualEnemyEntries.BeginUpdate();
        lstVisualEnemyEntries.Items.Clear();
        if (selectedEnemyScene != null) foreach (EslEnemyEntry e in selectedEnemyScene.Entries.Where(VisualEnemyPassesFilter)) lstVisualEnemyEntries.Items.Add(e);
        for (int i = 0; i < lstVisualEnemyEntries.Items.Count; i++) if (lstVisualEnemyEntries.Items[i] is EslEnemyEntry e && selected.Contains(e.Index)) lstVisualEnemyEntries.SetSelected(i, true);
        lstVisualEnemyEntries.EndUpdate();
    }

    private List<EslEnemyEntry> GetVisualSelectedEnemies() => lstVisualEnemyEntries?.SelectedItems.Cast<object>().OfType<EslEnemyEntry>().ToList() ?? new();

    private void lstVisualEnemyEntries_SelectedIndexChanged(object? sender, EventArgs e)
    {
        List<EslEnemyEntry> entries = GetVisualSelectedEnemies();
        if (entries.Count == 0) { pgVisualProperties.SelectedObject = null; lblVisualPropertiesTitle.Text = "PROPERTIES • SELECTION"; visualViewport?.SelectEnemyEntry(null); RefreshVisualEnemyModelParts(null); return; }
        if (entries.Count == 1) pgVisualProperties.SelectedObject = entries[0]; else pgVisualProperties.SelectedObjects = entries.Cast<object>().ToArray();
        lblVisualPropertiesTitle.Text = entries.Count == 1 ? $"PROPERTIES • ENEMY #{entries[0].Index:D3}" : $"PROPERTIES • {entries.Count} ENEMIES";
        visualViewport?.SelectEnemyEntry(entries[0]);
        RefreshVisualEnemyModelParts(entries[0]);
    }

    private void btnVisualEnemyGizmoMove_Click(object? sender, EventArgs e) => SetEnemyGizmoMode(EnemyGizmoMode.Move);
    private void btnVisualEnemyGizmoRotate_Click(object? sender, EventArgs e) => SetEnemyGizmoMode(EnemyGizmoMode.Rotate);

    private void SetEnemyGizmoMode(EnemyGizmoMode mode)
    {
        if (visualViewport == null) return;
        visualViewport.EnemyTransformMode = mode;
        if (btnVisualEnemyGizmoMove != null) btnVisualEnemyGizmoMove.BackColor = mode == EnemyGizmoMode.Move ? Accent : Surface2;
        if (btnVisualEnemyGizmoRotate != null) btnVisualEnemyGizmoRotate.BackColor = mode == EnemyGizmoMode.Rotate ? Accent : Surface2;
        visualViewport.Invalidate();
    }

    private void chkVisualEnemySnap_CheckedChanged(object? sender, EventArgs e)
    {
        settings.VisualEnemySnap = chkVisualEnemySnap.Checked;
        if (visualViewport != null) visualViewport.EnemySnapEnabled = chkVisualEnemySnap.Checked;
        if (!restoringSession) SaveSettings();
    }

    private void chkVisualEnemyAnimated_CheckedChanged(object? sender, EventArgs e)
    {
        settings.VisualEnemyAnimated = chkVisualEnemyAnimated.Checked;
        if (!restoringSession) SaveSettings();
        ApplyVisualEnemyIdleAnimationState();
    }

    private void ApplyVisualEnemyIdleAnimationState()
    {
        bool enabled = chkVisualEnemyAnimated != null && chkVisualEnemyAnimated.Checked;
        visualViewport?.SetEnemyIdleAnimation(enabled, visualEnemyIdleFrame);
        if (!enabled)
        {
            visualEnemyIdleTimer?.Stop();
            visualEnemyIdleFrame = 0f;
            visualViewport?.SetEnemyIdleAnimation(false, 0f);
            return;
        }
        visualEnemyIdleTimer ??= CreateVisualEnemyIdleTimer();
        if (!visualEnemyIdleTimer.Enabled) visualEnemyIdleTimer.Start();
    }

    private System.Windows.Forms.Timer CreateVisualEnemyIdleTimer()
    {
        var timer = new System.Windows.Forms.Timer { Interval = 33 };
        timer.Tick += (_, _) =>
        {
            if (chkVisualEnemyAnimated == null || !chkVisualEnemyAnimated.Checked || visualViewport == null || visualViewport.IsDisposed) return;
            int frameCount = 0;
            if (visualEnemyModelCache.TryGetValue(0x12, out EnemyModelScene? em12) && em12.IdleAnimation != null) frameCount = em12.IdleAnimation.FrameCount;
            if (frameCount <= 0) { visualEnemyIdleFrame = 0f; visualViewport.SetEnemyIdleAnimation(true, 0f); return; }
            visualEnemyIdleFrame += 1f;
            if (visualEnemyIdleFrame >= frameCount) visualEnemyIdleFrame = 0f;
            visualViewport.SetEnemyIdleAnimation(true, visualEnemyIdleFrame);
        };
        return timer;
    }

    private void FocusSelectedEnemy()
    {
        EslEnemyEntry? enemy = GetVisualSelectedEnemies().FirstOrDefault();
        if (enemy != null) visualViewport?.FocusEnemy(enemy);
    }

    private static EslEnemyEntry CloneEnemyEntry(EslEnemyEntry e) => new()
    {
        Index=e.Index, Active=e.Active, EnemyType=e.EnemyType, Subtype=e.Subtype, Animation=e.Animation, SightRange=e.SightRange,
        Equip1=e.Equip1, Equip2=e.Equip2, Weapon=e.Weapon, Health=e.Health, Unknown1=e.Unknown1, ReturnSpawn=e.ReturnSpawn,
        PosX=e.PosX, PosY=e.PosY, PosZ=e.PosZ, RotX=e.RotX, RotY=e.RotY, RotZ=e.RotZ, RoomID=e.RoomID, StageID=e.StageID,
        Unknown2=e.Unknown2, Unknown3=e.Unknown3, Unknown4=e.Unknown4, Unknown5=e.Unknown5, Unknown6=e.Unknown6, Unknown7=e.Unknown7
    };

    private static void CopyEnemyEntry(EslEnemyEntry source, EslEnemyEntry target, bool preserveIndex=true)
    {
        int index=target.Index; target.Active=source.Active; target.EnemyType=source.EnemyType; target.Subtype=source.Subtype; target.Animation=source.Animation; target.SightRange=source.SightRange;
        target.Equip1=source.Equip1; target.Equip2=source.Equip2; target.Weapon=source.Weapon; target.Health=source.Health; target.Unknown1=source.Unknown1; target.ReturnSpawn=source.ReturnSpawn;
        target.PosX=source.PosX; target.PosY=source.PosY; target.PosZ=source.PosZ; target.RotX=source.RotX; target.RotY=source.RotY; target.RotZ=source.RotZ; target.RoomID=source.RoomID; target.StageID=source.StageID;
        target.Unknown2=source.Unknown2; target.Unknown3=source.Unknown3; target.Unknown4=source.Unknown4; target.Unknown5=source.Unknown5; target.Unknown6=source.Unknown6; target.Unknown7=source.Unknown7;
        if(preserveIndex) target.Index=index; else target.Index=source.Index;
    }

    private void lstVisualEnemyEntries_KeyDown(object? sender, KeyEventArgs e)
    {
        if(e.KeyCode==Keys.F){e.Handled=true;e.SuppressKeyPress=true;FocusSelectedEnemy();}
        else if(e.KeyCode==Keys.G){e.Handled=true;e.SuppressKeyPress=true;SetEnemyGizmoMode(EnemyGizmoMode.Move);}
        else if(e.KeyCode==Keys.R){e.Handled=true;e.SuppressKeyPress=true;SetEnemyGizmoMode(EnemyGizmoMode.Rotate);}
    }

    private void RefreshVisualEnemyModelParts(EslEnemyEntry? entry)
    {
        if (clbVisualEnemyModelParts == null || lblVisualEnemyParts == null) return;
        syncingVisualEnemyModelParts = true;
        try
        {
            clbVisualEnemyModelParts.BeginUpdate(); clbVisualEnemyModelParts.Items.Clear();
            if (entry == null) { lblVisualEnemyParts.Text = "MODEL PARTS • selecione um inimigo"; return; }
            if (!visualEnemyModelCache.TryGetValue(entry.EnemyType, out EnemyModelScene? model))
            {
                lblVisualEnemyParts.Text = $"MODEL PARTS • em{entry.EnemyType:X2}.dat • carregando..."; return;
            }
            bool automatic = EnemyModelPartCatalog.CanApplyAutomaticCoreParts(model, entry.EnemyType, entry.Subtype);
            string mode = automatic ? "AUTO core + equipment" : "sem mapa automático";
            string equipment = EnemyEquipmentCatalog.GetSummary(entry);
            lblVisualEnemyParts.Text = $"MODEL PARTS • em{entry.EnemyType:X2}.dat • {mode} • {equipment}";
            foreach (EnemyModelPart part in model.Parts.OrderBy(x => x.BinIndex))
                clbVisualEnemyModelParts.Items.Add(new VisualEnemyModelPartItem(part), visualViewport?.IsEnemyModelPartAutomaticallyVisible(entry, part) != false);
            RefreshVisualEnemyAttachment(entry);
        }
        finally { clbVisualEnemyModelParts.EndUpdate(); syncingVisualEnemyModelParts = false; }
    }

    private void RefreshVisualEnemyAttachment(EslEnemyEntry? entry)
    {
        if (cmbVisualEnemyAttachBone == null || visualViewport == null) return;
        syncingVisualEnemyAttachment = true;
        try
        {
            cmbVisualEnemyAttachBone.BeginUpdate(); cmbVisualEnemyAttachBone.Items.Clear();
            if (entry == null) { lblVisualEnemyAttachment.Text = "ATTACHMENT DEBUG • selecione um inimigo"; return; }
            IReadOnlyList<Ps2BinBone> bones = visualViewport.GetEnemyAttachmentBones(entry.EnemyType);
            int source = visualViewport.GetEnemySkeletonSource(entry.EnemyType);
            lblVisualEnemyAttachment.Text = entry.EnemyType == 0x12 ? $"ATTACHMENT DEBUG • Village weapons • Left Hand bone 16 • skeleton #{source:D3}" : $"ATTACHMENT DEBUG • em{entry.EnemyType:X2} • experimental";
            foreach (Ps2BinBone bone in bones) cmbVisualEnemyAttachBone.Items.Add(new VisualEnemyAttachmentBoneItem(bone.Index,bone.Id,bone.ParentId));
            if (bones.Count > 0)
            {
                int wanted = visualViewport.EnemyAttachmentBoneIndex;
                // Village Ganados: bone/index 16 is the left hand; tested weapons are held there.
                if (wanted < 0 || wanted >= bones.Count) wanted = entry.EnemyType == 0x12 && bones.Count > 16 ? 16 : Math.Min(bones.Count-1,0);
                cmbVisualEnemyAttachBone.SelectedIndex = wanted; visualViewport.SetEnemyAttachmentBone(wanted);
            }
            var off=visualViewport.EnemyAttachmentOffset; var rot=visualViewport.EnemyAttachmentRotationDegrees;
            nudVisualEnemyAttachX.Value=ClampNud(nudVisualEnemyAttachX,(decimal)off.X); nudVisualEnemyAttachY.Value=ClampNud(nudVisualEnemyAttachY,(decimal)off.Y); nudVisualEnemyAttachZ.Value=ClampNud(nudVisualEnemyAttachZ,(decimal)off.Z);
            nudVisualEnemyAttachRX.Value=ClampNud(nudVisualEnemyAttachRX,(decimal)rot.X); nudVisualEnemyAttachRY.Value=ClampNud(nudVisualEnemyAttachRY,(decimal)rot.Y); nudVisualEnemyAttachRZ.Value=ClampNud(nudVisualEnemyAttachRZ,(decimal)rot.Z);
        }
        finally { cmbVisualEnemyAttachBone.EndUpdate(); syncingVisualEnemyAttachment=false; }
    }

    private static decimal ClampNud(NumericUpDown n, decimal value) => Math.Min(n.Maximum,Math.Max(n.Minimum,value));
    private void cmbVisualEnemyAttachBone_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if(syncingVisualEnemyAttachment || visualViewport==null || cmbVisualEnemyAttachBone.SelectedItem is not VisualEnemyAttachmentBoneItem b)return;
        visualViewport.SetEnemyAttachmentBone(b.Index);
    }
    private void visualEnemyAttachment_ValueChanged(object? sender, EventArgs e)
    {
        if(syncingVisualEnemyAttachment || visualViewport==null)return;
        visualViewport.SetEnemyAttachmentOffset((float)nudVisualEnemyAttachX.Value,(float)nudVisualEnemyAttachY.Value,(float)nudVisualEnemyAttachZ.Value);
        visualViewport.SetEnemyAttachmentRotation((float)nudVisualEnemyAttachRX.Value,(float)nudVisualEnemyAttachRY.Value,(float)nudVisualEnemyAttachRZ.Value);
    }

    private void clbVisualEnemyModelParts_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (syncingVisualEnemyModelParts || visualViewport == null || e.Index < 0 || e.Index >= clbVisualEnemyModelParts.Items.Count) return;
        if (GetVisualSelectedEnemies().FirstOrDefault() is not EslEnemyEntry enemy || clbVisualEnemyModelParts.Items[e.Index] is not VisualEnemyModelPartItem item) return;
        visualViewport.SetEnemyModelPartVisible(enemy.EnemyType, item.Part.BinIndex, e.NewValue == CheckState.Checked);
    }

    private void btnVisualEnemyPartsSolo_Click(object? sender, EventArgs e)
    {
        if (visualViewport == null || GetVisualSelectedEnemies().FirstOrDefault() is not EslEnemyEntry enemy || clbVisualEnemyModelParts.SelectedItem is not VisualEnemyModelPartItem item) return;
        visualViewport.SoloEnemyModelPart(enemy.EnemyType, item.Part.BinIndex);
        RefreshVisualEnemyModelParts(enemy);
    }

    private void btnVisualEnemyPartsAll_Click(object? sender, EventArgs e)
    {
        if (visualViewport == null || GetVisualSelectedEnemies().FirstOrDefault() is not EslEnemyEntry enemy) return;
        visualViewport.ShowAllEnemyModelParts(enemy.EnemyType);
        RefreshVisualEnemyModelParts(enemy);
    }

    private void btnVisualEnemyPartsAuto_Click(object? sender, EventArgs e)
    {
        if (visualViewport == null || GetVisualSelectedEnemies().FirstOrDefault() is not EslEnemyEntry enemy) return;
        visualViewport.UseAutomaticEnemyModelParts(enemy.EnemyType);
        RefreshVisualEnemyModelParts(enemy);
    }

    private void WireVisualEnemyEvents()
    {
        if (visualViewport == null) return;
        visualViewport.EnemyEntryClicked -= visualViewport_EnemyEntryClicked;
        visualViewport.EnemyEntryClicked += visualViewport_EnemyEntryClicked;
        visualViewport.EnemyEntryEdited -= visualViewport_EnemyEntryEdited;
        visualViewport.EnemyEntryEdited += visualViewport_EnemyEntryEdited;
    }

    private void visualViewport_EnemyEntryClicked(EslEnemyEntry? entry)
    {
        if (entry == null) { lstVisualEnemyEntries.ClearSelected(); return; }
        if (tabVisualEntities != null) tabVisualEntities.SelectedIndex = 1;
        int found = -1;
        for (int i = 0; i < lstVisualEnemyEntries.Items.Count; i++) if (lstVisualEnemyEntries.Items[i] is EslEnemyEntry x && x.Index == entry.Index) { found = i; break; }
        if (found < 0) { chkVisualEnemyInactive.Checked = true; RefreshVisualEnemyEntryList(new[]{entry.Index}); for (int i=0;i<lstVisualEnemyEntries.Items.Count;i++) if (lstVisualEnemyEntries.Items[i] is EslEnemyEntry x && x.Index==entry.Index) { found=i; break; } }
        if (found >= 0) { lstVisualEnemyEntries.ClearSelected(); lstVisualEnemyEntries.SetSelected(found, true); lstVisualEnemyEntries.TopIndex = Math.Max(0, found - 4); }
        pgVisualProperties.SelectedObject = entry; lblVisualPropertiesTitle.Text = $"PROPERTIES • ENEMY #{entry.Index:D3}";
        RefreshVisualEnemyModelParts(entry);
    }

    private void visualViewport_EnemyEntryEdited(EslEnemyEntry entry)
    {
        lstVisualEnemyEntries.Refresh(); lstEnemyEntries?.Refresh(); pgVisualProperties.Refresh(); pgEnemyProperties?.Refresh();
        visualViewport?.RefreshEnemyGeometry(entry);
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

            visualViewport.SetEslScene(selectedEnemyScene);
            PopulateVisualEnemyLocationFilter(item.DatName);

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
        visualViewport?.SetEslScene(null);
        visualViewport?.SetTextureSource(null);
        lstVisualAevEntries?.Items.Clear();
        lstVisualEnemyEntries?.Items.Clear();
        RefreshVisualEnemyModelParts(null);
        if (cmbVisualAevTypeFilter != null) cmbVisualAevTypeFilter.Items.Clear();
        if (pgVisualProperties != null) pgVisualProperties.SelectedObject = null;
        if (btnVisualSaveAev != null) btnVisualSaveAev.Enabled = false;
        if (btnVisualSaveEsl != null) btnVisualSaveEsl.Enabled = selectedEnemyScene != null && !string.IsNullOrWhiteSpace(currentEnemyEslPath);
        lblVisualStage.Text = status;
        UpdateVisualStatus();
    }

    private void btnVisualSaveEsl_Click(object? sender, EventArgs e) => SaveCurrentEnemyEsl();
    private async void btnVisualSaveAev_Click(object? sender, EventArgs e) => await SaveVisualAevAsync();

    private async Task<bool> SaveVisualAevAsync()
    {
        AevScene? scene = visualViewport?.AevScene;
        if (scene == null || string.IsNullOrWhiteSpace(visualAevPath)) return false;
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
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Salvar AEV", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ExtractLog("Visual Editor: erro ao salvar AEV: " + ex.Message);
            return false;
        }
        finally { btnVisualSaveAev.Enabled = visualViewport?.AevScene != null; }
    }

    private async Task<bool> SaveVisualEditorAllAsync(bool updateUi = true)
    {
        bool savedAnything = false;
        if (visualViewport?.AevScene != null && !string.IsNullOrWhiteSpace(visualAevPath)) savedAnything |= await SaveVisualAevAsync();
        if (selectedEnemyScene != null && !string.IsNullOrWhiteSpace(currentEnemyEslPath)) savedAnything |= SaveCurrentEnemyEsl(false);
        string? activeContent = GetActiveContentPath();
        if (!string.IsNullOrWhiteSpace(activeContent) && Directory.Exists(activeContent))
        {
            int pendingTpl = CountPendingTplChanges(activeContent);
            if (pendingTpl > 0)
            {
                int injected = InjectPendingTplChanges(activeContent);
                if (injected > 0) { savedAnything = true; ExtractLog($"Visual Editor: {injected} TPL(s) pendente(s) incorporado(s) ao SMD."); }
            }
        }
        if (savedAnything)
        {
            if(updateUi) lblVisualStatus.Text = "Salvo • pronto para Build & Test";
            ExtractLog("Visual Editor: dados carregados salvos (AEV/ESL). O Build & Test reinserirá DAT + ESL automaticamente na ISO de Build.");
        }
        return savedAnything;
    }

    private async void Form1_GlobalKeyDown(object? sender, KeyEventArgs e)
    {
        bool visualOpen = pnlVisualEditor != null && pnlVisualEditor.Visible;
        bool enemiesOpen = pnlEnemies != null && pnlEnemies.Visible;
        if(visualOpen && !e.Control && e.KeyCode==Keys.F){e.Handled=true;e.SuppressKeyPress=true;FocusSelectedEnemy();return;}
        if(visualOpen && !e.Control && e.KeyCode==Keys.G){e.Handled=true;e.SuppressKeyPress=true;SetEnemyGizmoMode(EnemyGizmoMode.Move);return;}
        if(visualOpen && !e.Control && e.KeyCode==Keys.R){e.Handled=true;e.SuppressKeyPress=true;SetEnemyGizmoMode(EnemyGizmoMode.Rotate);return;}
        if (!e.Control) return;
        if (visualOpen && e.KeyCode == Keys.S)
        {
            e.Handled = true; e.SuppressKeyPress = true;
            await SaveVisualEditorAllAsync();
            return;
        }
        if ((visualOpen || enemiesOpen) && e.KeyCode == Keys.Z)
        {
            bool enemyTab = enemiesOpen || (tabVisualEntities != null && tabVisualEntities.SelectedIndex == 1);
            bool undone = enemyTab ? visualViewport?.UndoEnemyEdit() == true : visualViewport?.UndoAevEdit() == true;
            if (!undone && visualOpen) undone = enemyTab ? visualViewport?.UndoAevEdit() == true : visualViewport?.UndoEnemyEdit() == true;
            if (undone) { e.Handled = true; e.SuppressKeyPress = true; }
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
        if (lblVisualPropertiesTitle != null) lblVisualPropertiesTitle.Text = entry == null ? "PROPERTIES • SELECTION" : $"PROPERTIES • AEV [{entry.Index:X2}]";
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
        EslEnemyEntry[] visualEnemies = pgVisualProperties.SelectedObjects.OfType<EslEnemyEntry>().ToArray();
        if (visualEnemies.Length == 0 && pgVisualProperties.SelectedObject is EslEnemyEntry oneEnemy) visualEnemies = new[] { oneEnemy };
        if (visualEnemies.Length > 0)
        {
            int[] indices = visualEnemies.Select(x => x.Index).ToArray();
            var enemyDescriptor = e.ChangedItem.PropertyDescriptor;
            object? enemyOldValue = e.OldValue;
            if (enemyDescriptor != null && visualViewport != null)
            {
                EslEnemyEntry[] targets = visualEnemies.ToArray();
                visualViewport.RegisterEnemyUndo(() =>
                {
                    foreach (EslEnemyEntry enemy in targets) try { enemyDescriptor.SetValue(enemy, enemyOldValue); } catch { }
                    NotifyEnemyEntriesChanged(targets.Select(x => x.Index));
                    pgVisualProperties.Refresh();
                });
            }
            NotifyEnemyEntriesChanged(indices);
            RefreshVisualEnemyEntryList(indices);
            return;
        }
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
        if (entry != null && tabVisualEntities != null) tabVisualEntities.SelectedIndex = 0;
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
        EslScene? esl = visualViewport?.EslScene;

        if (scene != null && aev != null && esl != null) lblVisualStatus.Text = $"{scene.Triangles.Count:N0} tris • {aev.Count:N0} AEV • {esl.ActiveCount:N0} enemies{modified}";
        else if (scene != null && aev != null) lblVisualStatus.Text = $"{scene.Triangles.Count:N0} tris • {aev.Count:N0} AEV{modified}";
        else if (scene != null) lblVisualStatus.Text = $"{scene.Triangles.Count:N0} tris • {visualViewport.LoadedTextureCount:N0} tex";
        else if (aev != null) lblVisualStatus.Text = $"{aev.Count:N0} AEV{modified}";
        else if(esl != null) lblVisualStatus.Text = $"{esl.ActiveCount:N0} enemies{modified}";
        else lblVisualStatus.Text = "v0.5.0 • Visual Editor";
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

    private void ApplyVisualLayerSettings()
    {
        if (clbVisualLayers == null || clbVisualLayers.Items.Count < 5) return;
        clbVisualLayers.SetItemChecked(0, settings.VisualScenarioLayer);
        clbVisualLayers.SetItemChecked(1, settings.VisualAevLayer);
        clbVisualLayers.SetItemChecked(2, settings.VisualEnemiesLayer);
        clbVisualLayers.SetItemChecked(3, settings.VisualObjectsLayer);
        clbVisualLayers.SetItemChecked(4, settings.VisualCollisionLayer);
        if (visualViewport != null && !visualViewport.IsDisposed)
        {
            visualViewport.ScenarioVisible = settings.VisualScenarioLayer;
            visualViewport.AevVisible = settings.VisualAevLayer;
            visualViewport.EnemiesVisible = settings.VisualEnemiesLayer;
            visualViewport.EnemySnapEnabled = settings.VisualEnemySnap;
        }
        if(chkVisualEnemySnap!=null) chkVisualEnemySnap.Checked=settings.VisualEnemySnap;
        if(chkVisualEnemyAnimated!=null) chkVisualEnemyAnimated.Checked=settings.VisualEnemyAnimated;
        SetEnemyGizmoMode(EnemyGizmoMode.Move);
    }

    private void clbVisualLayers_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        // During the constructor the persisted checks are restored before the Form handle exists.
        // ApplyVisualLayerSettings already updates the viewport directly in that case.
        if (!IsHandleCreated) return;
        BeginInvoke(new Action(() =>
        {
            if (clbVisualLayers == null || clbVisualLayers.Items.Count < 5) return;
            settings.VisualScenarioLayer = clbVisualLayers.GetItemChecked(0);
            settings.VisualAevLayer = clbVisualLayers.GetItemChecked(1);
            settings.VisualEnemiesLayer = clbVisualLayers.GetItemChecked(2);
            settings.VisualObjectsLayer = clbVisualLayers.GetItemChecked(3);
            settings.VisualCollisionLayer = clbVisualLayers.GetItemChecked(4);
            if (visualViewport != null && !visualViewport.IsDisposed)
            {
                visualViewport.ScenarioVisible = settings.VisualScenarioLayer;
                visualViewport.AevVisible = settings.VisualAevLayer;
                visualViewport.EnemiesVisible = settings.VisualEnemiesLayer;
                visualViewport.Invalidate();
            }
            if (!restoringSession) SaveSettings();
        }));
    }


    private void chkVisualEnemyModelParts_CheckedChanged(object? sender, EventArgs e)
    {
        if (pnlVisualEnemyModelParts != null) pnlVisualEnemyModelParts.Visible = chkVisualEnemyModelParts.Checked;
        settings.VisualEnemyModelParts = chkVisualEnemyModelParts.Checked;
        if (!restoringSession) SaveSettings();
        if (chkVisualEnemyModelParts.Checked) RefreshVisualEnemyModelParts(GetVisualSelectedEnemies().FirstOrDefault());
    }

    private void chkVisualEnemyInactive_CheckedChanged(object? sender, EventArgs e)
    {
        if (visualViewport != null && !visualViewport.IsDisposed) visualViewport.ShowInactiveEnemies = chkVisualEnemyInactive.Checked;
        if (chkEnemyActiveOnly != null && chkEnemyActiveOnly.Checked == chkVisualEnemyInactive.Checked) chkEnemyActiveOnly.Checked = !chkVisualEnemyInactive.Checked;
        RefreshVisualEnemyEntryList();
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

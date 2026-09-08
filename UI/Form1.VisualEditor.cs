using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using System.ComponentModel;
using RE4_PS2_MOD_WORKSPACE.Core.Collision;
using RE4_PS2_MOD_WORKSPACE.Core.Lighting;
using RE4_PS2_MOD_WORKSPACE.Core.Effects;

namespace RE4_PS2_MOD_WORKSPACE;

public partial class Form1
{
    private void ToggleVisualLayersPanel(){settings.VisualLayersPanelCollapsed=!settings.VisualLayersPanelCollapsed;ApplyVisualSidePanelState();if(!restoringSession)SaveSettings();}
    private void ToggleVisualPropertiesPanel(){settings.VisualPropertiesPanelCollapsed=!settings.VisualPropertiesPanelCollapsed;ApplyVisualSidePanelState();if(!restoringSession)SaveSettings();}
    private void ApplyVisualSidePanelState()
    {
        if(splitVisualWorkspace==null||tblVisualWorkspaceRight==null)return;
        splitVisualWorkspace.Panel1Collapsed=settings.VisualLayersPanelCollapsed;
        pnlVisualPropertiesHost.Visible=!settings.VisualPropertiesPanelCollapsed;
        tblVisualWorkspaceRight.ColumnStyles[1].Width=settings.VisualPropertiesPanelCollapsed?0F:(tblVisualWorkspaceRight.ClientSize.Width<850?260F:310F);
        if(btnVisualLayersToggle!=null){btnVisualLayersToggle.Text=settings.VisualLayersPanelCollapsed?"MOSTRAR LAYERS":"OCULTAR LAYERS";btnVisualLayersToggle.AccessibleName=settings.VisualLayersPanelCollapsed?"Mostrar Layers e abas":"Recolher Layers e abas";}
        if(btnVisualPropertiesToggle!=null){btnVisualPropertiesToggle.Text=settings.VisualPropertiesPanelCollapsed?"MOSTRAR PROPS":"OCULTAR PROPS";btnVisualPropertiesToggle.AccessibleName=settings.VisualPropertiesPanelCollapsed?"Mostrar Propriedades":"Recolher Propriedades";}
        splitVisualWorkspace.PerformLayout();
        visualViewport?.Invalidate();
    }

    private bool syncingVisualDat;
    private bool loadingVisualEditor;
    private bool visualAevModified;
    private bool syncingVisualAevFilter;
    private bool syncingVisualEnemyFilter;
    private bool syncingVisualEnemyModelParts;
    private bool syncingVisualEnemyAttachment;
    private System.Windows.Forms.Timer? visualEnemyIdleTimer;
    private float visualEnemyIdleFrame;
    private FcvAnimation? visualEnemyDebugAnimation;
    private int visualEnemyDebugFrameCount;
    private bool visualEnemyDebugStabilizeFeet=true;
    private bool syncingVisualEnemyAnimation;
    private bool visualCollisionModified;
    private bool visualCollisionMoveWholeFace,visualCollisionMoveFaceSide,visualCollisionMoveObject;
    private bool syncingVisualLitFile;
    private bool syncingVisualRtpSelection;
    private bool syncingVisualCamFrameSelection;
    private int[] selectedVisualCamFrames=Array.Empty<int>();
    private CamPreviewForm? visualCamPreview;

    private sealed class CamFrameBatchEditor
    {
        private readonly IReadOnlyList<CamFrame> frames;private readonly Action<Action<CamFrame>> apply;
        public CamFrameBatchEditor(IReadOnlyList<CamFrame> frames,Action<Action<CamFrame>> apply){this.frames=frames;this.apply=apply;}
        [Category("Lens"),DisplayName("FOV — aplicar a todos"),Description("Define o mesmo campo de visão em todos os frames selecionados.")]public float Fov{get=>frames[0].Fov;set=>apply(f=>f.Fov=value);}
        [Category("Lens"),DisplayName("Roll — aplicar a todos"),Description("Define o mesmo Roll em todos os frames selecionados.")]public float Roll{get=>frames[0].Roll;set=>apply(f=>f.Roll=value);}
        [Category("Animation"),DisplayName("Time frame — aplicar a todos"),Description("Define o mesmo tempo em todos os frames selecionados.")]public ushort Time{get=>frames[0].Time;set=>apply(f=>f.Time=value);}
        [Category("Mover grupo"),DisplayName("Deslocar X"),Description("Soma este deslocamento à posição e ao target, preservando o enquadramento relativo.")]public float MoveX{get=>0;set{if(value!=0)apply(f=>{var d=new System.Numerics.Vector3(value,0,0);f.Position+=d;f.Target+=d;});}}
        [Category("Mover grupo"),DisplayName("Deslocar Y"),Description("Soma este deslocamento à posição e ao target, preservando o enquadramento relativo.")]public float MoveY{get=>0;set{if(value!=0)apply(f=>{var d=new System.Numerics.Vector3(0,value,0);f.Position+=d;f.Target+=d;});}}
        [Category("Mover grupo"),DisplayName("Deslocar Z"),Description("Soma este deslocamento à posição e ao target, preservando o enquadramento relativo.")]public float MoveZ{get=>0;set{if(value!=0)apply(f=>{var d=new System.Numerics.Vector3(0,0,value);f.Position+=d;f.Target+=d;});}}
    }

    private sealed class VisualEnemyAnimationItem
    {
        public string? Path { get; init; }
        public string Name { get; init; } = string.Empty;
        public int FrameCount { get; init; }
        public bool UsesSeq { get; init; }
        public bool StabilizeFeet { get; init; }
        public override string ToString() => $"{Name} • {FrameCount}f{(UsesSeq ? " • SEQ" : "")}";
    }

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
    private sealed record VisualCollisionMeshItem(EsatKind? Kind, int MeshIndex, string Name)
    {
        public override string ToString() => Name;
    }
    private sealed record VisualLitFileItem(string Path){public override string ToString()=>System.IO.Path.GetFileName(Path);}
    private sealed record VisualEnemyAttachmentBoneItem(int Index, byte Id, byte ParentId)
    {
        public override string ToString() => $"#{Index:00} • Bone 0x{Id:X2} • Parent {(ParentId==0xFF?"ROOT":$"0x{ParentId:X2}")}";
    }
    private string? visualSmdPath;
    private string? visualAevPath;
    private string? visualEtmPath;
    private string? visualEtsPath;
    private string? visualSatPath;
    private string? visualEatPath;
    private string? visualRtpPath;
    private string? visualCamPath;
    private string? visualLitPath;
    private LitScene? visualLitScene;
    private string? visualEffPath;
    private Ps2EffFile? visualEffScene;
    private EffTextureCatalog? visualEffTextures;
    private EffTextureResource? visualEffTexture;
    private bool syncingVisualEffFrame;
    private EtmCatalog? visualEtmCatalog;
    private EtsScene? visualEtsScene;

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
        state.VisualFlySpeed = visualViewport.FlySpeed;
        if (trkVisualLookSpeed != null) state.VisualLookSpeedSlider = trkVisualLookSpeed.Value;

        if (!restoringSession) SaveProject();
    }

    private void RestoreVisualCameraState(string datName)
    {
        DatProjectState? state = GetDatState(datName, false);
        if (state == null) return;

        if (visualViewport != null)
        {
            visualViewport.MovementSpeedMultiplier = 1f;
            if (state.VisualFlySpeed > 0f) visualViewport.FlySpeed = state.VisualFlySpeed;
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

        if (visualAevModified || visualCollisionModified || visualViewport?.Scene?.IsModified == true || visualEtsScene?.IsModified == true || visualLitScene?.IsModified == true || visualViewport?.RtpScene?.IsModified == true)
        {
            DialogResult answer = MessageBox.Show(
                "O Visual Editor possui alterações não salvas.\n\nTrocar de DAT e descartar essas alterações?",
                "Visual Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
            {
                RefreshVisualDatList();
                return;
            }
        }

        // Preserve a câmera do cenário que está saindo antes de alterar o DAT ativo.
        SaveVisualCameraStateForActiveDat();

        if (!item.DatName.Equals(project.ActiveDatName, StringComparison.OrdinalIgnoreCase))
        {
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
            {
                for (int i=1;i<cmbVisualEnemyLocationFilter.Items.Count;i++)
                    if (cmbVisualEnemyLocationFilter.Items[i] is VisualEnemyLocationFilterItem f && f.StageId==stage && f.RoomId==room) { select=i; break; }

                // A scenario may legitimately have no ESL entries (r11b is one example).
                // Keep its exact location selected instead of falling back to "Todas as fases",
                // which would load and animate every enemy from the global emleon00.esl.
                if (select == 0)
                {
                    cmbVisualEnemyLocationFilter.Items.Add(new VisualEnemyLocationFilterItem(stage, room, $"r{stage:X1}{room:X2} • sem inimigos"));
                    select = cmbVisualEnemyLocationFilter.Items.Count - 1;
                }
            }
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
        visualViewport?.SetEnemyIdleAnimation(enabled, visualEnemyIdleFrame, visualEnemyDebugAnimation, visualEnemyDebugStabilizeFeet);
        if (!enabled)
        {
            visualEnemyIdleTimer?.Stop();
            visualEnemyIdleFrame = 0f;
            visualViewport?.SetEnemyIdleAnimation(false, 0f, visualEnemyDebugAnimation, visualEnemyDebugStabilizeFeet);
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
            if (!visualViewport.EnemiesVisible || !visualViewport.HasVisibleEnemyType(0x12)) return;
            int frameCount = visualEnemyDebugFrameCount;
            if (frameCount <= 0 && visualEnemyModelCache.TryGetValue(0x12, out EnemyModelScene? em12) && em12.IdleAnimation != null) frameCount = em12.IdleAnimation.FrameCount;
            if (frameCount <= 0) { visualEnemyIdleFrame = 0f; visualViewport.SetEnemyIdleAnimation(true, 0f, visualEnemyDebugAnimation, visualEnemyDebugStabilizeFeet); return; }
            visualEnemyIdleFrame += 1f;
            if (visualEnemyIdleFrame >= frameCount) visualEnemyIdleFrame = 0f;
            visualViewport.SetEnemyIdleAnimation(true, visualEnemyIdleFrame, visualEnemyDebugAnimation, visualEnemyDebugStabilizeFeet);
        };
        return timer;
    }

    private void RefreshVisualEnemyAnimationChoices()
    {
        if(cmbVisualEnemyAnimation==null) return;
        string? previous=(cmbVisualEnemyAnimation.SelectedItem as VisualEnemyAnimationItem)?.Path;
        syncingVisualEnemyAnimation=true;
        try
        {
            cmbVisualEnemyAnimation.BeginUpdate();
            cmbVisualEnemyAnimation.Items.Clear();
            string? directory=FindVisualEnemyAnimationDirectory();
            if(directory!=null)
            {
                IEnumerable<string> files=Directory.EnumerateFiles(directory,"em12_*.FCV",SearchOption.TopDirectoryOnly)
                    .OrderBy(GetVisualEnemyAnimationIndex).ThenBy(Path.GetFileName,StringComparer.OrdinalIgnoreCase);
                foreach(string path in files)
                {
                    try
                    {
                        int fcvFrames;
                        using(var stream=File.OpenRead(path)) using(var reader=new BinaryReader(stream)) fcvFrames=reader.ReadUInt16();
                        string? seqPath=Ps2SeqReader.FindFollowingSeq(path);
                        int frames=seqPath!=null ? Ps2SeqReader.ReadFrameCount(seqPath) : fcvFrames;
                        int index=GetVisualEnemyAnimationIndex(path);
                        cmbVisualEnemyAnimation.Items.Add(new VisualEnemyAnimationItem
                        {
                            Path=path,
                            Name=Path.GetFileNameWithoutExtension(path),
                            FrameCount=frames,
                            UsesSeq=seqPath!=null,
                            // 900 is the hand-authored crossed-arms breathing diagnostic. It has
                            // no lower-body tracks by design, so reuse the planted idle stance.
                            StabilizeFeet=index is 1 or 900
                        });
                    }
                    catch(Exception ex) { ExtractLog($"FCV debug: ignorado {Path.GetFileName(path)}: {ex.Message}"); }
                }
            }
            if(cmbVisualEnemyAnimation.Items.Count==0 && visualEnemyModelCache.TryGetValue(0x12,out EnemyModelScene? model) && model.IdleAnimation!=null)
                cmbVisualEnemyAnimation.Items.Add(new VisualEnemyAnimationItem { Name="em12_001 (DAT)", FrameCount=model.IdleAnimation.FrameCount, StabilizeFeet=true });
            cmbVisualEnemyAnimation.EndUpdate();
            cmbVisualEnemyAnimation.Enabled=cmbVisualEnemyAnimation.Items.Count>0;
            int selection=-1;
            for(int i=0;i<cmbVisualEnemyAnimation.Items.Count;i++)
            {
                if(cmbVisualEnemyAnimation.Items[i] is not VisualEnemyAnimationItem item) continue;
                if(previous!=null && string.Equals(item.Path,previous,StringComparison.OrdinalIgnoreCase)) { selection=i; break; }
                if(selection<0 && item.StabilizeFeet) selection=i;
            }
            if(selection<0 && cmbVisualEnemyAnimation.Items.Count>0) selection=0;
            cmbVisualEnemyAnimation.SelectedIndex=selection;
        }
        finally { syncingVisualEnemyAnimation=false; }
        LoadSelectedVisualEnemyAnimation();
    }

    private string? FindVisualEnemyAnimationDirectory()
    {
        var candidates=new List<string>
        {
            Path.Combine(AppContext.BaseDirectory,"Extracted","em12","Content","em12")
        };
        if(!string.IsNullOrWhiteSpace(project.RootPath))
            candidates.Add(Path.Combine(project.RootPath,"Extracted","em12","Content","em12"));
        if(visualEnemyModelCache.TryGetValue(0x12,out EnemyModelScene? model) && !string.IsNullOrWhiteSpace(model.SourcePath))
        {
            string? enemiesDirectory=Path.GetDirectoryName(model.SourcePath);
            string? extractedDirectory=enemiesDirectory==null ? null : Directory.GetParent(enemiesDirectory)?.FullName;
            if(extractedDirectory!=null) candidates.Add(Path.Combine(extractedDirectory,"em12","Content","em12"));
        }
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(directory => Directory.Exists(directory) && Directory.EnumerateFiles(directory,"em12_*.FCV",SearchOption.TopDirectoryOnly).Any());
    }

    private static int GetVisualEnemyAnimationIndex(string path)
    {
        string stem=Path.GetFileNameWithoutExtension(path);
        int separator=stem.LastIndexOf('_');
        return separator>=0 && int.TryParse(stem[(separator+1)..],out int index) ? index : int.MaxValue;
    }

    private void cmbVisualEnemyAnimation_SelectedIndexChanged(object? sender,EventArgs e)
    {
        if(syncingVisualEnemyAnimation) return;
        LoadSelectedVisualEnemyAnimation();
    }

    private void LoadSelectedVisualEnemyAnimation()
    {
        if(cmbVisualEnemyAnimation?.SelectedItem is not VisualEnemyAnimationItem item) return;
        try
        {
            visualEnemyDebugAnimation=item.Path!=null
                ? FcvReader.Read(item.Path)
                : visualEnemyModelCache.TryGetValue(0x12,out EnemyModelScene? model) ? model.IdleAnimation : null;
            visualEnemyDebugFrameCount=item.FrameCount;
            visualEnemyDebugStabilizeFeet=item.StabilizeFeet;
            visualEnemyIdleFrame=0f;
            visualViewport?.SetEnemyIdleAnimation(chkVisualEnemyAnimated?.Checked==true,0f,visualEnemyDebugAnimation,visualEnemyDebugStabilizeFeet);
            ExtractLog($"FCV debug: {item.Name} • {item.FrameCount} frames{(item.UsesSeq ? " pelo SEQ seguinte" : " pelo FCV")}.");
        }
        catch(Exception ex)
        {
            visualEnemyDebugAnimation=null;
            visualEnemyDebugFrameCount=0;
            ExtractLog($"FCV debug: erro ao abrir {item.Name}: {ex.Message}");
        }
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
        enemySceneModified = true;
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

            (string? smd, string? aev, string? etm, string? ets, string? sat, string? eat, string? lit, string? eff, string? rtp, string? cam) = FindVisualFiles(content, item.DatName);
            visualSmdPath = smd;
            visualAevPath = aev;
            visualEtmPath = etm;
            visualEtsPath = ets;
            visualSatPath = sat;
            visualEatPath = eat;
            visualLitPath = lit;
            visualEffPath = eff;
            visualRtpPath = rtp;
            visualCamPath = cam;
            visualAevModified = false;
            visualCollisionModified = false;

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
                WireVisualSmdEvents();
                RefreshVisualSmdEntryList();
                btnVisualSaveSmd.Enabled = true;

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

            if (etm != null && ets != null)
            {
                (visualEtmCatalog, visualEtsScene) = await Task.Run(() => (Ps2EtmReader.Read(etm), Ps2EtsReader.Read(ets)));
                visualViewport.SetEtsScene(visualEtsScene, visualEtmCatalog);
                WireVisualEtsEvents();
                RefreshVisualObjectList();
                btnVisualSaveEts.Enabled = true;
                ExtractLog($"Visual Editor: ETM/ETS carregados: {visualEtmCatalog.Objects.Count} tipos • {visualEtsScene.Entries.Count} instâncias.");
            }
            else
            {
                visualEtmCatalog = null; visualEtsScene = null;
                visualViewport.SetEtsScene(null, null);
                lstVisualObjectEntries.Items.Clear(); btnVisualSaveEts.Enabled = false;
            }

            EsatFile? satCollision = sat == null ? null : await Task.Run(() => Ps2EsatReader.Read(sat, EsatKind.Sat));
            EsatFile? eatCollision = eat == null ? null : await Task.Run(() => Ps2EsatReader.Read(eat, EsatKind.Eat));
            visualViewport.SetCollision(satCollision, eatCollision);
            WireVisualCollisionEvents();
            PopulateVisualCollisionMeshes(satCollision, eatCollision);
            if (satCollision != null) ExtractLog($"Visual Editor: SAT carregado: {Path.GetFileName(sat)} • {satCollision.Meshes.Count:N0} bloco(s) • {satCollision.FaceCount:N0} triângulos.");
            if (eatCollision != null) ExtractLog($"Visual Editor: EAT carregado: {Path.GetFileName(eat)} • {eatCollision.Meshes.Count:N0} bloco(s) • {eatCollision.FaceCount:N0} triângulos.");

            RtpScene? rtpScene = rtp == null ? null : await Task.Run(() => Ps2RtpReader.Read(rtp));
            visualViewport.SetRtpScene(rtpScene);
            WireVisualRtpEvents();
            RefreshVisualRtpNodes();
            if (rtpScene != null) ExtractLog($"Visual Editor: RTP carregado: {Path.GetFileName(rtp)} • {rtpScene.Nodes.Count:N0} waypoints • {rtpScene.Connections.Count:N0} conexões direcionadas.");

            CamScene? camScene=cam==null?null:await Task.Run(()=>Ps2CamReader.Read(cam));visualViewport.SetCamScene(camScene);WireVisualCamEvents();RefreshVisualCamEntries();btnVisualAddCam.Enabled=camScene!=null;
            if(camScene!=null)ExtractLog($"Visual Editor: CAM carregado: {Path.GetFileName(cam)} • {camScene.Count} zonas • {camScene.CameraCount} câmeras.");

            if(lit!=null)
            {
                visualLitScene=await Task.Run(()=>Ps2LitReader.Read(lit));visualViewport.SetLitScene(visualLitScene);RefreshVisualLitGroups();btnVisualSaveLit.Enabled=true;
                PopulateVisualLitFiles(content,lit);
                ExtractLog($"Visual Editor: LIT carregado: {Path.GetFileName(lit)} • {visualLitScene.Groups.Count:N0} grupo(s) • {visualLitScene.LightCount:N0} luz(es).");
            }
            else{visualLitScene=null;visualViewport.SetLitScene(null);cmbVisualLitFile.Items.Clear();cmbVisualLitGroup.Items.Clear();lstVisualLitEntries.Items.Clear();btnVisualSaveLit.Enabled=false;}

            if(eff!=null)
            {
                string[] coreEffs=FindCoreEffFiles();
                (visualEffScene,visualEffTextures)=await Task.Run(()=>(Ps2EffReader.Read(eff),EffTextureCatalog.Build(eff,coreEffs)));visualViewport.EffectsVisible=true;if(clbVisualLayers.Items.Count>6)clbVisualLayers.SetItemChecked(6,true);visualViewport.SetEffScene(visualEffScene);LoadVisualEffViewportTextures();WireVisualEffEvents();RefreshVisualEffEntries();
                ExtractLog($"Visual Editor: EFF carregado: {Path.GetFileName(eff)} • {visualEffScene.Effect0Groups.Count} grupos E0 • {visualEffScene.Effect1Groups.Count} grupos E1 • {visualEffScene.EntryCount} efeitos.");
                ExtractLog($"Visual Editor: diagnóstico EFF • {visualViewport.FireEmitterCount} emissores de fogo • {visualViewport.EffTextureCount} texturas enviadas ao viewport • camada ligada.");
            }
            else{visualEffScene=null;visualEffTextures=null;visualViewport.SetEffScene(null);lstVisualEffEntries.Items.Clear();ClearVisualEffTexture();}

            visualViewport.SetEslScene(selectedEnemyScene);
            PopulateVisualEnemyLocationFilter(item.DatName);

            // The selected tab is restored before its contextual action bars finish
            // being assembled. Reapply it after loading so its tools appear at once.
            tabVisualEntities_SelectedIndexChanged(null, EventArgs.Empty);
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

    private void WireVisualRtpEvents()
    {
        visualViewport.RtpSceneEdited -= visualViewport_RtpSceneEdited;
        visualViewport.RtpSceneEdited += visualViewport_RtpSceneEdited;
        visualViewport.RtpNodeSelected -= visualViewport_RtpNodeSelected;
        visualViewport.RtpNodeSelected += visualViewport_RtpNodeSelected;
    }

    private void visualViewport_RtpSceneEdited()
    {
        RefreshVisualRtpNodes(visualViewport.SelectedRtpNodeIndex);
        UpdateVisualStatus();
        ExtractLog("Visual Editor: RTP modificado • Ctrl+S para salvar.");
    }

    private void visualViewport_RtpNodeSelected(RtpNode? node)
    {
        syncingVisualRtpSelection=true;
        try
        {
            if(node==null)lstVisualRtpNodes.ClearSelected();
            else
            {
                if(tabVisualEntities!=null&&tabVisualRtp!=null)tabVisualEntities.SelectedTab=tabVisualRtp;
                if(node.Index>=0&&node.Index<lstVisualRtpNodes.Items.Count)lstVisualRtpNodes.SelectedIndex=node.Index;
            }
        }
        finally{syncingVisualRtpSelection=false;}
    }

    private void RefreshVisualRtpNodes(int selected=-1)
    {
        if(lstVisualRtpNodes==null)return;syncingVisualRtpSelection=true;lstVisualRtpNodes.BeginUpdate();
        try{lstVisualRtpNodes.Items.Clear();RtpScene? scene=visualViewport?.RtpScene;if(scene!=null)foreach(RtpNode node in scene.Nodes)lstVisualRtpNodes.Items.Add(node);if(selected>=0&&selected<lstVisualRtpNodes.Items.Count)lstVisualRtpNodes.SelectedIndex=selected;}
        finally{lstVisualRtpNodes.EndUpdate();syncingVisualRtpSelection=false;}
    }

    private void lstVisualRtpNodes_SelectedIndexChanged(object? sender,EventArgs e)
    {if(syncingVisualRtpSelection)return;visualViewport?.SelectRtpNode((lstVisualRtpNodes.SelectedItem as RtpNode)?.Index??-1);}
    private void lstVisualRtpNodes_KeyDown(object? sender,KeyEventArgs e)
    {if(e.Control&&e.Shift&&e.KeyCode==Keys.D){CreateChildRtpNode();e.Handled=true;e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.D){DuplicateSelectedRtpNode();e.Handled=true;e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Delete){DeleteSelectedRtpNode();e.Handled=true;e.SuppressKeyPress=true;}}
    private void DuplicateSelectedRtpNode(){visualViewport?.DuplicateSelectedRtpNodeInFront();}
    private void CreateChildRtpNode(){visualViewport?.AddChildToSelectedRtpNodeInFront();}
    private void DeleteSelectedRtpNode(){visualViewport?.DeleteSelectedRtpNode();}

    private static (string? Smd, string? Aev, string? Etm, string? Ets, string? Sat, string? Eat, string? Lit, string? Eff, string? Rtp, string? Cam) FindVisualFiles(string content, string datName)
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

        string? etm = Directory.GetFiles(content, "*.ETM", SearchOption.AllDirectories).OrderBy(x => x).FirstOrDefault();
        string? ets = Directory.GetFiles(content, "*.ETS", SearchOption.AllDirectories).OrderBy(x => x).FirstOrDefault();
        string? sat = Directory.GetFiles(content, "*.SAT", SearchOption.AllDirectories).OrderBy(x => x).FirstOrDefault();
        string? eat = Directory.GetFiles(content, "*.EAT", SearchOption.AllDirectories).OrderBy(x => x).FirstOrDefault();
        string? lit = Directory.GetFiles(content, "*.LIT", SearchOption.AllDirectories).OrderBy(x => x).FirstOrDefault();
        string[] effs=Directory.GetFiles(content,"*.EFF",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToArray();
        string? eff=effs.FirstOrDefault(x=>Path.GetFileNameWithoutExtension(x).Equals(preferredBase,StringComparison.OrdinalIgnoreCase))??effs.FirstOrDefault(x=>Path.GetFileNameWithoutExtension(x).Equals(datBase,StringComparison.OrdinalIgnoreCase))??effs.FirstOrDefault();
        string? rtp = Directory.GetFiles(content, "*.RTP", SearchOption.AllDirectories).OrderBy(x => x).FirstOrDefault();
        string[] cams=Directory.GetFiles(content,"*.CAM",SearchOption.AllDirectories).Where(IsSupportedCamFile).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToArray();
        string? cam=cams.FirstOrDefault(x=>Path.GetFileNameWithoutExtension(x).Equals(preferredBase,StringComparison.OrdinalIgnoreCase))??cams.FirstOrDefault(x=>Path.GetFileNameWithoutExtension(x).StartsWith(datBase+"_",StringComparison.OrdinalIgnoreCase))??cams.FirstOrDefault();
        return (smd, aev, etm, ets, sat, eat, lit, eff, rtp, cam);
    }

    private static bool IsSupportedCamFile(string path)
    {
        try{using FileStream stream=File.OpenRead(path);Span<byte> magic=stackalloc byte[4];return stream.Read(magic)==4&&magic[0]=='B'&&magic[1]=='4'&&magic[2]=='0';}
        catch{return false;}
    }

    private void ClearVisualEditor(string status)
    {
        visualSmdPath = null;
        visualAevPath = null;
        visualEtmPath = null; visualEtsPath = null; visualEtmCatalog = null; visualEtsScene = null;
        visualSatPath = null; visualEatPath = null; visualRtpPath = null; visualCamPath=null;
        visualLitPath=null;visualLitScene=null;
        visualEffPath=null;visualEffScene=null;visualEffTextures=null;
        visualAevModified = false;
        visualCollisionModified = false;
        visualViewport?.SetScene(null);
        visualViewport?.SetAevScene(null);
        visualViewport?.SetEslScene(null);
        visualViewport?.SetEtsScene(null, null);
        visualViewport?.SetCollision(null, null);
        visualViewport?.SetLitScene(null);
        visualViewport?.SetEffScene(null);
        visualViewport?.SetRtpScene(null);
        visualViewport?.SetCamScene(null);
        lstVisualRtpNodes?.Items.Clear();
        lstVisualCamEntries?.Items.Clear();lstVisualCamParts?.Items.Clear();
        cmbVisualCollisionMesh?.Items.Clear();
        visualViewport?.SetTextureSource(null);
        lstVisualAevEntries?.Items.Clear();
        lstVisualEnemyEntries?.Items.Clear();
        lstVisualObjectEntries?.Items.Clear();
        lstVisualSmdEntries?.Items.Clear();
        lstVisualLitEntries?.Items.Clear();cmbVisualLitGroup?.Items.Clear();cmbVisualLitFile?.Items.Clear();
        lstVisualEffEntries?.Items.Clear();
        ClearVisualEffTexture();
        RefreshVisualEnemyModelParts(null);
        if (cmbVisualAevTypeFilter != null) cmbVisualAevTypeFilter.Items.Clear();
        if (pgVisualProperties != null) pgVisualProperties.SelectedObject = null;
        if (btnVisualSaveAev != null) btnVisualSaveAev.Enabled = false;
        if (btnVisualSaveEsl != null) btnVisualSaveEsl.Enabled = selectedEnemyScene != null && !string.IsNullOrWhiteSpace(currentEnemyEslPath);
        if (btnVisualSaveEts != null) btnVisualSaveEts.Enabled = false;
        if (btnVisualSaveSmd != null) btnVisualSaveSmd.Enabled = false;
        if (btnVisualSaveLit != null) btnVisualSaveLit.Enabled = false;
        if (btnVisualAddCam != null) btnVisualAddCam.Enabled = false;
        lblVisualStage.Text = status;
        UpdateVisualStatus();
    }

    private void btnVisualSaveEsl_Click(object? sender, EventArgs e) => SaveCurrentEnemyEsl();

    private void RefreshVisualLitGroups()
    {
        cmbVisualLitGroup.BeginUpdate();cmbVisualLitGroup.Items.Clear();
        if(visualLitScene!=null)foreach(LitGroup group in visualLitScene.Groups)cmbVisualLitGroup.Items.Add(group);
        cmbVisualLitGroup.EndUpdate();if(cmbVisualLitGroup.Items.Count>0)cmbVisualLitGroup.SelectedIndex=0;else lstVisualLitEntries.Items.Clear();
    }

    private void PopulateVisualLitFiles(string content,string selected)
    {
        syncingVisualLitFile=true;try{cmbVisualLitFile.BeginUpdate();cmbVisualLitFile.Items.Clear();foreach(string path in Directory.GetFiles(content,"*.LIT",SearchOption.AllDirectories).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase))cmbVisualLitFile.Items.Add(new VisualLitFileItem(path));cmbVisualLitFile.EndUpdate();for(int i=0;i<cmbVisualLitFile.Items.Count;i++)if(cmbVisualLitFile.Items[i] is VisualLitFileItem item&&item.Path.Equals(selected,StringComparison.OrdinalIgnoreCase)){cmbVisualLitFile.SelectedIndex=i;break;}}finally{syncingVisualLitFile=false;}
    }

    private void cmbVisualLitFile_SelectedIndexChanged(object? sender,EventArgs e)
    {
        if(syncingVisualLitFile||cmbVisualLitFile.SelectedItem is not VisualLitFileItem item)return;
        if(visualLitScene?.IsModified==true&&MessageBox.Show(this,"Descartar alterações não salvas do LIT atual?","Trocar LIT",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes){syncingVisualLitFile=true;try{for(int i=0;i<cmbVisualLitFile.Items.Count;i++)if(cmbVisualLitFile.Items[i] is VisualLitFileItem f&&f.Path.Equals(visualLitPath,StringComparison.OrdinalIgnoreCase)){cmbVisualLitFile.SelectedIndex=i;break;}}finally{syncingVisualLitFile=false;}return;}
        try{visualLitPath=item.Path;visualLitScene=Ps2LitReader.Read(item.Path);visualViewport.SetLitScene(visualLitScene);RefreshVisualLitGroups();btnVisualSaveLit.Enabled=true;btnVisualSaveLit.Text="SAVE LIT";UpdateVisualStatus();}
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Abrir LIT",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }

    private void cmbVisualLitGroup_SelectedIndexChanged(object? sender,EventArgs e)
    {
        lstVisualLitEntries.BeginUpdate();lstVisualLitEntries.Items.Clear();
        if(cmbVisualLitGroup.SelectedItem is LitGroup group){foreach(LitLight light in group.Lights)lstVisualLitEntries.Items.Add(light);pgVisualProperties.SelectedObject=group;lblVisualPropertiesTitle.Text=$"PROPERTIES • LIT GROUP {group.SlotIndex:00}";}
        lstVisualLitEntries.EndUpdate();visualViewport?.SelectLitLight((cmbVisualLitGroup.SelectedItem as LitGroup)?.SlotIndex??-1,-1);
    }

    private void lstVisualLitEntries_SelectedIndexChanged(object? sender,EventArgs e)
    {
        if(cmbVisualLitGroup.SelectedItem is not LitGroup group||lstVisualLitEntries.SelectedItem is not LitLight light)return;
        pgVisualProperties.SelectedObject=light;lblVisualPropertiesTitle.Text=$"PROPERTIES • LIT G{group.SlotIndex:00} L{light.Index:00}";visualViewport?.SelectLitLight(group.SlotIndex,light.Index);
    }

    private void btnVisualSaveLit_Click(object? sender,EventArgs e)=>SaveVisualLit();
    private bool SaveVisualLit()
    {
        if(visualLitScene==null||string.IsNullOrWhiteSpace(visualLitPath))return false;
        try
        {
            string backup=GetVisualAevBackupPath(visualLitPath);Directory.CreateDirectory(Path.GetDirectoryName(backup)!);if(!File.Exists(backup))File.Copy(visualLitPath,backup);
            Ps2LitWriter.Write(visualLitScene,visualLitPath);btnVisualSaveLit.Text="SAVE LIT";ExtractLog($"Visual Editor: LIT salvo: {Path.GetFileName(visualLitPath)}.");UpdateVisualStatus();return true;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Salvar LIT",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog("Visual Editor: erro ao salvar LIT: "+ex.Message);return false;}
    }

    private void WireVisualEffEvents(){visualViewport.EffEntryClicked-=visualViewport_EffEntryClicked;visualViewport.EffEntryClicked+=visualViewport_EffEntryClicked;}
    private string[] FindCoreEffFiles()
    {
        var roots=new List<string>();
        if(!string.IsNullOrWhiteSpace(project.RootPath))roots.Add(Path.Combine(project.RootPath,"Extracted","Core","Content"));
        roots.Add(Path.Combine(AppContext.BaseDirectory,"Extracted","Core","Content"));
        return roots.Where(Directory.Exists).SelectMany(x=>Directory.GetFiles(x,"*.EFF",SearchOption.AllDirectories)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    private void RefreshVisualEffEntries(EffEntry? selected=null)
    {
        lstVisualEffEntries.BeginUpdate();try{lstVisualEffEntries.Items.Clear();if(visualEffScene!=null)foreach(EffEntry entry in visualEffScene.Entries)lstVisualEffEntries.Items.Add(entry);}finally{lstVisualEffEntries.EndUpdate();}
        if(selected!=null)for(int i=0;i<lstVisualEffEntries.Items.Count;i++)if(ReferenceEquals(lstVisualEffEntries.Items[i],selected)){lstVisualEffEntries.SelectedIndex=i;lstVisualEffEntries.TopIndex=Math.Max(0,i-4);break;}
    }
    private void lstVisualEffEntries_SelectedIndexChanged(object? sender,EventArgs e)
    {
        EffEntry? entry=lstVisualEffEntries.SelectedItem as EffEntry;visualViewport?.SelectEffEntry(entry);pgVisualProperties.SelectedObject=entry;
        lblVisualPropertiesTitle.Text=entry==null?"PROPERTIES • SELECTION":$"PROPERTIES • EFF E{entry.EffectTable} G{entry.GroupIndex:00} #{entry.EntryIndex:00}";
        ShowVisualEffTexture(entry);
    }
    private void visualViewport_EffEntryClicked(EffEntry? entry)
    {
        if(entry==null)return;if(tabVisualEntities!=null)tabVisualEntities.SelectedIndex=6;RefreshVisualEffEntries(entry);pgVisualProperties.SelectedObject=entry;
        lblVisualPropertiesTitle.Text=$"PROPERTIES • EFF E{entry.EffectTable} G{entry.GroupIndex:00} #{entry.EntryIndex:00}";
    }
    private async void btnVisualExtractCore_Click(object? sender,EventArgs e)
    {
        if(!RequireWorkspace())return;
        string? isoPath=!string.IsNullOrWhiteSpace(project.IsoPath)&&File.Exists(project.IsoPath)?project.IsoPath:Clean(txtIsoPath.Text);
        if(string.IsNullOrWhiteSpace(isoPath)||!File.Exists(isoPath)){MessageBox.Show("Selecione uma ISO base válida na tela Projeto primeiro.","Extrair Core.dat",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        try
        {
            btnVisualExtractCore.Enabled=false;btnVisualExtractCore.Text="EXTRAINDO...";UseWaitCursor=true;
            string root=Path.Combine(project.RootPath!,"Extracted","Core"),originalDir=Path.Combine(root,"OriginalDAT"),contentDir=Path.Combine(root,"Content"),datPath=Path.Combine(originalDir,"Core.dat");
            ExtractLog("Visual Editor: procurando Core.dat no BIO4DAT.AFS da ISO...");
            var result=await Task.Run(async()=>
            {
                AfsImage afs=AfsService.OpenDefaultAfsFromIso(isoPath,"BIO4DAT.AFS");
                AfsEntry entry=AfsService.FindFirstValidEntryByName(afs,"Core.dat")??throw new FileNotFoundException("Core.dat não foi encontrado no BIO4DAT.AFS desta ISO.");
                Directory.CreateDirectory(originalDir);AfsService.ExtractEntry(afs,entry,datPath);
                return await NativeDatService.ExtractAsync(datPath,contentDir);
            });
            ExtractLog($"Parser DAT nativo: {result.EntryCount:N0} entrada(s) extraída(s).");
            string[] coreEffs=FindCoreEffFiles();if(coreEffs.Length==0)throw new InvalidDataException("Core.dat foi extraído, mas nenhum Core_*.EFF foi encontrado em Content.");
            if(!string.IsNullOrWhiteSpace(visualEffPath)&&File.Exists(visualEffPath))
            {
                visualEffTextures=await Task.Run(()=>EffTextureCatalog.Build(visualEffPath,coreEffs));LoadVisualEffViewportTextures();ShowVisualEffTexture(lstVisualEffEntries.SelectedItem as EffEntry);
            }
            ExtractLog($"Visual Editor: Core.dat extraído • {coreEffs.Length} arquivo(s) EFF encontrado(s) • texturas recarregadas.");
            MessageBox.Show($"Core.dat foi extraído da ISO e as texturas dos efeitos foram recarregadas.\n\n{contentDir}","Core.dat pronto",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        catch(Exception ex){ExtractLog("Visual Editor: erro ao extrair Core.dat: "+ex.Message);MessageBox.Show(ex.Message,"Erro ao extrair Core.dat",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        finally{UseWaitCursor=false;btnVisualExtractCore.Text="EXTRAIR CORE.DAT";btnVisualExtractCore.Enabled=true;}
    }
    private void ShowVisualEffTexture(EffEntry? entry)
    {
        ClearVisualEffTexture();if(entry==null||visualEffTextures==null)return;
        IReadOnlyList<EffTextureResource> matches=visualEffTextures.Resolve(entry.ResourceId);
        if(matches.Count==0){lblVisualEffTexture.Text=$"ID {entry.ResourceId:X2} • textura não encontrada";return;}
        visualEffTexture=matches[0];syncingVisualEffFrame=true;try{nudVisualEffFrame.Minimum=0;nudVisualEffFrame.Maximum=Math.Max(0,visualEffTexture.FrameCount-1);nudVisualEffFrame.Value=0;nudVisualEffFrame.Enabled=visualEffTexture.FrameCount>1;}finally{syncingVisualEffFrame=false;}
        DecodeVisualEffFrame();string origin=Path.GetFullPath(visualEffTexture.SourceEffPath).Equals(Path.GetFullPath(visualEffPath!),StringComparison.OrdinalIgnoreCase)?"LOCAL":"CORE";
        lblVisualEffTexture.Text=$"ID {entry.ResourceId:X2} • {origin} {visualEffTexture.SourceName} • TPL {visualEffTexture.PackageIndex} • {visualEffTexture.FrameCount}f"+(matches.Count>1?$" • {matches.Count} fontes":"");
    }
    private void LoadVisualEffViewportTextures()
    {
        var decoded=new Dictionary<byte,Bitmap>();
        var animations=new Dictionary<byte,List<Bitmap>>();
        if(visualEffScene!=null&&visualEffTextures!=null)foreach(byte id in visualEffScene.Entries.Select(x=>x.ResourceId).Distinct())
        {
            EffTextureResource? resource=visualEffTextures.Resolve(id).FirstOrDefault();if(resource==null)continue;
            try{decoded[id]=resource.DecodeFrame(0);if(resource.FrameCount>1){var frames=new List<Bitmap>(resource.FrameCount);for(int frame=0;frame<resource.FrameCount;frame++)frames.Add(resource.DecodeFrame(frame));animations[id]=frames;}}catch{ }
        }
        visualViewport?.SetEffTextures(decoded);foreach(var pair in animations)visualViewport?.SetEffAnimation(pair.Key,pair.Value);foreach(Bitmap bitmap in decoded.Values)bitmap.Dispose();foreach(List<Bitmap> frames in animations.Values)foreach(Bitmap bitmap in frames)bitmap.Dispose();
    }
    private void DecodeVisualEffFrame()
    {
        if(visualEffTexture==null)return;try{Bitmap image=visualEffTexture.DecodeFrame((int)nudVisualEffFrame.Value);Image? old=picVisualEffTexture.Image;picVisualEffTexture.Image=image;visualViewport?.SetEffTexture(image);old?.Dispose();}catch(Exception ex){visualViewport?.SetEffTexture(null);lblVisualEffTexture.Text=$"Falha ao decodificar: {ex.Message}";}
    }
    private void nudVisualEffFrame_ValueChanged(object? sender,EventArgs e){if(!syncingVisualEffFrame)DecodeVisualEffFrame();}
    private void ClearVisualEffTexture()
    {
        visualEffTexture=null;visualViewport?.SetEffTexture(null);if(picVisualEffTexture!=null){Image? old=picVisualEffTexture.Image;picVisualEffTexture.Image=null;old?.Dispose();}
        if(lblVisualEffTexture!=null)lblVisualEffTexture.Text="Selecione um efeito";
        if(nudVisualEffFrame!=null){syncingVisualEffFrame=true;try{nudVisualEffFrame.Enabled=false;nudVisualEffFrame.Minimum=0;nudVisualEffFrame.Maximum=0;nudVisualEffFrame.Value=0;}finally{syncingVisualEffFrame=false;}}
    }
    private bool SaveVisualEff()
    {
        if(visualEffScene==null||string.IsNullOrWhiteSpace(visualEffPath)||!visualEffScene.IsModified)return false;
        try
        {
            string backup=GetVisualAevBackupPath(visualEffPath);Ps2EffWriter.Save(visualEffScene,backup);
            EffEntry? selected=lstVisualEffEntries.SelectedItem as EffEntry;int? offset=selected?.FileOffset;
            visualEffScene=Ps2EffReader.Read(visualEffPath);visualViewport.SetEffScene(visualEffScene);WireVisualEffEvents();
            RefreshVisualEffEntries(offset.HasValue?visualEffScene.Entries.FirstOrDefault(x=>x.FileOffset==offset.Value):null);
            ExtractLog($"Visual Editor: EFF salvo e validado: {Path.GetFileName(visualEffPath)} • backup preservado.");UpdateVisualStatus();return true;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Salvar EFF",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog("Visual Editor: erro ao salvar EFF: "+ex.Message);return false;}
    }
    private async void btnVisualSaveAev_Click(object? sender, EventArgs e) => await SaveVisualAevAsync();
    private void btnVisualSaveEts_Click(object? sender, EventArgs e) => SaveVisualEts();

    private bool SaveVisualEts()
    {
        if (visualEtsScene == null || string.IsNullOrWhiteSpace(visualEtsPath)) return false;
        try { Ps2EtsWriter.Write(visualEtsScene, visualEtsPath); btnVisualSaveEts.Text = "SAVE ETS"; ExtractLog($"Visual Editor: ETS salvo: {Path.GetFileName(visualEtsPath)}."); return true; }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Salvar ETS", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
    }

    private bool SaveVisualRtp()
    {
        RtpScene? scene=visualViewport?.RtpScene;
        if(scene?.IsModified!=true||string.IsNullOrWhiteSpace(visualRtpPath))return false;
        try
        {
            bool backup=Ps2RtpWriter.Save(scene,GetVisualAevBackupPath(visualRtpPath));
            RtpScene reloaded=Ps2RtpReader.Read(visualRtpPath);visualViewport!.SetRtpScene(reloaded);WireVisualRtpEvents();
            ExtractLog($"Visual Editor: RTP salvo e validado: {Path.GetFileName(visualRtpPath)}."+(backup?" • backup inicial preservado.":""));UpdateVisualStatus();return true;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Salvar RTP",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog("Visual Editor: erro ao salvar RTP: "+ex.Message);return false;}
    }

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
        if (visualViewport?.Scene?.IsModified == true && !string.IsNullOrWhiteSpace(visualSmdPath)) savedAnything |= SaveVisualSmd();
        if (visualAevModified && visualViewport?.AevScene != null && !string.IsNullOrWhiteSpace(visualAevPath)) savedAnything |= await SaveVisualAevAsync();
        if (enemySceneModified && selectedEnemyScene != null && !string.IsNullOrWhiteSpace(currentEnemyEslPath)) savedAnything |= SaveCurrentEnemyEsl(false);
        if (visualEtsScene?.IsModified == true) savedAnything |= SaveVisualEts();
        if (visualLitScene?.IsModified == true) savedAnything |= SaveVisualLit();
        if (visualEffScene?.IsModified == true) savedAnything |= SaveVisualEff();
        if (visualCollisionModified) savedAnything |= SaveVisualCollision();
        if (visualViewport?.RtpScene?.IsModified == true) savedAnything |= SaveVisualRtp();
        if (visualViewport?.CamScene?.IsModified == true) savedAnything |= SaveVisualCam();
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
            if(updateUi) { lblVisualStatus.UseMnemonic = false; lblVisualStatus.Text = "Salvo • pronto para Build & Test"; }
            ExtractLog("Visual Editor: dados carregados salvos (SMD/AEV/ESL/ETS/LIT/EFF/RTP/CAM). O Build & Test reinserirá os arquivos automaticamente na ISO de Build.");
        }
        UpdateTopVisualSaveState();
        return savedAnything;
    }

    private async void btnTopSaveScenario_Click(object? sender, EventArgs e)
    {
        if (loadingVisualEditor || autoSaveRunning) return;
        btnTopSaveScenario.Enabled = false;
        try
        {
            lblVisualStatus.Text = "Salvando cenário...";
            SaveVisualCameraStateForActiveDat();
            await SaveVisualEditorAllAsync();
        }
        finally
        {
            btnTopSaveScenario.Enabled = true;
            UpdateVisualStatus();
            UpdateTopVisualSaveState();
        }
    }

    private bool HasUnsavedVisualChanges() =>
        visualViewport?.Scene?.IsModified == true ||
        visualAevModified || enemySceneModified || visualCollisionModified ||
        visualEtsScene?.IsModified == true || visualLitScene?.IsModified == true ||
        visualEffScene?.IsModified == true || visualViewport?.RtpScene?.IsModified == true ||
        visualViewport?.CamScene?.IsModified == true;

    private void UpdateTopVisualSaveState()
    {
        if (lblTopVisualModified == null || btnTopSaveScenario == null) return;
        bool visualOpen = pnlVisualEditor?.Visible == true;
        bool modified = visualOpen && HasUnsavedVisualChanges();
        lblTopVisualModified.Visible = modified;
        btnTopSaveScenario.BackColor = modified ? Accent : Surface2;
    }

    private async void Form1_GlobalKeyDown(object? sender, KeyEventArgs e)
    {
        bool visualOpen = pnlVisualEditor != null && pnlVisualEditor.Visible;
        bool enemiesOpen = pnlEnemies != null && pnlEnemies.Visible;
        bool messagesOpen = pnlMessages != null && pnlMessages.Visible;
        bool objectTab = visualOpen && tabVisualEntities?.SelectedIndex == 2;
        bool smdTab = visualOpen && tabVisualEntities?.SelectedIndex == 4;
        bool camTab = visualOpen && tabVisualEntities?.SelectedIndex == 8;
        if(camTab&&!e.Control&&e.KeyCode==Keys.F){visualViewport?.FocusCam(lstVisualCamEntries.SelectedItem as CamEntry);e.Handled=true;e.SuppressKeyPress=true;return;}
        if(camTab&&!e.Control&&e.KeyCode is Keys.D1 or Keys.NumPad1 or Keys.D2 or Keys.NumPad2 or Keys.D3 or Keys.NumPad3 or Keys.D4 or Keys.NumPad4 or Keys.D5 or Keys.NumPad5){SetCamGizmoMode(e.KeyCode is Keys.D1 or Keys.NumPad1?CamGizmoMode.Move:e.KeyCode is Keys.D2 or Keys.NumPad2?CamGizmoMode.Scale:e.KeyCode is Keys.D3 or Keys.NumPad3?CamGizmoMode.Vertex:e.KeyCode is Keys.D4 or Keys.NumPad4?CamGizmoMode.Face:CamGizmoMode.Frame);e.Handled=true;e.SuppressKeyPress=true;return;}
        if(smdTab&&!e.Control&&e.KeyCode is Keys.D1 or Keys.NumPad1 or Keys.D2 or Keys.NumPad2 or Keys.D3 or Keys.NumPad3){SmdGizmoMode mode=e.KeyCode is Keys.D1 or Keys.NumPad1?SmdGizmoMode.Move:e.KeyCode is Keys.D2 or Keys.NumPad2?SmdGizmoMode.Rotate:SmdGizmoMode.Scale;if(chkVisualSmdEditMode.Checked)mode=SmdGizmoMode.Move;SetSmdGizmoMode(mode);e.Handled=true;e.SuppressKeyPress=true;return;}
        if(objectTab && !e.Control && e.KeyCode==Keys.F){e.Handled=true;e.SuppressKeyPress=true;visualViewport?.FocusEts(lstVisualObjectEntries.SelectedItem as EtsEntry);return;}
        if(objectTab && !e.Control && e.KeyCode==Keys.G){e.Handled=true;e.SuppressKeyPress=true;SetEtsGizmoMode(EtsGizmoMode.Move);return;}
        if(objectTab && !e.Control && e.KeyCode==Keys.R){e.Handled=true;e.SuppressKeyPress=true;SetEtsGizmoMode(EtsGizmoMode.Rotate);return;}
        if(visualOpen && !e.Control && e.KeyCode==Keys.F){e.Handled=true;e.SuppressKeyPress=true;FocusSelectedEnemy();return;}
        if(visualOpen && !e.Control && e.KeyCode==Keys.G){e.Handled=true;e.SuppressKeyPress=true;SetEnemyGizmoMode(EnemyGizmoMode.Move);return;}
        if(visualOpen && !e.Control && e.KeyCode==Keys.R){e.Handled=true;e.SuppressKeyPress=true;SetEnemyGizmoMode(EnemyGizmoMode.Rotate);return;}
        if (!e.Control) return;
        if (messagesOpen && e.KeyCode == Keys.F)
        {
            e.Handled = true; e.SuppressKeyPress = true;
            txtMessageSearch.Focus(); txtMessageSearch.SelectAll();
            return;
        }
        if (messagesOpen && e.KeyCode == Keys.S)
        {
            e.Handled = true; e.SuppressKeyPress = true;
            btnMessageSave_Click(null, EventArgs.Empty);
            return;
        }
        if (visualOpen && e.KeyCode == Keys.S)
        {
            e.Handled = true; e.SuppressKeyPress = true;
            await SaveVisualEditorAllAsync();
            return;
        }
        if ((visualOpen || enemiesOpen) && e.KeyCode == Keys.Z)
        {
            if(camTab&&e.Shift&&visualViewport?.RedoCamEdit()==true){RefreshVisualCamEntries(Math.Max(0,(lstVisualCamEntries.SelectedItem as CamEntry)?.FileOrder??0));pgVisualProperties.Refresh();e.Handled=true;e.SuppressKeyPress=true;return;}
            if(camTab&&visualViewport?.UndoCamEdit()==true){RefreshVisualCamEntries(Math.Max(0,(lstVisualCamEntries.SelectedItem as CamEntry)?.FileOrder??0));pgVisualProperties.Refresh();e.Handled=true;e.SuppressKeyPress=true;return;}
            if(visualOpen && tabVisualEntities?.SelectedIndex==4 && visualViewport?.UndoSmdEdit()==true){pgVisualProperties.Refresh();lstVisualSmdEntries.Refresh();btnVisualSaveSmd.Text="SAVE SMD *";e.Handled=true;e.SuppressKeyPress=true;return;}
            if(visualOpen && tabVisualEntities?.SelectedIndex==3 && visualViewport?.UndoCollisionEdit()==true){visualCollisionModified=true;btnVisualCollisionSave.Enabled=true;pgVisualProperties.Refresh();e.Handled=true;e.SuppressKeyPress=true;return;}
            if (objectTab && visualViewport?.UndoEtsEdit() == true) { MarkEtsModified(); pgVisualProperties.Refresh(); lstVisualObjectEntries.Refresh(); e.Handled=true;e.SuppressKeyPress=true;return; }
            bool enemyTab = enemiesOpen || (tabVisualEntities != null && tabVisualEntities.SelectedIndex == 1);
            bool undone = enemyTab ? visualViewport?.UndoEnemyEdit() == true : visualViewport?.UndoAevEdit() == true;
            if (!undone && visualOpen) undone = enemyTab ? visualViewport?.UndoAevEdit() == true : visualViewport?.UndoEnemyEdit() == true;
            if (undone) { e.Handled = true; e.SuppressKeyPress = true; }
        }
        if(camTab&&e.KeyCode==Keys.Y&&visualViewport?.RedoCamEdit()==true){RefreshVisualCamEntries(Math.Max(0,(lstVisualCamEntries.SelectedItem as CamEntry)?.FileOrder??0));pgVisualProperties.Refresh();e.Handled=true;e.SuppressKeyPress=true;}
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
        if(pgVisualProperties.SelectedObject is CamEntry or CamVertex or CamFrame)
        {
            object targetObject=pgVisualProperties.SelectedObject;PropertyDescriptor? property=e.ChangedItem.PropertyDescriptor;object? camOldValue=e.OldValue,camNewValue=property?.GetValue(targetObject);if(property!=null&&visualViewport!=null)visualViewport.RegisterCamEdit(()=>ApplyCamProperty(targetObject,property,camOldValue),()=>ApplyCamProperty(targetObject,property,camNewValue));
            if(visualViewport?.CamScene is CamScene cam){cam.IsModified=true;visualViewport.RefreshCamGeometry();lstVisualCamEntries.Refresh();lstVisualCamParts.Refresh();UpdateVisualStatus();}
            return;
        }
        if(pgVisualProperties.SelectedObject is EffEntry effEntry)
        {
            if(visualEffScene!=null)visualEffScene.IsModified=true;lstVisualEffEntries.Refresh();visualViewport?.RefreshEffGeometry(effEntry);UpdateVisualStatus();return;
        }
        if(pgVisualProperties.SelectedObject is LitLight or LitGroup)
        {
            if(visualLitScene!=null)visualLitScene.IsModified=true;
            btnVisualSaveLit.Text="SAVE LIT *";lstVisualLitEntries.Refresh();visualViewport?.RefreshLitGeometry();UpdateVisualStatus();return;
        }
        if (pgVisualProperties.SelectedObject is ScenarioEntry smdEntry)
        {
            ScenarioEntry[] smdEntries=pgVisualProperties.SelectedObjects.OfType<ScenarioEntry>().ToArray();if(smdEntries.Length==0)smdEntries=new[]{smdEntry};
            if(e.ChangedItem.PropertyDescriptor?.Name==nameof(ScenarioEntry.TextureIndex))
            {
                if(smdEntry.TextureIndex<0||smdEntry.TextureIndex>byte.MaxValue){MessageBox.Show(this,"O índice de textura deve estar entre 0 e 255.","Textura SMD",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                string tpl=GetTplWorkPath(visualSmdPath!,project.ActiveDatName??"");uint count=new TplReader().ReadTextureCount(tpl);if(smdEntry.TextureIndex>=count){MessageBox.Show(this,$"O TPL possui somente {count} textura(s).","Textura SMD",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
                if(visualViewport.Scene!.IsModified&&!SaveVisualSmd())return;EnsureVisualSmdBackup();foreach(byte bin in smdEntries.Select(x=>x.BinId).Distinct())SmdEmbeddedBinService.SetMaterialTexture(visualSmdPath!,bin,visualViewport.Scene.BinCount,(byte)smdEntry.TextureIndex);
                ReloadVisualSmd(smdEntry.FileOrder);return;
            }
            visualViewport.Scene!.IsModified = true;
            visualViewport.RefreshSmdGeometry(smdEntry);
            lstVisualSmdEntries.Refresh(); btnVisualSaveSmd.Text = "SAVE SMD *";
            UpdateTopVisualSaveState();
            return;
        }
        if (pgVisualProperties.SelectedObject is EtsEntry objectEntry)
        {
            visualEtsScene!.IsModified = true;
            visualViewport.RefreshEtsGeometry(objectEntry);
            lstVisualObjectEntries.Refresh();
            btnVisualSaveEts.Text = "SAVE ETS *";
            UpdateTopVisualSaveState();
            return;
        }
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
    private void ApplyCamProperty(object targetObject,PropertyDescriptor property,object? value){property.SetValue(targetObject,value);if(visualViewport?.CamScene is CamScene scene)scene.IsModified=true;visualViewport?.RefreshCamGeometry();lstVisualCamEntries.Refresh();lstVisualCamParts.Refresh();if(ReferenceEquals(pgVisualProperties.SelectedObject,targetObject))pgVisualProperties.Refresh();UpdateTopVisualSaveState();}

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
        EtsScene? ets = visualViewport?.EtsScene;
        LitScene? lit=visualViewport?.LitScene;
        if(lit!=null)modified += $" • {lit.LightCount:N0} lights"+(lit.IsModified?" • LIT Modified":"");
        Ps2EffFile? eff=visualViewport?.EffScene;
        if(eff!=null)modified += $" • {eff.EntryCount:N0} effects"+(eff.IsModified?" • EFF Modified":"");
        RtpScene? rtp=visualViewport?.RtpScene;
        if(rtp!=null)modified += $" • {rtp.Nodes.Count:N0} RTP nodes"+(rtp.IsModified?" • RTP Modified":"");

        if (scene != null && aev != null && esl != null) lblVisualStatus.Text = $"{scene.Triangles.Count:N0} tris • {aev.Count:N0} AEV • {esl.ActiveCount:N0} enemies • {ets?.Entries.Count ?? 0:N0} objects{modified}";
        else if (scene != null && aev != null) lblVisualStatus.Text = $"{scene.Triangles.Count:N0} tris • {aev.Count:N0} AEV{modified}";
        else if (scene != null) lblVisualStatus.Text = $"{scene.Triangles.Count:N0} tris • {visualViewport.LoadedTextureCount:N0} tex";
        else if (aev != null) lblVisualStatus.Text = $"{aev.Count:N0} AEV{modified}";
        else if(esl != null) lblVisualStatus.Text = $"{esl.ActiveCount:N0} enemies{modified}";
        else lblVisualStatus.Text = "v0.6.0 • Visual Editor";
        UpdateTopVisualSaveState();
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
        if (clbVisualLayers == null || clbVisualLayers.Items.Count < 8) return;
        clbVisualLayers.SetItemChecked(0, settings.VisualScenarioLayer);
        clbVisualLayers.SetItemChecked(1, settings.VisualAevLayer);
        clbVisualLayers.SetItemChecked(2, settings.VisualEnemiesLayer);
        clbVisualLayers.SetItemChecked(3, settings.VisualObjectsLayer);
        clbVisualLayers.SetItemChecked(4, settings.VisualCollisionLayer);
        clbVisualLayers.SetItemChecked(5, settings.VisualLightingLayer);
        clbVisualLayers.SetItemChecked(6, settings.VisualEffectsLayer);
        clbVisualLayers.SetItemChecked(7, settings.VisualRtpLayer);
        if (visualViewport != null && !visualViewport.IsDisposed)
        {
            visualViewport.ScenarioVisible = settings.VisualScenarioLayer;
            visualViewport.AevVisible = settings.VisualAevLayer;
            visualViewport.EnemiesVisible = settings.VisualEnemiesLayer;
            visualViewport.ObjectsVisible = settings.VisualObjectsLayer;
            visualViewport.CollisionVisible = settings.VisualCollisionLayer;
            visualViewport.LightingVisible = settings.VisualLightingLayer;
            visualViewport.EffectsVisible = settings.VisualEffectsLayer;
            visualViewport.RtpVisible = settings.VisualRtpLayer;
            visualViewport.EnemySnapEnabled = settings.VisualEnemySnap;
        }
        if(chkVisualEnemySnap!=null) chkVisualEnemySnap.Checked=settings.VisualEnemySnap;
        if(chkVisualEnemyAnimated!=null) chkVisualEnemyAnimated.Checked=settings.VisualEnemyAnimated;
        SetEnemyGizmoMode(EnemyGizmoMode.Move);
    }

    private void WireVisualCollisionEvents()
    {
        visualViewport.CollisionFaceClicked -= visualViewport_CollisionFaceClicked;
        visualViewport.CollisionFaceClicked += visualViewport_CollisionFaceClicked;
        visualViewport.CollisionVertexEdited -= visualViewport_CollisionVertexEdited;
        visualViewport.CollisionVertexEdited += visualViewport_CollisionVertexEdited;
    }

    private void PopulateVisualCollisionMeshes(EsatFile? sat, EsatFile? eat)
    {
        if (cmbVisualCollisionMesh == null) return;
        cmbVisualCollisionMesh.BeginUpdate();
        try
        {
            cmbVisualCollisionMesh.Items.Clear();
            cmbVisualCollisionMesh.Items.Add(new VisualCollisionMeshItem(null, -1, "Todos os submeshes"));
            if (sat != null) for (int i=0;i<sat.Meshes.Count;i++) cmbVisualCollisionMesh.Items.Add(new VisualCollisionMeshItem(EsatKind.Sat,i,$"SAT • submesh {i} • {sat.Meshes[i].Faces.Count:N0} faces"));
            if (eat != null) for (int i=0;i<eat.Meshes.Count;i++) cmbVisualCollisionMesh.Items.Add(new VisualCollisionMeshItem(EsatKind.Eat,i,$"EAT • submesh {i} • {eat.Meshes[i].Faces.Count:N0} faces"));
            cmbVisualCollisionMesh.SelectedIndex = 0;
        }
        finally { cmbVisualCollisionMesh.EndUpdate(); }
    }

    private void visualCollisionDisplay_Changed(object? sender, EventArgs e)
    {
        if (visualViewport == null) return;
        VisualCollisionMeshItem? item = cmbVisualCollisionMesh?.SelectedItem as VisualCollisionMeshItem;
        visualViewport.SatCollisionVisible = chkVisualCollisionSat.Checked && (item?.Kind == null || item.Kind == EsatKind.Sat);
        visualViewport.EatCollisionVisible = chkVisualCollisionEat.Checked && (item?.Kind == null || item.Kind == EsatKind.Eat);
        visualViewport.SatCollisionMeshFilter = item?.Kind == EsatKind.Sat ? item.MeshIndex : -1;
        visualViewport.EatCollisionMeshFilter = item?.Kind == EsatKind.Eat ? item.MeshIndex : -1;
        visualViewport.CollisionFloorVisible = chkVisualCollisionFloor.Checked;
        visualViewport.CollisionSlopeVisible = chkVisualCollisionSlope.Checked;
        visualViewport.CollisionWallVisible = chkVisualCollisionWall.Checked;
        visualViewport.CollisionOpacity = trkVisualCollisionOpacity.Value / 100f;
        visualViewport.CollisionStyle = cmbVisualCollisionStyle.SelectedIndex switch { 1 => CollisionRenderStyle.Solid, 2 => CollisionRenderStyle.Wireframe, _ => CollisionRenderStyle.SolidWireframe };
        visualViewport.RefreshCollisionDisplay();
    }

    private void visualViewport_CollisionFaceClicked(EsatFaceInspection? face)
    {
        if (face == null)
        {
            lblVisualCollisionInfo.Text = "Nenhuma face selecionada.";
            if (pgVisualProperties.SelectedObject is EsatFaceInspection) pgVisualProperties.SelectedObject = null;
            return;
        }
        if (tabVisualEntities != null) tabVisualEntities.SelectedIndex = 3;
        pgVisualProperties.SelectedObject = face;
        lblVisualPropertiesTitle.Text = $"PROPERTIES • {face.FileType} FACE #{face.FaceIndex}";
        lblVisualCollisionInfo.Text = $"{face.FileType} • submesh {face.MeshIndex} • face {face.FaceIndex} • {face.Category}\nBB {face.Blue}  GG {face.Green}  RR {face.Red}  YY {face.Connectivity}";
        if(visualCollisionMoveWholeFace||visualCollisionMoveFaceSide||visualCollisionMoveObject){var size=visualViewport.GetCollisionRegionSelectionSize();string mode=visualCollisionMoveObject?"OBJETO":visualCollisionMoveFaceSide?"LADO":"REGIÃO";lblVisualCollisionInfo.Text=$"{mode} • {size.Faces} faces, {size.Vertices} vértices • {face.FileType} submesh {face.MeshIndex}";}
        cmbVisualCollisionVertex.Enabled=!visualCollisionMoveWholeFace&&!visualCollisionMoveFaceSide&&!visualCollisionMoveObject;
        if(cmbVisualCollisionVertex.SelectedIndex<0)cmbVisualCollisionVertex.SelectedIndex=0;else visualViewport.SelectCollisionVertex(cmbVisualCollisionVertex.SelectedIndex);
    }

    private void cmbVisualCollisionVertex_SelectedIndexChanged(object? sender, EventArgs e) => visualViewport?.SelectCollisionVertex(cmbVisualCollisionVertex.SelectedIndex);
    private void SetCollisionMoveMode(CollisionVertexMoveMode mode)
    {
        visualViewport?.SetCollisionMoveMode(mode);
        btnVisualCollisionMoveXZ.BackColor=mode==CollisionVertexMoveMode.Horizontal?Accent:Surface2;
        btnVisualCollisionMoveY.BackColor=mode==CollisionVertexMoveMode.Vertical?Accent:Surface2;
        lblVisualCollisionInfo.Text=mode==CollisionVertexMoveMode.Horizontal?"Arraste a cruz amarela para mover o vértice em X/Z.":"Arraste a cruz amarela para mover o vértice verticalmente.";
    }
    private void ToggleCollisionFaceMove()
    {
        visualCollisionMoveWholeFace=!visualCollisionMoveWholeFace;if(visualCollisionMoveWholeFace){visualCollisionMoveFaceSide=false;visualCollisionMoveObject=false;visualViewport?.SetCollisionMoveFaceSide(false);visualViewport?.SetCollisionMoveObject(false);}visualViewport?.SetCollisionMoveWholeFace(visualCollisionMoveWholeFace);
        btnVisualCollisionMoveFace.BackColor=visualCollisionMoveWholeFace?Accent:Surface2;
        btnVisualCollisionMoveFace.Text=visualCollisionMoveWholeFace?"REGIÃO ✓":"REGIÃO";
        btnVisualCollisionMoveSide.BackColor=Surface2;btnVisualCollisionMoveSide.Text="LADO";
        btnVisualCollisionObject.BackColor=Surface2;btnVisualCollisionObject.Text="OBJETO";
        cmbVisualCollisionVertex.Enabled=!visualCollisionMoveWholeFace&&visualViewport?.SelectedCollisionFace!=null;
        var size=visualViewport?.GetCollisionRegionSelectionSize()??default;
        lblVisualCollisionInfo.Text=visualCollisionMoveWholeFace?$"REGIÃO • {size.Faces} faces, {size.Vertices} vértices • arraste a cruz central.":"VERTEX • escolha V0, V1 ou V2 para editar.";
    }
    private void ToggleCollisionSideMove()
    {
        visualCollisionMoveFaceSide=!visualCollisionMoveFaceSide;if(visualCollisionMoveFaceSide){visualCollisionMoveWholeFace=false;visualCollisionMoveObject=false;visualViewport?.SetCollisionMoveWholeFace(false);visualViewport?.SetCollisionMoveObject(false);}visualViewport?.SetCollisionMoveFaceSide(visualCollisionMoveFaceSide);
        btnVisualCollisionMoveSide.BackColor=visualCollisionMoveFaceSide?Accent:Surface2;btnVisualCollisionMoveSide.Text=visualCollisionMoveFaceSide?"LADO ✓":"LADO";btnVisualCollisionMoveFace.BackColor=Surface2;btnVisualCollisionMoveFace.Text="REGIÃO";
        btnVisualCollisionObject.BackColor=Surface2;btnVisualCollisionObject.Text="OBJETO";
        cmbVisualCollisionVertex.Enabled=!visualCollisionMoveFaceSide&&visualViewport?.SelectedCollisionFace!=null;var size=visualViewport?.GetCollisionRegionSelectionSize()??default;
        lblVisualCollisionInfo.Text=visualCollisionMoveFaceSide?$"LADO • {size.Faces} faces, {size.Vertices} vértices • mova o pivô central.":"VERTEX • escolha V0, V1 ou V2 para editar.";
    }
    private void ToggleCollisionObjectMove()
    {
        visualCollisionMoveObject=!visualCollisionMoveObject;if(visualCollisionMoveObject){visualCollisionMoveWholeFace=false;visualCollisionMoveFaceSide=false;visualViewport?.SetCollisionMoveWholeFace(false);visualViewport?.SetCollisionMoveFaceSide(false);}visualViewport?.SetCollisionMoveObject(visualCollisionMoveObject);
        btnVisualCollisionObject.BackColor=visualCollisionMoveObject?Accent:Surface2;btnVisualCollisionObject.Text=visualCollisionMoveObject?"OBJETO ✓":"OBJETO";btnVisualCollisionMoveFace.BackColor=Surface2;btnVisualCollisionMoveFace.Text="REGIÃO";btnVisualCollisionMoveSide.BackColor=Surface2;btnVisualCollisionMoveSide.Text="LADO";
        cmbVisualCollisionVertex.Enabled=!visualCollisionMoveObject&&visualViewport?.SelectedCollisionFace!=null;var size=visualViewport?.GetCollisionRegionSelectionSize()??default;
        lblVisualCollisionInfo.Text=visualCollisionMoveObject?(size.Faces==12?$"OBJETO reconhecido • {size.Faces} faces, {size.Vertices} vértices":"Esta face não pertence a uma primitiva criada pela ferramenta."):"VERTEX • escolha V0, V1 ou V2 para editar.";
    }
    private void TransformCollisionObject(float rotation,bool invert=false,bool remove=false)
    {
        if(visualViewport?.TransformSelectedCollisionObject(rotation,invert,remove)!=true){MessageBox.Show(this,"Ative OBJETO e selecione uma primitiva de 12 faces criada pela ferramenta.","Editar primitiva",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        visualCollisionModified=true;btnVisualCollisionSave.Enabled=true;lblVisualCollisionInfo.Text=remove?"Primitiva removida da área jogável • SAVE para confirmar":invert?"Lados da colisão invertidos • SAVE para confirmar":$"Primitiva girada {rotation:+0;-0}° • SAVE para confirmar";
    }
    private void btnVisualCollisionDuplicate_Click(object? sender,EventArgs e)
    {
        try
        {
            EsatFaceInspection? selected=visualViewport?.SelectedCollisionFace;var bounds=visualViewport?.GetSelectedCollisionObjectBounds();if(selected==null||bounds==null){MessageBox.Show(this,"Ative OBJETO e selecione uma primitiva criada pela ferramenta.","Duplicar primitiva",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
            string dat=string.IsNullOrWhiteSpace(project.ActiveDatName)?"active":Path.GetFileNameWithoutExtension(project.ActiveDatName);string root=project.RootPath??AppContext.BaseDirectory;
            var b=bounds.Value;var center=b.Center+new System.Numerics.Vector3(Math.Max(100f,b.Size.X*1.25f),0,0);
            Ps2EsatPrimitiveWriter.AddBoxAt(selected.File,selected.MeshIndex,selected.FaceIndex,b.Size,center,Path.Combine(root,".workspace","backups",dat,Path.GetFileName(selected.File.SourcePath)+".bak"));
            EsatFile refreshed=Ps2EsatReader.Read(selected.File.SourcePath,EsatKind.Sat);visualViewport!.SetCollision(refreshed,visualViewport.EatCollision);PopulateVisualCollisionMeshes(refreshed,visualViewport.EatCollision);visualCollisionModified=false;btnVisualCollisionSave.Enabled=false;lblVisualCollisionInfo.Text="Primitiva duplicada ao lado (+X) • arquivo validado e recarregado";
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Duplicar primitiva",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void btnVisualCollisionRemove_Click(object? sender,EventArgs e)
    {
        try
        {
            EsatFaceInspection? selected=visualViewport?.SelectedCollisionFace;if(selected==null){MessageBox.Show(this,"Ative OBJETO e selecione uma primitiva criada pela ferramenta.","Excluir primitiva",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
            if(visualViewport?.SelectedCollisionIsBoxPrimitive!=true){MessageBox.Show(this,"A exclusão de colisão original foi bloqueada porque ela altera índices e dependências internas do SAT que ainda não são conhecidas. Nos testes, remover até uma única face original fez o jogo crashar.\n\nVocê ainda pode mover ou deformar a geometria original sem alterar sua estrutura. Somente primitivas criadas pela ferramenta podem ser excluídas com segurança.","Exclusão original bloqueada",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
            if(MessageBox.Show(this,"Excluir definitivamente esta primitiva do SAT?\n\nAs 12 faces e seus dados exclusivos serão removidos. O backup original será preservado.","Excluir primitiva",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
            string dat=string.IsNullOrWhiteSpace(project.ActiveDatName)?"active":Path.GetFileNameWithoutExtension(project.ActiveDatName);string root=project.RootPath??AppContext.BaseDirectory;
            string backup=Path.Combine(root,".workspace","backups",dat,Path.GetFileName(selected.File.SourcePath)+".bak");Ps2EsatPrimitiveWriter.DeleteBox(selected.File,selected.MeshIndex,selected.FaceIndex,backup);
            EsatFile refreshed=Ps2EsatReader.Read(selected.File.SourcePath,EsatKind.Sat);visualViewport!.SetCollision(refreshed,visualViewport.EatCollision);PopulateVisualCollisionMeshes(refreshed,visualViewport.EatCollision);visualCollisionModified=false;btnVisualCollisionSave.Enabled=false;lblVisualCollisionInfo.Text="Primitiva excluída • 12 faces e dados exclusivos removidos • SAT validado";
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Excluir primitiva",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void visualViewport_CollisionVertexEdited(EsatFaceInspection face)
    {
        visualCollisionModified=true;btnVisualCollisionSave.Enabled=true;pgVisualProperties.Refresh();
        lblVisualCollisionInfo.Text=$"MODIFICADO • {face.FileType} face {face.FaceIndex} • use Ctrl+Z para desfazer";
    }
    private void btnVisualCollisionCube_Click(object? sender,EventArgs e)
    {
        try
        {
            EsatFaceInspection? selected=visualViewport?.SelectedCollisionFace;if(selected==null){MessageBox.Show(this,"Selecione uma face SAT para posicionar o cubo.","Criar cubo",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
            if(selected.File.Kind!=EsatKind.Sat){MessageBox.Show(this,"Selecione uma face do arquivo SAT.","Criar cubo",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
            string dat=string.IsNullOrWhiteSpace(project.ActiveDatName)?"active":Path.GetFileNameWithoutExtension(project.ActiveDatName);string root=project.RootPath??AppContext.BaseDirectory;
            float meters=cmbVisualCollisionPrimitiveSize.SelectedIndex switch{0=>2f,1=>5f,2=>10f,_=>20f};float units=meters*100f;string kind=cmbVisualCollisionPrimitive.SelectedItem?.ToString()??"Cubo";
            System.Numerics.Vector3 dimensions=kind switch{"Parede"=>new(units*2f,units,Math.Max(20f,units*0.1f)),"Plataforma"=>new(units*2f,Math.Max(20f,units*0.1f),units*2f),_=>new(units)};
            Ps2EsatPrimitiveWriter.AddBox(selected.File,selected.MeshIndex,selected.FaceIndex,dimensions,Path.Combine(root,".workspace","backups",dat,Path.GetFileName(selected.File.SourcePath)+".bak"));
            EsatFile refreshed=Ps2EsatReader.Read(selected.File.SourcePath,EsatKind.Sat);visualViewport!.SetCollision(refreshed,visualViewport.EatCollision);PopulateVisualCollisionMeshes(refreshed,visualViewport.EatCollision);lblVisualCollisionInfo.Text=$"{kind} experimental criado • escala {meters:0} m";ExtractLog($"Visual Editor: {kind.ToLowerInvariant()} SAT experimental criado com 12 faces.");
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Criar cubo",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    private void btnVisualCollisionSave_Click(object? sender, EventArgs e)=>SaveVisualCollision();
    private bool SaveVisualCollision()
    {
        try
        {
            bool saved=false;string dat=string.IsNullOrWhiteSpace(project.ActiveDatName)?"active":Path.GetFileNameWithoutExtension(project.ActiveDatName);string root=project.RootPath??AppContext.BaseDirectory;
            EsatFile? sat=visualViewport?.SatCollision, eat=visualViewport?.EatCollision;
            if(sat is not null&&sat.Meshes.Any(x=>x.IsModified))saved|=Ps2EsatVertexWriter.Save(sat,Path.Combine(root,".workspace","backups",dat,Path.GetFileName(sat.SourcePath)+".bak"));
            if(eat is not null&&eat.Meshes.Any(x=>x.IsModified))saved|=Ps2EsatVertexWriter.Save(eat,Path.Combine(root,".workspace","backups",dat,Path.GetFileName(eat.SourcePath)+".bak"));
            if(saved)
            {
                EsatFile? refreshedSat=sat is null?null:Ps2EsatReader.Read(sat.SourcePath,EsatKind.Sat);
                EsatFile? refreshedEat=eat is null?null:Ps2EsatReader.Read(eat.SourcePath,EsatKind.Eat);
                visualViewport.SetCollision(refreshedSat,refreshedEat);
                PopulateVisualCollisionMeshes(refreshedSat,refreshedEat);
                visualCollisionModified=false;btnVisualCollisionSave.Enabled=false;lblVisualCollisionInfo.Text="SAT/EAT salvo em modo compatível • backup preservado";
                ExtractLog("Visual Editor: SAT/EAT salvo sem alterar sua estrutura, validado e recarregado com backup.");
            }
            return saved;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Salvar colisão",MessageBoxButtons.OK,MessageBoxIcon.Error);return false;}
    }

    private void clbVisualLayers_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        // During the constructor the persisted checks are restored before the Form handle exists.
        // ApplyVisualLayerSettings already updates the viewport directly in that case.
        if (!IsHandleCreated) return;
        BeginInvoke(new Action(() =>
        {
            if (clbVisualLayers == null || clbVisualLayers.Items.Count < 8) return;
            settings.VisualScenarioLayer = clbVisualLayers.GetItemChecked(0);
            settings.VisualAevLayer = clbVisualLayers.GetItemChecked(1);
            settings.VisualEnemiesLayer = clbVisualLayers.GetItemChecked(2);
            settings.VisualObjectsLayer = clbVisualLayers.GetItemChecked(3);
            settings.VisualCollisionLayer = clbVisualLayers.GetItemChecked(4);
            settings.VisualLightingLayer = clbVisualLayers.GetItemChecked(5);
            settings.VisualEffectsLayer = clbVisualLayers.GetItemChecked(6);
            settings.VisualRtpLayer = clbVisualLayers.GetItemChecked(7);
            if (visualViewport != null && !visualViewport.IsDisposed)
            {
                visualViewport.ScenarioVisible = settings.VisualScenarioLayer;
                visualViewport.AevVisible = settings.VisualAevLayer;
                visualViewport.EnemiesVisible = settings.VisualEnemiesLayer;
                visualViewport.ObjectsVisible = settings.VisualObjectsLayer;
                visualViewport.CollisionVisible = settings.VisualCollisionLayer;
                visualViewport.LightingVisible = settings.VisualLightingLayer;
                visualViewport.EffectsVisible = settings.VisualEffectsLayer;
                visualViewport.RtpVisible = settings.VisualRtpLayer;
                if(clbVisualLayers.Items.Count>8)visualViewport.CamVisible=clbVisualLayers.GetItemChecked(8);
                visualViewport.Invalidate();
            }
            if (!restoringSession) SaveSettings();
        }));
    }

    private void RefreshVisualCamEntries(int selected=-1)
    {
        if(lstVisualCamEntries==null)return;lstVisualCamEntries.BeginUpdate();try{lstVisualCamEntries.Items.Clear();if(visualViewport?.CamScene is CamScene scene)foreach(CamEntry entry in scene.Entries)lstVisualCamEntries.Items.Add(entry);}finally{lstVisualCamEntries.EndUpdate();}
        if(selected>=0&&selected<lstVisualCamEntries.Items.Count)lstVisualCamEntries.SelectedIndex=selected;else if(lstVisualCamEntries.Items.Count>0)lstVisualCamEntries.SelectedIndex=0;
    }

    private void lstVisualCamEntries_SelectedIndexChanged(object? sender,EventArgs e)
    {
        selectedVisualCamFrames=Array.Empty<int>();if(btnVisualCamCapture!=null)btnVisualCamCapture.Enabled=true;if(btnVisualCamPosition!=null)btnVisualCamPosition.Enabled=true;if(btnVisualCamTarget!=null)btnVisualCamTarget.Enabled=true;
        CamEntry? entry=lstVisualCamEntries.SelectedItem as CamEntry;visualViewport?.SelectCamEntry(entry?.FileOrder??-1);lstVisualCamParts.BeginUpdate();try{lstVisualCamParts.Items.Clear();if(entry!=null){foreach(CamVertex vertex in entry.Vertices)lstVisualCamParts.Items.Add(vertex);foreach(CamFrame frame in entry.Frames)lstVisualCamParts.Items.Add(frame);}}finally{lstVisualCamParts.EndUpdate();}visualCamTimeline?.SetFrames(entry?.Frames,0);
        pgVisualProperties.SelectedObject=entry;lblVisualPropertiesTitle.Text=entry==null?"PROPERTIES • SELECTION":$"PROPERTIES • CAM [{entry.Index:00}-{entry.SubIndex}]";
    }

    private void lstVisualCamParts_SelectedIndexChanged(object? sender,EventArgs e)
    {
        if(syncingVisualCamFrameSelection)return;selectedVisualCamFrames=Array.Empty<int>();
        object? part=lstVisualCamParts.SelectedItem;if(part is CamVertex v)visualViewport?.SelectCamVertex(v.Index);else if(part is CamFrame f){visualViewport?.SelectCamFrame(f.Index);if(lstVisualCamEntries.SelectedItem is CamEntry entry)visualCamTimeline?.SetFrames(entry.Frames,f.Index);}if(part==null)return;pgVisualProperties.SelectedObject=part;lblVisualPropertiesTitle.Text=part is CamVertex vertex?$"PROPERTIES • CAM VERTEX {vertex.Index+1}":part is CamFrame frame?$"PROPERTIES • CAM FRAME {frame.Index:00}":"PROPERTIES • CAM";
    }
    private void SetCamGizmoMode(CamGizmoMode mode){if(visualViewport==null)return;visualViewport.CamTransformMode=mode;visualViewport.RefreshCamGeometry();btnVisualCamMove.BackColor=mode==CamGizmoMode.Move?Accent:Surface2;btnVisualCamScale.BackColor=mode==CamGizmoMode.Scale?Accent:Surface2;btnVisualCamVertex.BackColor=mode==CamGizmoMode.Vertex?Accent:Surface2;btnVisualCamFace.BackColor=mode==CamGizmoMode.Face?Accent:Surface2;btnVisualCamFrame.BackColor=mode==CamGizmoMode.Frame?Accent:Surface2;}
    private void lstVisualCamEntries_KeyDown(object? sender,KeyEventArgs e){if(e.KeyCode==Keys.F){visualViewport?.FocusCam(lstVisualCamEntries.SelectedItem as CamEntry);e.Handled=e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Delete){DeleteSelectedCam();e.Handled=e.SuppressKeyPress=true;}else if(e.Control&&e.KeyCode==Keys.D){DuplicateSelectedCam();e.Handled=e.SuppressKeyPress=true;}}

    private bool SaveVisualCam()
    {
        CamScene? scene=visualViewport?.CamScene;if(scene==null||string.IsNullOrWhiteSpace(visualCamPath))return false;
        try{int selected=(lstVisualCamEntries.SelectedItem as CamEntry)?.FileOrder??-1;string backup=GetVisualAevBackupPath(visualCamPath);int expectedEntries=scene.Count,expectedCameras=scene.CameraCount;bool made=Ps2CamWriter.Save(scene,backup);CamScene verify=Ps2CamReader.Read(visualCamPath);if(verify.Count!=expectedEntries||verify.CameraCount!=expectedCameras)throw new InvalidDataException("Validação CAM falhou após salvar.");visualViewport.SetCamScene(verify);WireVisualCamEvents();RefreshVisualCamEntries(Math.Clamp(selected,0,Math.Max(0,verify.Count-1)));ExtractLog($"Visual Editor: CAM salvo e validado: {Path.GetFileName(visualCamPath)}.{(made?" • backup inicial preservado.":"")}");return true;}
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Salvar CAM",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog("Visual Editor: erro ao salvar CAM: "+ex.Message);return false;}
    }

    private void WireVisualCamEvents(){visualViewport.CamEntrySelected-=visualViewport_CamEntrySelected;visualViewport.CamEntrySelected+=visualViewport_CamEntrySelected;visualViewport.CamEntryEdited-=visualViewport_CamEntryEdited;visualViewport.CamEntryEdited+=visualViewport_CamEntryEdited;visualViewport.CamVertexSelected-=visualViewport_CamVertexSelected;visualViewport.CamVertexSelected+=visualViewport_CamVertexSelected;visualViewport.CamFrameSelected-=visualViewport_CamFrameSelected;visualViewport.CamFrameSelected+=visualViewport_CamFrameSelected;visualViewport.DeleteCamRequested-=visualViewport_DeleteCamRequested;visualViewport.DeleteCamRequested+=visualViewport_DeleteCamRequested;}
    private void visualViewport_CamEntrySelected(CamEntry? entry){if(entry==null)return;for(int i=0;i<lstVisualCamEntries.Items.Count;i++)if(lstVisualCamEntries.Items[i] is CamEntry x&&x.FileOrder==entry.FileOrder){lstVisualCamEntries.SelectedIndex=i;break;}}
    private void visualViewport_CamVertexSelected(int? index){if(index.HasValue&&index.Value>=0&&index.Value<lstVisualCamParts.Items.Count)lstVisualCamParts.SelectedIndex=index.Value;else{lstVisualCamParts.SelectedIndexChanged-=lstVisualCamParts_SelectedIndexChanged;try{lstVisualCamParts.ClearSelected();}finally{lstVisualCamParts.SelectedIndexChanged+=lstVisualCamParts_SelectedIndexChanged;}if(lstVisualCamEntries.SelectedItem is CamEntry entry){pgVisualProperties.SelectedObject=entry;lblVisualPropertiesTitle.Text=$"PROPERTIES • CAM [{entry.Index:00}-{entry.SubIndex}]";}}}
    private void visualViewport_CamFrameSelected(int index,bool targetPoint){if(lstVisualCamEntries.SelectedItem is not CamEntry entry)return;int item=entry.Vertices.Count+index;if(item>=0&&item<lstVisualCamParts.Items.Count)lstVisualCamParts.SelectedIndex=item;lblVisualPropertiesTitle.Text=$"PROPERTIES • CAM FRAME {index:00} • {(targetPoint?"TARGET":"POSIÇÃO")}";}
    private void visualViewport_CamEntryEdited(CamEntry entry){if(visualViewport?.CamScene is CamScene scene)scene.IsModified=true;lstVisualCamEntries.Refresh();lstVisualCamParts.Refresh();pgVisualProperties.Refresh();UpdateTopVisualSaveState();}
    private void SelectVisualCamTimelineFrame(int index)=>SelectVisualCamTimelineFrames(index,new[]{index});
    private void SelectVisualCamTimelineFrames(int primary,IReadOnlyList<int> indices)
    {
        if(lstVisualCamEntries.SelectedItem is not CamEntry entry||primary<0||primary>=entry.Frames.Count)return;selectedVisualCamFrames=indices.Where(i=>i>=0&&i<entry.Frames.Count).Distinct().OrderBy(i=>i).ToArray();int item=entry.Vertices.Count+primary;syncingVisualCamFrameSelection=true;try{if(item<lstVisualCamParts.Items.Count)lstVisualCamParts.SelectedIndex=item;}finally{syncingVisualCamFrameSelection=false;}SetCamGizmoMode(CamGizmoMode.Frame);visualViewport.SelectCamFrames(selectedVisualCamFrames,primary);visualCamPreview?.SelectFrame(primary);bool multiple=selectedVisualCamFrames.Length>1;btnVisualCamCapture.Enabled=!multiple;btnVisualCamPosition.Enabled=!multiple;btnVisualCamTarget.Enabled=!multiple;if(multiple){CamFrame[] frames=selectedVisualCamFrames.Select(i=>entry.Frames[i]).ToArray();pgVisualProperties.SelectedObject=new CamFrameBatchEditor(frames,mutation=>ApplyVisualCamFrameBatch(entry,frames,mutation));lblVisualPropertiesTitle.Text=$"PROPERTIES • {frames.Length} CAM FRAMES • EDIÇÃO EM MASSA";}else{pgVisualProperties.SelectedObject=entry.Frames[primary];lblVisualPropertiesTitle.Text=$"PROPERTIES • CAM FRAME {primary:00}";}
    }
    private void ApplyVisualCamFrameBatch(CamEntry entry,IReadOnlyList<CamFrame> frames,Action<CamFrame> mutation)
    {
        if(visualViewport?.CamScene is not CamScene scene)return;var before=frames.Select(f=>(Frame:f,f.Position,f.Target,f.Roll,f.Fov,f.Time)).ToArray();foreach(CamFrame frame in frames)mutation(frame);var after=frames.Select(f=>(Frame:f,f.Position,f.Target,f.Roll,f.Fov,f.Time)).ToArray();void Restore((CamFrame Frame,System.Numerics.Vector3 Position,System.Numerics.Vector3 Target,float Roll,float Fov,ushort Time)[] state){foreach(var s in state){s.Frame.Position=s.Position;s.Frame.Target=s.Target;s.Frame.Roll=s.Roll;s.Frame.Fov=s.Fov;s.Frame.Time=s.Time;}scene.IsModified=true;visualViewport.RefreshCamGeometry();lstVisualCamParts.Refresh();pgVisualProperties.Refresh();visualCamPreview?.RefreshPreview();UpdateTopVisualSaveState();}visualViewport.RegisterCamEdit(()=>Restore(before),()=>Restore(after));scene.IsModified=true;visualViewport.RefreshCamGeometry();lstVisualCamParts.Refresh();visualCamPreview?.RefreshPreview();UpdateTopVisualSaveState();
    }
    private static void ReindexCamFrames(CamEntry entry){for(int i=0;i<entry.Frames.Count;i++)entry.Frames[i].Index=i;}
    private void RefreshVisualCamFrameUi(CamEntry entry,int selected){ReindexCamFrames(entry);lstVisualCamEntries_SelectedIndexChanged(null,EventArgs.Empty);if(entry.Frames.Count>0)SelectVisualCamTimelineFrame(Math.Clamp(selected,0,entry.Frames.Count-1));else visualCamTimeline.SetFrames(entry.Frames,-1);visualViewport.RefreshCamGeometry();visualCamPreview?.RefreshPreview();UpdateTopVisualSaveState();}
    private void AddVisualCamFrame(){if(visualViewport?.CamScene is not CamScene scene||lstVisualCamEntries.SelectedItem is not CamEntry entry)return;CamFrame? source=lstVisualCamParts.SelectedItem as CamFrame??entry.Frames.LastOrDefault();int at=source==null?entry.Frames.Count:Math.Clamp(source.Index+1,0,entry.Frames.Count);CamFrame added=source==null?new CamFrame{Fov=60f}:new CamFrame{Position=source.Position,Target=source.Target,Roll=source.Roll,Fov=source.Fov,Time=source.Time,PositionSuffix=source.PositionSuffix,TargetSuffix=source.TargetSuffix};entry.Frames.Insert(at,added);scene.IsModified=scene.StructureModified=true;visualViewport.RegisterCamEdit(()=>{entry.Frames.Remove(added);scene.IsModified=scene.StructureModified=true;RefreshVisualCamFrameUi(entry,Math.Max(0,at-1));},()=>{entry.Frames.Insert(Math.Min(at,entry.Frames.Count),added);scene.IsModified=scene.StructureModified=true;RefreshVisualCamFrameUi(entry,at);});RefreshVisualCamFrameUi(entry,at);}
    private void RemoveVisualCamFrame(){if(visualViewport?.CamScene is not CamScene scene||lstVisualCamEntries.SelectedItem is not CamEntry entry||entry.Frames.Count==0)return;CamFrame frame=lstVisualCamParts.SelectedItem as CamFrame??entry.Frames.Last();int at=entry.Frames.IndexOf(frame);if(at<0)return;entry.Frames.RemoveAt(at);scene.IsModified=scene.StructureModified=true;visualViewport.RegisterCamEdit(()=>{entry.Frames.Insert(Math.Min(at,entry.Frames.Count),frame);scene.IsModified=scene.StructureModified=true;RefreshVisualCamFrameUi(entry,at);},()=>{entry.Frames.Remove(frame);scene.IsModified=scene.StructureModified=true;RefreshVisualCamFrameUi(entry,Math.Min(at,entry.Frames.Count-1));});RefreshVisualCamFrameUi(entry,Math.Min(at,entry.Frames.Count-1));}
    private void SelectVisualCamFramePoint(bool targetPoint){if(lstVisualCamEntries.SelectedItem is not CamEntry entry||lstVisualCamParts.SelectedItem is not CamFrame frame)return;SetCamGizmoMode(CamGizmoMode.Frame);visualViewport.SelectCamFrame(frame.Index,targetPoint);lblVisualPropertiesTitle.Text=$"PROPERTIES • CAM FRAME {frame.Index:00} • {(targetPoint?"TARGET":"POSIÇÃO")}";}
    private void btnVisualCamCapture_Click(object? sender,EventArgs e){if(selectedVisualCamFrames.Length>1)return;if(lstVisualCamEntries.SelectedItem is not CamEntry entry||lstVisualCamParts.SelectedItem is not CamFrame frame)return;var before=(frame.Position,frame.Target,frame.Fov);var pose=visualViewport.GetCamCreationPose();void Apply(System.Numerics.Vector3 position,System.Numerics.Vector3 target,float fov){frame.Position=position;frame.Target=target;frame.Fov=fov;if(visualViewport.CamScene is CamScene scene)scene.IsModified=true;visualViewport.RefreshCamGeometry();visualViewport_CamEntryEdited(entry);}Apply(pose.Position,pose.Target,frame.Fov);var after=(frame.Position,frame.Target,frame.Fov);visualViewport.RegisterCamEdit(()=>Apply(before.Position,before.Target,before.Fov),()=>Apply(after.Position,after.Target,after.Fov));SelectVisualCamFramePoint(false);ExtractLog($"Visual Editor: posição e target do viewport capturados no CAM frame {frame.Index:00}; FOV preservado.");}
    private void btnVisualCamPreview_Click(object? sender,EventArgs e){if(lstVisualCamEntries.SelectedItem is not CamEntry entry||entry.Frames.Count==0){MessageBox.Show(this,"Selecione uma câmera com pelo menos um frame.","Preview CAM",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}if(visualCamPreview==null||visualCamPreview.IsDisposed){string? texture=null;if(!string.IsNullOrWhiteSpace(visualSmdPath))texture=GetTplWorkPath(visualSmdPath,project.ActiveDatName??"");visualCamPreview=new CamPreviewForm(visualViewport.Scene,texture,()=>lstVisualCamEntries.SelectedItem as CamEntry);visualCamPreview.FormClosed+=(_,_)=>visualCamPreview=null;visualCamPreview.Show(this);}else{visualCamPreview.BringToFront();visualCamPreview.Focus();}visualCamPreview.RefreshPreview();}
    private void visualViewport_DeleteCamRequested()=>DeleteSelectedCam();

    private void btnVisualAddCam_Click(object? sender,EventArgs e)
    {
        CamScene? scene=visualViewport?.CamScene;if(scene==null)return;settings.CamPresets??=new();using var dialog=new CamPresetDialog(settings.CamPresets);if(dialog.ShowDialog(this)!=DialogResult.OK||dialog.SelectedPreset==null){if(dialog.CustomPresetsChanged)SaveSettings();return;}if(dialog.CustomPresetsChanged)SaveSettings();AddCamFromPreset(scene,dialog.SelectedPreset);
    }
    private void DuplicateSelectedCam(){if(visualViewport?.CamScene is CamScene scene&&lstVisualCamEntries.SelectedItem is CamEntry source)AddCamClone(scene,source,shift:false);}
    private void AddCamFromPreset(CamScene scene,CamPresetDefinition preset)
    {
        if(scene.Entries.Count>=255||scene.CameraRecords.Count>=255){MessageBox.Show(this,"O formato CAM aceita no máximo 255 zonas e 255 câmeras.","Adicionar camera",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
        byte index=NextCamId(scene);var pose=visualViewport!.GetCamCreationPose();CamEntry entry=CreateCamFromPreset(preset,index,scene.Entries.Count,scene.CameraRecords.Count,pose.Position,pose.Target,pose.Fov);CamCameraRecord camera=entry.Camera!;int insertAt=scene.Entries.Count;scene.CameraRecords.Add(camera);scene.Entries.Add(entry);scene.IsModified=scene.StructureModified=true;
        visualViewport.RegisterCamEdit(()=>{scene.Entries.Remove(entry);scene.CameraRecords.Remove(camera);ReindexCam(scene);RefreshVisualCamEntries(Math.Max(0,insertAt-1));visualViewport.RefreshCamGeometry();},()=>{scene.CameraRecords.Add(camera);scene.Entries.Insert(Math.Min(insertAt,scene.Entries.Count),entry);ReindexCam(scene);RefreshVisualCamEntries(entry.FileOrder);visualViewport.RefreshCamGeometry();});
        RefreshVisualCamEntries(entry.FileOrder);visualViewport.RefreshCamGeometry();ExtractLog($"Visual Editor: camera {index:00} criada com preset {preset.Name} na posição do viewport • Ctrl+S para salvar.");
    }
    private void AddCamClone(CamScene scene,CamEntry source,bool shift,CamPresetDefinition? preset=null)
    {
        if(scene.Entries.Count>=255||scene.CameraRecords.Count>=255){MessageBox.Show(this,"O formato CAM aceita no máximo 255 zonas e 255 câmeras.","Adicionar camera",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}byte index=NextCamId(scene);CamEntry clone=CloneCamEntry(source,index,scene.Entries.Count,scene.CameraRecords.Count,shift?200f:0f);CamCameraRecord camera=clone.Camera!;int insertAt=scene.Entries.Count;scene.CameraRecords.Add(camera);scene.Entries.Add(clone);scene.IsModified=scene.StructureModified=true;visualViewport!.RegisterCamEdit(()=>{scene.Entries.Remove(clone);scene.CameraRecords.Remove(camera);ReindexCam(scene);RefreshVisualCamEntries(Math.Max(0,insertAt-1));visualViewport.RefreshCamGeometry();},()=>{scene.CameraRecords.Add(camera);scene.Entries.Insert(Math.Min(insertAt,scene.Entries.Count),clone);ReindexCam(scene);RefreshVisualCamEntries(clone.FileOrder);visualViewport.RefreshCamGeometry();});RefreshVisualCamEntries(clone.FileOrder);visualViewport.RefreshCamGeometry();ExtractLog($"Visual Editor: camera {index:00} duplicada • Ctrl+S para salvar.");
    }
    private void DeleteSelectedCam()
    {
        if(visualViewport?.CamScene is not CamScene scene||lstVisualCamEntries.SelectedItem is not CamEntry entry||scene.Entries.Count<=1)return;int removedAt=entry.FileOrder,next=Math.Min(removedAt,scene.Entries.Count-2);CamCameraRecord? camera=entry.Camera;int cameraAt=camera==null?-1:scene.CameraRecords.IndexOf(camera);bool unusedCamera=camera!=null&&!scene.Entries.Any(x=>!ReferenceEquals(x,entry)&&ReferenceEquals(x.Camera,camera));bool protectMotionSlot=unusedCamera&&settings.CamProtectMotionIndices&&cameraAt>=0&&scene.CameraRecords.Skip(cameraAt).Any(x=>x.Type==CamType.Motion);bool removeCamera=unusedCamera&&!protectMotionSlot;scene.Entries.Remove(entry);if(removeCamera)scene.CameraRecords.Remove(camera!);ReindexCam(scene);scene.IsModified=scene.StructureModified=true;visualViewport.RegisterCamEdit(()=>{if(removeCamera&&camera!=null)scene.CameraRecords.Insert(Math.Clamp(cameraAt,0,scene.CameraRecords.Count),camera);scene.Entries.Insert(Math.Min(removedAt,scene.Entries.Count),entry);ReindexCam(scene);RefreshVisualCamEntries(entry.FileOrder);visualViewport.RefreshCamGeometry();},()=>{scene.Entries.Remove(entry);if(removeCamera&&camera!=null)scene.CameraRecords.Remove(camera);ReindexCam(scene);RefreshVisualCamEntries(Math.Min(removedAt,scene.Entries.Count-1));visualViewport.RefreshCamGeometry();});RefreshVisualCamEntries(next);visualViewport.RefreshCamGeometry();string removalResult=removeCamera?" junto com seu registro":protectMotionSlot?"; slot físico preservado para proteger os índices Motion":"; registro compartilhado preservado";ExtractLog($"Visual Editor: camera {entry.Index:00} removida do CAM{removalResult} • Ctrl+S para salvar.");
    }
    private static void ReindexCam(CamScene scene){for(int i=0;i<scene.Entries.Count;i++)scene.Entries[i].FileOrder=i;for(int i=0;i<scene.CameraRecords.Count;i++)scene.CameraRecords[i].FileOrder=i;scene.IsModified=scene.StructureModified=true;}
    private static byte NextCamId(CamScene scene)
    {
        var used=scene.Entries.Select(x=>x.Index).Concat(scene.CameraRecords.Select(x=>x.Index)).ToHashSet();for(int value=1;value<=255;value++)if(!used.Contains((byte)value))return(byte)value;throw new InvalidOperationException("Não há IDs de câmera livres neste CAM.");
    }
    private static CamEntry CloneCamEntry(CamEntry s,byte index,int order,int cameraOrder,float shiftX)
    {
        CamCameraRecord camera=s.Camera is null?new CamCameraRecord{FileOrder=cameraOrder,Index=index,Enabled=1}:new CamCameraRecord{FileOrder=cameraOrder,Raw=(byte[])s.Camera.Raw.Clone(),Index=index,Enabled=s.Camera.Enabled,Type=s.Camera.Type,Attributes=s.Camera.Attributes,OffsetVector=s.Camera.OffsetVector};
        var clone=new CamEntry{FileOrder=order,EntryRaw=(byte[])s.EntryRaw.Clone(),AreaRaw=(byte[])s.AreaRaw.Clone(),AreaEnabled=s.AreaEnabled,Index=index,SubIndex=0,AreaAttributes=s.AreaAttributes,FacingRadians=s.FacingRadians,UpperY=s.UpperY,LowerY=s.LowerY,Camera=camera};
        foreach(CamVertex v in s.Vertices)clone.Vertices.Add(new CamVertex{Index=clone.Vertices.Count,Position=v.Position+new System.Numerics.Vector3(shiftX,0,0)});
        foreach(CamFrame f in s.Frames)camera.Frames.Add(new CamFrame{Index=camera.Frames.Count,Position=f.Position+new System.Numerics.Vector3(shiftX,0,0),Target=f.Target+new System.Numerics.Vector3(shiftX,0,0),Roll=f.Roll,Fov=f.Fov,Time=f.Time,PositionSuffix=f.PositionSuffix,TargetSuffix=f.TargetSuffix});
        return clone;
    }

    private static CamEntry CreateCamFromPreset(CamPresetDefinition preset,byte index,int order,int cameraOrder,System.Numerics.Vector3 viewPosition,System.Numerics.Vector3 viewTarget,float fov)
    {
        CamType type=(CamType)preset.CameraType;int vertexCount=Math.Clamp(preset.VertexCount,3,32),frameCount=Math.Clamp(preset.FrameCount,0,1024);float centerX=viewTarget.X,centerY=viewTarget.Y,centerZ=viewTarget.Z,radius=500f,height=1000f;
        CamAreaAttributes areaAttributes=(CamAreaAttributes)preset.AreaAttributes;byte[] entryRaw=new byte[Ps2CamReader.EntrySize],areaRaw=new byte[Ps2CamReader.AreaSize],cameraRaw=new byte[Ps2CamReader.CameraSize];entryRaw[0]=(byte)areaAttributes;areaRaw[8]=1;areaRaw[9]=0xFF;
        if(type is CamType.Fixed or CamType.Track or CamType.RailPan)
        {
            // Valor presente nas cameras Fixed/Track/RailPan adicionadas do CAM funcional.
            cameraRaw[0x14]=0x00;cameraRaw[0x15]=0x00;cameraRaw[0x16]=0x00;cameraRaw[0x17]=0x3F; // 0.5f
        }
        var camera=new CamCameraRecord{FileOrder=cameraOrder,Raw=cameraRaw,Index=index,Enabled=1,Type=type,Attributes=CamCameraAttributes.None,OffsetVector=new System.Numerics.Vector3(0,1000,0)};
        var entry=new CamEntry{FileOrder=order,EntryRaw=entryRaw,AreaRaw=areaRaw,AreaEnabled=1,Index=index,SubIndex=0,AreaAttributes=areaAttributes,FacingRadians=0,LowerY=centerY-height,UpperY=centerY+height,Camera=camera};
        // O jogo espera o contorno da triggerzone em sentido horario no plano X/Z.
        for(int i=0;i<vertexCount;i++){float angle=-MathF.PI/2f-i*MathF.Tau/vertexCount;entry.Vertices.Add(new CamVertex{Index=i,Position=new System.Numerics.Vector3(centerX+MathF.Cos(angle)*radius,centerY-height,centerZ+MathF.Sin(angle)*radius)});}
        if(type==CamType.ThirdPerson)
        {
            // Os valores canônicos são offsets de composição. Ancorá-los no ponto
            // atual mantém todos os frames novos junto da triggerzone criada.
            for(int i=0;i<frameCount;i++){System.Numerics.Vector3 position=viewTarget+CanonicalThirdPersonPosition(i),target=viewTarget+CanonicalThirdPersonTarget(i);camera.Frames.Add(new CamFrame{Index=i,Position=position,Target=target,Fov=60f,Time=0});}
        }
        else
        {
            System.Numerics.Vector3 safePosition=viewPosition;if(!float.IsFinite(safePosition.X)||!float.IsFinite(safePosition.Y)||!float.IsFinite(safePosition.Z))safePosition=viewTarget+new System.Numerics.Vector3(0,250,-1000);float safeFov=Math.Clamp(fov,20f,90f);
            // Todo preset com datasets nasce no mesmo contexto espacial do viewport.
            // Em especial, RailPan não deve criar seu marcador de target na origem
            // global do mapa, que pode estar quilômetros longe da nova triggerzone.
            System.Numerics.Vector3 target=viewTarget;
            for(int i=0;i<frameCount;i++)camera.Frames.Add(new CamFrame{Index=i,Position=safePosition,Target=target,Fov=safeFov,Roll=0,Time=(ushort)(i*30)});
        }
        return entry;
    }

    private static System.Numerics.Vector3 CanonicalThirdPersonPosition(int frame)=>(frame%12) switch{0=>V3(-527,867,-681),1=>V3(-530,1765,-590),2=>V3(-393,2058,-5),3=>V3(527,867,-681),4=>V3(530,1765,-590),5=>V3(393,2058,-5),6=>V3(-500,1385,-1250),7=>V3(-500,1765,-1190),8=>V3(-500,1920,-1080),9=>V3(500,1385,-1250),10=>V3(500,1765,-1190),_=>V3(500,1920,-1080)};
    private static System.Numerics.Vector3 CanonicalThirdPersonTarget(int frame)=>(frame%12) switch{0=>V3(-163,3084,940),1=>V3(-65,1340,1480),2=>V3(-179,365,943),3=>V3(163,3084,940),4=>V3(65,1340,1480),5=>V3(179,365,943),6=>V3(0,2225,1390),7=>V3(0,1340,1480),8=>V3(0,780,1250),9=>V3(0,2225,1390),10=>V3(0,1340,1480),_=>V3(0,780,1250)};
    private static System.Numerics.Vector3 V3(float x,float y,float z)=>new(x,y,z);


    private void chkVisualEnemyModelParts_CheckedChanged(object? sender, EventArgs e)
    {
        if (pnlVisualEnemyModelParts != null) pnlVisualEnemyModelParts.Visible = chkVisualEnemyModelParts.Checked;
        settings.VisualEnemyModelParts = chkVisualEnemyModelParts.Checked;
        if (!restoringSession) SaveSettings();
        if (chkVisualEnemyModelParts.Checked) RefreshVisualEnemyModelParts(GetVisualSelectedEnemies().FirstOrDefault());
    }

    private void chkVisualEnemyInactive_CheckedChanged(object? sender, EventArgs e)
    {
        settings.VisualShowInactiveEnemies = chkVisualEnemyInactive.Checked;
        if (!restoringSession) SaveSettings();
        if (visualViewport != null && !visualViewport.IsDisposed) visualViewport.ShowInactiveEnemies = chkVisualEnemyInactive.Checked;
        if (chkEnemyActiveOnly != null && chkEnemyActiveOnly.Checked == chkVisualEnemyInactive.Checked) chkEnemyActiveOnly.Checked = !chkVisualEnemyInactive.Checked;
        RefreshVisualEnemyEntryList();
    }

    private void tabVisualEntities_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (tabVisualEntities.SelectedIndex < 0) return;
        UpdateVisualContextActions();
        if (visualViewport != null) { visualViewport.SmdEditingEnabled = tabVisualEntities.SelectedIndex == 4; visualViewport.Invalidate(); }
        if (pnlVisualSmdTexturePreview != null) pnlVisualSmdTexturePreview.Visible = tabVisualEntities.SelectedIndex == 4;
        if (tabVisualEntities.SelectedIndex == 4) UpdateSmdTexturePreview(lstVisualSmdEntries?.SelectedItem as ScenarioEntry);
        ApplyCamTimelineVisibility();
        settings.VisualSelectedEntityTab = tabVisualEntities.SelectedIndex;
        if (!restoringSession) SaveSettings();
    }

    private void UpdateVisualContextActions()
    {
        if(pnlVisualContextActions==null||tabVisualEntities==null)return;pnlVisualContextActions.SuspendLayout();try{pnlVisualContextActions.Controls.Clear();if(visualContextActionBars.TryGetValue(tabVisualEntities.SelectedIndex,out Control? actions)){actions.Dock=DockStyle.Fill;actions.Margin=Padding.Empty;pnlVisualContextActions.Height=tabVisualEntities.SelectedIndex==3?76:44;pnlVisualContextActions.Controls.Add(actions);pnlVisualContextActions.Visible=true;pnlVisualContextActions.BringToFront();}else pnlVisualContextActions.Visible=false;if(lblVisualFps!=null){lblVisualFps.Top=pnlVisualContextActions.Visible?pnlVisualContextActions.Height+8:8;lblVisualFps.BringToFront();}}finally{pnlVisualContextActions.ResumeLayout();}
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

    private void RefreshVisualObjectList(EtsEntry? select = null)
    {
        lstVisualObjectEntries.BeginUpdate();
        try { lstVisualObjectEntries.Items.Clear(); if (visualEtsScene != null) foreach (EtsEntry e in visualEtsScene.Entries) lstVisualObjectEntries.Items.Add(e); }
        finally { lstVisualObjectEntries.EndUpdate(); }
        if (select != null) lstVisualObjectEntries.SelectedItem = select;
    }

    private void lstVisualObjectEntries_SelectedIndexChanged(object? sender, EventArgs e)
    {
        EtsEntry[] selected=lstVisualObjectEntries.SelectedItems.Cast<EtsEntry>().ToArray();
        EtsEntry? entry = lstVisualObjectEntries.SelectedItem as EtsEntry;
        pgVisualProperties.SelectedObject = entry;
        lblVisualPropertiesTitle.Text = selected.Length>1?$"PROPERTIES • {selected.Length} OBJECTS":entry == null ? "PROPERTIES • SELECTION" : GetEtmObjectTitle(entry);
        visualViewport.SelectEtsEntries(selected,entry);
        UpdateEtsTextureDebugger(entry,selected.Length);
    }

    private void SetEtsGizmoMode(EtsGizmoMode mode)
    {
        if(visualViewport==null)return;visualViewport.EtsTransformMode=mode;visualViewport.RefreshEtsGeometry(lstVisualObjectEntries.SelectedItem as EtsEntry);
        btnVisualObjectMove.BackColor=mode==EtsGizmoMode.Move?Accent:Surface2;btnVisualObjectRotate.BackColor=mode==EtsGizmoMode.Rotate?Accent:Surface2;
    }

    private void WireVisualEtsEvents()
    {
        visualViewport.EtsEntryClicked-=visualViewport_EtsEntryClicked;visualViewport.EtsEntryClicked+=visualViewport_EtsEntryClicked;
        visualViewport.EtsSelectionChanged-=visualViewport_EtsSelectionChanged;visualViewport.EtsSelectionChanged+=visualViewport_EtsSelectionChanged;
        visualViewport.EtsEntryEdited-=visualViewport_EtsEntryEdited;visualViewport.EtsEntryEdited+=visualViewport_EtsEntryEdited;
    }
    private void visualViewport_EtsEntryClicked(EtsEntry? entry)
    {
        if(entry==null){lstVisualObjectEntries.ClearSelected();return;}tabVisualEntities.SelectedIndex=2;lstVisualObjectEntries.SelectedItem=entry;pgVisualProperties.SelectedObject=entry;lblVisualPropertiesTitle.Text=GetEtmObjectTitle(entry);
    }
    private void visualViewport_EtsSelectionChanged(IReadOnlyList<EtsEntry> entries)
    {
        lstVisualObjectEntries.SelectedIndexChanged-=lstVisualObjectEntries_SelectedIndexChanged;try{lstVisualObjectEntries.ClearSelected();foreach(var entry in entries){int index=lstVisualObjectEntries.Items.IndexOf(entry);if(index>=0)lstVisualObjectEntries.SetSelected(index,true);}}finally{lstVisualObjectEntries.SelectedIndexChanged+=lstVisualObjectEntries_SelectedIndexChanged;}
        EtsEntry? primary=entries.LastOrDefault();pgVisualProperties.SelectedObject=primary;lblVisualPropertiesTitle.Text=entries.Count>1?$"PROPERTIES • {entries.Count} OBJECTS":primary==null?"PROPERTIES • SELECTION":GetEtmObjectTitle(primary);
        UpdateEtsTextureDebugger(primary,entries.Count);
    }

    private void UpdateEtsTextureDebugger(EtsEntry? entry,int selectionCount=1)
    {
        if(flpVisualObjectTextures==null)return;
        foreach(Control control in flpVisualObjectTextures.Controls)if(control is Panel panel)foreach(Control child in panel.Controls)if(child is PictureBox picture){Image? image=picture.Image;picture.Image=null;image?.Dispose();}
        flpVisualObjectTextures.Controls.Clear();
        if(entry==null||visualEtmCatalog?.ModelParts.TryGetValue(entry.ObjectId,out IReadOnlyList<EtmModelPart>? parts)!=true){lblVisualObjectTextureDebug.Text=entry==null?"TEXTURE DEBUG • selecione um objeto":$"TEXTURE DEBUG • {EtsObjectNames.Get(entry.ObjectId)} • sem modelo/textura";return;}
        lblVisualObjectTextureDebug.Text=$"TEXTURE DEBUG • {EtsObjectNames.Get(entry.ObjectId)} • ID {entry.ObjectId:X2}"+(selectionCount>1?$" • +{selectionCount-1} selecionados":"");
        var effReader=new EffTextureReader();var tplReader=new TplReader();var decoder=new TextureDecoder();var shown=new HashSet<(int,int)>();
        foreach(EtmModelPart part in parts)foreach(var material in part.Triangles.GroupBy(t=>t.TextureIndex).OrderBy(g=>g.Key))
        {
            int index=material.Key;if(index<0)continue;EtmResource? source=null;TPLDefinition.TPL texture=default;string kind="";
            try
            {
                if(part.Effect!=null){var embedded=effReader.ReadTextures(part.Effect.Data);if(index<embedded.Count){source=part.Effect;texture=embedded[index];kind="EFF";}}
                if(source==null&&part.TextureFallback!=null){source=part.TextureFallback;using var stream=new MemoryStream(source.Data,false);using var reader=new BinaryReader(stream);texture=tplReader.ReadTexture(reader,index);kind="TPL";}
                if(source==null||!shown.Add((source.FileOrder,index)))continue;
                using var dataStream=new MemoryStream(source.Data,false);using var dataReader=new BinaryReader(dataStream);using Bitmap decoded=decoder.Decode(texture,dataReader);Bitmap preview=new(decoded);
                var uvs=material.SelectMany(t=>new[]{t.UvA,t.UvB,t.UvC}).ToArray();string uv=$"U {uvs.Min(v=>v.X):0.###}…{uvs.Max(v=>v.X):0.###}  V {uvs.Min(v=>v.Y):0.###}…{uvs.Max(v=>v.Y):0.###}";
                var card=new Panel{Width=166,Height=145,BackColor=Surface,Margin=new Padding(3)};var picture=new PictureBox{Left=5,Top=5,Width=156,Height=100,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.Black,Image=preview};
                var meta=new Label{Left=5,Top=108,Width=158,Height=34,ForeColor=TextPrimary,Font=new Font("Consolas",7.2F),Text=$"{kind} #{index} • {texture.width}x{texture.height}\n{uv}",AutoEllipsis=true};
                var tip=new ToolTip();tip.SetToolTip(picture,$"{source.Name}\nMaterial {index}\n{texture.width}x{texture.height}\n{uv}");card.Controls.Add(picture);card.Controls.Add(meta);flpVisualObjectTextures.Controls.Add(card);
            }
            catch(Exception ex){var error=new Label{AutoSize=false,Width=190,Height=48,ForeColor=Color.IndianRed,Text=$"{part.Bin.Name} #{index}\n{ex.Message}",Font=new Font("Segoe UI",7.5F)};flpVisualObjectTextures.Controls.Add(error);}
        }
        if(flpVisualObjectTextures.Controls.Count==0)lblVisualObjectTextureDebug.Text+=" • nenhuma textura decodificada";
    }
    private void visualViewport_EtsEntryEdited(EtsEntry entry){MarkEtsModified();lstVisualObjectEntries.Refresh();if(ReferenceEquals(pgVisualProperties.SelectedObject,entry))pgVisualProperties.Refresh();}
    private string GetEtmObjectTitle(EtsEntry entry)
    {
        string name=EtsObjectNames.Get(entry.ObjectId);
        if(visualEtmCatalog?.Objects.TryGetValue(entry.ObjectId,out EtmObjectDefinition? definition)!=true)return $"PROPERTIES • {name} • missing";
        int bins=definition.Resources.Count(r=>r.Name.EndsWith(".bin",StringComparison.OrdinalIgnoreCase));int tpls=definition.Resources.Count(r=>r.Name.EndsWith(".tpl",StringComparison.OrdinalIgnoreCase));int anim=definition.Resources.Count(r=>r.Name.EndsWith(".fcv",StringComparison.OrdinalIgnoreCase)||r.Name.EndsWith(".seq",StringComparison.OrdinalIgnoreCase));
        return $"PROPERTIES • {name} • {bins} BIN • {tpls} TPL • {anim} anim";
    }

    private void lstVisualObjectEntries_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.D) { DuplicateSelectedEtsObject(); e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Delete) { DeleteSelectedEtsObject(); e.SuppressKeyPress = true; }
    }

    private void DuplicateSelectedEtsObject()
    {
        if (visualEtsScene == null) return;
        EtsEntry[] sources=lstVisualObjectEntries.SelectedItems.Cast<EtsEntry>().ToArray();if(sources.Length==0)return;
        ushort next=visualEtsScene.Entries.Count==0?(ushort)0:checked((ushort)(visualEtsScene.Entries.Max(x=>x.InstanceIndex)+1));
        var clones=new List<EtsEntry>();foreach(EtsEntry source in sources){EtsEntry clone=source.Clone();clone.InstanceIndex=next++;clone.FileOrder=visualEtsScene.Entries.Count;clone.PositionX+=100f;visualEtsScene.Entries.Add(clone);clones.Add(clone);}
        visualViewport.RegisterEtsUndo(()=>{foreach(var clone in clones)visualEtsScene.Entries.Remove(clone);ReindexEtsEntries();RefreshVisualObjectList(sources[0]);visualViewport.RefreshEtsGeometry(sources[0]);MarkEtsModified();});
        MarkEtsModified();RefreshVisualObjectList();foreach(var clone in clones)lstVisualObjectEntries.SetSelected(lstVisualObjectEntries.Items.IndexOf(clone),true);visualViewport.SelectEtsEntries(clones,clones[0]);visualViewport.RefreshEtsGeometry(clones[0]);
    }

    private void DeleteSelectedEtsObject()
    {
        if (visualEtsScene == null) return;EtsEntry[] selected=lstVisualObjectEntries.SelectedItems.Cast<EtsEntry>().ToArray();if(selected.Length==0)return;
        var removed=selected.Select(e=>(Index:visualEtsScene.Entries.IndexOf(e),Entry:e)).OrderBy(x=>x.Index).ToArray();foreach(var item in removed.Reverse())visualEtsScene.Entries.RemoveAt(item.Index);ReindexEtsEntries();
        visualViewport.RegisterEtsUndo(()=>{foreach(var item in removed)visualEtsScene.Entries.Insert(Math.Clamp(item.Index,0,visualEtsScene.Entries.Count),item.Entry);ReindexEtsEntries();RefreshVisualObjectList();foreach(var item in removed)lstVisualObjectEntries.SetSelected(lstVisualObjectEntries.Items.IndexOf(item.Entry),true);visualViewport.SelectEtsEntries(removed.Select(x=>x.Entry),removed[0].Entry);visualViewport.RefreshEtsGeometry(removed[0].Entry);MarkEtsModified();});
        MarkEtsModified(); RefreshVisualObjectList(); visualViewport.RefreshEtsGeometry();
    }

    private void ReindexEtsEntries(){if(visualEtsScene==null)return;for(int i=0;i<visualEtsScene.Entries.Count;i++)visualEtsScene.Entries[i].FileOrder=i;}

    private void MarkEtsModified() { if (visualEtsScene == null) return; visualEtsScene.IsModified = true; btnVisualSaveEts.Text = "SAVE ETS *"; UpdateTopVisualSaveState(); }

    private void RefreshVisualSmdEntryList(ScenarioEntry? select = null)
    {
        if (lstVisualSmdEntries == null) return; lstVisualSmdEntries.BeginUpdate();
        try { lstVisualSmdEntries.Items.Clear(); if (visualViewport?.Scene != null) foreach (ScenarioEntry e in visualViewport.Scene.Entries) lstVisualSmdEntries.Items.Add(e); }
        finally { lstVisualSmdEntries.EndUpdate(); }
        if (select != null) lstVisualSmdEntries.SelectedItem = select;
    }
    private void lstVisualSmdEntries_SelectedIndexChanged(object? sender, EventArgs e)
    {
        ScenarioEntry[] selected=lstVisualSmdEntries.SelectedItems.Cast<ScenarioEntry>().ToArray();ScenarioEntry? entry=lstVisualSmdEntries.SelectedItem as ScenarioEntry;if(selected.Length>1)pgVisualProperties.SelectedObjects=selected.Cast<object>().ToArray();else pgVisualProperties.SelectedObject=entry;lblVisualPropertiesTitle.Text=selected.Length>1?$"PROPERTIES • {selected.Length} SMD ENTRIES":entry==null?"PROPERTIES • SELECTION":$"PROPERTIES • SMD ENTRY {entry.FileOrder:D3} • BIN {entry.BinId:D3}";visualViewport.SelectSmdEntry(entry);btnVisualSmdImport.Enabled=entry!=null;btnVisualSmdNew.Enabled=entry!=null;UpdateSmdTexturePreview(entry);
    }
    private void UpdateSmdTexturePreview(ScenarioEntry? entry)
    {
        if(flpVisualSmdTextures==null)return;
        foreach(Control card in flpVisualSmdTextures.Controls)foreach(Control child in card.Controls)if(child is PictureBox picture){Image? image=picture.Image;picture.Image=null;image?.Dispose();}
        flpVisualSmdTextures.Controls.Clear();
        if(entry==null||string.IsNullOrWhiteSpace(visualSmdPath)){lblVisualSmdTextures.Text="TEXTURAS USADAS • selecione uma entry";return;}
        int[] indices=entry.LocalTriangles.Select(t=>t.TextureIndex).Where(i=>i>=0).Distinct().OrderBy(i=>i).ToArray();
        lblVisualSmdTextures.Text=$"TEXTURAS USADAS • BIN {entry.BinId:D3} • {indices.Length}";
        if(indices.Length==0){lblVisualSmdTextures.Text+=" • nenhuma";return;}
        string tpl=GetTplWorkPath(visualSmdPath,project.ActiveDatName??"");
        IReadOnlyList<TextureInfo> catalog;
        try{catalog=textureService.ReadCatalog(tpl);}catch(Exception ex){lblVisualSmdTextures.Text+=$" • preview indisponível: {ex.Message}";return;}
        foreach(int index in indices)
        {
            try
            {
                Bitmap preview=textureService.CreateThumbnail(tpl,index,112);var info=catalog.FirstOrDefault(x=>x.Index==index);
                var card=new Panel{Width=132,Height=145,BackColor=Surface,Margin=new Padding(3),Cursor=Cursors.Hand,Tag=index};
                var picture=new PictureBox{Left=5,Top=5,Width=122,Height=108,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.Black,Image=preview,Cursor=Cursors.Hand,Tag=index};
                var meta=new Label{Left=5,Top=116,Width=122,Height=24,TextAlign=ContentAlignment.MiddleCenter,ForeColor=TextPrimary,Font=new Font("Consolas",7.5F),Text=info==null?$"TEXTURE #{index:D3}":$"#{index:D3} • {info.Width}x{info.Height}",Cursor=Cursors.Hand,Tag=index};
                EventHandler open=async (_,_)=>await OpenTextureModuleForSmdAsync(visualSmdPath,index);picture.DoubleClick+=open;meta.DoubleClick+=open;card.DoubleClick+=open;
                var tip=new ToolTip();tip.SetToolTip(picture,$"Textura #{index:D3}\nDuplo clique para abrir no módulo de Texturas");card.Controls.Add(picture);card.Controls.Add(meta);flpVisualSmdTextures.Controls.Add(card);
            }
            catch(Exception ex){flpVisualSmdTextures.Controls.Add(new Label{Width=180,Height=45,ForeColor=Color.IndianRed,Text=$"#{index:D3} • preview indisponível\n{ex.Message}",Font=new Font("Segoe UI",7.5F)});}
        }
    }
    private void WireVisualSmdEvents()
    {
        visualViewport.SmdEntryClicked-=visualViewport_SmdEntryClicked;visualViewport.SmdEntryClicked+=visualViewport_SmdEntryClicked;
        visualViewport.SmdEntryEdited-=visualViewport_SmdEntryEdited;visualViewport.SmdEntryEdited+=visualViewport_SmdEntryEdited;
        visualViewport.SmdTransformModeRequested-=visualViewport_SmdTransformModeRequested;visualViewport.SmdTransformModeRequested+=visualViewport_SmdTransformModeRequested;
        visualViewport.DuplicateSmdRequested-=visualViewport_DuplicateSmdRequested;visualViewport.DuplicateSmdRequested+=visualViewport_DuplicateSmdRequested;
        visualViewport.DeleteSmdRequested-=visualViewport_DeleteSmdRequested;visualViewport.DeleteSmdRequested+=visualViewport_DeleteSmdRequested;
    }
    private void visualViewport_SmdEntryClicked(ScenarioEntry? entry)
    { if(entry==null){lstVisualSmdEntries.ClearSelected();pgVisualProperties.SelectedObject=null;return;}tabVisualEntities.SelectedIndex=4;lstVisualSmdEntries.SelectedIndexChanged-=lstVisualSmdEntries_SelectedIndexChanged;try{lstVisualSmdEntries.ClearSelected();lstVisualSmdEntries.SelectedItem=entry;}finally{lstVisualSmdEntries.SelectedIndexChanged+=lstVisualSmdEntries_SelectedIndexChanged;}lstVisualSmdEntries_SelectedIndexChanged(null,EventArgs.Empty); }
    private void visualViewport_SmdTransformModeRequested(SmdGizmoMode mode)=>SetSmdGizmoMode(mode);
    private void visualViewport_DuplicateSmdRequested()=>DuplicateSelectedSmdEntries();
    private void visualViewport_DeleteSmdRequested()=>DeleteSelectedSmdEntries();
    private void visualViewport_SmdEntryEdited(ScenarioEntry entry)
    { btnVisualSaveSmd.Text="SAVE SMD *";lstVisualSmdEntries.Refresh();if(ReferenceEquals(pgVisualProperties.SelectedObject,entry))pgVisualProperties.Refresh();UpdateTopVisualSaveState(); }
    private void SetSmdGizmoMode(SmdGizmoMode mode)
    { if(visualViewport==null)return;visualViewport.SmdTransformMode=mode;visualViewport.RefreshSmdGeometry(lstVisualSmdEntries.SelectedItem as ScenarioEntry);btnVisualSmdMove.BackColor=mode==SmdGizmoMode.Move?Accent:Surface2;btnVisualSmdRotate.BackColor=mode==SmdGizmoMode.Rotate?Accent:Surface2;btnVisualSmdScale.BackColor=mode==SmdGizmoMode.Scale?Accent:Surface2; }
    private bool SaveVisualSmd()
    {
        if(visualViewport?.Scene==null||string.IsNullOrWhiteSpace(visualSmdPath))return false;
        try
        {
            string backup=GetVisualAevBackupPath(visualSmdPath);Directory.CreateDirectory(Path.GetDirectoryName(backup)!);if(!File.Exists(backup))File.Copy(visualSmdPath,backup);
            Ps2ScenarioWriter.WriteTransforms(visualViewport.Scene,visualSmdPath);btnVisualSaveSmd.Text="SAVE SMD";ExtractLog($"Visual Editor: transformações SMD salvas em {Path.GetFileName(visualSmdPath)}.");return true;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Salvar SMD",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog("Visual Editor: erro ao salvar SMD: "+ex.Message);return false;}
    }
    private void btnVisualSaveSmd_Click(object? sender, EventArgs e) => SaveVisualSmd();

    private void btnVisualSmdNew_Click(object? sender,EventArgs e)
    {
        ScenarioEntry? source=lstVisualSmdEntries.SelectedItem as ScenarioEntry;if(source==null||visualViewport?.Scene==null||string.IsNullOrWhiteSpace(visualSmdPath))return;if(visualViewport.Scene.IsModified&&!SaveVisualSmd())return;EnsureVisualSmdBackup();var fresh=CloneSmdEntry(source);fresh.PositionX=fresh.PositionY=fresh.PositionZ=0;fresh.RotationX=fresh.RotationY=fresh.RotationZ=0;fresh.ScaleX=fresh.ScaleY=fresh.ScaleZ=1;int index=SmdEmbeddedBinService.AppendEntry(visualSmdPath,fresh,visualViewport.Scene.EntryCount,visualViewport.Scene.BinCount);ReloadVisualSmd(index);ExtractLog($"Visual Editor: nova entry SMD #{index:D3} adicionada no final usando BIN {source.BinId}.");
    }
    private void DuplicateSelectedSmdEntries()
    {
        ScenarioEntry[] selected=lstVisualSmdEntries.SelectedItems.Cast<ScenarioEntry>().ToArray();if(selected.Length==0||visualViewport?.Scene==null||string.IsNullOrWhiteSpace(visualSmdPath))return;if(visualViewport.Scene.IsModified&&!SaveVisualSmd())return;EnsureVisualSmdBackup();int entries=visualViewport.Scene.EntryCount,bins=visualViewport.Scene.BinCount,last=-1;foreach(ScenarioEntry source in selected){var clone=CloneSmdEntry(source);clone.PositionX+=1f;last=SmdEmbeddedBinService.AppendEntry(visualSmdPath,clone,entries++,bins);}ReloadVisualSmd(last);ExtractLog($"Visual Editor: {selected.Length} entry(s) SMD duplicada(s) no final.");
    }
    private void DeleteSelectedSmdEntries()
    {
        ScenarioEntry[] selected=lstVisualSmdEntries.SelectedItems.Cast<ScenarioEntry>().ToArray();ScenarioScene? scene=visualViewport?.Scene;if(selected.Length==0||scene==null||string.IsNullOrWhiteSpace(visualSmdPath))return;if(selected.Length>=scene.EntryCount){MessageBox.Show(this,"O SMD precisa manter pelo menos uma entry.","Excluir entries",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
        int lastProtected=settings.SmdProtectSpecialEntryIndices?scene.Entries.Where(e=>e.RawData.Length>0x38&&e.RawData[0x38]!=0x08).Select(e=>e.FileOrder).DefaultIfEmpty(-1).Max():-1;
        ScenarioEntry[] placeholders=settings.SmdProtectSpecialEntryIndices?selected.Where(e=>e.FileOrder<=lastProtected).ToArray():Array.Empty<ScenarioEntry>();ScenarioEntry[] physical=selected.Except(placeholders).ToArray();
        string protection=placeholders.Length>0?$"\n\nA trava preservará {placeholders.Length} índice(s), deixando esses slots invisíveis com escala zero.":"";
        if(MessageBox.Show(this,$"Excluir {selected.Length} entry(s) SMD?\n\nOs BINs incorporados serão preservados para não afetar outras referências.{protection}","Excluir entries",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        foreach(ScenarioEntry entry in placeholders)entry.ScaleX=entry.ScaleY=entry.ScaleZ=0f;if(placeholders.Length>0)scene.IsModified=true;if(scene.IsModified&&!SaveVisualSmd())return;EnsureVisualSmdBackup();
        int next=Math.Min(selected.Min(x=>x.FileOrder),scene.EntryCount-physical.Length-1);if(physical.Length>0)SmdEmbeddedBinService.RemoveEntries(visualSmdPath,physical.Select(x=>x.FileOrder).ToArray(),scene.EntryCount,scene.BinCount);ReloadVisualSmd(next);ExtractLog($"Visual Editor: {physical.Length} entry(s) removida(s) e {placeholders.Length} índice(s) protegido(s) preservado(s) como slots invisíveis.");
    }
    private void lstVisualSmdEntries_KeyDown(object? sender,KeyEventArgs e)
    {if(e.Control&&e.KeyCode==Keys.D){DuplicateSelectedSmdEntries();e.Handled=true;e.SuppressKeyPress=true;}else if(e.KeyCode==Keys.Delete){if(chkVisualSmdEditMode.Checked&&visualViewport.DeleteSelectedSmdFaces()){btnVisualSaveSmd.Text="SAVE SMD *";lstVisualSmdEntries.Refresh();}else DeleteSelectedSmdEntries();e.Handled=true;e.SuppressKeyPress=true;}}

    private async void btnVisualSmdImport_Click(object? sender, EventArgs e)
    {
        ScenarioEntry? entry=lstVisualSmdEntries.SelectedItem as ScenarioEntry;ScenarioScene? current=visualViewport?.Scene;
        if(entry==null||current==null||string.IsNullOrWhiteSpace(visualSmdPath))return;
        using var modelDialog=new OpenFileDialog{Filter="Modelos compatíveis (*.obj;*.smd)|*.obj;*.smd|Wavefront OBJ (*.obj)|*.obj|StudioModelData (*.smd)|*.smd",Title="Importar modelo para a entry SMD selecionada"};
        if(modelDialog.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            ExternalModelStats stats=ExternalPs2ModelConverter.Analyze(modelDialog.FileName);int shared=current.Entries.Count(x=>x.BinId==entry.BinId);
            string? texture=FindExternalModelTexture(modelDialog.FileName);
            if(texture==null)
            {
                using var textureDialog=new OpenFileDialog{Filter="Imagens (*.png;*.bmp;*.jpg;*.jpeg)|*.png;*.bmp;*.jpg;*.jpeg|Todos os arquivos (*.*)|*.*",Title="Selecione a textura do modelo (Cancelar = manter textura atual)"};
                if(textureDialog.ShowDialog(this)==DialogResult.OK)texture=textureDialog.FileName;
            }
            string warning=shared>1?$"\n\nATENÇÃO: o BIN {entry.BinId} é compartilhado por {shared} entries. Todas usarão o novo modelo.":"";
            string textureText=texture==null?"manter a textura atual":Path.GetFileName(texture);
            DialogResult importChoice=MessageBox.Show(this,$"Modelo: {Path.GetFileName(modelDialog.FileName)}\nVértices: {stats.Vertices:N0}\nFaces: {stats.Faces:N0}\nReceptor: BIN {entry.BinId}\nTextura: {textureText}{warning}\n\nSIM: adicionar como novo BIN + nova entry no final.\nNÃO: substituir o BIN selecionado.\nCANCELAR: não importar.","Importar modelo SMD",MessageBoxButtons.YesNoCancel,shared>1?MessageBoxIcon.Warning:MessageBoxIcon.Question);if(importChoice==DialogResult.Cancel)return;bool addAsNew=importChoice==DialogResult.Yes;
            string? converter=ResolveSmdExternalConverterPath();if(converter==null)return;
            if(current.IsModified&&!SaveVisualSmd())return;
            string backup=GetVisualAevBackupPath(visualSmdPath);Directory.CreateDirectory(Path.GetDirectoryName(backup)!);if(!File.Exists(backup))File.Copy(visualSmdPath,backup);
            byte[] receiver=SmdEmbeddedBinService.Extract(visualSmdPath,entry.BinId,current.BinCount);
            string tplPath=GetTplWorkPath(visualSmdPath,project.ActiveDatName??"");
            if(!File.Exists(tplPath))RE4_PS2_MOD_WORKSPACE.Core.Smd.SmdTextureService.ExtractTpl(visualSmdPath,tplPath);
            UseWaitCursor=true;btnVisualSmdImport.Enabled=false;lblVisualStatus.Text="Convertendo modelo para BIN PS2...";
            string generated=await ExternalPs2ModelConverter.ConvertAsync(converter,modelDialog.FileName,receiver);
            byte[] converted=File.ReadAllBytes(generated);int selectedAfter=entry.FileOrder;
            try
            {
                if(addAsNew&&texture!=null){int newTexture=new TextureWorkspaceService().AppendFromImage(tplPath,texture);if(newTexture>byte.MaxValue)throw new InvalidDataException("O TPL atingiu o limite de índices suportado pelo material BIN.");SmdEmbeddedBinService.SetBinMaterialTexture(converted,(byte)newTexture);}
                if(addAsNew)selectedAfter=SmdEmbeddedBinService.AppendEntry(visualSmdPath,entry,current.EntryCount,current.BinCount,converted);else SmdEmbeddedBinService.Replace(visualSmdPath,entry.BinId,current.BinCount,converted);
            }
            finally{try{File.Delete(generated);}catch{}}
            if(texture!=null&&!addAsNew)
            {
                int[] textureIndices=entry.LocalTriangles.Select(x=>x.TextureIndex).Where(x=>x>=0).Distinct().ToArray();
                if(textureIndices.Length==0)throw new InvalidDataException("O BIN receptor não possui materiais com textura para receber a imagem.");
                var textureService=new TextureWorkspaceService();foreach(int index in textureIndices)textureService.ReplaceFromImage(tplPath,index,texture);
            }
            if(texture!=null)RE4_PS2_MOD_WORKSPACE.Core.Smd.SmdTextureService.InjectTpl(visualSmdPath,tplPath);
            ScenarioCameraState camera=visualViewport.GetCameraState();ScenarioScene reloaded=await Task.Run(()=>Ps2ScenarioReader.Read(visualSmdPath));visualViewport.SetScene(reloaded);visualViewport.SetCameraState(camera);WireVisualSmdEvents();RefreshVisualSmdEntryList(reloaded.Entries.FirstOrDefault(x=>x.FileOrder==selectedAfter));visualViewport.SetTextureSource(tplPath);visualViewport.ReloadTextures(tplPath);
            btnVisualSaveSmd.Enabled=true;lblVisualStatus.Text=$"Modelo importado • BIN {entry.BinId} • {stats.Faces:N0} faces";ExtractLog($"Visual Editor: {Path.GetFileName(modelDialog.FileName)} importado no BIN SMD {entry.BinId} com {(texture==null?"textura preservada":Path.GetFileName(texture))}.");
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Importar modelo SMD",MessageBoxButtons.OK,MessageBoxIcon.Error);ExtractLog("Visual Editor: erro ao importar modelo SMD: "+ex.Message);}
        finally{UseWaitCursor=false;btnVisualSmdImport.Enabled=lstVisualSmdEntries?.SelectedItem is ScenarioEntry;}
    }

    private string? ResolveSmdExternalConverterPath()
    {
        if(!string.IsNullOrWhiteSpace(settings.Ps2BinToolPath)&&File.Exists(settings.Ps2BinToolPath))return settings.Ps2BinToolPath;
        string[] names={"RE4_PS2_BIN_TOOL.exe","Re4Ps2BINrepack.exe"};foreach(string folder in new[]{Application.StartupPath,Path.Combine(Application.StartupPath,"Tools")})foreach(string name in names){string path=Path.Combine(folder,name);if(File.Exists(path))return path;}
        using var dialog=new OpenFileDialog{Filter="RE4 PS2 BIN Tool (*.exe)|*.exe",Title="Selecione a RE4 PS2 BIN Tool"};if(dialog.ShowDialog(this)!=DialogResult.OK)return null;settings.Ps2BinToolPath=dialog.FileName;SaveSettings();return dialog.FileName;
    }
    private static string? FindExternalModelTexture(string modelPath)
    {
        string directory=Path.GetDirectoryName(modelPath)??"";string mtl=Path.ChangeExtension(modelPath,".mtl");
        if(File.Exists(mtl))foreach(string raw in File.ReadLines(mtl)){string line=raw.Trim();if(!line.StartsWith("map_Kd ",StringComparison.OrdinalIgnoreCase))continue;string value=line[7..].Trim().Trim('"');string candidate=Path.IsPathRooted(value)?value:Path.Combine(directory,value);if(File.Exists(candidate))return candidate;}
        string stem=Path.Combine(directory,Path.GetFileNameWithoutExtension(modelPath));foreach(string extension in new[]{".png",".bmp",".jpg",".jpeg"})if(File.Exists(stem+extension))return stem+extension;return null;
    }
    private void EnsureVisualSmdBackup(){string backup=GetVisualAevBackupPath(visualSmdPath!);Directory.CreateDirectory(Path.GetDirectoryName(backup)!);if(!File.Exists(backup))File.Copy(visualSmdPath!,backup);}
    private static ScenarioEntry CloneSmdEntry(ScenarioEntry e)=>new(){RawData=(byte[])e.RawData.Clone(),BinId=e.BinId,PositionX=e.PositionX,PositionY=e.PositionY,PositionZ=e.PositionZ,RotationX=e.RotationX,RotationY=e.RotationY,RotationZ=e.RotationZ,ScaleX=e.ScaleX,ScaleY=e.ScaleY,ScaleZ=e.ScaleZ,TextureIndex=e.TextureIndex,LocalTriangles=e.LocalTriangles};
    private void ReloadVisualSmd(int selectIndex)
    {
        ScenarioCameraState camera=visualViewport.GetCameraState();ScenarioScene scene=Ps2ScenarioReader.Read(visualSmdPath!);visualViewport.SetScene(scene);visualViewport.SetCameraState(camera);WireVisualSmdEvents();RefreshVisualSmdEntryList(scene.Entries.FirstOrDefault(x=>x.FileOrder==selectIndex));string tpl=GetTplWorkPath(visualSmdPath!,project.ActiveDatName??"");visualViewport.SetTextureSource(tpl);visualViewport.ReloadTextures(tpl);btnVisualSaveSmd.Enabled=true;
    }
}

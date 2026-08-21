using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using NVector3 = System.Numerics.Vector3;
using NQuaternion = System.Numerics.Quaternion;

namespace RE4_PS2_MOD_WORKSPACE;

public enum ScenarioRenderMode
{
    Solid,
    SolidWireframe,
    Wireframe
}

public enum EnemyGizmoMode
{
    Move,
    Rotate
}

public readonly record struct ScenarioCameraState(float X, float Y, float Z, float Yaw, float Pitch);

public sealed class ScenarioViewport : GLControl
{
    private ScenarioScene? scene;
    private float yaw = 0.75f;
    private float pitch = -0.35f;
    private float distance = 1000f;
    private NVector3 target = NVector3.Zero;
    private NVector3 cameraPosition = new(0f, 0f, -1000f);
    private float moveSpeed = 100f;
    private Point lastMouse;
    private MouseButtons dragButton = MouseButtons.None;
    private readonly HashSet<Keys> movementKeys = new();
    private readonly System.Windows.Forms.Timer movementTimer;
    private long lastMovementTick;

    private bool glReady;
    private bool gpuDirty;
    private int shaderProgram;
    private int uMvp;
    private int uColor;
    private int uUnlit;
    private int meshVao;
    private int meshVbo;
    private int meshVertexCount;
    private readonly List<ScenarioDrawBatch> meshBatches = new();
    private readonly Dictionary<int, int> glTextures = new();
    private readonly Dictionary<int, bool> glTextureHasTransparency = new();
    private string? textureSourcePath;
    private bool texturesDirty;
    private int uTexture;
    private int uUseTexture;
    private int gridVao;
    private int gridVbo;
    private int gridVertexCount;
    private AevScene? aevScene;
    private bool aevGpuDirty;
    private int aevVao;
    private int aevVbo;
    private int aevVertexCount;
    private int aevSelectedVao;
    private int aevSelectedVbo;
    private int aevSelectedVertexCount;
    private int aevFaceVao;
    private int aevFaceVbo;
    private int aevFaceVertexCount;
    private int aevSelectedFaceVao;
    private int aevSelectedFaceVbo;
    private int aevSelectedFaceVertexCount;
    private int aevHandleVao;
    private int aevHandleVbo;
    private int aevHandleVertexCount;
    private int selectedAevFileOrder = -1;
    private byte? aevTypeFilter;
    private Point mouseDownPoint;
    private bool leftMouseMoved;
    private int uOpacity;
    private EslScene? eslScene; private bool enemyGpuDirty; private int enemyVao, enemyVbo, enemyVertexCount, selectedEnemyVao, selectedEnemyVbo, selectedEnemyVertexCount; private int selectedEnemyIndex=-1;
    private IReadOnlyDictionary<byte, EnemyModelScene> enemyModels = new Dictionary<byte, EnemyModelScene>();
    // v0.4.3 model-parts debugger: hidden BIN ordinals are stored per emXX type.
    private readonly Dictionary<byte, HashSet<int>> hiddenEnemyModelParts = new();
    // Types touched by the MODEL PARTS debugger use the manual visibility mask.
    // Untouched types use the automatic v0.4.5 body/head/hands filter.
    private readonly HashSet<byte> manualEnemyModelPartTypes = new();
    private int enemyModelVao, enemyModelVbo, enemyModelVertexCount, selectedEnemyModelVao, selectedEnemyModelVbo, selectedEnemyModelVertexCount;
    private readonly List<EnemyModelDrawBatch> enemyModelBatches = new();
    private readonly List<EnemyModelDrawBatch> selectedEnemyModelBatches = new();
    private readonly Dictionary<EnemyTextureKey, int> glEnemyTextures = new();
    // Experimental v0.4.9 weapon attachment. The body skeleton is read from the enemy DAT.
    private FcvAnimation? enemyAttachmentAnimation;
    private float enemyAttachmentFrame;
    private bool enemyIdleAnimationEnabled;
    private float enemyIdleAnimationFrame;
    private int enemyAttachmentBoneIndex = -1;
    private NVector3 enemyAttachmentOffset = NVector3.Zero;
    private NVector3 enemyAttachmentRotationDegrees = NVector3.Zero;
    private bool enemyTexturesDirty = true;
    private byte? enemyStageFilter, enemyRoomFilter;
    private bool showInactiveEnemies;
    private const float EslWorldScale = 0.1f;
    private int enemyDragMode; // 1=X, 2=Y, 3=Z, 4=RotX, 5=RotY, 6=RotZ, 7=free X/Z
    private EslEnemyEntry? draggingEnemy;
    private short enemyDragStartX, enemyDragStartY, enemyDragStartZ, enemyDragStartRotX, enemyDragStartRotY, enemyDragStartRotZ;
    private Point enemyDragStartMouse;
    private NVector3 enemyDragStartWorld;
    private float enemyVerticalPixelsPerWorldUnit = 1f;

    private int labelShaderProgram;
    private int labelVao;
    private int labelVbo;
    private int labelTextureUniform;
    private readonly Dictionary<string, LabelTexture> labelTextures = new(StringComparer.Ordinal);

    private int draggingAevHandle = -1; // 0..3 corners, 4 bottom, 5 top, 6 move X/Z, 7 move Y
    private AevEntry? draggingAevEntry;
    private AevVertexState? dragStartState;
    private float heightDragStartMouseY;
    private float heightDragStartBottomY;
    private float heightDragStartTopY;
    private float heightDragPixelsPerWorldUnit = 1f;
    private float verticalMoveDragStartMouseY;
    private float verticalMoveStartY;
    private float verticalMovePixelsPerWorldUnit = 1f;
    private readonly Stack<Action> aevUndo = new();
    private readonly Stack<Action> enemyUndo = new();

    public event Action<AevEntry?>? AevEntryClicked;
    public event Action<AevEntry>? AevEntryEdited;
    public event Action? DuplicateAevRequested;
    public event Action? DeleteAevRequested;

    public bool ScenarioVisible { get; set; } = true;
    public bool AevVisible { get; set; } = true;
    public bool EnemiesVisible { get; set; } = true;
    public bool ShowInactiveEnemies
    {
        get => showInactiveEnemies;
        set { if (showInactiveEnemies == value) return; showInactiveEnemies = value; enemyGpuDirty = true; Invalidate(); }
    }
    public EslScene? EslScene => eslScene;
    public event Action<EslEnemyEntry?>? EnemyEntryClicked;
    public event Action<EslEnemyEntry>? EnemyEntryEdited;
    public AevScene? AevScene => aevScene;
    public ScenarioScene? Scene => scene;
    public int LoadedTextureCount => glTextures.Count;
    public int TexturedBatchCount => meshBatches.Count(x => x.TextureIndex >= 0 && glTextures.ContainsKey(x.TextureIndex));
    public int MeshBatchCount => meshBatches.Count;
    public float MovementSpeedMultiplier { get; set; } = 1f;
    public float LookSensitivity { get; set; } = 0.0032f;
    public bool ShowAevLabels { get; set; } = true;
    public bool ShowEnemyLabels { get; set; } = false;
    public ScenarioRenderMode RenderMode { get; set; } = ScenarioRenderMode.Solid;
    public EnemyGizmoMode EnemyTransformMode { get; set; } = EnemyGizmoMode.Move;
    public bool EnemySnapEnabled { get; set; }

    public ScenarioCameraState GetCameraState() =>
        new(cameraPosition.X, cameraPosition.Y, cameraPosition.Z, yaw, pitch);

    public void SetCameraState(ScenarioCameraState state)
    {
        if (!float.IsFinite(state.X) || !float.IsFinite(state.Y) || !float.IsFinite(state.Z) ||
            !float.IsFinite(state.Yaw) || !float.IsFinite(state.Pitch)) return;

        cameraPosition = new NVector3(state.X, state.Y, state.Z);
        yaw = state.Yaw;
        pitch = Math.Clamp(state.Pitch, -1.553f, 1.553f);
        target = cameraPosition + GetForward() * Math.Max(1f, distance);
        Invalidate();
    }

    public ScenarioViewport() : base(new GLControlSettings
    {
        API = ContextAPI.OpenGL,
        APIVersion = new Version(3, 3),
        Profile = ContextProfile.Core,
        NumberOfSamples = 4,
        IsEventDriven = true
    })
    {
        BackColor = Color.FromArgb(8, 10, 13);
        ForeColor = Color.FromArgb(175, 181, 191);
        TabStop = true;

        movementTimer = new System.Windows.Forms.Timer { Interval = 16 };
        movementTimer.Tick += MovementTimer_Tick;
        movementTimer.Start();
        lastMovementTick = Environment.TickCount64;
    }

    public void SetScene(ScenarioScene? value)
    {
        scene = value;
        gpuDirty = true;
        FitScene();
        Invalidate();
    }

    public void SetTextureSource(string? tplPath)
    {
        textureSourcePath = !string.IsNullOrWhiteSpace(tplPath) && File.Exists(tplPath) ? tplPath : null;
        texturesDirty = true;
        Invalidate();
    }

    public void ReloadTextures(string? tplPath = null)
    {
        if (!string.IsNullOrWhiteSpace(tplPath)) textureSourcePath = tplPath;
        texturesDirty = true;
        Invalidate();
    }

    public void SetEslScene(EslScene? value) { eslScene=value; selectedEnemyIndex=-1; enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyModels(IReadOnlyDictionary<byte, EnemyModelScene>? models)
    {
        enemyModels = models ?? new Dictionary<byte, EnemyModelScene>();
        enemyGpuDirty = true;
        enemyTexturesDirty = true;
        Invalidate();
    }
    public IReadOnlyDictionary<byte, EnemyModelScene> EnemyModels => enemyModels;
    public int EnemyAttachmentBoneIndex => enemyAttachmentBoneIndex;
    public NVector3 EnemyAttachmentOffset => enemyAttachmentOffset;
    public NVector3 EnemyAttachmentRotationDegrees => enemyAttachmentRotationDegrees;
    public IReadOnlyList<Ps2BinBone> GetEnemyAttachmentBones(byte enemyType) => enemyModels.TryGetValue(enemyType, out EnemyModelScene? m) && m.Skeleton != null ? m.Skeleton.Bones : Array.Empty<Ps2BinBone>();
    public int GetEnemySkeletonSource(byte enemyType) => enemyModels.TryGetValue(enemyType, out EnemyModelScene? m) ? m.SkeletonSourceDatEntryIndex : -1;
    public void SetEnemyAttachmentBone(int index) { enemyAttachmentBoneIndex=index; enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyAttachmentOffset(float x,float y,float z) { enemyAttachmentOffset=new NVector3(x,y,z); enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyAttachmentRotation(float x,float y,float z) { enemyAttachmentRotationDegrees=new NVector3(x,y,z); enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyAttachmentAnimation(FcvAnimation? animation,float frame) { enemyAttachmentAnimation=animation; enemyAttachmentFrame=frame; enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyIdleAnimation(bool enabled, float frame) { enemyIdleAnimationEnabled=enabled; enemyIdleAnimationFrame=frame; enemyGpuDirty=true; Invalidate(); }
    public bool IsEnemyModelPartVisible(byte enemyType, int binIndex) => !hiddenEnemyModelParts.TryGetValue(enemyType, out HashSet<int>? hidden) || !hidden.Contains(binIndex);
    public bool IsEnemyModelPartAutomaticallyVisible(EslEnemyEntry entry, EnemyModelPart part)
    {
        if (manualEnemyModelPartTypes.Contains(entry.EnemyType)) return IsEnemyModelPartVisible(entry.EnemyType, part.BinIndex);
        if (!enemyModels.TryGetValue(entry.EnemyType, out EnemyModelScene? model) || !EnemyModelPartCatalog.CanApplyAutomaticCoreParts(model, entry.EnemyType, entry.Subtype))
            return IsEnemyModelPartVisible(entry.EnemyType, part.BinIndex);
        IReadOnlySet<int> core = EnemyModelPartCatalog.GetAutomaticCoreParts(entry.EnemyType, entry.Subtype)!;
        if (core.Contains(part.DatEntryIndex)) return true;
        if (enemyModels.TryGetValue(entry.EnemyType, out EnemyModelScene? equipmentModel))
            return EnemyEquipmentCatalog.GetRenderableParts(entry, equipmentModel).Contains(part.DatEntryIndex);
        return false;
    }
    public void SetEnemyModelPartVisible(byte enemyType, int binIndex, bool visible)
    {
        manualEnemyModelPartTypes.Add(enemyType);
        if (!hiddenEnemyModelParts.TryGetValue(enemyType, out HashSet<int>? hidden)) { hidden = new HashSet<int>(); hiddenEnemyModelParts[enemyType] = hidden; }
        if (visible) hidden.Remove(binIndex); else hidden.Add(binIndex);
        if (hidden.Count == 0) hiddenEnemyModelParts.Remove(enemyType);
        enemyGpuDirty = true; Invalidate();
    }
    public void ShowAllEnemyModelParts(byte enemyType) { manualEnemyModelPartTypes.Add(enemyType); hiddenEnemyModelParts.Remove(enemyType); enemyGpuDirty=true; Invalidate(); }
    public void UseAutomaticEnemyModelParts(byte enemyType) { manualEnemyModelPartTypes.Remove(enemyType); hiddenEnemyModelParts.Remove(enemyType); enemyGpuDirty=true; Invalidate(); }
    public void SoloEnemyModelPart(byte enemyType, int binIndex)
    {
        if (!enemyModels.TryGetValue(enemyType, out EnemyModelScene? model)) return;
        manualEnemyModelPartTypes.Add(enemyType);
        var hidden = new HashSet<int>(model.Parts.Where(x => x.BinIndex != binIndex).Select(x => x.BinIndex));
        if (hidden.Count == 0) hiddenEnemyModelParts.Remove(enemyType); else hiddenEnemyModelParts[enemyType] = hidden;
        enemyGpuDirty=true; Invalidate();
    }
    public void SetEnemyLocationFilter(byte? stageId, byte? roomId) { enemyStageFilter=stageId; enemyRoomFilter=roomId; selectedEnemyIndex=-1; enemyGpuDirty=true; Invalidate(); }
    private bool EnemyPassesLocationFilter(EslEnemyEntry e) => (!enemyStageFilter.HasValue || e.StageID==enemyStageFilter.Value) && (!enemyRoomFilter.HasValue || e.RoomID==enemyRoomFilter.Value);
    private bool EnemyIsVisible(EslEnemyEntry e) => (showInactiveEnemies || e.Active != 0) && EnemyPassesLocationFilter(e);
    private static NVector3 EslToWorld(EslEnemyEntry e) => new(e.PosX*EslWorldScale,e.PosY*EslWorldScale,e.PosZ*EslWorldScale);
    public void SelectEnemyEntry(EslEnemyEntry? entry) { selectedEnemyIndex=entry?.Index ?? -1; enemyGpuDirty=true; Invalidate(); }
    public void RefreshEnemyGeometry(EslEnemyEntry? entry=null) { if(entry!=null) selectedEnemyIndex=entry.Index; enemyGpuDirty=true; Invalidate(); }

    public void SetAevScene(AevScene? value)
    {
        aevScene = value;
        selectedAevFileOrder = -1;
        aevGpuDirty = true;
        Invalidate();
    }

    public void SelectAevEntry(AevEntry? entry)
    {
        selectedAevFileOrder = entry?.FileOrder ?? -1;
        aevGpuDirty = true;
        Invalidate();
    }

    public void SetAevTypeFilter(byte? type)
    {
        aevTypeFilter = type;
        if (selectedAevFileOrder >= 0)
        {
            AevEntry? selected = GetSelectedAevEntry();
            if (selected != null && aevTypeFilter.HasValue && selected.Type != aevTypeFilter.Value)
                selectedAevFileOrder = -1;
        }
        aevGpuDirty = true;
        Invalidate();
    }

    public void RegisterAevUndo(Action undoAction)
    {
        if (undoAction == null) return;
        aevUndo.Push(undoAction);
        TrimUndoStack();
    }

    public void RegisterEnemyUndo(Action undoAction)
    {
        if (undoAction == null) return;
        enemyUndo.Push(undoAction);
        TrimEnemyUndoStack();
    }

    public bool UndoEnemyEdit()
    {
        if (enemyUndo.Count == 0) return false;
        Action undo = enemyUndo.Pop();
        undo();
        return true;
    }

    public bool UndoAevEdit()
    {
        if (aevUndo.Count == 0) return false;
        Action undo = aevUndo.Pop();
        undo();
        return true;
    }

    public void RefreshAevSceneGeometry(AevEntry? selected = null)
    {
        selectedAevFileOrder = selected?.FileOrder ?? -1;
        aevGpuDirty = true;
        Invalidate();
    }

    public void NotifyAevPropertyEdited(AevEntry entry, string propertyName, object? oldValue)
    {
        AevVertexState after = AevVertexState.From(entry);
        AevVertexState before = after;

        try
        {
            float oldFloat = oldValue == null ? 0f : Convert.ToSingle(oldValue, System.Globalization.CultureInfo.InvariantCulture);
            before = before.WithOldProperty(propertyName, oldFloat);
        }
        catch
        {
            // If a future non-float property reaches here, redraw it but do not create
            // an invalid undo state.
            aevGpuDirty = true;
            AevEntryEdited?.Invoke(entry);
            Invalidate();
            return;
        }

        if (!before.Equals(after))
        {
            AevVertexState restore = before;
            aevUndo.Push(() =>
            {
                restore.Apply(entry);
                selectedAevFileOrder = entry.FileOrder;
                aevGpuDirty = true;
                AevEntryEdited?.Invoke(entry);
                AevEntryClicked?.Invoke(entry);
                Invalidate();
            });
            TrimUndoStack();
        }

        selectedAevFileOrder = entry.FileOrder;
        aevGpuDirty = true;
        AevEntryEdited?.Invoke(entry);
        Invalidate();
    }

    public void SetRenderMode(ScenarioRenderMode mode)
    {
        RenderMode = mode;
        Invalidate();
    }

    public void FitScene()
    {
        if (scene == null)
        {
            target = NVector3.Zero;
            distance = 1000f;
            yaw = 0.75f;
            pitch = -0.35f;
            cameraPosition = new NVector3(0f, 0f, -1000f);
            moveSpeed = 100f;
            Invalidate();
            return;
        }

        target = scene.Center;
        yaw = 0.75f;
        // Start Fit with a level camera. This makes W/S neutral and prevents
        // the initial view from injecting vertical movement into true-forward navigation.
        pitch = 0f;

        // Enquadra a bounding sphere levando em conta tanto o FOV vertical quanto
        // o horizontal. Assim F continua centralizado e funciona corretamente em
        // janelas largas, estreitas ou redimensionadas.
        float aspect = Math.Max(0.01f, ClientSize.Width / (float)Math.Max(1, ClientSize.Height));
        float vfov = MathHelper.DegreesToRadians(60f);
        float hfov = 2f * MathF.Atan(MathF.Tan(vfov * 0.5f) * aspect);
        float limitingFov = Math.Min(vfov, hfov);
        distance = Math.Max(10f, scene.Radius / MathF.Max(0.05f, MathF.Sin(limitingFov * 0.5f)) * 1.08f);
        NVector3 forward = GetForward();
        cameraPosition = target - forward * distance;
        moveSpeed = Math.Max(scene.Radius * 0.12f, 0.25f);
        Invalidate();
    }

    public void FocusEnemy(EslEnemyEntry? enemy)
    {
        if (enemy == null) return;

        NVector3 origin = EslToWorld(enemy);
        target = origin + new NVector3(0f, 8f, 0f);
        distance = 42f;

        // Focus is intentionally independent from the current camera pitch/yaw.
        // RotY describes the enemy's horizontal facing direction. Put the camera
        // in front of that direction at the same height as the target, then look
        // straight back at the enemy. This prevents F from diving under the floor
        // or flying above the model when the previous view was pitched up/down.
        float enemyYaw = enemy.RotY * (MathF.PI / 32768f);
        NVector3 enemyForward = new(MathF.Sin(enemyYaw), 0f, MathF.Cos(enemyYaw));
        if (enemyForward.LengthSquared() < 0.000001f) enemyForward = NVector3.UnitZ;
        else enemyForward = NVector3.Normalize(enemyForward);

        cameraPosition = target + enemyForward * distance;
        NVector3 viewDirection = NVector3.Normalize(target - cameraPosition);
        yaw = MathF.Atan2(viewDirection.X, viewDirection.Z);
        pitch = 0f;

        // Do not touch moveSpeed here. F is a framing operation only; changing
        // moveSpeed made navigation progressively slower after every focus.
        movementKeys.Clear();
        lastMovementTick = Environment.TickCount64;
        Invalidate();
        Focus();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (IsDesignMode) return;
        MakeCurrent();
        InitializeGl();
        gpuDirty = true;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (!glReady || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        MakeCurrent();
        GL.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (IsDesignMode || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        MakeCurrent();
        if (!glReady) InitializeGl();
        if (gpuDirty) UploadScene();
        if (texturesDirty) UploadTextures();
        if (enemyTexturesDirty) UploadEnemyTextures();
        if (aevGpuDirty) UploadAev();
        if (enemyGpuDirty) UploadEnemies();

        GL.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
        GL.ClearColor(8f / 255f, 10f / 255f, 13f / 255f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if ((scene != null && ScenarioVisible && meshVertexCount > 0) || (aevScene != null && AevVisible && aevVertexCount > 0) || (eslScene != null && EnemiesVisible && enemyVertexCount > 0))
        {
            Matrix4 mvp = BuildMvp();
            GL.UseProgram(shaderProgram);
            GL.UniformMatrix4(uMvp, true, ref mvp);
            GL.Uniform1(uOpacity, 1.0f);

            if (scene != null && ScenarioVisible && meshVertexCount > 0)
            {
                DrawGridGpu();
                DrawMeshGpu();
            }
            if (aevScene != null && AevVisible && aevVertexCount > 0) DrawAevGpu();
            if (eslScene != null && EnemiesVisible && enemyVertexCount > 0) DrawEnemiesGpu();

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        if (ShowAevLabels && AevVisible && aevScene != null) DrawAevLabelsGpu();
        if (ShowEnemyLabels && EnemiesVisible && eslScene != null) DrawEnemyLabelsGpu();

        SwapBuffers();
    }

    private void DrawAevLabelsGpu()
    {
        if (aevScene == null || labelShaderProgram == 0) return;

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.UseProgram(labelShaderProgram);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Uniform1(labelTextureUniform, 0);
        GL.BindVertexArray(labelVao);

        foreach (AevEntry entry in aevScene.Entries)
        {
            if (aevTypeFilter.HasValue && entry.Type != aevTypeFilter.Value) continue;
            if (!entry.IsSquare && !entry.IsCircle) continue;

            GetAevYRange(entry, out _, out float y1);
            System.Numerics.Vector2 center = entry.IsCircle ? entry.Position1 : GetAevCenterXZ(entry);
            NVector3 world = new(center.X, y1 + Math.Max(0.05f, (scene?.Radius ?? 1f) * 0.001f), center.Y);
            if (!TryProjectWorldToScreen(world, out PointF screen)) continue;

            bool selected = entry.FileOrder == selectedAevFileOrder;
            string text = $"#{entry.Index:X2} {AevNames.EventTypeName(entry.Type)}";
            LabelTexture label = GetOrCreateLabelTexture(text, selected);

            float leftPx = screen.X - label.Width * 0.5f;
            float topPx = screen.Y - label.Height - 8f;
            if (leftPx + label.Width < 0 || topPx + label.Height < 0 ||
                leftPx > ClientSize.Width || topPx > ClientSize.Height)
                continue;

            float x0 = leftPx / ClientSize.Width * 2f - 1f;
            float x1 = (leftPx + label.Width) / ClientSize.Width * 2f - 1f;
            float y0 = 1f - topPx / ClientSize.Height * 2f;
            float y1Ndc = 1f - (topPx + label.Height) / ClientSize.Height * 2f;

            float[] quad =
            {
                x0, y0,    0f, 0f,
                x0, y1Ndc, 0f, 1f,
                x1, y1Ndc, 1f, 1f,
                x0, y0,    0f, 0f,
                x1, y1Ndc, 1f, 1f,
                x1, y0,    1f, 0f
            };

            GL.BindTexture(TextureTarget.Texture2D, label.TextureId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, labelVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StreamDraw);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void DrawEnemyLabelsGpu()
    {
        if (eslScene == null || labelShaderProgram == 0) return;

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.UseProgram(labelShaderProgram);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Uniform1(labelTextureUniform, 0);
        GL.BindVertexArray(labelVao);

        foreach (EslEnemyEntry entry in eslScene.Entries.Where(EnemyIsVisible))
        {
            NVector3 world = EslToWorld(entry) + new NVector3(0f, Math.Max(0.08f, (scene?.Radius ?? 1f) * 0.0012f), 0f);
            if (!TryProjectWorldToScreen(world, out PointF screen)) continue;

            bool selected = entry.Index == selectedEnemyIndex;
            string text = $"#{entry.Index:D3} {entry.FriendlyName}";
            LabelTexture label = GetOrCreateLabelTexture(text, selected);
            float leftPx = screen.X - label.Width * 0.5f;
            float topPx = screen.Y - label.Height - 10f;
            if (leftPx + label.Width < 0 || topPx + label.Height < 0 || leftPx > ClientSize.Width || topPx > ClientSize.Height) continue;

            float x0 = leftPx / ClientSize.Width * 2f - 1f;
            float x1 = (leftPx + label.Width) / ClientSize.Width * 2f - 1f;
            float y0 = 1f - topPx / ClientSize.Height * 2f;
            float y1 = 1f - (topPx + label.Height) / ClientSize.Height * 2f;
            float[] quad = { x0,y0,0f,0f, x0,y1,0f,1f, x1,y1,1f,1f, x0,y0,0f,0f, x1,y1,1f,1f, x1,y0,1f,0f };
            GL.BindTexture(TextureTarget.Texture2D, label.TextureId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, labelVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StreamDraw);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        GL.BindTexture(TextureTarget.Texture2D, 0); GL.BindBuffer(BufferTarget.ArrayBuffer, 0); GL.BindVertexArray(0); GL.UseProgram(0);
        GL.Disable(EnableCap.Blend); GL.Enable(EnableCap.DepthTest); GL.Enable(EnableCap.CullFace);
    }

    private LabelTexture GetOrCreateLabelTexture(string text, bool selected)
    {
        string key = (selected ? "S|" : "N|") + text;
        if (labelTextures.TryGetValue(key, out LabelTexture? existing)) return existing;

        using Font font = new Font("Segoe UI Semibold", 8.5f);
        Size textSize = TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
        int width = Math.Max(1, textSize.Width + 10);
        int height = Math.Max(1, textSize.Height + 6);

        using Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(selected ? Color.FromArgb(220, 74, 48, 8) : Color.FromArgb(195, 12, 17, 23));
            TextRenderer.DrawText(g, text, font, new Rectangle(5, 3, width - 10, height - 6),
                selected ? Color.FromArgb(255, 232, 176) : Color.FromArgb(238, 242, 248),
                TextFormatFlags.NoPadding | TextFormatFlags.Left | TextFormatFlags.Top);
        }

        int texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        Rectangle rect = new Rectangle(0, 0, width, height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        var created = new LabelTexture(texture, width, height);
        labelTextures[key] = created;
        return created;
    }

    private sealed record LabelTexture(int TextureId, int Width, int Height);

    private void InitializeGl()
    {
        if (glReady) return;

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Lequal);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.Multisample);

        const string vertexShader = @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUv;
uniform mat4 uMvp;
out vec3 vNormal;
out vec2 vUv;
void main()
{
    gl_Position = vec4(aPos, 1.0) * uMvp;
    vNormal = aNormal;
    vUv = aUv;
}";

        const string fragmentShader = @"#version 330 core
in vec3 vNormal;
in vec2 vUv;
uniform vec3 uColor;
uniform int uUnlit;
uniform int uUseTexture;
uniform sampler2D uTexture;
uniform float uOpacity;
out vec4 FragColor;
void main()
{
    float shade = 1.0;
    if (uUnlit == 0)
    {
        vec3 n = normalize(vNormal);
        vec3 l = normalize(vec3(-0.35, 0.75, -0.55));
        float diffuse = abs(dot(n, l));
        shade = 0.42 + diffuse * 0.58;
    }

    vec4 baseColor = vec4(uColor, 1.0);
    if (uUseTexture != 0)
    {
        baseColor = texture(uTexture, vUv);
        // PS2 scenario textures frequently use transparent black texels for
        // foliage/fences/cutout geometry. Do not force those texels opaque.
        if (baseColor.a <= 0.01) discard;
    }
    FragColor = vec4(baseColor.rgb * shade, baseColor.a * uOpacity);
}";

        int vs = CompileShader(ShaderType.VertexShader, vertexShader);
        int fs = CompileShader(ShaderType.FragmentShader, fragmentShader);
        shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vs);
        GL.AttachShader(shaderProgram, fs);
        GL.LinkProgram(shaderProgram);
        GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int linked);
        if (linked == 0) throw new InvalidOperationException("OpenGL shader link failed: " + GL.GetProgramInfoLog(shaderProgram));
        GL.DetachShader(shaderProgram, vs);
        GL.DetachShader(shaderProgram, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        uMvp = GL.GetUniformLocation(shaderProgram, "uMvp");
        uColor = GL.GetUniformLocation(shaderProgram, "uColor");
        uUnlit = GL.GetUniformLocation(shaderProgram, "uUnlit");
        uTexture = GL.GetUniformLocation(shaderProgram, "uTexture");
        uUseTexture = GL.GetUniformLocation(shaderProgram, "uUseTexture");
        uOpacity = GL.GetUniformLocation(shaderProgram, "uOpacity");

        const string labelVertexShader = @"#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aUv;
out vec2 vUv;
void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0);
    vUv = aUv;
}";
        const string labelFragmentShader = @"#version 330 core
in vec2 vUv;
uniform sampler2D uLabelTexture;
out vec4 FragColor;
void main()
{
    FragColor = texture(uLabelTexture, vUv);
}";

        int labelVs = CompileShader(ShaderType.VertexShader, labelVertexShader);
        int labelFs = CompileShader(ShaderType.FragmentShader, labelFragmentShader);
        labelShaderProgram = GL.CreateProgram();
        GL.AttachShader(labelShaderProgram, labelVs);
        GL.AttachShader(labelShaderProgram, labelFs);
        GL.LinkProgram(labelShaderProgram);
        GL.GetProgram(labelShaderProgram, GetProgramParameterName.LinkStatus, out int labelLinked);
        if (labelLinked == 0) throw new InvalidOperationException("OpenGL label shader link failed: " + GL.GetProgramInfoLog(labelShaderProgram));
        GL.DetachShader(labelShaderProgram, labelVs);
        GL.DetachShader(labelShaderProgram, labelFs);
        GL.DeleteShader(labelVs);
        GL.DeleteShader(labelFs);
        labelTextureUniform = GL.GetUniformLocation(labelShaderProgram, "uLabelTexture");

        labelVao = GL.GenVertexArray();
        labelVbo = GL.GenBuffer();
        GL.BindVertexArray(labelVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, labelVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 6 * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        meshVao = GL.GenVertexArray();
        meshVbo = GL.GenBuffer();
        gridVao = GL.GenVertexArray();
        gridVbo = GL.GenBuffer();
        aevVao = GL.GenVertexArray();
        aevVbo = GL.GenBuffer();
        aevSelectedVao = GL.GenVertexArray();
        aevSelectedVbo = GL.GenBuffer();
        aevFaceVao = GL.GenVertexArray();
        aevFaceVbo = GL.GenBuffer();
        aevSelectedFaceVao = GL.GenVertexArray();
        aevSelectedFaceVbo = GL.GenBuffer();
        aevHandleVao = GL.GenVertexArray();
        aevHandleVbo = GL.GenBuffer();
        enemyVao=GL.GenVertexArray(); enemyVbo=GL.GenBuffer(); selectedEnemyVao=GL.GenVertexArray(); selectedEnemyVbo=GL.GenBuffer();
        enemyModelVao=GL.GenVertexArray(); enemyModelVbo=GL.GenBuffer(); selectedEnemyModelVao=GL.GenVertexArray(); selectedEnemyModelVbo=GL.GenBuffer();
        glReady = true;
    }

    private static int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0)
        {
            string log = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"OpenGL {type} compile failed: {log}");
        }
        return shader;
    }

    private void UploadScene()
    {
        gpuDirty = false;
        meshVertexCount = 0;
        gridVertexCount = 0;
        meshBatches.Clear();
        if (scene == null) return;

        int triCount = scene.Triangles.Count;

        // Keep triangles grouped by diffuse texture. This lets one VBO serve the
        // entire SMD while OpenGL changes texture only between material batches.
        ScenarioTriangle[] ordered = scene.Triangles
            .OrderBy(x => x.TextureIndex)
            .ToArray();

        float[] meshData = new float[triCount * 24]; // 3 vertices * (position3 + normal3 + uv2)

        var normalSums = new Dictionary<NVector3, NVector3>(Math.Min(triCount * 2, 1_000_000));
        foreach (ScenarioTriangle tri in ordered)
        {
            NVector3 n = NVector3.Cross(tri.B - tri.A, tri.C - tri.A);
            float lenSq = n.LengthSquared();
            if (lenSq < 0.000001f || !float.IsFinite(lenSq)) continue;
            AddNormal(normalSums, tri.A, n);
            AddNormal(normalSums, tri.B, n);
            AddNormal(normalSums, tri.C, n);
        }

        int o = 0;
        int currentTexture = int.MinValue;
        int batchFirst = 0;
        int batchVertices = 0;

        foreach (ScenarioTriangle tri in ordered)
        {
            if (tri.TextureIndex != currentTexture)
            {
                if (batchVertices > 0) meshBatches.Add(new ScenarioDrawBatch(currentTexture, batchFirst, batchVertices));
                currentTexture = tri.TextureIndex;
                batchFirst = o / 8;
                batchVertices = 0;
            }

            WriteTexturedVertex(meshData, ref o, tri.A, GetSmoothNormal(normalSums, tri.A), tri.UvA);
            WriteTexturedVertex(meshData, ref o, tri.B, GetSmoothNormal(normalSums, tri.B), tri.UvB);
            WriteTexturedVertex(meshData, ref o, tri.C, GetSmoothNormal(normalSums, tri.C), tri.UvC);
            batchVertices += 3;
        }
        if (batchVertices > 0) meshBatches.Add(new ScenarioDrawBatch(currentTexture, batchFirst, batchVertices));
        meshVertexCount = o / 8;

        GL.BindVertexArray(meshVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, meshVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, o * sizeof(float), meshData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
        GL.EnableVertexAttribArray(2);

        float[] gridData = BuildGridData(scene);
        gridVertexCount = gridData.Length / 6;
        GL.BindVertexArray(gridVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, gridVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, gridData.Length * sizeof(float), gridData, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.DisableVertexAttribArray(2);
        GL.VertexAttrib2(2, 0f, 0f);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    private void UploadTextures()
    {
        texturesDirty = false;
        ReleaseTextures();
        if (string.IsNullOrWhiteSpace(textureSourcePath) || !File.Exists(textureSourcePath)) return;

        var service = new TextureWorkspaceService();
        IReadOnlyList<TextureInfo> catalog;
        try { catalog = service.ReadCatalog(textureSourcePath); }
        catch { return; }

        foreach (TextureInfo info in catalog)
        {
            try
            {
                using Bitmap bitmap = service.Decode(textureSourcePath, info.Index);
                bool hasTransparency = BitmapHasTransparency(bitmap);
                int texture = CreateGlTexture(bitmap);
                glTextures[info.Index] = texture;
                glTextureHasTransparency[info.Index] = hasTransparency;
            }
            catch
            {
                // One unsupported/broken texture must not prevent the rest of the SMD.
            }
        }
    }

    private void UploadEnemyTextures()
    {
        enemyTexturesDirty = false;
        ReleaseEnemyTextures();
        if (enemyModels.Count == 0) return;

        var reader = new TplReader();
        var decoder = new TextureDecoder();

        foreach (var modelPair in enemyModels)
        {
            byte enemyType = modelPair.Key;
            EnemyModelScene model = modelPair.Value;

            foreach (EnemyTexturePackage package in model.TexturePackages.Values)
            {
                try
                {
                    using var stream = new MemoryStream(package.Data, writable: false);
                    using var br = new BinaryReader(stream);
                    if (stream.Length < 8) continue;
                    stream.Position = 4;
                    uint rawCount = br.ReadUInt32();
                    int count = rawCount > 128 ? 128 : (int)rawCount;

                    for (int textureIndex = 0; textureIndex < count; textureIndex++)
                    {
                        try
                        {
                            stream.Position = 0;
                            var tpl = reader.ReadTexture(br, textureIndex);
                            stream.Position = 0;
                            using Bitmap bitmap = decoder.Decode(tpl, br);
                            int texture = CreateGlTexture(bitmap);
                            glEnemyTextures[new EnemyTextureKey(enemyType, package.DatEntryIndex, textureIndex)] = texture;
                        }
                        catch
                        {
                            // One unsupported texture should not prevent the remaining enemy textures.
                        }
                    }
                }
                catch
                {
                    // Keep the model usable as untextured if a package is malformed.
                }
            }
        }
    }

    private void ReleaseEnemyTextures()
    {
        foreach (int texture in glEnemyTextures.Values)
            if (texture != 0) GL.DeleteTexture(texture);
        glEnemyTextures.Clear();
    }

    private static bool BitmapHasTransparency(Bitmap source)
    {
        using var bitmap = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap)) g.DrawImageUnscaled(source, 0, 0);

        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            int stride = Math.Abs(data.Stride);
            byte[] row = new byte[stride];

            for (int y = 0; y < bitmap.Height; y++)
            {
                IntPtr rowPtr = IntPtr.Add(data.Scan0, y * data.Stride);
                Marshal.Copy(rowPtr, row, 0, stride);
                for (int x = 0; x < bitmap.Width; x++)
                {
                    byte alpha = row[x * 4 + 3];
                    if (alpha < 250) return true;
                }
            }
            return false;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static int CreateGlTexture(Bitmap source)
    {
        // Convert once to a known BGRA byte layout and upload directly.
        // OpenGL receives BGRA bytes and stores them internally as RGBA8.
        using var bitmap = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap)) g.DrawImageUnscaled(source, 0, 0);

        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try
        {
            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                bitmap.Width, bitmap.Height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return texture;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private void ReleaseTextures()
    {
        foreach (int texture in glTextures.Values)
            if (texture != 0) GL.DeleteTexture(texture);
        glTextures.Clear();
        glTextureHasTransparency.Clear();
    }

    private void UploadAev()
    {
        aevGpuDirty = false;
        aevVertexCount = 0;
        aevSelectedVertexCount = 0;
        aevFaceVertexCount = 0;
        aevSelectedFaceVertexCount = 0;
        aevHandleVertexCount = 0;
        if (aevScene == null || !glReady) return;

        var allLines = new List<float>(aevScene.Count * 96);
        var selectedLines = new List<float>(96);
        var allFaces = new List<float>(aevScene.Count * 216);
        var selectedFaces = new List<float>(216);
        var handles = new List<float>(192);

        foreach (AevEntry entry in aevScene.Entries)
        {
            if (aevTypeFilter.HasValue && entry.Type != aevTypeFilter.Value) continue;

            AddAevVolumeLines(allLines, entry);
            AddAevVolumeFaces(allFaces, entry);

            if (entry.FileOrder == selectedAevFileOrder)
            {
                AddAevVolumeLines(selectedLines, entry);
                AddAevVolumeFaces(selectedFaces, entry);
                if (entry.IsSquare)
                {
                    AddAevCornerHandles(handles, entry, scene?.Radius ?? 1f);
                    AddAevHeightHandles(handles, entry, scene?.Radius ?? 1f);
                }
                if (entry.IsSquare || entry.IsCircle)
                    AddAevMoveHandle(handles, entry, scene?.Radius ?? 1f);
            }
        }

        UploadLineBuffer(aevVao, aevVbo, allLines, out aevVertexCount);
        UploadLineBuffer(aevSelectedVao, aevSelectedVbo, selectedLines, out aevSelectedVertexCount);
        UploadLineBuffer(aevFaceVao, aevFaceVbo, allFaces, out aevFaceVertexCount);
        UploadLineBuffer(aevSelectedFaceVao, aevSelectedFaceVbo, selectedFaces, out aevSelectedFaceVertexCount);
        UploadLineBuffer(aevHandleVao, aevHandleVbo, handles, out aevHandleVertexCount);
    }

    private static void UploadLineBuffer(int vao, int vbo, List<float> values, out int vertexCount)
    {
        float[] data = values.ToArray();
        vertexCount = data.Length / 6;
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    private static void AddAevVolumeLines(List<float> values, AevEntry entry)
    {
        // RE4 PS2 AEV uses the opposite vertical sign from the SMD geometry as decoded
        // by this viewport. Keep the raw values intact in Properties and convert only for GL.
        GetAevYRange(entry, out float y0, out float y1);

        if (entry.IsCircle)
        {
            float r = entry.VisualRadius;
            const int segments = 32;
            for (int i = 0; i < segments; i++)
            {
                float a0 = MathF.Tau * i / segments;
                float a1 = MathF.Tau * (i + 1) / segments;
                NVector3 b0 = new(entry.Position1.X + MathF.Cos(a0) * r, y0, entry.Position1.Y + MathF.Sin(a0) * r);
                NVector3 b1 = new(entry.Position1.X + MathF.Cos(a1) * r, y0, entry.Position1.Y + MathF.Sin(a1) * r);
                NVector3 t0 = new(b0.X, y1, b0.Z);
                NVector3 t1 = new(b1.X, y1, b1.Z);
                AddAevLine(values, b0, b1);
                AddAevLine(values, t0, t1);
                if (i % 8 == 0) AddAevLine(values, b0, t0);
            }
            return;
        }

        if (entry.IsSquare)
        {
            NVector3[] bottom =
            {
                new(entry.Position1.X, y0, entry.Position1.Y), new(entry.Position2.X, y0, entry.Position2.Y),
                new(entry.Position3.X, y0, entry.Position3.Y), new(entry.Position4.X, y0, entry.Position4.Y)
            };
            NVector3[] top = bottom.Select(v => new NVector3(v.X, y1, v.Z)).ToArray();
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) & 3;
                AddAevLine(values, bottom[i], bottom[j]);
                AddAevLine(values, top[i], top[j]);
                AddAevLine(values, bottom[i], top[i]);
            }
            return;
        }

        // Unknown/eye-trigger categories still get a small location marker.
        // This is useful while reverse-engineering and makes sure an entry can
        // never disappear completely just because its category is unfamiliar.
        float marker = Math.Max(Math.Abs(entry.Height) * 0.10f, 0.15f);
        NVector3 c = new(entry.Position1.X, y0, entry.Position1.Y);
        AddAevLine(values, c - new NVector3(marker, 0, 0), c + new NVector3(marker, 0, 0));
        AddAevLine(values, c - new NVector3(0, 0, marker), c + new NVector3(0, 0, marker));
        AddAevLine(values, c, new NVector3(c.X, y1, c.Z));
    }

    private static void AddAevVolumeFaces(List<float> values, AevEntry entry)
    {
        GetAevYRange(entry, out float y0, out float y1);

        if (entry.IsCircle)
        {
            float r = entry.VisualRadius;
            if (r <= 0f) return;
            const int segments = 32;
            NVector3 bottomCenter = new(entry.Position1.X, y0, entry.Position1.Y);
            NVector3 topCenter = new(entry.Position1.X, y1, entry.Position1.Y);

            for (int i = 0; i < segments; i++)
            {
                float a0 = MathF.Tau * i / segments;
                float a1 = MathF.Tau * (i + 1) / segments;
                NVector3 b0 = new(bottomCenter.X + MathF.Cos(a0) * r, y0, bottomCenter.Z + MathF.Sin(a0) * r);
                NVector3 b1 = new(bottomCenter.X + MathF.Cos(a1) * r, y0, bottomCenter.Z + MathF.Sin(a1) * r);
                NVector3 t0 = new(b0.X, y1, b0.Z);
                NVector3 t1 = new(b1.X, y1, b1.Z);

                AddAevTriangle(values, b0, b1, t1);
                AddAevTriangle(values, b0, t1, t0);
                AddAevTriangle(values, bottomCenter, b1, b0);
                AddAevTriangle(values, topCenter, t0, t1);
            }
            return;
        }

        if (entry.IsSquare)
        {
            NVector3[] b =
            {
                new(entry.Position1.X, y0, entry.Position1.Y),
                new(entry.Position2.X, y0, entry.Position2.Y),
                new(entry.Position3.X, y0, entry.Position3.Y),
                new(entry.Position4.X, y0, entry.Position4.Y)
            };
            NVector3[] t = b.Select(v => new NVector3(v.X, y1, v.Z)).ToArray();

            AddAevQuad(values, b[0], b[1], b[2], b[3]);
            AddAevQuad(values, t[3], t[2], t[1], t[0]);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) & 3;
                AddAevQuad(values, b[i], b[j], t[j], t[i]);
            }
        }
    }

    private static void AddAevQuad(List<float> values, NVector3 a, NVector3 b, NVector3 c, NVector3 d)
    {
        AddAevTriangle(values, a, b, c);
        AddAevTriangle(values, a, c, d);
    }

    private static void AddAevTriangle(List<float> values, NVector3 a, NVector3 b, NVector3 c)
    {
        AddAevPoint(values, a);
        AddAevPoint(values, b);
        AddAevPoint(values, c);
    }

    private static void GetAevYRange(AevEntry entry, out float y0, out float y1)
    {
        float rawY0 = entry.Y;
        float rawY1 = entry.Y + entry.Height;
        y0 = entry.IsPs2Layout ? -rawY0 : rawY0;
        y1 = entry.IsPs2Layout ? -rawY1 : rawY1;
        if (y1 < y0) (y0, y1) = (y1, y0);
    }

    private static void AddAevMoveHandle(List<float> values, AevEntry entry, float sceneRadius)
    {
        GetAevYRange(entry, out float y0, out float y1);
        System.Numerics.Vector2 center2 = entry.IsCircle ? entry.Position1 : GetAevCenterXZ(entry);

        float extent = entry.IsCircle
            ? Math.Max(entry.VisualRadius, 0.25f)
            : Math.Max(System.Numerics.Vector2.Distance(entry.Position1, entry.Position3), 0.25f);

        float size = Math.Max(0.12f, Math.Max(sceneRadius * 0.0023f, extent * 0.05f));
        NVector3 origin = new(center2.X, (y0 + y1) * 0.5f, center2.Y);

        // Horizontal X/Z move arrow. Dragging this handle remains free on the X/Z plane.
        NVector3 sideEnd = origin + new NVector3(size * 4.2f, 0f, 0f);
        AddAevLine(values, origin, sideEnd);
        AddAevLine(values, sideEnd, sideEnd + new NVector3(-size, 0f, -size * 0.65f));
        AddAevLine(values, sideEnd, sideEnd + new NVector3(-size, 0f,  size * 0.65f));

        // Vertical move arrow. This moves the complete volume in Y without changing Height.
        NVector3 upEnd = origin + new NVector3(0f, size * 4.2f, 0f);
        AddAevLine(values, origin, upEnd);
        AddAevLine(values, upEnd, upEnd + new NVector3(-size * 0.65f, -size, 0f));
        AddAevLine(values, upEnd, upEnd + new NVector3( size * 0.65f, -size, 0f));
    }

    private static void AddAevHeightHandles(List<float> values, AevEntry entry, float sceneRadius)
    {
        GetAevYRange(entry, out float y0, out float y1);
        System.Numerics.Vector2 center2 = GetAevCenterXZ(entry);

        System.Numerics.Vector2[] points = { entry.Position1, entry.Position2, entry.Position3, entry.Position4 };
        float diagonal = System.Numerics.Vector2.Distance(points[0], points[2]);
        float size = Math.Max(0.09f, Math.Max(sceneRadius * 0.0020f, diagonal * 0.055f));

        AddHeightHandleAt(values, new NVector3(center2.X, y0, center2.Y), size);
        AddHeightHandleAt(values, new NVector3(center2.X, y1, center2.Y), size);
    }

    private static void AddHeightHandleAt(List<float> values, NVector3 c, float size)
    {
        // Diamond/cross centered on the face. It is visually distinct from corner handles.
        AddAevLine(values, c + new NVector3(-size, 0, 0), c + new NVector3(0, 0, -size));
        AddAevLine(values, c + new NVector3(0, 0, -size), c + new NVector3(size, 0, 0));
        AddAevLine(values, c + new NVector3(size, 0, 0), c + new NVector3(0, 0, size));
        AddAevLine(values, c + new NVector3(0, 0, size), c + new NVector3(-size, 0, 0));
        AddAevLine(values, c - new NVector3(size * 0.65f, 0, 0), c + new NVector3(size * 0.65f, 0, 0));
        AddAevLine(values, c - new NVector3(0, 0, size * 0.65f), c + new NVector3(0, 0, size * 0.65f));
    }

    private static System.Numerics.Vector2 GetAevCenterXZ(AevEntry entry)
    {
        return (entry.Position1 + entry.Position2 + entry.Position3 + entry.Position4) * 0.25f;
    }

    private static void AddAevCornerHandles(List<float> values, AevEntry entry, float sceneRadius)
    {
        GetAevYRange(entry, out _, out float y1);
        System.Numerics.Vector2[] points = { entry.Position1, entry.Position2, entry.Position3, entry.Position4 };

        float diagonal = System.Numerics.Vector2.Distance(points[0], points[2]);
        float size = Math.Max(0.06f, Math.Max(sceneRadius * 0.0015f, diagonal * 0.035f));

        foreach (System.Numerics.Vector2 point in points)
        {
            NVector3 c = new(point.X, y1, point.Y);
            AddAevLine(values, c - new NVector3(size, 0, 0), c + new NVector3(size, 0, 0));
            AddAevLine(values, c - new NVector3(0, 0, size), c + new NVector3(0, 0, size));
            AddAevLine(values, c - new NVector3(0, size, 0), c + new NVector3(0, size, 0));
        }
    }

    private static void AddAevLine(List<float> values, NVector3 a, NVector3 b)
    {
        AddAevPoint(values, a); AddAevPoint(values, b);
    }

    private static void AddAevPoint(List<float> values, NVector3 p)
    {
        values.Add(p.X); values.Add(p.Y); values.Add(p.Z);
        values.Add(0f); values.Add(1f); values.Add(0f);
    }

    private void UploadEnemies()
    {
        enemyGpuDirty=false;
        var all=new List<float>();
        var sel=new List<float>();
        var modelBuckets = new Dictionary<EnemyTextureKey, List<float>>();
        var selectedModelBuckets = new Dictionary<EnemyTextureKey, List<float>>();
        enemyModelBatches.Clear();
        selectedEnemyModelBatches.Clear();

        if(eslScene!=null) foreach(var e in eslScene.Entries.Where(EnemyIsVisible))
        {
            bool selected=e.Index==selectedEnemyIndex;
            AddEnemyMarker(selected?sel:all,e,selected);
            if (enemyModels.TryGetValue(e.EnemyType, out EnemyModelScene? model))
                AddEnemyModel(selected ? selectedModelBuckets : modelBuckets, e, model);
        }

        UploadLineBuffer(enemyVao,enemyVbo,all,out enemyVertexCount);
        UploadLineBuffer(selectedEnemyVao,selectedEnemyVbo,sel,out selectedEnemyVertexCount);

        List<float> modelAll = BuildEnemyModelBatches(modelBuckets, enemyModelBatches);
        List<float> modelSelected = BuildEnemyModelBatches(selectedModelBuckets, selectedEnemyModelBatches);
        UploadEnemyModelBuffer(enemyModelVao, enemyModelVbo, modelAll, out enemyModelVertexCount);
        UploadEnemyModelBuffer(selectedEnemyModelVao, selectedEnemyModelVbo, modelSelected, out selectedEnemyModelVertexCount);
    }

    private static List<float> BuildEnemyModelBatches(Dictionary<EnemyTextureKey, List<float>> buckets, List<EnemyModelDrawBatch> batches)
    {
        var combined = new List<float>();
        foreach (var bucket in buckets.OrderBy(x => x.Key.EnemyType).ThenBy(x => x.Key.TplEntryIndex).ThenBy(x => x.Key.TextureIndex))
        {
            int first = combined.Count / 8;
            combined.AddRange(bucket.Value);
            int count = bucket.Value.Count / 8;
            if (count > 0) batches.Add(new EnemyModelDrawBatch(bucket.Key, first, count));
        }
        return combined;
    }

    private void AddEnemyModel(Dictionary<EnemyTextureKey, List<float>> buckets, EslEnemyEntry entry, EnemyModelScene model)
    {
        NVector3 origin = EslToWorld(entry);
        float rx = entry.RotX * (MathF.PI / 32768f);
        float ry = entry.RotY * (MathF.PI / 32768f);
        float rz = entry.RotZ * (MathF.PI / 32768f);
        IEnumerable<EnemyModelPart> parts = model.Parts.Count > 0 ? model.Parts.Where(x => IsEnemyModelPartAutomaticallyVisible(entry, x)) : Array.Empty<EnemyModelPart>();

        if (model.Parts.Count == 0)
        {
            foreach (EnemyModelTriangle tri in model.Triangles) AddEnemyTriangleToBucket(buckets, entry, tri, origin, rx, ry, rz, model, false);
            return;
        }

        // Visual Editor autonomous idle: every em12 can use FCV 001 embedded in em12.dat,
        // independent from the Animations page. Manual FCV preview remains available for the
        // selected enemy when autonomous idle is disabled.
        FcvAnimation? bodyAnimation = null;
        float bodyFrame = 0f;
        if (enemyIdleAnimationEnabled && entry.EnemyType == 0x12 && model.IdleAnimation != null)
        {
            bodyAnimation = model.IdleAnimation;
            bodyFrame = enemyIdleAnimationFrame;
        }
        else if (entry.Index == selectedEnemyIndex && enemyAttachmentAnimation != null)
        {
            bodyAnimation = enemyAttachmentAnimation;
            bodyFrame = enemyAttachmentFrame;
        }
        bool animateBody = bodyAnimation != null && model.Skeleton != null;
        FcvSkeletonPose? animationPose = model.Skeleton != null && (animateBody || enemyAttachmentBoneIndex >= 0)
            ? FcvSkeletonEvaluator.Evaluate(model.Skeleton, animateBody ? bodyAnimation : null, animateBody ? bodyFrame : 0f) : null;
        FcvSkeletonPose? bindPose = model.Skeleton != null && animationPose != null
            ? FcvSkeletonEvaluator.Evaluate(model.Skeleton, null, 0f) : null;
        foreach (EnemyModelPart part in parts)
        {
            bool attachToHand = EnemyEquipmentCatalog.IsHandHeldPart(entry, part.DatEntryIndex) && animationPose != null && bindPose != null;
            NVector3 attachmentPivot = attachToHand ? GetEnemyPartPivot(part) : NVector3.Zero;
            foreach (EnemyModelTriangle tri in part.Triangles)
                AddEnemyTriangleToBucket(buckets, entry, tri, origin, rx, ry, rz, model, attachToHand, animationPose, bindPose, attachmentPivot, animateBody);
        }
    }

    private void AddEnemyTriangleToBucket(Dictionary<EnemyTextureKey,List<float>> buckets,EslEnemyEntry entry,EnemyModelTriangle tri,NVector3 origin,float rx,float ry,float rz,EnemyModelScene model,bool attachToHand,FcvSkeletonPose? animationPose=null,FcvSkeletonPose? bindPose=null,NVector3 attachmentPivot=default,bool animateBody=false)
    {
        var key=new EnemyTextureKey(entry.EnemyType,tri.TplEntryIndex,tri.TextureIndex);
        if(!buckets.TryGetValue(key,out List<float>? values)){values=new List<float>();buckets[key]=values;}
        if(attachToHand && animationPose!=null && bindPose!=null) WriteEnemyAttachedTriangle(values,tri,origin,rx,ry,rz,model,animationPose,bindPose,attachmentPivot);
        else if(animateBody && animationPose!=null && bindPose!=null) WriteEnemySkinnedTriangle(values,tri,origin,rx,ry,rz,model,animationPose,bindPose);
        else WriteEnemyTriangle(values,tri,origin,rx,ry,rz);
    }

    private static NVector3 GetEnemyPartPivot(EnemyModelPart part)
    {
        bool has=false; NVector3 min=NVector3.Zero,max=NVector3.Zero;
        foreach(EnemyModelTriangle tri in part.Triangles)
        {
            NVector3[] verts={tri.A,tri.B,tri.C};
            foreach(NVector3 v in verts)
            {
                if(!has){min=max=v;has=true;}
                else{min=NVector3.Min(min,v);max=NVector3.Max(max,v);}
            }
        }
        return has ? (min+max)*0.5f : NVector3.Zero;
    }

    private void WriteEnemyAttachedTriangle(List<float> values,EnemyModelTriangle tri,NVector3 origin,float rx,float ry,float rz,EnemyModelScene model,FcvSkeletonPose pose,FcvSkeletonPose bindPose,NVector3 attachmentPivot)
    {
        if(model.Skeleton==null || enemyAttachmentBoneIndex<0 || enemyAttachmentBoneIndex>=model.Skeleton.Bones.Count){WriteEnemyTriangle(values,tri,origin,rx,ry,rz);return;}
        // Keep preview animations anchored to the ESL position. FCV root translation is
        // animation/root-motion data and must not be added on top of the enemy world position.
        NVector3 rootMotion = GetEnemyAnimationRootMotion(model.Skeleton, pose, bindPose);
        NVector3 bonePos=pose.WorldPositions[enemyAttachmentBoneIndex]/100f - rootMotion;
        NQuaternion boneRot=pose.WorldRotations[enemyAttachmentBoneIndex];
        NVector3 a=TransformEnemyAttachedVertex(tri.A,attachmentPivot,bonePos,boneRot,origin,rx,ry,rz);
        NVector3 b=TransformEnemyAttachedVertex(tri.B,attachmentPivot,bonePos,boneRot,origin,rx,ry,rz);
        NVector3 c=TransformEnemyAttachedVertex(tri.C,attachmentPivot,bonePos,boneRot,origin,rx,ry,rz);
        NVector3 n=NVector3.Cross(b-a,c-a); float len=n.Length(); if(!float.IsFinite(len)||len<0.000001f)return; n/=len;
        WriteEnemyModelVertex(values,a,n,tri.UvA);WriteEnemyModelVertex(values,b,n,tri.UvB);WriteEnemyModelVertex(values,c,n,tri.UvC);
    }

    private NVector3 TransformEnemyAttachedVertex(NVector3 v,NVector3 weaponPivot,NVector3 bonePos,NQuaternion boneRot,NVector3 origin,float rx,float ry,float rz)
    {
        // Rigid bind-pose attachment: put the weapon's own pivot on the selected bone.
        // This deliberately avoids bind/current cancellation so changing bones is visible immediately.
        v-=weaponPivot;
        float ax=enemyAttachmentRotationDegrees.X*MathF.PI/180f, ay=enemyAttachmentRotationDegrees.Y*MathF.PI/180f, az=enemyAttachmentRotationDegrees.Z*MathF.PI/180f;
        if(MathF.Abs(ax)>0.000001f){float c=MathF.Cos(ax),ss=MathF.Sin(ax);v=new NVector3(v.X,v.Y*c-v.Z*ss,v.Y*ss+v.Z*c);}
        if(MathF.Abs(ay)>0.000001f){float c=MathF.Cos(ay),ss=MathF.Sin(ay);v=new NVector3(v.X*c+v.Z*ss,v.Y,-v.X*ss+v.Z*c);}
        if(MathF.Abs(az)>0.000001f){float c=MathF.Cos(az),ss=MathF.Sin(az);v=new NVector3(v.X*c-v.Y*ss,v.X*ss+v.Y*c,v.Z);}
        v+=enemyAttachmentOffset;
        v=NVector3.Transform(v,boneRot)+bonePos;
        return TransformEnemyModelVertex(v,origin,rx,ry,rz);
    }

    private void WriteEnemySkinnedTriangle(List<float> values, EnemyModelTriangle tri, NVector3 origin, float rx, float ry, float rz, EnemyModelScene model, FcvSkeletonPose pose, FcvSkeletonPose bindPose)
    {
        if (model.Skeleton == null) { WriteEnemyTriangle(values, tri, origin, rx, ry, rz); return; }
        NVector3 a = TransformEnemySkinnedVertex(tri.A, tri.SkinA, model.Skeleton, pose, bindPose);
        NVector3 b = TransformEnemySkinnedVertex(tri.B, tri.SkinB, model.Skeleton, pose, bindPose);
        NVector3 c = TransformEnemySkinnedVertex(tri.C, tri.SkinC, model.Skeleton, pose, bindPose);
        a = TransformEnemyModelVertex(a, origin, rx, ry, rz);
        b = TransformEnemyModelVertex(b, origin, rx, ry, rz);
        c = TransformEnemyModelVertex(c, origin, rx, ry, rz);
        NVector3 n = NVector3.Cross(b-a,c-a); float len=n.Length(); if(!float.IsFinite(len)||len<0.000001f)return; n/=len;
        WriteEnemyModelVertex(values,a,n,tri.UvA); WriteEnemyModelVertex(values,b,n,tri.UvB); WriteEnemyModelVertex(values,c,n,tri.UvC);
    }

    private static NVector3 TransformEnemySkinnedVertex(NVector3 v, EnemyVertexSkin skin, Ps2BinSkeleton skeleton, FcvSkeletonPose pose, FcvSkeletonPose bindPose)
    {
        if (skin.Count <= 0) return v;
        NVector3 result = NVector3.Zero; float used = 0f;
        void Apply(EnemySkinInfluence inf)
        {
            if (inf.Weight <= 0f || !skeleton.FirstIndexById.TryGetValue(inf.BoneId, out int bi) || bi < 0 || bi >= pose.WorldPositions.Length) return;
            NVector3 bindPos = bindPose.WorldPositions[bi] / 100f;
            NVector3 nowPos = pose.WorldPositions[bi] / 100f - GetEnemyAnimationRootMotion(skeleton, pose, bindPose);
            NQuaternion bindRot = bindPose.WorldRotations[bi];
            NQuaternion nowRot = pose.WorldRotations[bi];
            NQuaternion invBind = NQuaternion.Inverse(bindRot);
            NVector3 boneLocal = NVector3.Transform(v - bindPos, invBind);
            NVector3 animated = NVector3.Transform(boneLocal, nowRot) + nowPos;
            result += animated * inf.Weight; used += inf.Weight;
        }
        Apply(skin.A); if (skin.Count > 1) Apply(skin.B); if (skin.Count > 2) Apply(skin.C);
        return used > 0.000001f ? result / used : v;
    }

    private static NVector3 GetEnemyAnimationRootMotion(Ps2BinSkeleton skeleton, FcvSkeletonPose pose, FcvSkeletonPose bindPose)
    {
        // FCV clips may animate the root translation. In-game that motion is handled by the
        // enemy/gameplay position; applying it again in the Visual Editor makes the mesh float
        // away from its ESL marker. Remove only the root translation delta, preserving all
        // rotations and child-bone motion.
        int rootIndex = -1;
        for (int i = 0; i < skeleton.Bones.Count; i++)
        {
            if (skeleton.Bones[i].ParentIndex < 0) { rootIndex = i; break; }
        }
        if (rootIndex < 0 || rootIndex >= pose.WorldPositions.Length || rootIndex >= bindPose.WorldPositions.Length)
            return NVector3.Zero;

        NVector3 delta = (pose.WorldPositions[rootIndex] - bindPose.WorldPositions[rootIndex]) / 100f;
        return float.IsFinite(delta.X) && float.IsFinite(delta.Y) && float.IsFinite(delta.Z) ? delta : NVector3.Zero;
    }

    private static void WriteEnemyTriangle(List<float> values, EnemyModelTriangle tri, NVector3 origin, float rx, float ry, float rz)
    {
        NVector3 a = TransformEnemyModelVertex(tri.A, origin, rx, ry, rz);
        NVector3 b = TransformEnemyModelVertex(tri.B, origin, rx, ry, rz);
        NVector3 c = TransformEnemyModelVertex(tri.C, origin, rx, ry, rz);
        NVector3 n = NVector3.Cross(b-a,c-a);
        float len = n.Length();
        if (!float.IsFinite(len) || len < 0.000001f) return;
        n /= len;
        WriteEnemyModelVertex(values,a,n,tri.UvA);
        WriteEnemyModelVertex(values,b,n,tri.UvB);
        WriteEnemyModelVertex(values,c,n,tri.UvC);
    }

    private static NVector3 TransformEnemyModelVertex(NVector3 v, NVector3 origin, float rx, float ry, float rz)
    {
        if (MathF.Abs(rx) > 0.000001f) { float c=MathF.Cos(rx),s=MathF.Sin(rx); v=new NVector3(v.X,v.Y*c-v.Z*s,v.Y*s+v.Z*c); }
        if (MathF.Abs(ry) > 0.000001f) { float c=MathF.Cos(ry),s=MathF.Sin(ry); v=new NVector3(v.X*c+v.Z*s,v.Y,-v.X*s+v.Z*c); }
        if (MathF.Abs(rz) > 0.000001f) { float c=MathF.Cos(rz),s=MathF.Sin(rz); v=new NVector3(v.X*c-v.Y*s,v.X*s+v.Y*c,v.Z); }
        return v + origin;
    }

    private static void WriteEnemyModelVertex(List<float> values, NVector3 p, NVector3 n, System.Numerics.Vector2 uv)
    {
        values.Add(p.X); values.Add(p.Y); values.Add(p.Z); values.Add(n.X); values.Add(n.Y); values.Add(n.Z); values.Add(uv.X); values.Add(uv.Y);
    }

    private static void UploadEnemyModelBuffer(int vao, int vbo, List<float> values, out int vertexCount)
    {
        vertexCount = values.Count / 8;
        float[] data = values.ToArray();
        GL.BindVertexArray(vao); GL.BindBuffer(BufferTarget.ArrayBuffer,vbo);
        GL.BufferData(BufferTarget.ArrayBuffer,data.Length*sizeof(float),data,BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,8*sizeof(float),0); GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,8*sizeof(float),3*sizeof(float)); GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(2,2,VertexAttribPointerType.Float,false,8*sizeof(float),6*sizeof(float)); GL.EnableVertexAttribArray(2);
        GL.BindBuffer(BufferTarget.ArrayBuffer,0); GL.BindVertexArray(0);
    }
    private void AddEnemyMarker(List<float> v,EslEnemyEntry e,bool selected)
    {
        float x=e.PosX*EslWorldScale,y=e.PosY*EslWorldScale,z=e.PosZ*EslWorldScale,r=selected?1.6f:1.2f;
        void L(float ax,float ay,float az,float bx,float by,float bz){v.AddRange(new[]{ax,ay,az,0f,0f,0f,bx,by,bz,0f,0f,0f});}
        L(x-r,y,z,x+r,y,z); L(x,y,z-r,x,y,z+r); L(x,y,z,x,y+0.65f,z);
        float a=(float)(e.RotY*(Math.PI/32768.0)); float dx=(float)Math.Sin(a), dz=(float)Math.Cos(a), len=r*1.8f;
        float tx=x+dx*len,tz=z+dz*len; L(x,y+0.08f,z,tx,y+0.08f,tz);
        float px=-dz,pz=dx,head=r*0.42f; L(tx,y+0.08f,tz,tx-dx*head+px*head,y+0.08f,tz-dz*head+pz*head); L(tx,y+0.08f,tz,tx-dx*head-px*head,y+0.08f,tz-dz*head-pz*head);
        if(!selected) return;
        float axis=3.6f;
        if (EnemyTransformMode == EnemyGizmoMode.Move)
        {
            L(x,y+0.03f,z,x+axis,y+0.03f,z); L(x,y,z,x,y+axis,z); L(x,y+0.03f,z,x,y+0.03f,z+axis);
            // small arrow heads make each selectable axis easier to read
            L(x+axis,y+0.03f,z,x+axis-0.45f,y+0.28f,z); L(x+axis,y+0.03f,z,x+axis-0.45f,y-0.22f,z);
            L(x,y+axis,z,x+0.25f,y+axis-0.45f,z); L(x,y+axis,z,x-0.25f,y+axis-0.45f,z);
            L(x,y+0.03f,z+axis,x,y+0.28f,z+axis-0.45f); L(x,y+0.03f,z+axis,x,y-0.22f,z+axis-0.45f);
        }
        else
        {
            const int seg=36; float rr=2.7f;
            for(int i=0;i<seg;i++)
            {
                float a0=(float)(i*Math.PI*2/seg),a1=(float)((i+1)*Math.PI*2/seg);
                // X ring (YZ), Y ring (XZ), Z ring (XY)
                L(x,y+MathF.Cos(a0)*rr,z+MathF.Sin(a0)*rr,x,y+MathF.Cos(a1)*rr,z+MathF.Sin(a1)*rr);
                L(x+MathF.Cos(a0)*rr,y+0.04f,z+MathF.Sin(a0)*rr,x+MathF.Cos(a1)*rr,y+0.04f,z+MathF.Sin(a1)*rr);
                L(x+MathF.Cos(a0)*rr,y+MathF.Sin(a0)*rr,z,x+MathF.Cos(a1)*rr,y+MathF.Sin(a1)*rr,z);
            }
        }
    }
    private void DrawAevGpu()
    {
        // Editor overlay: faces give spatial context while the outline stays readable
        // even inside the scenario geometry.
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.Uniform1(uUnlit, 1);
        GL.Uniform1(uUseTexture, 0);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        if (aevFaceVertexCount > 0)
        {
            GL.Uniform1(uOpacity, 0.16f);
            GL.Uniform3(uColor, 0.05f, 0.72f, 0.95f);
            GL.BindVertexArray(aevFaceVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, aevFaceVertexCount);
        }

        if (aevSelectedFaceVertexCount > 0)
        {
            GL.Uniform1(uOpacity, 0.28f);
            GL.Uniform3(uColor, 1.00f, 0.62f, 0.08f);
            GL.BindVertexArray(aevSelectedFaceVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, aevSelectedFaceVertexCount);
        }

        GL.Uniform1(uOpacity, 1.0f);
        GL.Disable(EnableCap.Blend);

        GL.Uniform3(uColor, 0.10f, 0.88f, 1.00f);
        GL.BindVertexArray(aevVao);
        GL.LineWidth(2f);
        GL.DrawArrays(PrimitiveType.Lines, 0, aevVertexCount);

        if (aevSelectedVertexCount > 0)
        {
            GL.Uniform3(uColor, 1.0f, 0.72f, 0.10f);
            GL.BindVertexArray(aevSelectedVao);
            GL.LineWidth(4f);
            GL.DrawArrays(PrimitiveType.Lines, 0, aevSelectedVertexCount);
        }

        if (aevHandleVertexCount > 0)
        {
            GL.Uniform3(uColor, 1.0f, 0.95f, 0.30f);
            GL.BindVertexArray(aevHandleVao);
            GL.LineWidth(5f);
            GL.DrawArrays(PrimitiveType.Lines, 0, aevHandleVertexCount);
        }

        GL.LineWidth(1f);
        GL.Uniform1(uOpacity, 1.0f);
        GL.DepthMask(true);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private static void AddNormal(Dictionary<NVector3, NVector3> sums, NVector3 position, NVector3 normal)
    {
        if (sums.TryGetValue(position, out NVector3 current)) sums[position] = current + normal;
        else sums[position] = normal;
    }

    private static NVector3 GetSmoothNormal(Dictionary<NVector3, NVector3> sums, NVector3 position)
    {
        if (!sums.TryGetValue(position, out NVector3 n)) return NVector3.UnitY;
        float lenSq = n.LengthSquared();
        if (lenSq < 0.000001f || !float.IsFinite(lenSq)) return NVector3.UnitY;
        return n / MathF.Sqrt(lenSq);
    }

    private static void WriteTexturedVertex(float[] output, ref int o, NVector3 p, NVector3 n, System.Numerics.Vector2 uv)
    {
        output[o++] = p.X; output[o++] = p.Y; output[o++] = p.Z;
        output[o++] = n.X; output[o++] = n.Y; output[o++] = n.Z;
        output[o++] = uv.X; output[o++] = uv.Y;
    }

    private static void WriteVertex(float[] data, ref int o, NVector3 p, NVector3 n)
    {
        data[o++] = p.X; data[o++] = p.Y; data[o++] = p.Z;
        data[o++] = n.X; data[o++] = n.Y; data[o++] = n.Z;
    }

    private static float[] BuildGridData(ScenarioScene scene)
    {
        float radius = scene.Radius;
        float rawStep = Math.Max(1f, radius / 10f);
        float power = (float)Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        float normalized = rawStep / power;
        float step = normalized < 2f ? power : normalized < 5f ? 2f * power : 5f * power;
        float extent = step * 12f;
        float y = scene.BoundsMin.Y;
        float cx = scene.Center.X;
        float cz = scene.Center.Z;

        var values = new List<float>(25 * 4 * 6);
        for (int i = -12; i <= 12; i++)
        {
            float x = cx + i * step;
            float z = cz + i * step;
            AddGridVertex(values, x, y, cz - extent); AddGridVertex(values, x, y, cz + extent);
            AddGridVertex(values, cx - extent, y, z); AddGridVertex(values, cx + extent, y, z);
        }
        return values.ToArray();
    }

    private static void AddGridVertex(List<float> values, float x, float y, float z)
    {
        values.Add(x); values.Add(y); values.Add(z);
        values.Add(0f); values.Add(1f); values.Add(0f);
    }

    private void DrawGridGpu()
    {
        if (gridVertexCount <= 0) return;
        GL.Uniform1(uOpacity, 1.0f);
        GL.Uniform3(uColor, 52f / 255f, 61f / 255f, 70f / 255f);
        GL.Uniform1(uUnlit, 1);
        GL.Uniform1(uUseTexture, 0);
        GL.BindVertexArray(gridVao);
        GL.LineWidth(1f);
        GL.DrawArrays(PrimitiveType.Lines, 0, gridVertexCount);
    }

    private void DrawMeshGpu()
    {
        if (meshVertexCount <= 0) return;
        GL.Uniform1(uOpacity, 1.0f);

        GL.BindVertexArray(meshVao);
        GL.Uniform1(uUnlit, 0);
        GL.Uniform3(uColor, 185f / 255f, 190f / 255f, 198f / 255f);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Uniform1(uTexture, 0);

        if (RenderMode == ScenarioRenderMode.Wireframe)
        {
            GL.Uniform1(uUseTexture, 0);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.DrawArrays(PrimitiveType.Triangles, 0, meshVertexCount);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            return;
        }

        // PASS 1: opaque materials. No blending; they populate the depth buffer first.
        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);

        foreach (ScenarioDrawBatch batch in meshBatches)
        {
            bool transparent = glTextureHasTransparency.TryGetValue(batch.TextureIndex, out bool value) && value;
            if (transparent) continue;

            DrawScenarioBatch(batch);
        }

        // PASS 2: materials with alpha. The opaque scene is already present behind them.
        // Transparent geometry tests against depth but does not write new depth, preventing
        // black foliage/shadows from hiding surfaces that should remain visible behind it.
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthMask(false);

        foreach (ScenarioDrawBatch batch in meshBatches)
        {
            bool transparent = glTextureHasTransparency.TryGetValue(batch.TextureIndex, out bool value) && value;
            if (!transparent) continue;

            DrawScenarioBatch(batch);
        }

        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        if (RenderMode == ScenarioRenderMode.SolidWireframe)
        {
            GL.Uniform1(uUseTexture, 0);
            GL.Uniform1(uUnlit, 1);
            GL.Uniform3(uColor, 25f / 255f, 30f / 255f, 36f / 255f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.DrawArrays(PrimitiveType.Triangles, 0, meshVertexCount);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }
    }

    private void DrawScenarioBatch(ScenarioDrawBatch batch)
    {
        if (glTextures.TryGetValue(batch.TextureIndex, out int texture))
        {
            GL.Uniform1(uUseTexture, 1);
            GL.BindTexture(TextureTarget.Texture2D, texture);
        }
        else
        {
            GL.Uniform1(uUseTexture, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        GL.DrawArrays(PrimitiveType.Triangles, batch.FirstVertex, batch.VertexCount);
    }

    private Matrix4 BuildMvp()
    {
        NVector3 forward = GetForward();
        Vector3 eye = new(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);
        Vector3 center = new(cameraPosition.X + forward.X, cameraPosition.Y + forward.Y, cameraPosition.Z + forward.Z);
        Matrix4 view = Matrix4.LookAt(eye, center, Vector3.UnitY);
        float aspect = Math.Max(0.01f, ClientSize.Width / (float)Math.Max(1, ClientSize.Height));
        float radius = scene?.Radius ?? 1000f;
        float distanceToScene = scene == null ? 1000f : NVector3.Distance(cameraPosition, scene.Center);
        float near = Math.Max(0.001f, radius * 0.00005f);
        float far = Math.Max(near + 100f, distanceToScene + radius * 30f);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), aspect, near, far);
        return view * projection;
    }

    private static float DistancePointToSegment(PointF p, PointF a, PointF b)
    {
        float vx=b.X-a.X, vy=b.Y-a.Y, wx=p.X-a.X, wy=p.Y-a.Y;
        float len2=vx*vx+vy*vy; if(len2<0.0001f) return MathF.Sqrt(wx*wx+wy*wy);
        float t=Math.Clamp((wx*vx+wy*vy)/len2,0f,1f); float dx=p.X-(a.X+t*vx),dy=p.Y-(a.Y+t*vy); return MathF.Sqrt(dx*dx+dy*dy);
    }

    private int PickEnemyGizmoHandle(Point mouse, EslEnemyEntry enemy)
    {
        NVector3 o=EslToWorld(enemy); const float axis=3.6f; const float threshold=10f;
        if (EnemyTransformMode == EnemyGizmoMode.Move)
        {
            NVector3[] ends={o+NVector3.UnitX*axis,o+NVector3.UnitY*axis,o+NVector3.UnitZ*axis};
            if(!TryProjectWorldToScreen(o,out PointF po)) return 0;
            for(int i=0;i<3;i++) if(TryProjectWorldToScreen(ends[i],out PointF pe) && DistancePointToSegment(mouse,po,pe)<=threshold) return i+1;
            return 0;
        }
        const int seg=36; float rr=2.7f; int best=0; float bestD=threshold;
        for(int ring=0;ring<3;ring++)
        {
            PointF? prev=null;
            for(int i=0;i<=seg;i++)
            {
                float a=(float)(i*Math.PI*2/seg); NVector3 w=ring switch { 0=>o+new NVector3(0,MathF.Cos(a)*rr,MathF.Sin(a)*rr), 1=>o+new NVector3(MathF.Cos(a)*rr,0.04f,MathF.Sin(a)*rr), _=>o+new NVector3(MathF.Cos(a)*rr,MathF.Sin(a)*rr,0) };
                if(!TryProjectWorldToScreen(w,out PointF sp)){prev=null; continue;}
                if(prev.HasValue){float d=DistancePointToSegment(mouse,prev.Value,sp); if(d<bestD){bestD=d;best=4+ring;}}
                prev=sp;
            }
        }
        return best;
    }

    private static short SnapEnemyShort(float raw, int step)
    {
        int v=(int)MathF.Round(raw); if(step>1) v=(int)MathF.Round(v/(float)step)*step; return ClampShort(v);
    }

    private EslEnemyEntry? GetSelectedEnemyEntry() => eslScene?.Entries.FirstOrDefault(x => x.Index == selectedEnemyIndex);
    private bool IsMouseNearEnemy(Point mouse, EslEnemyEntry enemy, float radius=20f)
    {
        if (!TryProjectWorldToScreen(EslToWorld(enemy), out PointF p)) return false;
        float dx=p.X-mouse.X, dy=p.Y-mouse.Y; return dx*dx+dy*dy <= radius*radius;
    }
    private static short ClampShort(float value) => (short)Math.Clamp((int)MathF.Round(value), short.MinValue, short.MaxValue);

    private void DrawEnemiesGpu()
    {
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.Uniform1(uUnlit,0);
        GL.Uniform1(uOpacity,1f);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.Uniform1(uTexture, 0);

        if(enemyModelVertexCount>0)
        {
            GL.BindVertexArray(enemyModelVao);
            GL.Uniform3(uColor,0.58f,0.62f,0.68f);
            foreach (EnemyModelDrawBatch batch in enemyModelBatches)
            {
                if (batch.Key.TextureIndex >= 0 && batch.Key.TplEntryIndex >= 0 && glEnemyTextures.TryGetValue(batch.Key, out int texture))
                {
                    GL.Uniform1(uUseTexture,1);
                    GL.BindTexture(TextureTarget.Texture2D, texture);
                }
                else
                {
                    GL.Uniform1(uUseTexture,0);
                    GL.BindTexture(TextureTarget.Texture2D,0);
                }
                GL.DrawArrays(PrimitiveType.Triangles,batch.FirstVertex,batch.VertexCount);
            }
            GL.BindTexture(TextureTarget.Texture2D,0);
        }

        if(selectedEnemyModelVertexCount>0)
        {
            GL.BindVertexArray(selectedEnemyModelVao);
            GL.Uniform3(uColor,0.72f,0.74f,0.78f);
            foreach (EnemyModelDrawBatch batch in selectedEnemyModelBatches)
            {
                if (batch.Key.TextureIndex >= 0 && batch.Key.TplEntryIndex >= 0 && glEnemyTextures.TryGetValue(batch.Key, out int texture))
                {
                    GL.Uniform1(uUseTexture,1);
                    GL.BindTexture(TextureTarget.Texture2D, texture);
                }
                else
                {
                    GL.Uniform1(uUseTexture,0);
                    GL.BindTexture(TextureTarget.Texture2D,0);
                }
                GL.DrawArrays(PrimitiveType.Triangles,batch.FirstVertex,batch.VertexCount);
            }
            GL.BindTexture(TextureTarget.Texture2D,0);

            // A thin wire overlay keeps selection obvious without hiding the texture.
            GL.Uniform1(uUseTexture,0);
            GL.Uniform1(uUnlit,1);
            GL.Uniform3(uColor,1f,0.78f,0.12f);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.DrawArrays(PrimitiveType.Triangles,0,selectedEnemyModelVertexCount);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Uniform1(uUnlit,0);
        }

        // Editor marker/gizmo remains an overlay on top of the model.
        GL.Uniform1(uUseTexture,0);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Uniform1(uUnlit,1);
        GL.Uniform3(uColor,0.95f,0.18f,0.18f);
        GL.BindVertexArray(enemyVao);
        GL.LineWidth(3f);
        GL.DrawArrays(PrimitiveType.Lines,0,enemyVertexCount);
        if(selectedEnemyVertexCount>0)
        {
            GL.Uniform3(uColor,1f,0.85f,0.1f);
            GL.BindVertexArray(selectedEnemyVao);
            GL.LineWidth(5f);
            GL.DrawArrays(PrimitiveType.Lines,0,selectedEnemyVertexCount);
        }
        GL.LineWidth(1f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button is MouseButtons.Left or MouseButtons.Right or MouseButtons.Middle)
        {
            dragButton = e.Button;
            lastMouse = e.Location;
            lastMovementTick = Environment.TickCount64;
            if (e.Button == MouseButtons.Left)
            {
                mouseDownPoint = e.Location;
                leftMouseMoved = false;

                AevEntry? selected = GetSelectedAevEntry();
                if (selected != null && (selected.IsSquare || selected.IsCircle))
                {
                    int handle = -1;
                    if (selected.IsSquare)
                    {
                        handle = PickAevCornerHandle(e.Location, selected);
                        if (handle < 0) handle = PickAevHeightHandle(e.Location, selected);
                    }
                    if (handle < 0) handle = PickAevMoveHandle(e.Location, selected);

                    if (handle >= 0)
                    {
                        draggingAevHandle = handle;
                        draggingAevEntry = selected;
                        dragStartState = AevVertexState.From(selected);

                        if (handle is 4 or 5)
                        {
                            GetAevYRange(selected, out heightDragStartBottomY, out heightDragStartTopY);
                            heightDragStartMouseY = e.Y;
                            heightDragPixelsPerWorldUnit = CalculateVerticalPixelsPerWorldUnit(selected);
                        }
                        else if (handle == 7)
                        {
                            verticalMoveDragStartMouseY = e.Y;
                            verticalMoveStartY = selected.Y;
                            verticalMovePixelsPerWorldUnit = CalculateVerticalPixelsPerWorldUnit(selected);
                        }
                    }
                }

                if (draggingAevHandle < 0 && EnemiesVisible)
                {
                    EslEnemyEntry? enemy = GetSelectedEnemyEntry();
                    int pickedEnemyHandle = enemy == null ? 0 : PickEnemyGizmoHandle(e.Location, enemy);
                    if (enemy != null && (pickedEnemyHandle > 0 || IsMouseNearEnemy(e.Location, enemy)))
                    {
                        draggingEnemy = enemy; enemyDragStartMouse = e.Location; enemyDragStartX=enemy.PosX; enemyDragStartY=enemy.PosY; enemyDragStartZ=enemy.PosZ; enemyDragStartRotX=enemy.RotX; enemyDragStartRotY=enemy.RotY; enemyDragStartRotZ=enemy.RotZ;
                        enemyDragStartWorld = EslToWorld(enemy);
                        enemyDragMode = pickedEnemyHandle;
                        if (enemyDragMode == 0) enemyDragMode = (ModifierKeys & Keys.Control) != 0 ? 5 : (ModifierKeys & Keys.Shift) != 0 ? 2 : 7;
                        if (enemyDragMode == 2)
                        {
                            NVector3 a=enemyDragStartWorld,b=a+NVector3.UnitY;
                            if (TryProjectWorldToScreen(a,out PointF pa) && TryProjectWorldToScreen(b,out PointF pb)) enemyVerticalPixelsPerWorldUnit=Math.Max(0.05f,MathF.Abs(pb.Y-pa.Y));
                        }
                    }
                }
            }
            Capture = true;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == dragButton)
        {
            bool wasHandleDrag = e.Button == MouseButtons.Left && draggingAevHandle >= 0 && draggingAevEntry != null;
            bool wasEnemyDrag = e.Button == MouseButtons.Left && enemyDragMode != 0 && draggingEnemy != null;
            bool clickAev = e.Button == MouseButtons.Left && !leftMouseMoved && !wasHandleDrag && !wasEnemyDrag;

            if (wasHandleDrag)
            {
                AevVertexState after = AevVertexState.From(draggingAevEntry!);
                if (dragStartState.HasValue && !dragStartState.Value.Equals(after))
                {
                    AevEntry undoEntry = draggingAevEntry!;
                    AevVertexState restore = dragStartState.Value;
                    aevUndo.Push(() =>
                    {
                        restore.Apply(undoEntry);
                        selectedAevFileOrder = undoEntry.FileOrder;
                        aevGpuDirty = true;
                        AevEntryEdited?.Invoke(undoEntry);
                        AevEntryClicked?.Invoke(undoEntry);
                        Invalidate();
                    });
                    TrimUndoStack();
                    AevEntryEdited?.Invoke(draggingAevEntry!);
                }

                draggingAevHandle = -1;
                draggingAevEntry = null;
                dragStartState = null;
            }

            if (wasEnemyDrag)
            {
                EslEnemyEntry edited = draggingEnemy!;
                bool changed = edited.PosX!=enemyDragStartX || edited.PosY!=enemyDragStartY || edited.PosZ!=enemyDragStartZ || edited.RotX!=enemyDragStartRotX || edited.RotY!=enemyDragStartRotY || edited.RotZ!=enemyDragStartRotZ;
                if (changed)
                {
                    short oldX=enemyDragStartX, oldY=enemyDragStartY, oldZ=enemyDragStartZ, oldRotX=enemyDragStartRotX, oldRotY=enemyDragStartRotY, oldRotZ=enemyDragStartRotZ;
                    RegisterEnemyUndo(() =>
                    {
                        edited.PosX=oldX; edited.PosY=oldY; edited.PosZ=oldZ; edited.RotX=oldRotX; edited.RotY=oldRotY; edited.RotZ=oldRotZ;
                        selectedEnemyIndex=edited.Index; enemyGpuDirty=true; EnemyEntryEdited?.Invoke(edited); EnemyEntryClicked?.Invoke(edited); Invalidate();
                    });
                }
                enemyDragMode=0; draggingEnemy=null;
                if(changed) EnemyEntryEdited?.Invoke(edited);
            }

            dragButton = MouseButtons.None;
            Capture = false;

            if (clickAev && AevVisible && aevScene != null)
            {
                AevEntry? hit = PickAevEntry(e.Location);
                SelectAevEntry(hit);
                AevEntryClicked?.Invoke(hit);
                if(hit==null && EnemiesVisible && eslScene!=null){EslEnemyEntry? eh=null; float best=18f; foreach(var en in eslScene.Entries.Where(EnemyIsVisible)){if(!TryProjectWorldToScreen(EslToWorld(en),out PointF sp)) continue; float dx=sp.X-e.X,dy=sp.Y-e.Y,d=MathF.Sqrt(dx*dx+dy*dy); if(d<best){best=d;eh=en;}} SelectEnemyEntry(eh); EnemyEntryClicked?.Invoke(eh);}
            }
            else if (e.Button==MouseButtons.Left && !leftMouseMoved && EnemiesVisible && eslScene!=null)
            {
                EslEnemyEntry? hit=null; float best=18f; foreach(var en in eslScene.Entries.Where(EnemyIsVisible)){if(!TryProjectWorldToScreen(EslToWorld(en),out PointF sp)) continue; float dx=sp.X-e.X,dy=sp.Y-e.Y,d=MathF.Sqrt(dx*dx+dy*dy); if(d<best){best=d;hit=en;}} SelectEnemyEntry(hit); EnemyEntryClicked?.Invoke(hit);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragButton == MouseButtons.None) return;

        int dx = e.X - lastMouse.X;
        int dy = e.Y - lastMouse.Y;
        lastMouse = e.Location;

        if (dragButton == MouseButtons.Left &&
            (Math.Abs(e.X - mouseDownPoint.X) > 4 || Math.Abs(e.Y - mouseDownPoint.Y) > 4))
            leftMouseMoved = true;


        if (dragButton == MouseButtons.Right)
        {
            // Fly camera: mouse right rotates only the view. The camera position
            // never jumps around a pivot, so WASD remains predictable.
            yaw -= dx * LookSensitivity;
            pitch -= dy * LookSensitivity;
            pitch = Math.Clamp(pitch, -1.553f, 1.553f);

            // MouseMove can flood the UI queue while RMB is held. Updating movement
            // here prevents the WinForms timer from being starved during simultaneous
            // look + WASD navigation.
            UpdateCameraMovement();
        }
        else if (dragButton == MouseButtons.Middle)
        {
            GetCameraBasis(out _, out NVector3 right, out NVector3 up);
            float amount = Math.Max(moveSpeed * 0.006f, 0.0005f);
            NVector3 delta = (-right * dx + up * dy) * amount;
            cameraPosition += delta;
            target += delta;
        }
        else if (dragButton == MouseButtons.Left)
        {
            if (enemyDragMode != 0 && draggingEnemy != null)
            {
                if (enemyDragMode is 1 or 3 or 7)
                {
                    if (TryScreenPointOnHorizontalPlane(enemyDragStartMouse, enemyDragStartWorld.Y, out NVector3 start) && TryScreenPointOnHorizontalPlane(e.Location, enemyDragStartWorld.Y, out NVector3 now))
                    {
                        float rawX=enemyDragStartX+(now.X-start.X)/EslWorldScale, rawZ=enemyDragStartZ+(now.Z-start.Z)/EslWorldScale;
                        int step=EnemySnapEnabled?10:1;
                        if(enemyDragMode is 1 or 7) draggingEnemy.PosX=SnapEnemyShort(rawX,step);
                        if(enemyDragMode is 3 or 7) draggingEnemy.PosZ=SnapEnemyShort(rawZ,step);
                    }
                }
                else if (enemyDragMode == 2)
                {
                    float worldDelta = -(e.Y-enemyDragStartMouse.Y)/Math.Max(0.05f,enemyVerticalPixelsPerWorldUnit); draggingEnemy.PosY=SnapEnemyShort(enemyDragStartY + worldDelta/EslWorldScale,EnemySnapEnabled?10:1);
                }
                else if (enemyDragMode is >=4 and <=6)
                {
                    int delta=e.X-enemyDragStartMouse.X; int rawDelta=(int)MathF.Round(delta*(65536f/720f)); int step=EnemySnapEnabled?(int)MathF.Round(65536f*5f/360f):1;
                    short R(short start)=>SnapEnemyShort(start+rawDelta,step);
                    if(enemyDragMode==4) draggingEnemy.RotX=R(enemyDragStartRotX); else if(enemyDragMode==5) draggingEnemy.RotY=R(enemyDragStartRotY); else draggingEnemy.RotZ=R(enemyDragStartRotZ);
                }
                enemyGpuDirty=true; EnemyEntryEdited?.Invoke(draggingEnemy);
            }
            else if (draggingAevHandle >= 0 && draggingAevEntry != null)
            {
                if (draggingAevHandle <= 3)
                {
                    GetAevYRange(draggingAevEntry, out _, out float editY);
                    if (TryScreenPointOnHorizontalPlane(e.Location, editY, out NVector3 world))
                    {
                        SetAevCorner(draggingAevEntry, draggingAevHandle, new System.Numerics.Vector2(world.X, world.Z));
                        aevGpuDirty = true;
                        AevEntryEdited?.Invoke(draggingAevEntry);
                    }
                }
                else if (draggingAevHandle is 4 or 5)
                {
                    float pixelDelta = e.Y - heightDragStartMouseY;
                    float worldDelta = -pixelDelta / Math.Max(0.001f, heightDragPixelsPerWorldUnit);

                    float bottom = heightDragStartBottomY;
                    float top = heightDragStartTopY;

                    if (draggingAevHandle == 4)
                        bottom = Math.Min(top - 0.01f, heightDragStartBottomY + worldDelta);
                    else
                        top = Math.Max(bottom + 0.01f, heightDragStartTopY + worldDelta);

                    SetAevDisplayedYRange(draggingAevEntry, bottom, top);
                    aevGpuDirty = true;
                    AevEntryEdited?.Invoke(draggingAevEntry);
                }
                else if (draggingAevHandle == 6)
                {
                    GetAevYRange(draggingAevEntry, out _, out float topY);
                    float planeY = topY + Math.Max(0.05f, (scene?.Radius ?? 1f) * 0.002f);
                    if (TryScreenPointOnHorizontalPlane(mouseDownPoint, planeY, out NVector3 startWorld) &&
                        TryScreenPointOnHorizontalPlane(e.Location, planeY, out NVector3 currentWorld) &&
                        dragStartState.HasValue)
                    {
                        System.Numerics.Vector2 delta = new(currentWorld.X - startWorld.X, currentWorld.Z - startWorld.Z);
                        dragStartState.Value.Apply(draggingAevEntry);
                        TranslateAev(draggingAevEntry, delta);
                        aevGpuDirty = true;
                        AevEntryEdited?.Invoke(draggingAevEntry);
                    }
                }
                else if (draggingAevHandle == 7)
                {
                    float pixelDelta = e.Y - verticalMoveDragStartMouseY;
                    float displayDelta = -pixelDelta / Math.Max(0.001f, verticalMovePixelsPerWorldUnit);

                    // PS2 raw Y is sign-inverted by the viewport.
                    float rawDelta = draggingAevEntry.IsPs2Layout ? -displayDelta : displayDelta;
                    draggingAevEntry.Y = verticalMoveStartY + rawDelta;

                    aevGpuDirty = true;
                    AevEntryEdited?.Invoke(draggingAevEntry);
                }
            }
            // Without a handle drag, LMB remains selection-only.
        }
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        // Wheel controls fly speed instead of moving an invisible orbit pivot.
        // This makes close inspection much less sensitive and more predictable.
        float factor = e.Delta > 0 ? 1.20f : (1f / 1.20f);
        moveSpeed = Math.Clamp(moveSpeed * factor, 0.001f, Math.Max(100000f, (scene?.Radius ?? 1000f) * 100f));
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        if (key is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Q or Keys.E or Keys.F or Keys.Z or Keys.Delete or Keys.ShiftKey or Keys.ControlKey) return true;
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Control && e.KeyCode == Keys.Z)
        {
            if (!UndoEnemyEdit()) UndoAevEdit();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D)
        {
            DuplicateAevRequested?.Invoke();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.Delete)
        {
            DeleteAevRequested?.Invoke();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.F)
        {
            FitScene();
            e.Handled = true;
            return;
        }

        if (e.KeyCode is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Q or Keys.E or Keys.ShiftKey or Keys.ControlKey)
        {
            movementKeys.Add(e.KeyCode);
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode is Keys.W or Keys.A or Keys.S or Keys.D or Keys.Q or Keys.E or Keys.ShiftKey or Keys.ControlKey)
        {
            movementKeys.Remove(e.KeyCode);
            e.Handled = true;
        }
    }

    protected override void OnLostFocus(EventArgs e)
    {
        movementKeys.Clear();
        base.OnLostFocus(e);
    }

    private void MovementTimer_Tick(object? sender, EventArgs e)
    {
        UpdateCameraMovement();
    }

    private void UpdateCameraMovement()
    {
        long now = Environment.TickCount64;
        float dt = Math.Clamp((now - lastMovementTick) / 1000f, 0f, 0.05f);
        lastMovementTick = now;

        if (dt <= 0f || movementKeys.Count == 0 || (!ContainsFocus && !Capture)) return;

        NVector3 forward = GetForward();
        NVector3 horizontalForward = GetHorizontalForward();
        NVector3 right = NVector3.Cross(NVector3.UnitY, horizontalForward);
        if (right.LengthSquared() < 0.000001f) right = NVector3.UnitX;
        else right = NVector3.Normalize(right);

        NVector3 move = NVector3.Zero;
        if (movementKeys.Contains(Keys.W)) move += forward;
        if (movementKeys.Contains(Keys.S)) move -= forward;
        if (movementKeys.Contains(Keys.A)) move += right;
        if (movementKeys.Contains(Keys.D)) move -= right;
        if (movementKeys.Contains(Keys.E)) move += NVector3.UnitY;
        if (movementKeys.Contains(Keys.Q)) move -= NVector3.UnitY;
        if (move.LengthSquared() < 0.000001f) return;

        move = NVector3.Normalize(move);
        float modifier = 1f;
        if (movementKeys.Contains(Keys.ShiftKey)) modifier *= 4f;
        if (movementKeys.Contains(Keys.ControlKey)) modifier *= 0.25f;

        NVector3 delta = move * moveSpeed * MovementSpeedMultiplier * modifier * dt;
        cameraPosition += delta;
        target = cameraPosition + GetForward() * Math.Max(1f, distance);
        Invalidate();
    }

    private AevEntry? GetSelectedAevEntry()
    {
        if (aevScene == null || selectedAevFileOrder < 0) return null;
        return aevScene.Entries.FirstOrDefault(x => x.FileOrder == selectedAevFileOrder);
    }

    private int PickAevMoveHandle(Point screen, AevEntry entry)
    {
        GetAevYRange(entry, out float y0, out float y1);
        System.Numerics.Vector2 center2 = entry.IsCircle ? entry.Position1 : GetAevCenterXZ(entry);

        float extent = entry.IsCircle
            ? Math.Max(entry.VisualRadius, 0.25f)
            : Math.Max(System.Numerics.Vector2.Distance(entry.Position1, entry.Position3), 0.25f);
        float size = Math.Max(0.12f, Math.Max((scene?.Radius ?? 1f) * 0.0023f, extent * 0.05f));

        NVector3 origin = new(center2.X, (y0 + y1) * 0.5f, center2.Y);
        NVector3 sideEnd = origin + new NVector3(size * 4.2f, 0f, 0f);
        NVector3 upEnd = origin + new NVector3(0f, size * 4.2f, 0f);

        float sideDistance = ScreenDistanceToWorldSegment(screen, origin, sideEnd);
        float upDistance = ScreenDistanceToWorldSegment(screen, origin, upEnd);

        const float threshold = 11f;
        if (sideDistance <= threshold && sideDistance <= upDistance) return 6;
        if (upDistance <= threshold) return 7;
        return -1;
    }

    private float ScreenDistanceToWorldSegment(Point screen, NVector3 a, NVector3 b)
    {
        if (!TryProjectWorldToScreen(a, out PointF pa) || !TryProjectWorldToScreen(b, out PointF pb))
            return float.PositiveInfinity;

        float vx = pb.X - pa.X, vy = pb.Y - pa.Y;
        float wx = screen.X - pa.X, wy = screen.Y - pa.Y;
        float lenSq = vx * vx + vy * vy;
        float t = lenSq < 0.0001f ? 0f : Math.Clamp((wx * vx + wy * vy) / lenSq, 0f, 1f);
        float px = pa.X + vx * t, py = pa.Y + vy * t;
        float dx = screen.X - px, dy = screen.Y - py;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private int PickAevHeightHandle(Point screen, AevEntry entry)
    {
        GetAevYRange(entry, out float y0, out float y1);
        System.Numerics.Vector2 center2 = GetAevCenterXZ(entry);

        NVector3[] handles =
        {
            new(center2.X, y0, center2.Y),
            new(center2.X, y1, center2.Y)
        };

        int best = -1;
        float bestDistanceSq = 18f * 18f;

        for (int i = 0; i < handles.Length; i++)
        {
            if (!TryProjectWorldToScreen(handles[i], out PointF projected)) continue;
            float dx = projected.X - screen.X;
            float dy = projected.Y - screen.Y;
            float d2 = dx * dx + dy * dy;
            if (d2 <= bestDistanceSq)
            {
                bestDistanceSq = d2;
                best = 4 + i;
            }
        }

        return best;
    }

    private float CalculateVerticalPixelsPerWorldUnit(AevEntry entry)
    {
        GetAevYRange(entry, out float y0, out float y1);
        System.Numerics.Vector2 center2 = GetAevCenterXZ(entry);
        float centerY = (y0 + y1) * 0.5f;

        NVector3 a = new(center2.X, centerY, center2.Y);
        NVector3 b = new(center2.X, centerY + 1f, center2.Y);

        if (!TryProjectWorldToScreen(a, out PointF pa) || !TryProjectWorldToScreen(b, out PointF pb))
            return 10f;

        float pixels = MathF.Sqrt((pb.X - pa.X) * (pb.X - pa.X) + (pb.Y - pa.Y) * (pb.Y - pa.Y));
        return Math.Max(0.25f, pixels);
    }

    private static void SetAevDisplayedYRange(AevEntry entry, float bottom, float top)
    {
        if (top < bottom) (bottom, top) = (top, bottom);
        float height = Math.Max(0.01f, top - bottom);

        if (entry.IsPs2Layout)
        {
            // PS2 AEV display conversion is worldY = -rawY.
            // A positive raw Height therefore extends downward in display space.
            entry.Y = -top;
            entry.Height = height;
        }
        else
        {
            entry.Y = bottom;
            entry.Height = height;
        }
    }

    private int PickAevCornerHandle(Point screen, AevEntry entry)
    {
        GetAevYRange(entry, out _, out float y1);
        System.Numerics.Vector2[] points = { entry.Position1, entry.Position2, entry.Position3, entry.Position4 };

        int best = -1;
        float bestDistanceSq = 14f * 14f;

        for (int i = 0; i < points.Length; i++)
        {
            NVector3 world = new(points[i].X, y1, points[i].Y);
            if (!TryProjectWorldToScreen(world, out PointF projected)) continue;

            float dx = projected.X - screen.X;
            float dy = projected.Y - screen.Y;
            float d2 = dx * dx + dy * dy;
            if (d2 <= bestDistanceSq)
            {
                bestDistanceSq = d2;
                best = i;
            }
        }

        return best;
    }

    private bool TryProjectWorldToScreen(NVector3 world, out PointF screen)
    {
        screen = default;
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return false;

        Matrix4 mvp = BuildMvp();
        Vector4 clip = new Vector4(world.X, world.Y, world.Z, 1f) * mvp;
        if (clip.W <= 0.000001f) return false;

        float ndcX = clip.X / clip.W;
        float ndcY = clip.Y / clip.W;
        float ndcZ = clip.Z / clip.W;
        if (ndcZ < -1f || ndcZ > 1f) return false;

        screen = new PointF(
            (ndcX * 0.5f + 0.5f) * ClientSize.Width,
            (1f - (ndcY * 0.5f + 0.5f)) * ClientSize.Height);
        return true;
    }

    private bool TryScreenPointOnHorizontalPlane(Point screen, float planeY, out NVector3 world)
    {
        world = default;
        if (!TryBuildPickRay(screen, out NVector3 origin, out NVector3 direction)) return false;
        if (MathF.Abs(direction.Y) < 0.00001f) return false;

        float t = (planeY - origin.Y) / direction.Y;
        if (!float.IsFinite(t) || t <= 0f) return false;

        world = origin + direction * t;
        return float.IsFinite(world.X) && float.IsFinite(world.Y) && float.IsFinite(world.Z);
    }

    private bool TryBuildPickRay(Point screen, out NVector3 rayOrigin, out NVector3 rayDirection)
    {
        rayOrigin = default;
        rayDirection = default;
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0) return false;

        NVector3 forward = GetForward();
        Vector3 eye = new(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);
        Vector3 center = new(cameraPosition.X + forward.X, cameraPosition.Y + forward.Y, cameraPosition.Z + forward.Z);
        Matrix4 view = Matrix4.LookAt(eye, center, Vector3.UnitY);

        float aspect = Math.Max(0.01f, ClientSize.Width / (float)Math.Max(1, ClientSize.Height));
        float radius = scene?.Radius ?? 1000f;
        float distanceToScene = scene == null ? 1000f : NVector3.Distance(cameraPosition, scene.Center);
        float near = Math.Max(0.001f, radius * 0.00005f);
        float far = Math.Max(near + 100f, distanceToScene + radius * 30f);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), aspect, near, far);

        Matrix4 viewProjection = view * projection;
        Matrix4.Invert(viewProjection, out Matrix4 inverseViewProjection);

        float ndcX = (2f * screen.X / Math.Max(1, ClientSize.Width)) - 1f;
        float ndcY = 1f - (2f * screen.Y / Math.Max(1, ClientSize.Height));

        Vector4 nearClip = new(ndcX, ndcY, -1f, 1f);
        Vector4 farClip = new(ndcX, ndcY, 1f, 1f);
        Vector4 nearWorld4 = nearClip * inverseViewProjection;
        Vector4 farWorld4 = farClip * inverseViewProjection;

        if (MathF.Abs(nearWorld4.W) < 0.000001f || MathF.Abs(farWorld4.W) < 0.000001f) return false;

        nearWorld4 /= nearWorld4.W;
        farWorld4 /= farWorld4.W;

        rayOrigin = new NVector3(nearWorld4.X, nearWorld4.Y, nearWorld4.Z);
        rayDirection = new NVector3(
            farWorld4.X - nearWorld4.X,
            farWorld4.Y - nearWorld4.Y,
            farWorld4.Z - nearWorld4.Z);

        if (rayDirection.LengthSquared() < 0.000001f) return false;
        rayDirection = NVector3.Normalize(rayDirection);
        return true;
    }

    private static void TranslateAev(AevEntry entry, System.Numerics.Vector2 delta)
    {
        entry.Position1 += delta;
        entry.Position2 += delta;
        entry.Position3 += delta;
        entry.Position4 += delta;
    }

    private static void SetAevCorner(AevEntry entry, int corner, System.Numerics.Vector2 position)
    {
        switch (corner)
        {
            case 0: entry.Position1 = position; break;
            case 1: entry.Position2 = position; break;
            case 2: entry.Position3 = position; break;
            case 3: entry.Position4 = position; break;
        }
    }

    private void UndoAevVertexEdit() => UndoAevEdit();

    private void TrimEnemyUndoStack()
    {
        Action[] current = enemyUndo.ToArray();
        enemyUndo.Clear();
        for (int i = Math.Min(63, current.Length - 1); i >= 0; i--) enemyUndo.Push(current[i]);
    }

    private void TrimUndoStack()
    {
        // Keep the 64 most recent operations without exposing Stack internals.
        Action[] current = aevUndo.ToArray();
        aevUndo.Clear();
        for (int i = Math.Min(63, current.Length - 1); i >= 0; i--)
            aevUndo.Push(current[i]);
    }

    private readonly struct AevVertexState : IEquatable<AevVertexState>
    {
        public readonly System.Numerics.Vector2 P1, P2, P3, P4;
        public readonly float Y, Height;

        public AevVertexState(System.Numerics.Vector2 p1, System.Numerics.Vector2 p2,
            System.Numerics.Vector2 p3, System.Numerics.Vector2 p4, float y, float height)
        {
            P1 = p1; P2 = p2; P3 = p3; P4 = p4;
            Y = y; Height = height;
        }

        public static AevVertexState From(AevEntry entry) =>
            new(entry.Position1, entry.Position2, entry.Position3, entry.Position4, entry.Y, entry.Height);

        public void Apply(AevEntry entry)
        {
            entry.Position1 = P1; entry.Position2 = P2; entry.Position3 = P3; entry.Position4 = P4;
            entry.Y = Y; entry.Height = Height;
        }

        public AevVertexState WithOldProperty(string propertyName, float oldValue)
        {
            System.Numerics.Vector2 p1 = P1, p2 = P2, p3 = P3, p4 = P4;
            float y = Y, height = Height;

            switch (propertyName)
            {
                case nameof(AevEntry.Y): y = oldValue; break;
                case nameof(AevEntry.Height): height = oldValue; break;
                case nameof(AevEntry.Point1X): p1.X = oldValue; break;
                case nameof(AevEntry.Point1Z): p1.Y = oldValue; break;
                case nameof(AevEntry.Point2X): p2.X = oldValue; break;
                case nameof(AevEntry.Point2Z): p2.Y = oldValue; break;
                case nameof(AevEntry.Point3X): p3.X = oldValue; break;
                case nameof(AevEntry.Point3Z): p3.Y = oldValue; break;
                case nameof(AevEntry.Point4X): p4.X = oldValue; break;
                case nameof(AevEntry.Point4Z): p4.Y = oldValue; break;
                default: return this;
            }
            return new AevVertexState(p1, p2, p3, p4, y, height);
        }

        public bool Equals(AevVertexState other) =>
            P1.Equals(other.P1) && P2.Equals(other.P2) && P3.Equals(other.P3) && P4.Equals(other.P4) &&
            Y.Equals(other.Y) && Height.Equals(other.Height);

        public override bool Equals(object? obj) => obj is AevVertexState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(P1, P2, P3, P4, Y, Height);
    }



    private AevEntry? PickAevEntry(Point screen)
    {
        if (aevScene == null || !TryBuildPickRay(screen, out NVector3 rayOrigin, out NVector3 rayDirection))
            return null;

        AevEntry? best = null;
        float bestDistance = float.PositiveInfinity;

        foreach (AevEntry entry in aevScene.Entries)
        {
            if (aevTypeFilter.HasValue && entry.Type != aevTypeFilter.Value) continue;
            if (!entry.IsSquare && !entry.IsCircle) continue;

            if (RayIntersectsAev(rayOrigin, rayDirection, entry, out float distance) &&
                distance >= 0f && distance < bestDistance)
            {
                bestDistance = distance;
                best = entry;
            }
        }

        return best;
    }

    private static bool RayIntersectsAev(NVector3 origin, NVector3 direction, AevEntry entry, out float bestDistance)
    {
        bestDistance = float.PositiveInfinity;
        var triangles = new List<(NVector3 A, NVector3 B, NVector3 C)>(80);
        BuildAevPickTriangles(triangles, entry);

        bool hit = false;
        foreach (var tri in triangles)
        {
            if (RayTriangle(origin, direction, tri.A, tri.B, tri.C, out float distance) && distance < bestDistance)
            {
                bestDistance = distance;
                hit = true;
            }
        }
        return hit;
    }

    private static void BuildAevPickTriangles(List<(NVector3 A, NVector3 B, NVector3 C)> output, AevEntry entry)
    {
        GetAevYRange(entry, out float y0, out float y1);

        if (entry.IsCircle)
        {
            float r = entry.VisualRadius;
            const int segments = 24;
            NVector3 bc = new(entry.Position1.X, y0, entry.Position1.Y);
            NVector3 tc = new(entry.Position1.X, y1, entry.Position1.Y);
            for (int i = 0; i < segments; i++)
            {
                float a0 = MathF.Tau * i / segments;
                float a1 = MathF.Tau * (i + 1) / segments;
                NVector3 b0 = new(bc.X + MathF.Cos(a0) * r, y0, bc.Z + MathF.Sin(a0) * r);
                NVector3 b1 = new(bc.X + MathF.Cos(a1) * r, y0, bc.Z + MathF.Sin(a1) * r);
                NVector3 t0 = new(b0.X, y1, b0.Z);
                NVector3 t1 = new(b1.X, y1, b1.Z);
                output.Add((b0, b1, t1)); output.Add((b0, t1, t0));
                output.Add((bc, b1, b0)); output.Add((tc, t0, t1));
            }
            return;
        }

        if (entry.IsSquare)
        {
            NVector3[] b =
            {
                new(entry.Position1.X, y0, entry.Position1.Y),
                new(entry.Position2.X, y0, entry.Position2.Y),
                new(entry.Position3.X, y0, entry.Position3.Y),
                new(entry.Position4.X, y0, entry.Position4.Y)
            };
            NVector3[] t = b.Select(v => new NVector3(v.X, y1, v.Z)).ToArray();

            AddPickQuad(output, b[0], b[1], b[2], b[3]);
            AddPickQuad(output, t[3], t[2], t[1], t[0]);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) & 3;
                AddPickQuad(output, b[i], b[j], t[j], t[i]);
            }
        }
    }

    private static void AddPickQuad(List<(NVector3 A, NVector3 B, NVector3 C)> output,
        NVector3 a, NVector3 b, NVector3 c, NVector3 d)
    {
        output.Add((a, b, c));
        output.Add((a, c, d));
    }

    private static bool RayTriangle(NVector3 origin, NVector3 direction,
        NVector3 a, NVector3 b, NVector3 c, out float distance)
    {
        const float epsilon = 0.000001f;
        NVector3 edge1 = b - a;
        NVector3 edge2 = c - a;
        NVector3 h = NVector3.Cross(direction, edge2);
        float det = NVector3.Dot(edge1, h);
        if (MathF.Abs(det) < epsilon) { distance = 0f; return false; }

        float invDet = 1f / det;
        NVector3 s = origin - a;
        float u = invDet * NVector3.Dot(s, h);
        if (u < 0f || u > 1f) { distance = 0f; return false; }

        NVector3 q = NVector3.Cross(s, edge1);
        float v = invDet * NVector3.Dot(direction, q);
        if (v < 0f || u + v > 1f) { distance = 0f; return false; }

        distance = invDet * NVector3.Dot(edge2, q);
        return distance > epsilon;
    }

    private NVector3 GetForward()
    {
        float cp = MathF.Cos(pitch);
        NVector3 forward = new(cp * MathF.Sin(yaw), MathF.Sin(pitch), cp * MathF.Cos(yaw));
        return NVector3.Normalize(forward);
    }

    private NVector3 GetHorizontalForward()
    {
        // Yaw-only forward vector for FPS/editor navigation.
        NVector3 forward = new(MathF.Sin(yaw), 0f, MathF.Cos(yaw));
        if (forward.LengthSquared() < 0.000001f) return NVector3.UnitZ;
        return NVector3.Normalize(forward);
    }

    private void GetCameraBasis(out NVector3 forward, out NVector3 right, out NVector3 up)
    {
        forward = GetForward();
        right = NVector3.Cross(NVector3.UnitY, forward);
        if (right.LengthSquared() < 0.000001f) right = NVector3.UnitX;
        else right = NVector3.Normalize(right);
        up = NVector3.Normalize(NVector3.Cross(forward, right));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            movementTimer.Stop();
            movementTimer.Dispose();
        }
        if (disposing && glReady && !IsDesignMode)
        {
            try
            {
                MakeCurrent();
                if (meshVbo != 0) GL.DeleteBuffer(meshVbo);
                if (meshVao != 0) GL.DeleteVertexArray(meshVao);
                if (gridVbo != 0) GL.DeleteBuffer(gridVbo);
                if (gridVao != 0) GL.DeleteVertexArray(gridVao);
                if (aevVbo != 0) GL.DeleteBuffer(aevVbo);
                if (aevVao != 0) GL.DeleteVertexArray(aevVao);
                if (aevSelectedVbo != 0) GL.DeleteBuffer(aevSelectedVbo);
                if (aevSelectedVao != 0) GL.DeleteVertexArray(aevSelectedVao);
                if (enemyVbo != 0) GL.DeleteBuffer(enemyVbo);
                if (enemyVao != 0) GL.DeleteVertexArray(enemyVao);
                if (selectedEnemyVbo != 0) GL.DeleteBuffer(selectedEnemyVbo);
                if (selectedEnemyVao != 0) GL.DeleteVertexArray(selectedEnemyVao);
                if (enemyModelVbo != 0) GL.DeleteBuffer(enemyModelVbo);
                if (enemyModelVao != 0) GL.DeleteVertexArray(enemyModelVao);
                if (selectedEnemyModelVbo != 0) GL.DeleteBuffer(selectedEnemyModelVbo);
                if (selectedEnemyModelVao != 0) GL.DeleteVertexArray(selectedEnemyModelVao);
                ReleaseEnemyTextures();
                ReleaseTextures();
                if (shaderProgram != 0) GL.DeleteProgram(shaderProgram);
            }
            catch { }
        }
        base.Dispose(disposing);
    }
    private readonly record struct EnemyTextureKey(byte EnemyType, int TplEntryIndex, int TextureIndex);
    private readonly record struct EnemyModelDrawBatch(EnemyTextureKey Key, int FirstVertex, int VertexCount);

    private readonly struct ScenarioDrawBatch
    {
        public readonly int TextureIndex;
        public readonly int FirstVertex;
        public readonly int VertexCount;
        public ScenarioDrawBatch(int textureIndex, int firstVertex, int vertexCount)
        {
            TextureIndex = textureIndex;
            FirstVertex = firstVertex;
            VertexCount = vertexCount;
        }
    }

}

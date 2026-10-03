using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;
using RE4_PS2_MOD_WORKSPACE.Core.Effects;
using System.Drawing.Imaging;
using System.Diagnostics;
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
public enum AevTransformSpace { World, Local }

public enum EnemyMeshGizmoMode { Move, Rotate }

public enum EnemyGizmoMode
{
    Move,
    Rotate
}

public enum EtsGizmoMode { Move, Rotate }
public enum AevGizmoMode { Move, Rotate }
public enum CamGizmoMode { Move, Scale, Vertex, Face, Frame }

public readonly record struct ScenarioCameraState(float X, float Y, float Z, float Yaw, float Pitch);

public sealed partial class ScenarioViewport : GLControl
{
    private ScenarioScene? scene;
    private float yaw = 0.75f;
    private float pitch = -0.35f;
    private float distance = 1000f;
    private float fieldOfViewDegrees = 60f;
    private int backgroundBrightness = 8;
    public int BackgroundBrightness
    {
        get => backgroundBrightness;
        set
        {
            backgroundBrightness = Math.Clamp(value, 0, 160);
            BackColor = Color.FromArgb(backgroundBrightness, Math.Min(255, backgroundBrightness + 2), Math.Min(255, backgroundBrightness + 5));
            Invalidate();
        }
    }
    public float? ForcedAspectRatio { get; set; }
    public bool FieldOfViewIsHorizontal { get; set; }
    private NVector3 target = NVector3.Zero;
    private NVector3 cameraPosition = new(0f, 0f, -1000f);
    private float moveSpeed = 100f;
    private Point lastMouse;
    private MouseButtons dragButton = MouseButtons.None;
    private readonly HashSet<Keys> movementKeys = new();
    private readonly System.Windows.Forms.Timer movementTimer;
    private readonly System.Windows.Forms.Timer fpsRenderTimer;
    private long lastMovementTick;
    private long fpsSampleStart = Stopwatch.GetTimestamp();
    private int fpsSampleFrames;
    private int displayedFps;
    private string? statusMessage;

    private bool glReady;
    private bool gpuDirty;
    private int shaderProgram;
    private int uMvp;
    private int uModel;
    private int uNormalMatrix;
    private int uNormalSign;
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
    private int uTextureTint;
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
    private readonly int[] aevGizmoVaos = new int[3], aevGizmoVbos = new int[3], aevGizmoVertexCounts = new int[3];
    private int aevHandleVao;
    private int aevHandleVbo;
    private int aevHandleVertexCount;
    private int selectedAevFileOrder = -1;
    private byte? aevTypeFilter;
    private Point mouseDownPoint;
    private bool leftMouseMoved;
    private int uOpacity;
    private EslScene? eslScene; private bool enemyGpuDirty; private int enemyVao, enemyVbo, enemyVertexCount, selectedEnemyVao, selectedEnemyVbo, selectedEnemyVertexCount; private readonly int[] enemyGizmoVaos=new int[3],enemyGizmoVbos=new int[3],enemyGizmoCounts=new int[3]; private int selectedEnemyIndex=-1;
    private bool enemyGpuPreviewActive;
    private Matrix4 enemyGpuPreviewModel=Matrix4.Identity;
    private EtsScene? etsScene; private EtmCatalog? etmCatalog; private bool etsGpuDirty; private int etsVao, etsVbo, etsVertexCount, selectedEtsVao, selectedEtsVbo, selectedEtsVertexCount; private int etsModelVao, etsModelVbo, etsModelVertexCount, selectedEtsModelVao, selectedEtsModelVbo, selectedEtsModelVertexCount; private int selectedEtsFileOrder=-1; private readonly HashSet<int> selectedEtsFileOrders=new();
    private IReadOnlyDictionary<byte, EnemyModelScene> enemyModels = new Dictionary<byte, EnemyModelScene>();
    // v0.4.3 model-parts debugger: hidden BIN ordinals are stored per emXX type.
    private readonly Dictionary<byte, HashSet<int>> hiddenEnemyModelParts = new();
    // Types touched by the MODEL PARTS debugger use the manual visibility mask.
    // Untouched types use the automatic v0.4.5 body/head/hands filter.
    private readonly HashSet<byte> manualEnemyModelPartTypes = new();
    private byte? highlightedEnemyModelType;
    private int highlightedEnemyModelPart = -1;
    private bool showEnemySkeletonDiagnostic;
    private byte? enemyDiagnosticBoneId;
    private readonly HashSet<int> selectedEnemyModelFaceFlags = new();
    private int selectedEnemyModelFaceBin = -1;
    private int enemyFaceGizmoAxis;
    private Point enemyFaceGizmoStartMouse;
    private NVector3 enemyFaceGizmoDelta;
    private NVector3 enemyFaceGizmoRotationDegrees;
    private readonly Dictionary<(byte EnemyType, int BinIndex), (int TplEntry, int TextureIndex)> enemyTextureAssignments = new();
    private int enemyModelVao, enemyModelVbo, enemyModelVertexCount, selectedEnemyModelVao, selectedEnemyModelVbo, selectedEnemyModelVertexCount;
    private readonly List<EnemyModelDrawBatch> enemyModelBatches = new();
    private readonly List<EnemyModelDrawBatch> selectedEnemyModelBatches = new();
    private readonly Dictionary<EnemyTextureKey, int> glEnemyTextures = new();
    private readonly Dictionary<EnemyTextureKey, bool> glEnemyTextureHasTransparency = new();
    // Experimental v0.4.9 weapon attachment. The body skeleton is read from the enemy DAT.
    private FcvAnimation? enemyAttachmentAnimation;
    private bool enemyAttachmentAnimationForAll;
    private bool enemyAnimationIgnoreRootMotion;
    private float enemyAttachmentFrame;
    private bool enemyIdleAnimationEnabled;
    private float enemyIdleAnimationFrame;
    private FcvAnimation? enemyIdleAnimationOverride;
    private bool enemyIdleStabilizeFeet;
    private readonly Dictionary<(byte EnemyType,int AnimationId),FcvSkeletonPose> enemyPoseFrameCache=new();
    private readonly Dictionary<string,FcvSkeletonPose> enemyBindPoseCache=new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(byte EnemyType,int AnimationId,NVector3 Position,EnemyVertexSkin Skin),NVector3> enemySkinnedVertexFrameCache=new();
    private readonly Dictionary<EnemyModelPart,IReadOnlyDictionary<NVector3,NVector3>> enemySmoothNormalCache=new();
    private int enemyAttachmentBoneIndex = -1;
    private readonly HashSet<(byte EnemyType,int BinIndex)> forcedEnemyHandHeldParts=new();
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

    private int draggingAevHandle = -1; // edit: 0..3 corners, 4 bottom, 5 top; transform: 6 X, 7 Y, 8 Z, 9 rotate Y
    private AevEntry? draggingAevEntry;
    private AevVertexState? dragStartState;
    private float heightDragStartMouseY;
    private float heightDragStartBottomY;
    private float heightDragStartTopY;
    private float heightDragPixelsPerWorldUnit = 1f;
    private float verticalMoveDragStartMouseY;
    private float verticalMoveStartY;
    private float verticalMovePixelsPerWorldUnit = 1f;
    private float aevRotationStartAngle;
    public AevTransformSpace AevTransformSpace { get; private set; }
    public AevGizmoMode AevTransformMode { get; set; } = AevGizmoMode.Move;
    public bool AevEditMode { get; private set; }
    private readonly Stack<Action> aevUndo = new();
    private readonly Stack<Action> enemyUndo = new();

    public event Action<AevEntry?>? AevEntryClicked;
    public event Action<AevEntry>? AevEntryEdited;
    public event Action? DuplicateAevRequested;
    public event Action? DeleteAevRequested;

    public bool ScenarioVisible { get; set; } = true;
    public bool AevVisible { get; set; } = true;
    public bool EnemiesVisible { get; set; } = true;
    public bool ObjectsVisible { get; set; } = true;
    public bool ShowInactiveEnemies
    {
        get => showInactiveEnemies;
        set { if (showInactiveEnemies == value) return; showInactiveEnemies = value; enemyGpuDirty = true; Invalidate(); }
    }
    public EslScene? EslScene => eslScene;
    public EtsScene? EtsScene => etsScene;
    public event Action<EslEnemyEntry?>? EnemyEntryClicked;
    public event Action<EslEnemyEntry, Point>? EnemyLabelClicked;
    public event Action<EnemyModelPart?>? EnemyModelPartClicked;
    public event Action<EnemyModelFaceHit?, bool>? EnemyModelFaceClicked;
    public event Action<NVector3>? EnemyModelFaceTranslationRequested;
    public event Action<NVector3>? EnemyModelPartTranslationRequested;
    public event Action<NVector3>? EnemyModelPartRotationRequested;
    public event Action? ExternalUndoRequested;
    public event Action? ExternalRedoRequested;
    public event Action<EslEnemyEntry>? EnemyEntryEdited;
    public event Action<EtsEntry?>? EtsEntryClicked;
    public event Action<IReadOnlyList<EtsEntry>>? EtsSelectionChanged;
    public event Action<EtsEntry>? EtsEntryEdited;
    public AevScene? AevScene => aevScene;
    public ScenarioScene? Scene => scene;
    public int LoadedTextureCount => glTextures.Count;
    public int TexturedBatchCount => meshBatches.Count(x => x.TextureIndex >= 0 && glTextures.ContainsKey(x.TextureIndex));
    public int MeshBatchCount => meshBatches.Count;
    public float MovementSpeedMultiplier { get; set; } = 1f;
    public float FlySpeed
    {
        get => moveSpeed;
        set
        {
            if (!float.IsFinite(value) || value <= 0f) return;
            moveSpeed = Math.Clamp(value, 0.001f, Math.Max(100000f, (scene?.Radius ?? 1000f) * 100f));
        }
    }
    public event EventHandler? MovementSpeedChanged;
    public event Action<int>? FpsUpdated;
    public bool ShowFps
    {
        get => showFps;
        set
        {
            if (showFps == value) return;
            showFps = value;
            fpsSampleFrames = 0;
            fpsSampleStart = Stopwatch.GetTimestamp();
            if (value) { fpsRenderTimer.Start(); Invalidate(); }
            else fpsRenderTimer.Stop();
        }
    }
    private bool showFps;
    public float LookSensitivity { get; set; } = 0.0032f;
    public bool ShowAevLabels { get; set; } = true;
    public bool ShowEnemyLabels { get; set; } = false;
    public bool ShowEmiLabels { get; set; } = true;
    public bool EnemyModelPartPickingEnabled { get; set; }
    public bool EnemyModelGizmoEnabled { get; set; } = true;
    public EnemyMeshGizmoMode EnemyModelGizmoMode { get; private set; } = EnemyMeshGizmoMode.Move;
    public void SetEnemyModelGizmoMode(EnemyMeshGizmoMode mode)
    {
        EnemyModelGizmoMode = mode;
        enemyFaceGizmoAxis = 0;
        enemyFaceGizmoDelta = NVector3.Zero;
        enemyFaceGizmoRotationDegrees = NVector3.Zero;
        enemyGpuDirty = true;
        Invalidate();
    }
    public bool EnemyModelFacePickingEnabled { get; set; }
    public bool EnemyModelEditWireframeVisible { get; set; }
    public bool ShowWorldOriginMarker { get; set; }
    public bool GridAtWorldOrigin { get; set; }
    public float GridRadiusOverride { get; set; }
    public ScenarioRenderMode RenderMode { get; set; } = ScenarioRenderMode.Solid;

    public void SetStatusMessage(string? message)
    {
        statusMessage = string.IsNullOrWhiteSpace(message) ? null : message;
        Invalidate();
    }
    public EnemyGizmoMode EnemyTransformMode { get; set; } = EnemyGizmoMode.Move;
    public bool EnemySnapEnabled { get; set; }
    public EtsGizmoMode EtsTransformMode { get; set; } = EtsGizmoMode.Move;
    public bool EtsSnapEnabled { get; set; }

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

    public void SetCameraLookAt(NVector3 position, NVector3 lookTarget, float fovDegrees = 60f)
    {
        NVector3 direction = lookTarget - position;
        if (!float.IsFinite(direction.X) || !float.IsFinite(direction.Y) || !float.IsFinite(direction.Z) || direction.LengthSquared() < .000001f) return;
        direction = NVector3.Normalize(direction);
        cameraPosition = position;
        yaw = MathF.Atan2(direction.X, direction.Z);
        pitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));
        target = lookTarget;
        distance = Math.Max(.01f, NVector3.Distance(position, lookTarget));
        fieldOfViewDegrees = Math.Clamp(float.IsFinite(fovDegrees) ? fovDegrees : 60f, 5f, 150f);
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
        fpsRenderTimer = new System.Windows.Forms.Timer { Interval = 16 };
        fpsRenderTimer.Tick += (_, _) => { if ((showFps || HasAnimatedLit) && Visible) Invalidate(); };
        lastMovementTick = Environment.TickCount64;
    }

    public void SetScene(ScenarioScene? value, bool fit = true)
    {
        scene = value;
        unlitSceneExposure=CalculateUnlitSceneExposure(value);
        if(value!=null)RebuildScenarioGeometry();
        selectedSmdEntry = -1;
        smdOverlayDirty = true;
        gpuDirty = true;
        if (fit) FitScene();
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
    public void SetEtsScene(EtsScene? value, EtmCatalog? catalog) { etsScene=value; etmCatalog=catalog; selectedEtsFileOrder=-1; selectedEtsFileOrders.Clear(); etsGpuDirty=true; etsTexturesDirty=true; Invalidate(); }
    public void SelectEtsEntry(EtsEntry? entry) { SelectEtsEntries(entry==null?Array.Empty<EtsEntry>():new[]{entry},entry); }
    public void SelectEtsEntries(IEnumerable<EtsEntry> entries,EtsEntry? primary=null) { var chosen=entries.ToArray();selectedEtsFileOrders.Clear();foreach(var e in chosen)selectedEtsFileOrders.Add(e.FileOrder);selectedEtsFileOrder=(primary??chosen.LastOrDefault())?.FileOrder??-1;etsGpuDirty=true;Invalidate(); }
    public void RefreshEtsGeometry(EtsEntry? selected=null) { if(selected!=null) selectedEtsFileOrder=selected.FileOrder; etsGpuDirty=true; Invalidate(); }
    public void SetEnemyModels(IReadOnlyDictionary<byte, EnemyModelScene>? models)
    {
        enemyModels = models ?? new Dictionary<byte, EnemyModelScene>();
        ClearEnemyAnimationFrameCaches();enemyBindPoseCache.Clear();enemySmoothNormalCache.Clear();
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
    public void SetEnemyForcedHandHeldPart(byte enemyType,int? binIndex)
    {
        forcedEnemyHandHeldParts.RemoveWhere(x=>x.EnemyType==enemyType);
        if(binIndex.HasValue)forcedEnemyHandHeldParts.Add((enemyType,binIndex.Value));
        enemyGpuDirty=true;Invalidate();
    }
    public void SetEnemyAttachmentOffset(float x,float y,float z) { enemyAttachmentOffset=new NVector3(x,y,z); enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyAttachmentRotation(float x,float y,float z) { enemyAttachmentRotationDegrees=new NVector3(x,y,z); enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyAttachmentAnimation(FcvAnimation? animation,float frame,bool applyToAll=false) { enemyAttachmentAnimation=animation; enemyAttachmentFrame=frame; enemyAttachmentAnimationForAll=applyToAll;ClearEnemyAnimationFrameCaches();enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyAnimationIgnoreRootMotion(bool ignore) { if(enemyAnimationIgnoreRootMotion==ignore)return; enemyAnimationIgnoreRootMotion=ignore; ClearEnemyAnimationFrameCaches(); enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyIdleAnimation(bool enabled, float frame, FcvAnimation? animation=null, bool stabilizeFeet=false)
    {
        enemyIdleAnimationEnabled=enabled;
        enemyIdleAnimationFrame=frame;
        enemyIdleAnimationOverride=animation;
        enemyIdleStabilizeFeet=stabilizeFeet;
        // Keep the already uploaded pose while an enemy is being transformed. The
        // latest animation frame is rebuilt once on mouse-up together with the edit.
        if(enemyGpuPreviewActive)return;
        ClearEnemyAnimationFrameCaches();
        enemyGpuDirty=true;
        Invalidate();
    }
    private void ClearEnemyAnimationFrameCaches(){enemyPoseFrameCache.Clear();enemySkinnedVertexFrameCache.Clear();}
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
    public void SetVisibleEnemyModelParts(byte enemyType, IReadOnlySet<int> visibleBins)
    {
        if (!enemyModels.TryGetValue(enemyType, out EnemyModelScene? model)) return;
        manualEnemyModelPartTypes.Add(enemyType);
        var hidden = model.Parts.Where(part => !visibleBins.Contains(part.BinIndex)).Select(part => part.BinIndex).ToHashSet();
        if (hidden.Count == 0) hiddenEnemyModelParts.Remove(enemyType);
        else hiddenEnemyModelParts[enemyType] = hidden;
        enemyGpuDirty = true;
        Invalidate();
    }
    public void UseAutomaticEnemyModelParts(byte enemyType) { manualEnemyModelPartTypes.Remove(enemyType); hiddenEnemyModelParts.Remove(enemyType); enemyGpuDirty=true; Invalidate(); }
    public void SoloEnemyModelPart(byte enemyType, int binIndex)
    {
        if (!enemyModels.TryGetValue(enemyType, out EnemyModelScene? model)) return;
        manualEnemyModelPartTypes.Add(enemyType);
        var hidden = new HashSet<int>(model.Parts.Where(x => x.BinIndex != binIndex).Select(x => x.BinIndex));
        if (hidden.Count == 0) hiddenEnemyModelParts.Remove(enemyType); else hiddenEnemyModelParts[enemyType] = hidden;
        enemyGpuDirty=true; Invalidate();
    }
    public void HighlightEnemyModelPart(byte enemyType, int? binIndex)
    {
        highlightedEnemyModelType = binIndex.HasValue ? enemyType : null;
        highlightedEnemyModelPart = binIndex ?? -1;
        enemyGpuDirty = true;
        Invalidate();
    }
    public void SetEnemyBoneDiagnostic(bool showSkeleton, byte? boneId)
    {
        showEnemySkeletonDiagnostic = showSkeleton;
        enemyDiagnosticBoneId = boneId;
        enemyGpuDirty = true;
        Invalidate();
    }
    public void SetSelectedEnemyModelFaces(int binIndex, IEnumerable<int>? stripFlagOffsets)
    {
        selectedEnemyModelFaceBin = binIndex;
        selectedEnemyModelFaceFlags.Clear();
        if (stripFlagOffsets != null) foreach (int offset in stripFlagOffsets) selectedEnemyModelFaceFlags.Add(offset);
        enemyGpuDirty = true; Invalidate();
    }
    public void SetEnemyTextureAssignments(byte enemyType, IReadOnlyDictionary<int, (int TplEntry, int TextureIndex)>? assignments)
    {
        foreach (var key in enemyTextureAssignments.Keys.Where(x => x.EnemyType == enemyType).ToArray()) enemyTextureAssignments.Remove(key);
        if (assignments != null) foreach (var pair in assignments) enemyTextureAssignments[(enemyType, pair.Key)] = pair.Value;
        enemyGpuDirty = true; Invalidate();
    }
    public EnemyModelPart? PickEnemyModelPartAt(Point clientPoint) => PickEnemyModelPart(clientPoint);
    public void SetEnemyLocationFilter(byte? stageId, byte? roomId) { enemyStageFilter=stageId; enemyRoomFilter=roomId; selectedEnemyIndex=-1; enemyGpuDirty=true; Invalidate(); }
    private bool EnemyPassesLocationFilter(EslEnemyEntry e) => (!enemyStageFilter.HasValue || e.StageID==enemyStageFilter.Value) && (!enemyRoomFilter.HasValue || e.RoomID==enemyRoomFilter.Value);
    private bool EnemyIsVisible(EslEnemyEntry e) => (showInactiveEnemies || e.Active != 0) && EnemyPassesLocationFilter(e);
    public bool HasVisibleEnemyType(byte enemyType) => eslScene?.Entries.Any(e => e.EnemyType == enemyType && EnemyIsVisible(e)) == true;
    private static NVector3 EslToWorld(EslEnemyEntry e) => new(e.PosX*EslWorldScale,e.PosY*EslWorldScale,e.PosZ*EslWorldScale);
    public void SelectEnemyEntry(EslEnemyEntry? entry) { selectedEnemyIndex=entry?.Index ?? -1; enemyGpuDirty=true; Invalidate(); }
    public void RefreshEnemyGeometry(EslEnemyEntry? entry=null) { if(entry!=null) selectedEnemyIndex=entry.Index; enemyGpuDirty=true; Invalidate(); }
    public void SetEnemyTransformMode(EnemyGizmoMode mode) { EnemyTransformMode=mode;enemyGpuDirty=true;Invalidate(); }

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

    public void SetAevTransformMode(AevGizmoMode mode)
    {
        AevTransformMode = mode;
        aevGpuDirty = true;
        Invalidate();
    }

    public void SetAevTransformSpace(AevTransformSpace space)
    {
        AevTransformSpace=space;aevGpuDirty=true;Invalidate();
    }

    public void SetAevEditMode(bool enabled)
    {
        AevEditMode = enabled;
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

    public void FocusEnemyModel(EslEnemyEntry? enemy,EnemyModelScene? model)
    {
        if(enemy==null||model==null){FocusEnemy(enemy);return;}
        NVector3 origin=EslToWorld(enemy);
        NVector3 center=(model.BoundsMin+model.BoundsMax)*0.5f+origin;
        float radius=Math.Max(1f,(model.BoundsMax-model.BoundsMin).Length()*0.55f);
        target=center;distance=Math.Max(4f,radius*2.25f);yaw=MathF.PI;pitch=-0.08f;
        cameraPosition=target-GetForward()*distance;moveSpeed=Math.Max(0.2f,radius*0.12f);
        movementKeys.Clear();lastMovementTick=Environment.TickCount64;Invalidate();Focus();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (IsDesignMode) return;
        MakeCurrent();
        InitializeGl();
        gpuDirty = true;
        // Models can be assigned before a detached editor window is shown. In that
        // case their invalidation happens before the GLControl owns a visible handle.
        // Queue the first real frame after the context has finished loading.
        Invalidate();
        BeginInvoke(() => { if (!IsDisposed) { Invalidate(); Update(); } });
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
        UploadDirtySmdEntryGpu();
        RefreshSmdOverlayCamera();
        if (smdOverlayDirty) UploadSmdOverlay();
        if (texturesDirty) UploadTextures();
        if (enemyTexturesDirty) UploadEnemyTextures();
        if (etsTexturesDirty) UploadEtsTextures();
        if (itaTexturesDirty) UploadItaTextures();
        if (aevGpuDirty) UploadAev();
        if (enemyGpuDirty) UploadEnemies();
        else if(selectedEnemyIndex>=0&&!enemyGpuPreviewActive)UploadEnemyTransformGizmo();
        if (etsGpuDirty) UploadEts();
        else if(etsScene!=null&&selectedEtsFileOrder>=0)UploadEtsGizmoOnly();
        if (itaGpuDirty) UploadIta();
        else if(itaScene!=null&&selectedItaOrder>=0)UploadItaGizmoOnly();
        if (collisionGpuDirty) UploadCollision();
        else if(selectedCollision!=null)UploadCollisionHandles();
        if (litGpuDirty) UploadLit();
        if (effGpuDirty) UploadEff();
        if (rtpGpuDirty) UploadRtp();
        if (emiGpuDirty) UploadEmi();
        // CAM edit handles are screen-sized, so their world-space geometry must be
        // refreshed whenever the event-driven viewport paints after camera motion.
        if(camScene!=null&&selectedCamEntry>=0)camGpuDirty=true;
        if (camGpuDirty) UploadCam();
        if (soundGpuDirty) UploadSound();
        // Component-edit gizmos also retain a constant size while the camera moves.
        if(assetMeshSelection!=null)assetOverlayDirty=true;
        if (assetOverlayDirty) UploadAssetOverlay();

        Rectangle renderViewport=GetRenderViewport();
        GL.Viewport(renderViewport.X,ClientSize.Height-renderViewport.Bottom,renderViewport.Width,renderViewport.Height);
        GL.ClearColor(backgroundBrightness / 255f, Math.Min(255, backgroundBrightness + 2) / 255f, Math.Min(255, backgroundBrightness + 5) / 255f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Establish the original scenario renderer baseline every frame. Overlay/model
        // layers are not allowed to leak depth, blend, culling or polygon state into it.
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Disable(EnableCap.Blend);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

        bool hasEtsGeometry = etsVertexCount > 0 || selectedEtsVertexCount > 0 || etsModelVertexCount > 0 || selectedEtsModelVertexCount > 0;
        if ((scene != null && ScenarioVisible) || (aevScene != null && AevVisible && aevVertexCount > 0) || (eslScene != null && EnemiesVisible && enemyVertexCount > 0) || (etsScene != null && ObjectsVisible && hasEtsGeometry) || (itaScene != null && ItaVisible) || (SoundVisible && (eseScene!=null||fseScene!=null)) || (CollisionVisible && (satCollisionVertexCount > 0 || eatCollisionVertexCount > 0)) || (litScene != null && LightingVisible && litVertexCount > 0) || (effScene != null && EffectsVisible) || (rtpScene != null && RtpVisible) || (emiScene != null && EmiVisible) || (camScene != null && CamVisible))
        {
            Matrix4 mvp = BuildMvp();
            GL.UseProgram(shaderProgram);
            GL.UniformMatrix4(uMvp, true, ref mvp);
            Matrix4 identityModel=Matrix4.Identity;GL.UniformMatrix4(uModel,true,ref identityModel);GL.UniformMatrix4(uNormalMatrix,true,ref identityModel);GL.Uniform1(uNormalSign,1f);
            GL.Uniform1(uOpacity, 1.0f);
            GL.Uniform4(uTextureTint,1f,1f,1f,1f);
            ApplyLitShaderUniforms();

            if (scene != null && ScenarioVisible)
            {
                DrawGridGpu();
                if (smdEntryGpu.Count>0||meshVertexCount>0) DrawMeshGpu();
                DrawSmdOverlay();
            }
            if (aevScene != null && AevVisible && aevVertexCount > 0) DrawAevGpu();
            if (eslScene != null && EnemiesVisible && enemyVertexCount > 0) DrawEnemiesGpu();
            if (etsScene != null && ObjectsVisible && hasEtsGeometry) DrawEtsGpu();
            if (itaScene != null && ItaVisible) DrawItaGpu();
            DrawSoundGpu();
            DrawCollisionGpu();
            DrawLitGpu();
            DrawEffGpu();
            DrawRtpGpu();
            DrawEmiGpu();
            DrawCamGpu();
            DrawAssetOverlay();

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        if (ShowAevLabels && AevVisible && aevScene != null) DrawAevLabelsGpu();
        if (ShowEnemyLabels && EnemiesVisible && eslScene != null) DrawEnemyLabelsGpu();
        if (CamVisible && camScene != null) DrawCamFrameLabelsGpu();
        if (ShowEmiLabels && EmiVisible && emiScene != null) DrawEmiLabelsGpu();
        if (statusMessage != null) DrawStatusMessageGpu(statusMessage);
        DrawSmdFaceSelectionBox();
        SwapBuffers();
        fpsSampleFrames++;
        double fpsElapsed = (Stopwatch.GetTimestamp() - fpsSampleStart) / (double)Stopwatch.Frequency;
        if (fpsElapsed >= 0.5)
        {
            displayedFps = Math.Clamp((int)Math.Round(fpsSampleFrames / fpsElapsed), 0, 999);
            FpsUpdated?.Invoke(displayedFps);
            fpsSampleFrames = 0;
            fpsSampleStart = Stopwatch.GetTimestamp();
        }
    }

    private readonly record struct EnemyTextureKey(byte EnemyType, int TplEntryIndex, int TextureIndex);
    private readonly record struct EnemyModelDrawBatch(EnemyTextureKey Key, int FirstVertex, int VertexCount);

    private readonly struct ScenarioDrawBatch
    {
        public readonly int TextureIndex;
        public readonly int FirstVertex;
        public readonly int VertexCount;
        public readonly bool HasVertexAlpha;
        public ScenarioDrawBatch(int textureIndex, int firstVertex, int vertexCount, bool hasVertexAlpha = false)
        {
            TextureIndex = textureIndex;
            FirstVertex = firstVertex;
            VertexCount = vertexCount;
            HasVertexAlpha = hasVertexAlpha;
        }
    }

}

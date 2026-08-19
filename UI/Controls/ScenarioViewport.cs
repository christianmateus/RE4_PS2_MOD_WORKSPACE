using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public enum ScenarioRenderMode
{
    Solid,
    SolidWireframe,
    Wireframe
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

    public event Action<AevEntry?>? AevEntryClicked;
    public event Action<AevEntry>? AevEntryEdited;
    public event Action? DuplicateAevRequested;
    public event Action? DeleteAevRequested;

    public bool ScenarioVisible { get; set; } = true;
    public bool AevVisible { get; set; } = true;
    public AevScene? AevScene => aevScene;
    public ScenarioScene? Scene => scene;
    public int LoadedTextureCount => glTextures.Count;
    public int TexturedBatchCount => meshBatches.Count(x => x.TextureIndex >= 0 && glTextures.ContainsKey(x.TextureIndex));
    public int MeshBatchCount => meshBatches.Count;
    public float MovementSpeedMultiplier { get; set; } = 1f;
    public float LookSensitivity { get; set; } = 0.0032f;
    public bool ShowAevLabels { get; set; } = true;
    public ScenarioRenderMode RenderMode { get; set; } = ScenarioRenderMode.Solid;

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
        if (aevGpuDirty) UploadAev();

        GL.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
        GL.ClearColor(8f / 255f, 10f / 255f, 13f / 255f, 1f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if ((scene != null && ScenarioVisible && meshVertexCount > 0) || (aevScene != null && AevVisible && aevVertexCount > 0))
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

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        if (ShowAevLabels && AevVisible && aevScene != null)
            DrawAevLabelsGpu();

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
            bool clickAev = e.Button == MouseButtons.Left && !leftMouseMoved && !wasHandleDrag;

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

            dragButton = MouseButtons.None;
            Capture = false;

            if (clickAev && AevVisible && aevScene != null)
            {
                AevEntry? hit = PickAevEntry(e.Location);
                SelectAevEntry(hit);
                AevEntryClicked?.Invoke(hit);
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
            if (draggingAevHandle >= 0 && draggingAevEntry != null)
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
            UndoAevVertexEdit();
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

    private void UndoAevVertexEdit()
    {
        if (aevUndo.Count == 0) return;
        Action undo = aevUndo.Pop();
        undo();
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
                if (shaderProgram != 0) GL.DeleteProgram(shaderProgram);
            }
            catch { }
        }
        base.Dispose(disposing);
    }
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

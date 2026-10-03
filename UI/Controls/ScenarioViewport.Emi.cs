using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using OpenTK.Graphics.OpenGL4;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class ScenarioViewport
{
    private EmiScene? emiScene;
    private bool emiGpuDirty;
    private int emiRoutesVao, emiRoutesVbo, emiRoutesCount;
    private int emiRollRoutesVao, emiRollRoutesVbo, emiRollRoutesCount;
    private int emiPointsVao, emiPointsVbo, emiPointsCount;
    private int emiSelectedVao, emiSelectedVbo, emiSelectedCount;
    private int emiTriggerVao, emiTriggerVbo, emiTriggerCount;
    private int emiSelectedTriggerVao, emiSelectedTriggerVbo, emiSelectedTriggerCount;
    private int emiTakeawayVao, emiTakeawayVbo, emiTakeawayCount;
    private int emiTakeawayAreaVao, emiTakeawayAreaVbo, emiTakeawayAreaCount;
    private int emiSelectedTakeawayAreaVao, emiSelectedTakeawayAreaVbo, emiSelectedTakeawayAreaCount;
    private readonly List<(int First, int Count, float R, float G, float B)> emiTakeawayBatches = new();
    private readonly int[] emiGizmoVaos = new int[3], emiGizmoVbos = new int[3], emiGizmoCounts = new int[3];
    private int selectedEmiEntry = -1, emiDragAxis;
    private Point emiDragMouse;
    private NVector3 emiDragStart;

    public bool EmiVisible { get; set; } = true;
    public EmiScene? EmiScene => emiScene;
    public int SelectedEmiEntryIndex => selectedEmiEntry;
    public bool IsEmiDragging => emiDragAxis != 0;
    public event Action<EmiEntry?>? EmiEntrySelected;
    public event Action? EmiSceneEdited;

    public void SetEmiScene(EmiScene? value)
    {
        emiScene = value; selectedEmiEntry = -1; emiGpuDirty = true; Invalidate();
    }

    public void RefreshEmiGeometry(EmiEntry? entry = null)
    {
        emiGpuDirty = true; Invalidate();
    }

    private void UploadEmi()
    {
        emiGpuDirty = false; emiRoutesCount = emiRollRoutesCount = emiPointsCount = emiSelectedCount = emiTriggerCount = emiSelectedTriggerCount = emiTakeawayCount = emiTakeawayAreaCount = emiSelectedTakeawayAreaCount = 0;
        emiTakeawayBatches.Clear();
        if (emiScene == null || !glReady) return;
        EnsureEmiBuffer(ref emiRoutesVao, ref emiRoutesVbo); EnsureEmiBuffer(ref emiRollRoutesVao, ref emiRollRoutesVbo); EnsureEmiBuffer(ref emiPointsVao, ref emiPointsVbo); EnsureEmiBuffer(ref emiSelectedVao, ref emiSelectedVbo);
        EnsureEmiBuffer(ref emiTriggerVao, ref emiTriggerVbo); EnsureEmiBuffer(ref emiSelectedTriggerVao, ref emiSelectedTriggerVbo);
        EnsureEmiBuffer(ref emiTakeawayVao, ref emiTakeawayVbo); EnsureEmiBuffer(ref emiTakeawayAreaVao, ref emiTakeawayAreaVbo); EnsureEmiBuffer(ref emiSelectedTakeawayAreaVao, ref emiSelectedTakeawayAreaVbo);
        for (int i = 0; i < 3; i++) EnsureEmiBuffer(ref emiGizmoVaos[i], ref emiGizmoVbos[i]);
        float radius = Math.Clamp((scene?.Radius ?? 1000f) * .003f, .35f, 3f);
        var routes = new List<float>(); var rollRoutes = new List<float>(); var points = new List<float>(); var selected = new List<float>();
        var triggers = new List<float>(); var selectedTriggers = new List<float>();
        var takeaway = new List<float>(); var takeawayAreas = new List<float>(); var selectedTakeawayAreas = new List<float>();

        AddSequentialRoute(routes, emiScene.Entries.Where(x => x.Type == 2).ToArray(), close: true);
        // The game starts at the first type-06 entry and repeatedly scans forward for
        // the next one. It stops at the last point; this path is intentionally open.
        AddDirectedOpenRoute(rollRoutes, emiScene.Entries.Where(x => x.Type == 6).ToArray());
        foreach (IGrouping<byte, EmiEntry> group in emiScene.Entries.Where(x => x.Type == 19 && x.Work1 != 0).GroupBy(x => x.Work1))
            AddSequentialRoute(routes, group.OrderBy(x => x.Work2).ToArray(), close: false);
        foreach (IGrouping<byte, EmiEntry> group in emiScene.Entries.Where(x => x.Type == 5).GroupBy(x => x.Work1).OrderBy(x => x.Key))
        {
            int first = takeaway.Count / 6;
            EmiEntry? exit = group.FirstOrDefault(x => x.Work0 == 0);
            foreach (EmiEntry destination in group.Where(x => x.Work0 == 0)) AddEmiTakeawayExit(takeaway, destination.Position);
            if (exit != null)
                foreach (EmiEntry waypoint in group.Where(x => x.Work0 == 1)) AddDirectedEmiConnection(takeaway, waypoint.Position, exit.Position);
            int count = takeaway.Count / 6 - first;
            if (count > 0)
            {
                (float r, float g, float b) = EmiRouteColor(group.Key);
                emiTakeawayBatches.Add((first, count, r, g, b));
            }
        }

        foreach (EmiEntry entry in emiScene.Entries)
        {
            List<float> target = entry.Index == selectedEmiEntry ? selected : points;
            NVector3 p = entry.Position;
            List<float> triggerTarget = entry.Index == selectedEmiEntry ? selectedTriggers : triggers;
            if (entry.Type == 7)
            {
                float bottom = scene == null ? p.Y - 50f : MathF.Min(scene.BoundsMin.Y, p.Y) - 5f;
                float top = scene == null ? p.Y + 50f : MathF.Max(scene.BoundsMax.Y, p.Y) + 5f;
                if (top - bottom < 10f) { bottom = p.Y - 50f; top = p.Y + 50f; }
                AddEmiCylinder(triggerTarget, p, 30f, bottom, top);
            }
            if (entry.Type == 5 && entry.Work0 == 1)
                AddEmiCylinder(entry.Index == selectedEmiEntry ? selectedTakeawayAreas : takeawayAreas, p, 40f, p.Y - 10f, p.Y + 10f);
            // Type 08 is not an AEV box: the game performs a fixed 3D distance
            // check of 3000 game units, equivalent to 30 viewport units.
            if (entry.Type == 8) AddEmiSphere(triggerTarget, p, 30f);
            AddEmiSegment(target, p - NVector3.UnitX * radius, p + NVector3.UnitX * radius);
            AddEmiSegment(target, p - NVector3.UnitY * radius, p + NVector3.UnitY * radius);
            AddEmiSegment(target, p - NVector3.UnitZ * radius, p + NVector3.UnitZ * radius);
            float a = entry.DirectionDegrees * MathF.PI / 180f;
            NVector3 direction = new(MathF.Sin(a), 0, MathF.Cos(a));
            AddEmiSegment(target, p, p + direction * radius * 2.5f);
        }
        UploadLineBuffer(emiRoutesVao, emiRoutesVbo, routes, out emiRoutesCount);
        UploadLineBuffer(emiRollRoutesVao, emiRollRoutesVbo, rollRoutes, out emiRollRoutesCount);
        UploadLineBuffer(emiPointsVao, emiPointsVbo, points, out emiPointsCount);
        UploadLineBuffer(emiSelectedVao, emiSelectedVbo, selected, out emiSelectedCount);
        UploadLineBuffer(emiTriggerVao, emiTriggerVbo, triggers, out emiTriggerCount);
        UploadLineBuffer(emiSelectedTriggerVao, emiSelectedTriggerVbo, selectedTriggers, out emiSelectedTriggerCount);
        UploadLineBuffer(emiTakeawayVao, emiTakeawayVbo, takeaway, out emiTakeawayCount);
        UploadLineBuffer(emiTakeawayAreaVao, emiTakeawayAreaVbo, takeawayAreas, out emiTakeawayAreaCount);
        UploadLineBuffer(emiSelectedTakeawayAreaVao, emiSelectedTakeawayAreaVbo, selectedTakeawayAreas, out emiSelectedTakeawayAreaCount);

        var axes = new[] { new List<float>(), new List<float>(), new List<float>() };
        if (selectedEmiEntry >= 0 && selectedEmiEntry < emiScene.Entries.Count)
        {
            NVector3 p = emiScene.Entries[selectedEmiEntry].Position; float len = Math.Clamp(radius * 4f, 3f, 12f);
            AddEmiArrow(axes[0], p, NVector3.UnitX, len); AddEmiArrow(axes[1], p, NVector3.UnitY, len); AddEmiArrow(axes[2], p, NVector3.UnitZ, len);
        }
        for (int i = 0; i < 3; i++) UploadLineBuffer(emiGizmoVaos[i], emiGizmoVbos[i], axes[i], out emiGizmoCounts[i]);
    }

    private static void AddSequentialRoute(List<float> values, IReadOnlyList<EmiEntry> entries, bool close)
    {
        for (int i = 1; i < entries.Count; i++) AddEmiSegment(values, entries[i - 1].Position, entries[i].Position);
        if (close && entries.Count > 2) AddEmiSegment(values, entries[^1].Position, entries[0].Position);
    }

    private static void AddDirectedOpenRoute(List<float> values, IReadOnlyList<EmiEntry> entries)
    {
        for (int i = 1; i < entries.Count; i++)
        {
            NVector3 from=entries[i-1].Position,to=entries[i].Position,delta=to-from;
            AddEmiSegment(values,from,to);
            float distance=delta.Length();if(distance<.05f)continue;
            NVector3 direction=delta/distance,tip=NVector3.Lerp(from,to,.72f);
            NVector3 side=NVector3.Cross(direction,NVector3.UnitY);if(side.LengthSquared()<.001f)side=NVector3.UnitX;else side=NVector3.Normalize(side);
            float head=Math.Clamp(distance*.12f,.2f,2f);
            AddEmiSegment(values,tip,tip-direction*head+side*head*.55f);
            AddEmiSegment(values,tip,tip-direction*head-side*head*.55f);
        }
    }

    private static void AddEmiSphere(List<float> values, NVector3 center, float radius)
    {
        const int slices = 24, stacks = 12;
        for (int stack = 0; stack < stacks; stack++)
        {
            float latitude0 = -MathF.PI * .5f + MathF.PI * stack / stacks;
            float latitude1 = -MathF.PI * .5f + MathF.PI * (stack + 1) / stacks;
            for (int slice = 0; slice < slices; slice++)
            {
                float longitude0 = MathF.Tau * slice / slices;
                float longitude1 = MathF.Tau * (slice + 1) / slices;
                NVector3 n00 = SphereNormal(latitude0, longitude0), n01 = SphereNormal(latitude0, longitude1);
                NVector3 n10 = SphereNormal(latitude1, longitude0), n11 = SphereNormal(latitude1, longitude1);
                AddEmiSphereVertex(values, center + n00 * radius, n00); AddEmiSphereVertex(values, center + n10 * radius, n10); AddEmiSphereVertex(values, center + n11 * radius, n11);
                AddEmiSphereVertex(values, center + n00 * radius, n00); AddEmiSphereVertex(values, center + n11 * radius, n11); AddEmiSphereVertex(values, center + n01 * radius, n01);
            }
        }
    }

    private static void AddEmiCylinder(List<float> values, NVector3 center, float radius, float bottom, float top)
    {
        const int slices = 32;
        for (int slice = 0; slice < slices; slice++)
        {
            float angle0 = MathF.Tau * slice / slices, angle1 = MathF.Tau * (slice + 1) / slices;
            NVector3 normal0 = new(MathF.Sin(angle0), 0f, MathF.Cos(angle0));
            NVector3 normal1 = new(MathF.Sin(angle1), 0f, MathF.Cos(angle1));
            NVector3 p00 = new(center.X + normal0.X * radius, bottom, center.Z + normal0.Z * radius);
            NVector3 p01 = new(center.X + normal1.X * radius, bottom, center.Z + normal1.Z * radius);
            NVector3 p10 = new(p00.X, top, p00.Z), p11 = new(p01.X, top, p01.Z);
            AddEmiSphereVertex(values, p00, normal0); AddEmiSphereVertex(values, p10, normal0); AddEmiSphereVertex(values, p11, normal1);
            AddEmiSphereVertex(values, p00, normal0); AddEmiSphereVertex(values, p11, normal1); AddEmiSphereVertex(values, p01, normal1);
        }
    }

    private static void AddEmiTakeawayExit(List<float> values, NVector3 center)
    {
        AddEmiCircle(values, center, 5f); AddEmiCircle(values, center, 9f);
        AddEmiSegment(values, center - NVector3.UnitX * 11f, center + NVector3.UnitX * 11f);
        AddEmiSegment(values, center - NVector3.UnitZ * 11f, center + NVector3.UnitZ * 11f);
        AddEmiSegment(values, center, center + NVector3.UnitY * 14f);
    }

    private static void AddEmiCircle(List<float> values, NVector3 center, float radius)
    {
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float a = MathF.Tau * i / segments, b = MathF.Tau * (i + 1) / segments;
            AddEmiSegment(values, center + new NVector3(MathF.Sin(a) * radius, 0, MathF.Cos(a) * radius), center + new NVector3(MathF.Sin(b) * radius, 0, MathF.Cos(b) * radius));
        }
    }

    private static void AddDirectedEmiConnection(List<float> values, NVector3 from, NVector3 to)
    {
        NVector3 delta = to - from; float distance = delta.Length(); AddEmiSegment(values, from, to);
        if (distance < .05f) return;
        NVector3 direction = delta / distance, side = NVector3.Cross(direction, NVector3.UnitY);
        if (side.LengthSquared() < .001f) side = NVector3.UnitX; else side = NVector3.Normalize(side);
        float head = Math.Clamp(distance * .08f, .5f, 3f);
        AddEmiSegment(values, to, to - direction * head + side * head * .55f); AddEmiSegment(values, to, to - direction * head - side * head * .55f);
    }

    private static (float R, float G, float B) EmiRouteColor(byte id)
    {
        ReadOnlySpan<(float, float, float)> colors = [(0.20f,0.82f,1f),(0.78f,0.38f,1f),(0.20f,1f,0.55f),(1f,0.38f,0.62f),(1f,0.78f,0.15f),(0.48f,0.62f,1f)];
        return colors[id % colors.Length];
    }

    private static NVector3 SphereNormal(float latitude, float longitude)
    {
        float horizontal = MathF.Cos(latitude);
        return new NVector3(horizontal * MathF.Sin(longitude), MathF.Sin(latitude), horizontal * MathF.Cos(longitude));
    }

    private static void AddEmiSphereVertex(List<float> values, NVector3 position, NVector3 normal)
    { values.Add(position.X); values.Add(position.Y); values.Add(position.Z); values.Add(normal.X); values.Add(normal.Y); values.Add(normal.Z); }

    private static void EnsureEmiBuffer(ref int vao, ref int vbo) { if (vao == 0) { vao = GL.GenVertexArray(); vbo = GL.GenBuffer(); } }
    private static void AddEmiVertex(List<float> values, NVector3 p) { values.Add(p.X); values.Add(p.Y); values.Add(p.Z); values.Add(0); values.Add(1); values.Add(0); }
    private static void AddEmiSegment(List<float> values, NVector3 a, NVector3 b) { AddEmiVertex(values, a); AddEmiVertex(values, b); }
    private static void AddEmiArrow(List<float> values, NVector3 p, NVector3 axis, float len) { NVector3 end = p + axis * len, side = axis == NVector3.UnitY ? NVector3.UnitX : NVector3.UnitY; AddEmiSegment(values, p, end); AddEmiSegment(values, end, end - axis * len * .16f + side * len * .07f); AddEmiSegment(values, end, end - axis * len * .16f - side * len * .07f); }

    private void DrawEmiGpu()
    {
        if (!EmiVisible || emiScene == null) return;
        GL.Uniform1(uOpacity, 1f); GL.Uniform1(uUnlit, 1); GL.Uniform1(uUseTexture, 0); GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend); GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); GL.DepthMask(false);
        DrawEmiTrigger(emiTriggerVao, emiTriggerCount, .08f, .72f, 1f, .12f);
        DrawEmiTrigger(emiSelectedTriggerVao, emiSelectedTriggerCount, 1f, .45f, .05f, .24f);
        DrawEmiTrigger(emiTakeawayAreaVao, emiTakeawayAreaCount, .72f, .20f, 1f, .11f);
        DrawEmiTrigger(emiSelectedTakeawayAreaVao, emiSelectedTakeawayAreaCount, 1f, .45f, .05f, .24f);
        GL.DepthMask(true); GL.Disable(EnableCap.Blend); GL.Uniform1(uOpacity, 1f);
        DrawEmiLines(emiRoutesVao, emiRoutesCount, .75f, .22f, 1f, 2.5f);
        DrawEmiLines(emiRollRoutesVao, emiRollRoutesCount, 1f, .34f, .05f, 3.5f);
        GL.BindVertexArray(emiTakeawayVao); GL.LineWidth(3f);
        foreach (var batch in emiTakeawayBatches) { GL.Uniform3(uColor, batch.R, batch.G, batch.B); GL.DrawArrays(PrimitiveType.Lines, batch.First, batch.Count); }
        DrawEmiLines(emiPointsVao, emiPointsCount, 1f, .72f, .05f, 4f);
        DrawEmiLines(emiSelectedVao, emiSelectedCount, 1f, .18f, .08f, 6f);
        var colors = new[] { (1f, .15f, .12f), (.2f, .95f, .25f), (.12f, .48f, 1f) };
        for (int i = 0; i < 3; i++) DrawEmiLines(emiGizmoVaos[i], emiGizmoCounts[i], colors[i].Item1, colors[i].Item2, colors[i].Item3, 5f);
        GL.LineWidth(1); GL.Enable(EnableCap.CullFace);
    }

    private void DrawEmiLabelsGpu()
    {
        if (!EmiVisible || emiScene == null || labelShaderProgram == 0) return;
        GL.Disable(EnableCap.DepthTest); GL.Disable(EnableCap.CullFace); GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha); GL.UseProgram(labelShaderProgram);
        GL.ActiveTexture(TextureUnit.Texture0); GL.Uniform1(labelTextureUniform, 0); GL.BindVertexArray(labelVao);
        foreach (EmiEntry entry in emiScene.Entries)
        {
            if (!TryProjectWorldToScreen(entry.Position, out PointF screen)) continue;
            string role = entry.Type == 5 ? entry.Work0 == 0 ? $" • SAÍDA R{entry.Work1:D2}" : entry.Work0 == 1 ? $" • DESVIO R{entry.Work1:D2}" : $" • ROTA R{entry.Work1:D2}" : string.Empty;
            LabelTexture label = GetOrCreateLabelTexture($"EMI {entry.Index:D2} • T{entry.Type:D2}{role}", entry.Index == selectedEmiEntry);
            float left = screen.X - label.Width * .5f, top = screen.Y - label.Height - 9f;
            if (left + label.Width < 0 || top + label.Height < 0 || left > ClientSize.Width || top > ClientSize.Height) continue;
            float x0=left/ClientSize.Width*2f-1f,x1=(left+label.Width)/ClientSize.Width*2f-1f,y0=1f-top/ClientSize.Height*2f,y1=1f-(top+label.Height)/ClientSize.Height*2f;
            float[] quad={x0,y0,0,0,x0,y1,0,1,x1,y1,1,1,x0,y0,0,0,x1,y1,1,1,x1,y0,1,0};
            GL.BindTexture(TextureTarget.Texture2D,label.TextureId);GL.BindBuffer(BufferTarget.ArrayBuffer,labelVbo);GL.BufferData(BufferTarget.ArrayBuffer,quad.Length*sizeof(float),quad,BufferUsageHint.StreamDraw);GL.DrawArrays(PrimitiveType.Triangles,0,6);
        }
        GL.BindTexture(TextureTarget.Texture2D,0);GL.BindBuffer(BufferTarget.ArrayBuffer,0);GL.BindVertexArray(0);GL.UseProgram(0);GL.Disable(EnableCap.Blend);GL.Enable(EnableCap.DepthTest);GL.Enable(EnableCap.CullFace);
    }

    private void DrawEmiLines(int vao, int count, float r, float g, float b, float width)
    { if (count <= 0) return; GL.Uniform3(uColor, r, g, b); GL.BindVertexArray(vao); GL.LineWidth(width); GL.DrawArrays(PrimitiveType.Lines, 0, count); }

    private void DrawEmiTrigger(int vao, int count, float r, float g, float b, float opacity)
    { if (count <= 0) return; GL.Uniform3(uColor, r, g, b); GL.Uniform1(uOpacity, opacity); GL.BindVertexArray(vao); GL.DrawArrays(PrimitiveType.Triangles, 0, count); }

    private EmiEntry? PickEmiEntry(Point mouse)
    {
        EmiEntry? hit = null; float best = 18f; if (emiScene == null) return null;
        foreach (EmiEntry entry in emiScene.Entries) if (TryProjectWorldToScreen(entry.Position, out PointF p)) { float dx = p.X - mouse.X, dy = p.Y - mouse.Y, d = MathF.Sqrt(dx * dx + dy * dy); if (d < best) { best = d; hit = entry; } }
        return hit;
    }

    public bool HandleEmiClick(Point mouse)
    {
        if (!EmiVisible || emiScene == null) return false; EmiEntry? hit = PickEmiEntry(mouse); if (hit == null) return false; SelectEmiEntry(hit.Index); return true;
    }

    public void SelectEmiEntry(int index)
    {
        selectedEmiEntry = emiScene != null && index >= 0 && index < emiScene.Entries.Count ? index : -1;
        emiGpuDirty = true; EmiEntrySelected?.Invoke(selectedEmiEntry >= 0 ? emiScene!.Entries[selectedEmiEntry] : null); Invalidate();
    }

    private int PickEmiAxis(Point mouse, EmiEntry entry)
    {
        float len = Math.Clamp((scene?.Radius ?? 1000f) * .012f, 3f, 12f); if (!TryProjectWorldToScreen(entry.Position, out PointF p)) return 0;
        int axis = 0; float best = 11f; NVector3[] ends = { entry.Position + NVector3.UnitX * len, entry.Position + NVector3.UnitY * len, entry.Position + NVector3.UnitZ * len };
        for (int i = 0; i < 3; i++) if (TryProjectWorldToScreen(ends[i], out PointF end)) { float d = DistancePointToSegment(mouse, p, end); if (d < best) { best = d; axis = i + 1; } }
        return axis;
    }

    private bool TryBeginEmiDrag(Point mouse)
    {
        if (!EmiVisible || emiScene == null || selectedEmiEntry < 0 || selectedEmiEntry >= emiScene.Entries.Count) return false;
        int axis = PickEmiAxis(mouse, emiScene.Entries[selectedEmiEntry]); if (axis == 0) return false;
        emiDragAxis = axis; emiDragMouse = mouse; emiDragStart = emiScene.Entries[selectedEmiEntry].Position; return true;
    }

    private void UpdateEmiDrag(Point mouse)
    {
        if (emiScene == null || selectedEmiEntry < 0 || emiDragAxis == 0) return;
        NVector3 axis = emiDragAxis == 1 ? NVector3.UnitX : emiDragAxis == 2 ? NVector3.UnitY : NVector3.UnitZ;
        float len = Math.Clamp((scene?.Radius ?? 1000f) * .012f, 3f, 12f);
        if (TryProjectWorldToScreen(emiDragStart, out PointF a) && TryProjectWorldToScreen(emiDragStart + axis * len, out PointF b))
        {
            System.Numerics.Vector2 screenAxis = new(b.X - a.X, b.Y - a.Y); float pixels = screenAxis.Length();
            if (pixels > .1f) { screenAxis /= pixels; System.Numerics.Vector2 delta = new(mouse.X - emiDragMouse.X, mouse.Y - emiDragMouse.Y); emiScene.Entries[selectedEmiEntry].Position = emiDragStart + axis * (System.Numerics.Vector2.Dot(delta, screenAxis) * len / pixels); emiGpuDirty = true; Invalidate(); }
        }
    }

    private void EndEmiDrag() { if (emiScene == null || emiDragAxis == 0) return; emiDragAxis = 0; emiScene.IsModified = true; emiGpuDirty = true; EmiSceneEdited?.Invoke(); }

    public bool DuplicateSelectedEmiEntry()
    {
        if (emiScene == null || selectedEmiEntry < 0 || emiScene.Entries.Count >= 256) return false;
        EmiEntry clone = emiScene.Entries[selectedEmiEntry].Clone(emiScene.Entries.Count); clone.Position += GetForward() * 4f; emiScene.Entries.Add(clone); selectedEmiEntry = clone.Index; MarkEmiEdited(); EmiEntrySelected?.Invoke(clone); return true;
    }

    public bool AddEmiEntry()
    {
        if (emiScene == null || emiScene.Entries.Count >= 256) return false;
        byte[] raw = new byte[0x40]; EmiEntry entry = new(emiScene.Entries.Count, raw) { Type = 1, Position = cameraPosition + GetForward() * 6f };
        emiScene.Entries.Add(entry); selectedEmiEntry = entry.Index; MarkEmiEdited(); EmiEntrySelected?.Invoke(entry); return true;
    }

    public bool DeleteSelectedEmiEntry()
    {
        if (emiScene == null || selectedEmiEntry < 0) return false; emiScene.Entries.RemoveAt(selectedEmiEntry); for (int i = 0; i < emiScene.Entries.Count; i++) emiScene.Entries[i].Index = i; selectedEmiEntry = -1; MarkEmiEdited(); EmiEntrySelected?.Invoke(null); return true;
    }

    private void MarkEmiEdited() { if (emiScene == null) return; emiScene.IsModified = true; emiGpuDirty = true; EmiSceneEdited?.Invoke(); Invalidate(); }

    private void DisposeEmiGpu()
    {
        DeleteEmi(ref emiRoutesVao, ref emiRoutesVbo); DeleteEmi(ref emiRollRoutesVao, ref emiRollRoutesVbo); DeleteEmi(ref emiPointsVao, ref emiPointsVbo); DeleteEmi(ref emiSelectedVao, ref emiSelectedVbo);
        DeleteEmi(ref emiTriggerVao, ref emiTriggerVbo); DeleteEmi(ref emiSelectedTriggerVao, ref emiSelectedTriggerVbo);
        DeleteEmi(ref emiTakeawayVao, ref emiTakeawayVbo); DeleteEmi(ref emiTakeawayAreaVao, ref emiTakeawayAreaVbo); DeleteEmi(ref emiSelectedTakeawayAreaVao, ref emiSelectedTakeawayAreaVbo);
        for (int i = 0; i < 3; i++) DeleteEmi(ref emiGizmoVaos[i], ref emiGizmoVbos[i]);
    }
    private static void DeleteEmi(ref int vao, ref int vbo) { if (vbo != 0) GL.DeleteBuffer(vbo); if (vao != 0) GL.DeleteVertexArray(vao); vao = vbo = 0; }
}

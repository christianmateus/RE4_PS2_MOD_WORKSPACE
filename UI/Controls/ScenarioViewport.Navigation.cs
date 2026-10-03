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

public sealed partial class ScenarioViewport : GLControl
{
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

    public AevEntry? SelectedAevEntry => GetSelectedAevEntry();

    private int PickAevTransformHandle(Point screen, AevEntry entry)
    {
        GetAevYRange(entry, out float y0, out float y1);
        System.Numerics.Vector2 center2 = entry.IsCircle ? entry.Position1 : GetAevCenterXZ(entry);

        float extent = entry.IsCircle
            ? Math.Max(entry.VisualRadius, 0.25f)
            : Math.Max(System.Numerics.Vector2.Distance(entry.Position1, entry.Position3), 0.25f);
        float size = Math.Max(0.12f, Math.Max((scene?.Radius ?? 1f) * 0.0023f, extent * 0.05f));

        NVector3 origin = new(center2.X, (y0 + y1) * 0.5f, center2.Y);
        float length = size * 4.2f;
        if (AevTransformMode == AevGizmoMode.Rotate)
        {
            if (!entry.IsSquare) return -1;
            float rotateDistance = float.PositiveInfinity;
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float a0 = MathF.Tau * i / segments, a1 = MathF.Tau * (i + 1) / segments;
                rotateDistance = Math.Min(rotateDistance, ScreenDistanceToWorldSegment(screen, origin + new NVector3(MathF.Cos(a0) * length, 0, MathF.Sin(a0) * length), origin + new NVector3(MathF.Cos(a1) * length, 0, MathF.Sin(a1) * length)));
            }
            return rotateDistance <= 11f ? 9 : -1;
        }

        var directions=GetAevTransformAxes(entry);
        NVector3 sideEnd = origin + directions[0]*length;
        NVector3 upEnd = origin + new NVector3(0f, size * 4.2f, 0f);
        NVector3 depthEnd = origin + directions[2]*length;

        float sideDistance = ScreenDistanceToWorldSegment(screen, origin, sideEnd);
        float upDistance = ScreenDistanceToWorldSegment(screen, origin, upEnd);
        float depthDistance = ScreenDistanceToWorldSegment(screen, origin, depthEnd);

        const float threshold = 11f;
        float best = Math.Min(sideDistance, Math.Min(upDistance, depthDistance));
        if (best > threshold) return -1;
        if (sideDistance == best) return 6;
        if (upDistance == best) return 7;
        if (depthDistance == best) return 8;
        return -1;
    }

    private PointF ProjectAevCenter(AevEntry entry)
    {
        GetAevYRange(entry, out float y0, out float y1);
        System.Numerics.Vector2 c = entry.IsCircle ? entry.Position1 : GetAevCenterXZ(entry);
        return TryProjectWorldToScreen(new NVector3(c.X, (y0 + y1) * .5f, c.Y), out PointF screen) ? screen : PointF.Empty;
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

        entry.Y = bottom;
        entry.Height = height;
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

        Rectangle renderViewport=GetRenderViewport();
        float aspect = Math.Max(0.01f, renderViewport.Width / (float)Math.Max(1, renderViewport.Height));
        float radius = scene?.Radius ?? 1000f;
        float distanceToScene = scene == null ? 1000f : NVector3.Distance(cameraPosition, scene.Center);
        float near = Math.Max(0.001f, radius * 0.00005f);
        float far = Math.Max(near + 100f, distanceToScene + radius * 30f);
        float verticalFov=FieldOfViewIsHorizontal?2f*MathF.Atan(MathF.Tan(MathHelper.DegreesToRadians(fieldOfViewDegrees)*.5f)/aspect):MathHelper.DegreesToRadians(fieldOfViewDegrees);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(verticalFov, aspect, near, far);

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

    private static void RotateAev(AevEntry entry, float radians)
    {
        if (!entry.IsSquare) return;
        System.Numerics.Vector2 center = GetAevCenterXZ(entry);
        float c = MathF.Cos(radians), s = MathF.Sin(radians);
        System.Numerics.Vector2 Rotate(System.Numerics.Vector2 p)
        {
            p -= center;
            return center + new System.Numerics.Vector2(p.X * c - p.Y * s, p.X * s + p.Y * c);
        }
        entry.Position1 = Rotate(entry.Position1); entry.Position2 = Rotate(entry.Position2);
        entry.Position3 = Rotate(entry.Position3); entry.Position4 = Rotate(entry.Position4);
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
            fpsRenderTimer.Stop();
            fpsRenderTimer.Dispose();
        }
        if (disposing && glReady && !IsDesignMode)
        {
            try
            {
                MakeCurrent();
                ReleaseSmdEntryGpu();
                if (meshVbo != 0) GL.DeleteBuffer(meshVbo);
                ReleaseSmdEntryGpu();
                if (meshVao != 0) GL.DeleteVertexArray(meshVao);
                if (gridVbo != 0) GL.DeleteBuffer(gridVbo);
                if (gridVao != 0) GL.DeleteVertexArray(gridVao);
                if (aevVbo != 0) GL.DeleteBuffer(aevVbo);
                if (aevVao != 0) GL.DeleteVertexArray(aevVao);
                if (aevSelectedVbo != 0) GL.DeleteBuffer(aevSelectedVbo);
                if (aevSelectedVao != 0) GL.DeleteVertexArray(aevSelectedVao);
                if (aevHandleVbo != 0) GL.DeleteBuffer(aevHandleVbo);
                if (aevHandleVao != 0) GL.DeleteVertexArray(aevHandleVao);
                for(int i=0;i<3;i++){if(aevGizmoVbos[i]!=0)GL.DeleteBuffer(aevGizmoVbos[i]);if(aevGizmoVaos[i]!=0)GL.DeleteVertexArray(aevGizmoVaos[i]);}
                if (enemyVbo != 0) GL.DeleteBuffer(enemyVbo);
                if (enemyVao != 0) GL.DeleteVertexArray(enemyVao);
                if (selectedEnemyVbo != 0) GL.DeleteBuffer(selectedEnemyVbo);
                if (selectedEnemyVao != 0) GL.DeleteVertexArray(selectedEnemyVao);
                if (enemyModelVbo != 0) GL.DeleteBuffer(enemyModelVbo);
                if (enemyModelVao != 0) GL.DeleteVertexArray(enemyModelVao);
                if (selectedEnemyModelVbo != 0) GL.DeleteBuffer(selectedEnemyModelVbo);
                if (selectedEnemyModelVao != 0) GL.DeleteVertexArray(selectedEnemyModelVao);
                for(int i=0;i<3;i++){if(enemyGizmoVbos[i]!=0)GL.DeleteBuffer(enemyGizmoVbos[i]);if(enemyGizmoVaos[i]!=0)GL.DeleteVertexArray(enemyGizmoVaos[i]);}
                if (etsVbo != 0) GL.DeleteBuffer(etsVbo);
                if (etsVao != 0) GL.DeleteVertexArray(etsVao);
                if (selectedEtsVbo != 0) GL.DeleteBuffer(selectedEtsVbo);
                if (selectedEtsVao != 0) GL.DeleteVertexArray(selectedEtsVao);
                if (etsModelVbo != 0) GL.DeleteBuffer(etsModelVbo);
                if (etsModelVao != 0) GL.DeleteVertexArray(etsModelVao);
                if (selectedEtsModelVbo != 0) GL.DeleteBuffer(selectedEtsModelVbo);
                if (selectedEtsModelVao != 0) GL.DeleteVertexArray(selectedEtsModelVao);
                if(assetOverlayVbo!=0)GL.DeleteBuffer(assetOverlayVbo);if(assetOverlayVao!=0)GL.DeleteVertexArray(assetOverlayVao);
                if(assetGizmoVbo!=0)GL.DeleteBuffer(assetGizmoVbo);if(assetGizmoVao!=0)GL.DeleteVertexArray(assetGizmoVao);
                for(int i=0;i<3;i++){if(etsGizmoVbos[i]!=0)GL.DeleteBuffer(etsGizmoVbos[i]);if(etsGizmoVaos[i]!=0)GL.DeleteVertexArray(etsGizmoVaos[i]);}
                ReleaseEtsTextures();
                ReleaseEnemyTextures();
                ReleaseTextures();
                DisposeCollisionGpu();
                DisposeLitGpu();
                DisposeEffGpu();
                DisposeRtpGpu();
                DisposeEmiGpu();
                DisposeCamGpu();
                DisposeSoundGpu();
                if (shaderProgram != 0) GL.DeleteProgram(shaderProgram);
            }
            catch { }
        }
        base.Dispose(disposing);
    }
}

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
    private void UploadAev()
    {
        aevGpuDirty = false;
        aevVertexCount = 0;
        aevSelectedVertexCount = 0;
        aevFaceVertexCount = 0;
        aevSelectedFaceVertexCount = 0;
        aevHandleVertexCount = 0;
        Array.Clear(aevGizmoVertexCounts);
        if (aevScene == null || !glReady) return;

        var allLines = new List<float>(aevScene.Count * 96);
        var selectedLines = new List<float>(96);
        var allFaces = new List<float>(aevScene.Count * 216);
        var selectedFaces = new List<float>(216);
        var handles = new List<float>(192);
        var gizmoAxes = new[] { new List<float>(64), new List<float>(64), new List<float>(64) };

        foreach (AevEntry entry in aevScene.Entries)
        {
            if (aevTypeFilter.HasValue && entry.Type != aevTypeFilter.Value) continue;

            AddAevVolumeLines(allLines, entry);
            AddAevVolumeFaces(allFaces, entry);

            if (entry.FileOrder == selectedAevFileOrder)
            {
                AddAevVolumeLines(selectedLines, entry);
                AddAevVolumeFaces(selectedFaces, entry);
                if (AevEditMode && entry.IsSquare)
                {
                    AddAevCornerHandles(handles, entry, scene?.Radius ?? 1f);
                    AddAevHeightHandles(handles, entry, scene?.Radius ?? 1f);
                }
                if (!AevEditMode && (entry.IsSquare || entry.IsCircle) && (AevTransformMode == AevGizmoMode.Move || entry.IsSquare))
                    AddAevTransformGizmo(gizmoAxes, entry, scene?.Radius ?? 1f, AevTransformMode);
            }
        }

        UploadLineBuffer(aevVao, aevVbo, allLines, out aevVertexCount);
        UploadLineBuffer(aevSelectedVao, aevSelectedVbo, selectedLines, out aevSelectedVertexCount);
        UploadLineBuffer(aevFaceVao, aevFaceVbo, allFaces, out aevFaceVertexCount);
        UploadLineBuffer(aevSelectedFaceVao, aevSelectedFaceVbo, selectedFaces, out aevSelectedFaceVertexCount);
        UploadLineBuffer(aevHandleVao, aevHandleVbo, handles, out aevHandleVertexCount);
        for (int i = 0; i < 3; i++) UploadLineBuffer(aevGizmoVaos[i], aevGizmoVbos[i], gizmoAxes[i], out aevGizmoVertexCounts[i]);
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
        // The game treats this value as AreaXZ4::floor and the upper bound as
        // floor + height (areaHitCheck_xz4 / area_xz4_Disp). SMD and AEV therefore
        // share the same vertical sign in the decoded PS2 coordinate space.
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
        y0 = entry.Y;
        y1 = entry.Y + entry.Height;
        if (y1 < y0) (y0, y1) = (y1, y0);
    }

    private NVector3[] GetAevTransformAxes(AevEntry entry)
    {
        if(AevTransformSpace==AevTransformSpace.Local&&entry.IsSquare)
        {
            var edge=entry.Position2-entry.Position1;
            if(edge.LengthSquared()>1e-8f)
            {
                var x=NVector3.Normalize(new NVector3(edge.X,0,edge.Y));
                return new[]{x,NVector3.UnitY,new NVector3(-x.Z,0,x.X)};
            }
        }
        return new[]{NVector3.UnitX,NVector3.UnitY,NVector3.UnitZ};
    }

    private void AddAevTransformGizmo(List<float>[] axes, AevEntry entry, float sceneRadius, AevGizmoMode mode)
    {
        GetAevYRange(entry, out float y0, out float y1);
        System.Numerics.Vector2 center2 = entry.IsCircle ? entry.Position1 : GetAevCenterXZ(entry);

        float extent = entry.IsCircle
            ? Math.Max(entry.VisualRadius, 0.25f)
            : Math.Max(System.Numerics.Vector2.Distance(entry.Position1, entry.Position3), 0.25f);

        float size = Math.Max(0.12f, Math.Max(sceneRadius * 0.0023f, extent * 0.05f));
        NVector3 origin = new(center2.X, (y0 + y1) * 0.5f, center2.Y);

        float length = size * 4.2f;
        if (mode == AevGizmoMode.Move)
        {
            var directions=GetAevTransformAxes(entry);
            AddAevAxisArrow(axes[0], origin, directions[0], length, size);
            AddAevAxisArrow(axes[1], origin, NVector3.UnitY, length, size);
            AddAevAxisArrow(axes[2], origin, directions[2], length, size);
        }
        else
        {
            const int segments = 48;
            for (int i = 0; i < segments; i++)
            {
                float a0 = MathF.Tau * i / segments, a1 = MathF.Tau * (i + 1) / segments;
                AddAevLine(axes[1], origin + new NVector3(MathF.Cos(a0) * length, 0, MathF.Sin(a0) * length), origin + new NVector3(MathF.Cos(a1) * length, 0, MathF.Sin(a1) * length));
            }
        }
    }

    private static void AddAevAxisArrow(List<float> values, NVector3 origin, NVector3 axis, float length, float size)
    {
        NVector3 end = origin + axis * length;
        AddAevLine(values, origin, end);
        NVector3 side = axis == NVector3.UnitY ? NVector3.UnitX : NVector3.UnitY;
        AddAevLine(values, end, end - axis * size + side * size * .55f);
        AddAevLine(values, end, end - axis * size - side * size * .55f);
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
        var gizmoAxes=new[]{new List<float>(),new List<float>(),new List<float>()};
        var modelBuckets = new Dictionary<EnemyTextureKey, List<float>>();
        var selectedModelBuckets = new Dictionary<EnemyTextureKey, List<float>>();
        enemyModelBatches.Clear();
        selectedEnemyModelBatches.Clear();

        if(eslScene!=null) foreach(var e in eslScene.Entries.Where(EnemyIsVisible))
        {
            bool selected=e.Index==selectedEnemyIndex;
            AddEnemyMarker(selected?sel:all,e,selected);
            if(selected)AddEnemyTransformGizmo(gizmoAxes,e);
            if (enemyModels.TryGetValue(e.EnemyType, out EnemyModelScene? model))
            {
                AddEnemyModel(selected ? selectedModelBuckets : modelBuckets, e, model);
                if (highlightedEnemyModelType == e.EnemyType && highlightedEnemyModelPart >= 0)
                    AddHighlightedEnemyModelPart(selectedModelBuckets, e, model, highlightedEnemyModelPart);
                if (showEnemySkeletonDiagnostic && model.Skeleton != null)
                {
                    FcvSkeletonPose? skeletonPose=(enemyAttachmentAnimationForAll||e.Index==selectedEnemyIndex)&&enemyAttachmentAnimation!=null
                        ? FcvSkeletonEvaluator.Evaluate(model.Skeleton,enemyAttachmentAnimation,enemyAttachmentFrame,false,enemyAnimationIgnoreRootMotion)
                        : null;
                    AddEnemySkeletonLines(sel,e,model.Skeleton,skeletonPose);
                }
                if ((EnemyModelFacePickingEnabled && selectedEnemyModelFaceFlags.Count > 0) ||
                    (EnemyModelPartPickingEnabled && highlightedEnemyModelType == e.EnemyType && highlightedEnemyModelPart >= 0))
                    AddEnemyMeshGizmoLines(gizmoAxes, e);
            }
        }

        UploadLineBuffer(enemyVao,enemyVbo,all,out enemyVertexCount);
        UploadLineBuffer(selectedEnemyVao,selectedEnemyVbo,sel,out selectedEnemyVertexCount);
        for(int i=0;i<3;i++)UploadLineBuffer(enemyGizmoVaos[i],enemyGizmoVbos[i],gizmoAxes[i],out enemyGizmoCounts[i]);

        List<float> modelAll = BuildEnemyModelBatches(modelBuckets, enemyModelBatches);
        List<float> modelSelected = BuildEnemyModelBatches(selectedModelBuckets, selectedEnemyModelBatches);
        UploadEnemyModelBuffer(enemyModelVao, enemyModelVbo, modelAll, out enemyModelVertexCount);
        UploadEnemyModelBuffer(selectedEnemyModelVao, selectedEnemyModelVbo, modelSelected, out selectedEnemyModelVertexCount);
    }

    private void UploadEnemyTransformGizmo()
    {
        var axes=new[]{new List<float>(),new List<float>(),new List<float>()};
        EslEnemyEntry? selected=GetSelectedEnemyEntry();
        if(selected!=null&&EnemyIsVisible(selected))AddEnemyTransformGizmo(axes,selected);
        for(int i=0;i<3;i++)UploadLineBuffer(enemyGizmoVaos[i],enemyGizmoVbos[i],axes[i],out enemyGizmoCounts[i]);
    }

    private void AddHighlightedEnemyModelPart(Dictionary<EnemyTextureKey, List<float>> buckets, EslEnemyEntry entry, EnemyModelScene model, int binIndex)
    {
        EnemyModelPart? part = model.Parts.FirstOrDefault(x => x.BinIndex == binIndex);
        if (part == null || !IsEnemyModelPartVisible(entry.EnemyType, part.BinIndex)) return;
        NVector3 origin = EslToWorld(entry);
        float rx = entry.RotX * (MathF.PI / 32768f), ry = entry.RotY * (MathF.PI / 32768f), rz = entry.RotZ * (MathF.PI / 32768f);
        var smoothNormals = BuildEnemySmoothNormals(part.Triangles);
        NVector3 rotationPivot = (part.BoundsMin + part.BoundsMax) * 0.5f;
        NQuaternion previewRotation = NQuaternion.CreateFromYawPitchRoll(
            enemyFaceGizmoRotationDegrees.Y * MathF.PI / 180f,
            enemyFaceGizmoRotationDegrees.X * MathF.PI / 180f,
            enemyFaceGizmoRotationDegrees.Z * MathF.PI / 180f);
        foreach (EnemyModelTriangle sourceTriangle in part.Triangles)
        {
            if (EnemyModelFacePickingEnabled && selectedEnemyModelFaceFlags.Count > 0 && (part.BinIndex != selectedEnemyModelFaceBin || !selectedEnemyModelFaceFlags.Contains(sourceTriangle.StripFlagOffset))) continue;
            if (enemyDiagnosticBoneId.HasValue && !TriangleUsesBone(sourceTriangle, enemyDiagnosticBoneId.Value)) continue;
            EnemyModelTriangle triangle = ApplyEnemyTextureAssignment(entry.EnemyType, part.BinIndex, sourceTriangle);
            triangle = ApplyEnemyGameplayModelTransform(entry,triangle);
            bool movingSelectedFace = EnemyModelFacePickingEnabled && part.BinIndex == selectedEnemyModelFaceBin && selectedEnemyModelFaceFlags.Contains(sourceTriangle.StripFlagOffset);
            bool movingSelectedPart = !EnemyModelFacePickingEnabled && highlightedEnemyModelType == entry.EnemyType && part.BinIndex == highlightedEnemyModelPart;
            if (movingSelectedPart && EnemyModelGizmoMode == EnemyMeshGizmoMode.Rotate && enemyFaceGizmoRotationDegrees.LengthSquared() > 0.000001f)
                triangle = triangle with
                {
                    A = NVector3.Transform(triangle.A - rotationPivot, previewRotation) + rotationPivot,
                    B = NVector3.Transform(triangle.B - rotationPivot, previewRotation) + rotationPivot,
                    C = NVector3.Transform(triangle.C - rotationPivot, previewRotation) + rotationPivot
                };
            if (enemyFaceGizmoAxis != 0 && (movingSelectedFace || movingSelectedPart))
                triangle = triangle with { A = triangle.A + enemyFaceGizmoDelta, B = triangle.B + enemyFaceGizmoDelta, C = triangle.C + enemyFaceGizmoDelta };
            if (enemyDiagnosticBoneId.HasValue)
            {
                float weight = TriangleBoneWeight(sourceTriangle, enemyDiagnosticBoneId.Value);
                int band = weight < 0.34f ? -101 : weight < 0.67f ? -102 : -103;
                triangle = triangle with { TplEntryIndex = -1, TextureIndex = band };
            }
            AddEnemyTriangleToBucket(buckets, entry, triangle, origin, rx, ry, rz, model, false, smoothNormals: smoothNormals);
        }
    }

    private static bool TriangleUsesBone(EnemyModelTriangle triangle, byte boneId)
    {
        static bool Uses(EnemyVertexSkin skin, byte id) =>
            skin.Count > 0 && skin.A.BoneId == id && skin.A.Weight > 0 ||
            skin.Count > 1 && skin.B.BoneId == id && skin.B.Weight > 0 ||
            skin.Count > 2 && skin.C.BoneId == id && skin.C.Weight > 0;
        return Uses(triangle.SkinA, boneId) || Uses(triangle.SkinB, boneId) || Uses(triangle.SkinC, boneId);
    }

    private static float TriangleBoneWeight(EnemyModelTriangle triangle, byte boneId)
    {
        static float Weight(EnemyVertexSkin skin, byte id)
        {
            float result = 0;
            if (skin.Count > 0 && skin.A.BoneId == id) result = Math.Max(result, skin.A.Weight);
            if (skin.Count > 1 && skin.B.BoneId == id) result = Math.Max(result, skin.B.Weight);
            if (skin.Count > 2 && skin.C.BoneId == id) result = Math.Max(result, skin.C.Weight);
            return result;
        }
        return Math.Max(Weight(triangle.SkinA, boneId), Math.Max(Weight(triangle.SkinB, boneId), Weight(triangle.SkinC, boneId)));
    }

    private static void AddEnemySkeletonLines(List<float> values,EslEnemyEntry entry,Ps2BinSkeleton skeleton,FcvSkeletonPose? pose=null)
    {
        var globals = new NVector3[skeleton.Bones.Count];
        NVector3 rootMotion=pose?.RootMotionToRemove/100f??NVector3.Zero;
        for (int i = 0; i < skeleton.Bones.Count; i++)
        {
            Ps2BinBone bone = skeleton.Bones[i];
            globals[i]=pose!=null&&i<pose.WorldPositions.Length?pose.WorldPositions[i]/100f-rootMotion:bone.LocalPosition/100f+(bone.ParentIndex>=0&&bone.ParentIndex<i?globals[bone.ParentIndex]:NVector3.Zero);
        }
        NVector3 origin = EslToWorld(entry);
        void Point(NVector3 p) { p += origin; values.Add(p.X); values.Add(p.Y); values.Add(p.Z); values.Add(0); values.Add(0); values.Add(0); }
        float jointSize=Math.Max(0.025f,globals.Length==0?0.05f:(globals.Max(x=>x.Y)-globals.Min(x=>x.Y))*0.008f);
        for (int i = 0; i < skeleton.Bones.Count; i++)
        {
            int parent = skeleton.Bones[i].ParentIndex;
            if(parent>=0&&parent<globals.Length){Point(globals[parent]);Point(globals[i]);}
            NVector3 p=globals[i];Point(p-new NVector3(jointSize,0,0));Point(p+new NVector3(jointSize,0,0));Point(p-new NVector3(0,jointSize,0));Point(p+new NVector3(0,jointSize,0));Point(p-new NVector3(0,0,jointSize));Point(p+new NVector3(0,0,jointSize));
        }
    }

    private bool TryGetEnemyMeshGizmo(out NVector3 center, out float length)
    {
        center = NVector3.Zero; length = 1f;
        if (!EnemyModelGizmoEnabled) return false;
        byte? modelType = EnemyModelFacePickingEnabled ? enemyModels.Keys.FirstOrDefault() : highlightedEnemyModelType;
        if (!modelType.HasValue || !enemyModels.TryGetValue(modelType.Value, out EnemyModelScene? model)) return false;
        int binIndex = EnemyModelFacePickingEnabled ? selectedEnemyModelFaceBin : highlightedEnemyModelPart;
        if (EnemyModelFacePickingEnabled && selectedEnemyModelFaceFlags.Count == 0) return false;
        EnemyModelPart? part = model.Parts.FirstOrDefault(x => x.BinIndex == binIndex);
        if (part == null || !IsEnemyModelPartVisible(modelType.Value, binIndex)) return false;
        EnemyModelTriangle[] faces = EnemyModelFacePickingEnabled
            ? part.Triangles.Where(x => selectedEnemyModelFaceFlags.Contains(x.StripFlagOffset)).ToArray()
            : part.Triangles.ToArray();
        if (faces.Length == 0) return false;
        if (EnemyModelFacePickingEnabled)
        {
            foreach (EnemyModelTriangle face in faces) center += (face.A + face.B + face.C) / 3f;
            center /= faces.Length;
        }
        else center = (part.BoundsMin + part.BoundsMax) * 0.5f;
        center += enemyFaceGizmoDelta;
        length = Math.Max(0.35f, Math.Max(model.Size.X, Math.Max(model.Size.Y, model.Size.Z)) * 0.09f);
        return true;
    }

    private void AddEnemyMeshGizmoLines(List<float>[] axes, EslEnemyEntry entry)
    {
        if (!TryGetEnemyMeshGizmo(out NVector3 center, out float length)) return;
        center += EslToWorld(entry);
        void P(List<float> values, NVector3 p) { values.Add(p.X); values.Add(p.Y); values.Add(p.Z); values.Add(0); values.Add(0); values.Add(0); }
        void L(List<float> values, NVector3 a, NVector3 b) { P(values, a); P(values, b); }
        NVector3[] directions = { NVector3.UnitX, NVector3.UnitY, NVector3.UnitZ };
        for (int i = 0; i < directions.Length; i++)
        {
            NVector3 axis = directions[i];
            if (EnemyModelGizmoMode == EnemyMeshGizmoMode.Rotate && !EnemyModelFacePickingEnabled)
            {
                NVector3 u = directions[(i + 1) % 3], v = directions[(i + 2) % 3];
                const int segments = 64;
                for (int segment = 0; segment < segments; segment++)
                {
                    float a = MathF.Tau * segment / segments, b = MathF.Tau * (segment + 1) / segments;
                    L(axes[i], center + (u * MathF.Cos(a) + v * MathF.Sin(a)) * length,
                               center + (u * MathF.Cos(b) + v * MathF.Sin(b)) * length);
                }
                continue;
            }
            NVector3 tip = center + axis * length;
            NVector3 side = axis == NVector3.UnitY ? NVector3.UnitX : NVector3.UnitY;
            L(axes[i], center, tip);
            L(axes[i], tip, tip - axis * length * 0.22f + side * length * 0.10f);
            L(axes[i], tip, tip - axis * length * 0.22f - side * length * 0.10f);
        }
    }

    private bool TryBeginEnemyFaceGizmoDrag(Point mouse)
    {
        if ((!EnemyModelFacePickingEnabled && !EnemyModelPartPickingEnabled) || !TryGetEnemyMeshGizmo(out NVector3 center, out float length)) return false;
        if (eslScene?.Entries.FirstOrDefault(EnemyIsVisible) is EslEnemyEntry entry) center += EslToWorld(entry);
        if (!TryProjectWorldToScreen(center, out PointF start)) return false;
        float best = 13f; int axisIndex = 0;
        NVector3[] axes = { NVector3.UnitX, NVector3.UnitY, NVector3.UnitZ };
        for (int i = 0; i < axes.Length; i++)
        {
            if (EnemyModelGizmoMode == EnemyMeshGizmoMode.Rotate && !EnemyModelFacePickingEnabled)
            {
                NVector3 u = axes[(i + 1) % 3], v = axes[(i + 2) % 3];
                for (int segment = 0; segment < 64; segment++)
                {
                    float a = MathF.Tau * segment / 64f, b = MathF.Tau * (segment + 1) / 64f;
                    if (!TryProjectWorldToScreen(center + (u * MathF.Cos(a) + v * MathF.Sin(a)) * length, out PointF first) ||
                        !TryProjectWorldToScreen(center + (u * MathF.Cos(b) + v * MathF.Sin(b)) * length, out PointF second)) continue;
                    float ringDistance = DistanceToScreenSegment(mouse, first, second);
                    if (ringDistance < best) { best = ringDistance; axisIndex = i + 1; }
                }
            }
            else if (TryProjectWorldToScreen(center + axes[i] * length, out PointF end))
            {
                float distance = DistanceToScreenSegment(mouse, start, end);
                if (distance < best) { best = distance; axisIndex = i + 1; }
            }
        }
        if (axisIndex == 0) return false;
        enemyFaceGizmoAxis = axisIndex; enemyFaceGizmoStartMouse = mouse;
        enemyFaceGizmoDelta = NVector3.Zero; enemyFaceGizmoRotationDegrees = NVector3.Zero;
        return true;
    }

    private static float DistanceToScreenSegment(Point point, PointF a, PointF b)
    {
        System.Numerics.Vector2 ab = new(b.X - a.X, b.Y - a.Y), ap = new(point.X - a.X, point.Y - a.Y);
        float denominator = ab.LengthSquared(); float t = denominator < 0.001f ? 0 : Math.Clamp(System.Numerics.Vector2.Dot(ap, ab) / denominator, 0, 1);
        return System.Numerics.Vector2.Distance(ap, ab * t);
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
        EnemyModelPart[] parts = model.Parts.Count > 0 ? model.Parts.Where(x => IsEnemyModelPartAutomaticallyVisible(entry, x)).ToArray() : Array.Empty<EnemyModelPart>();

        if (model.Parts.Count == 0)
        {
            foreach (EnemyModelTriangle tri in model.Triangles) AddEnemyTriangleToBucket(buckets, entry, tri, origin, rx, ry, rz, model, false);
            return;
        }

        // Visual Editor autonomous idle: every enemy can use the first FCV embedded in its DAT.
        // The optional debug override remains specific to em12, whose external clip browser is
        // intentionally specialized; other types always retain their own compatible skeleton/FCV.
        FcvAnimation? bodyAnimation = null;
        float bodyFrame = 0f;
        int? equipmentIdleEntry=EnemyEquipmentCatalog.GetEquipmentIdleAnimationEntry(entry);
        FcvAnimation? equipmentIdle=equipmentIdleEntry.HasValue && model.EquipmentIdleAnimations.TryGetValue(equipmentIdleEntry.Value,out FcvAnimation? knownIdle)
            ? knownIdle : null;
        FcvAnimation? automaticIdle = equipmentIdle ?? (entry.EnemyType == 0x12 && enemyIdleAnimationOverride != null
            ? enemyIdleAnimationOverride : model.IdleAnimation);
        if (enemyIdleAnimationEnabled && automaticIdle is FcvAnimation selectedAnimation)
        {
            bodyAnimation = selectedAnimation;
            bodyFrame = enemyIdleAnimationFrame;
        }
        else if ((enemyAttachmentAnimationForAll || entry.Index == selectedEnemyIndex) && enemyAttachmentAnimation != null)
        {
            bodyAnimation = enemyAttachmentAnimation;
            bodyFrame = enemyAttachmentFrame;
        }
        bool animateBody = bodyAnimation != null && model.Skeleton != null && ShouldAnimateEnemy(entry,model);
        bool stabilizeIdleFeet = enemyIdleAnimationEnabled && enemyIdleStabilizeFeet;
        int animationId=bodyAnimation==null?0:System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(bodyAnimation);
        FcvSkeletonPose? animationPose=null;
        bool hasAutomaticAttachment=parts.Any(x=>EnemyEquipmentCatalog.GetAttachmentPoint(entry,x.DatEntryIndex)!=EnemyEquipmentCatalog.AttachmentPoint.Model);
        if(model.Skeleton!=null&&(animateBody||hasAutomaticAttachment||enemyAttachmentBoneIndex>=0))
        {
            var poseKey=(entry.EnemyType,animateBody?animationId:0);
            if(!enemyPoseFrameCache.TryGetValue(poseKey,out animationPose))
            {
                animationPose=FcvSkeletonEvaluator.Evaluate(model.Skeleton,animateBody?bodyAnimation:null,animateBody?bodyFrame:0f,stabilizeIdleFeet,enemyAnimationIgnoreRootMotion);
                enemyPoseFrameCache[poseKey]=animationPose;
            }
        }
        FcvSkeletonPose? bindPose=null;
        if(model.Skeleton!=null&&animationPose!=null)
        {
            string bindKey=$"{model.SourcePath}|{model.SkeletonSourceDatEntryIndex}";
            if(!enemyBindPoseCache.TryGetValue(bindKey,out bindPose)){bindPose=FcvSkeletonEvaluator.Evaluate(model.Skeleton,null,0f);enemyBindPoseCache[bindKey]=bindPose;}
        }
        foreach (EnemyModelPart part in parts)
        {
            EnemyEquipmentCatalog.AttachmentPoint attachmentPoint=EnemyEquipmentCatalog.GetAttachmentPoint(entry,part.DatEntryIndex);
            bool attachToHand = (attachmentPoint!=EnemyEquipmentCatalog.AttachmentPoint.Model||forcedEnemyHandHeldParts.Contains((entry.EnemyType,part.BinIndex))) && animationPose != null && bindPose != null;
            bool forcedAttachment=forcedEnemyHandHeldParts.Contains((entry.EnemyType,part.BinIndex));
            int attachmentBone=forcedAttachment?enemyAttachmentBoneIndex:ResolveEnemyAttachmentBone(model.Skeleton,attachmentPoint);
            var equipmentRotation=EnemyEquipmentCatalog.GetAttachmentRotation(entry,part.DatEntryIndex);
            var equipmentOffset=EnemyEquipmentCatalog.GetAttachmentOffset(entry,part.DatEntryIndex);
            NVector3 attachmentRotation=enemyAttachmentRotationDegrees+new NVector3(equipmentRotation.X,equipmentRotation.Y,equipmentRotation.Z);
            NVector3 attachmentOffset=enemyAttachmentOffset+new NVector3(equipmentOffset.X,equipmentOffset.Y,equipmentOffset.Z);
            // Enemy weapon BINs are authored around their grip/origin. Centering their bounds on
            // the hand displaced long weapons by half their length and made their rotation wrong.
            NVector3 attachmentPivot = NVector3.Zero;
            IReadOnlyDictionary<NVector3, NVector3>? smoothNormals = !attachToHand && !animateBody ? GetEnemySmoothNormals(part) : null;
            foreach (EnemyModelTriangle sourceTriangle in part.Triangles)
            {
                EnemyModelTriangle tri = ApplyEnemyTextureAssignment(entry.EnemyType, part.BinIndex, sourceTriangle);
                tri = ApplyEnemyGameplayModelTransform(entry, tri);
                AddEnemyTriangleToBucket(buckets, entry, tri, origin, rx, ry, rz, model, attachToHand, animationPose, bindPose, attachmentPivot, animateBody, smoothNormals,animationId,attachmentBone,attachmentRotation,attachmentOffset);
            }
        }
    }

    private static EnemyModelTriangle ApplyEnemyGameplayModelTransform(EslEnemyEntry entry,EnemyModelTriangle triangle)
    {
        // em2a_R0_Init uses the ESL HP as the tripwire length for both bomb variants:
        // scale = hp * 0.001 * 0.5, then parts 1/2 have their local Z multiplied by it.
        // Apply it in model space, before the ESL RotMatrix-equivalent rotation below. This is
        // important for pitched/rolled traps: scaling world Z makes the wire appear mis-rotated.
        if(entry.EnemyType!=0x2A || entry.Subtype is not (0x01 or 0x02))return triangle;
        float wireScale=Math.Max(1,(int)entry.Health)*0.0005f;
        NVector3 ScaleWire(NVector3 value)=>new(value.X,value.Y,value.Z*wireScale);
        return triangle with { A=ScaleWire(triangle.A),B=ScaleWire(triangle.B),C=ScaleWire(triangle.C) };
    }

    private static int ResolveEnemyAttachmentBone(Ps2BinSkeleton? skeleton,EnemyEquipmentCatalog.AttachmentPoint point)
    {
        if(skeleton==null)return -1;
        // In the common em10/Ganado rig 0x0A is the character's right hand and 0x10
        // the left hand. Resolve by ID, rather than assuming that every DAT uses index 10/16.
        byte wanted=point switch
        {
            EnemyEquipmentCatalog.AttachmentPoint.Head => 0x04,
            EnemyEquipmentCatalog.AttachmentPoint.LeftHand => 0x10,
            _ => 0x0A
        };
        return skeleton.FirstIndexById.TryGetValue(wanted,out int index)?index:-1;
    }

    private bool ShouldAnimateEnemy(EslEnemyEntry entry,EnemyModelScene model)
    {
        if(entry.Index==selectedEnemyIndex)return true;
        NVector3 center=EslToWorld(entry)+(model.BoundsMin+model.BoundsMax)*.5f;
        float modelRadius=Math.Max(1f,model.Size.Length()*.5f),maxDistance=Math.Max(140f,modelRadius*10f);
        if(NVector3.DistanceSquared(cameraPosition,center)>maxDistance*maxDistance)return false;
        if(!TryProjectWorldToScreen(center,out PointF screen))return false;
        float margin=96f;return screen.X>=-margin&&screen.Y>=-margin&&screen.X<=ClientSize.Width+margin&&screen.Y<=ClientSize.Height+margin;
    }

    private IReadOnlyDictionary<NVector3,NVector3> GetEnemySmoothNormals(EnemyModelPart part)
    {
        if(!enemySmoothNormalCache.TryGetValue(part,out IReadOnlyDictionary<NVector3,NVector3>? normals)){normals=BuildEnemySmoothNormals(part.Triangles);enemySmoothNormalCache[part]=normals;}
        return normals;
    }

    private EnemyModelTriangle ApplyEnemyTextureAssignment(byte enemyType, int binIndex, EnemyModelTriangle triangle) =>
        enemyTextureAssignments.TryGetValue((enemyType, binIndex), out var assignment)
            ? triangle with { TplEntryIndex = assignment.TplEntry, TextureIndex = assignment.TextureIndex }
            : triangle;

    private void AddEnemyTriangleToBucket(Dictionary<EnemyTextureKey,List<float>> buckets,EslEnemyEntry entry,EnemyModelTriangle tri,NVector3 origin,float rx,float ry,float rz,EnemyModelScene model,bool attachToHand,FcvSkeletonPose? animationPose=null,FcvSkeletonPose? bindPose=null,NVector3 attachmentPivot=default,bool animateBody=false,IReadOnlyDictionary<NVector3,NVector3>? smoothNormals=null,int animationId=0,int attachmentBone=-1,NVector3 attachmentRotation=default,NVector3 attachmentOffset=default)
    {
        var key=new EnemyTextureKey(entry.EnemyType,tri.TplEntryIndex,tri.TextureIndex);
        if(!buckets.TryGetValue(key,out List<float>? values)){values=new List<float>();buckets[key]=values;}
        if(attachToHand && animationPose!=null && bindPose!=null) WriteEnemyAttachedTriangle(values,tri,origin,rx,ry,rz,model,animationPose,bindPose,attachmentPivot,attachmentBone,attachmentRotation,attachmentOffset);
        else if(animateBody && animationPose!=null && bindPose!=null) WriteEnemySkinnedTriangle(values,tri,origin,rx,ry,rz,model,animationPose,bindPose,entry.EnemyType,animationId);
        else WriteEnemyTriangle(values,tri,origin,rx,ry,rz,smoothNormals);
    }

    private static Dictionary<NVector3, NVector3> BuildEnemySmoothNormals(IReadOnlyList<EnemyModelTriangle> triangles)
    {
        var sums = new Dictionary<NVector3, NVector3>();
        foreach (EnemyModelTriangle triangle in triangles)
        {
            NVector3 normal = NVector3.Cross(triangle.B - triangle.A, triangle.C - triangle.A);
            if (!float.IsFinite(normal.LengthSquared()) || normal.LengthSquared() < 0.0000001f) continue;
            void Add(NVector3 vertex) { sums.TryGetValue(vertex, out NVector3 value); sums[vertex] = value + normal; }
            Add(triangle.A); Add(triangle.B); Add(triangle.C);
        }
        foreach (NVector3 key in sums.Keys.ToArray())
        {
            NVector3 value = sums[key]; float length = value.Length();
            sums[key] = length > 0.000001f && float.IsFinite(length) ? value / length : NVector3.UnitY;
        }
        return sums;
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

    private void WriteEnemyAttachedTriangle(List<float> values,EnemyModelTriangle tri,NVector3 origin,float rx,float ry,float rz,EnemyModelScene model,FcvSkeletonPose pose,FcvSkeletonPose bindPose,NVector3 attachmentPivot,int attachmentBone,NVector3 attachmentRotation,NVector3 attachmentOffset)
    {
        if(model.Skeleton==null || attachmentBone<0 || attachmentBone>=model.Skeleton.Bones.Count){WriteEnemyTriangle(values,tri,origin,rx,ry,rz);return;}
        // Keep preview animations anchored to the ESL position. FCV root translation is
        // animation/root-motion data and must not be added on top of the enemy world position.
        NVector3 rootMotion = GetEnemyAnimationRootMotion(model.Skeleton, pose, bindPose);
        NVector3 bonePos=pose.WorldPositions[attachmentBone]/100f - rootMotion;
        NQuaternion boneRot=pose.WorldRotations[attachmentBone];
        NVector3 a=TransformEnemyAttachedVertex(tri.A,attachmentPivot,bonePos,boneRot,origin,rx,ry,rz,attachmentRotation,attachmentOffset);
        NVector3 b=TransformEnemyAttachedVertex(tri.B,attachmentPivot,bonePos,boneRot,origin,rx,ry,rz,attachmentRotation,attachmentOffset);
        NVector3 c=TransformEnemyAttachedVertex(tri.C,attachmentPivot,bonePos,boneRot,origin,rx,ry,rz,attachmentRotation,attachmentOffset);
        NVector3 n=NVector3.Cross(b-a,c-a); float len=n.Length(); if(!float.IsFinite(len)||len<0.000001f)return; n/=len;
        WriteEnemyModelVertex(values,a,n,tri.UvA);WriteEnemyModelVertex(values,b,n,tri.UvB);WriteEnemyModelVertex(values,c,n,tri.UvC);
    }

    private static NVector3 TransformEnemyAttachedVertex(NVector3 v,NVector3 weaponPivot,NVector3 bonePos,NQuaternion boneRot,NVector3 origin,float rx,float ry,float rz,NVector3 attachmentRotation,NVector3 attachmentOffset)
    {
        // Rigid bind-pose attachment: put the weapon's own pivot on the selected bone.
        // This deliberately avoids bind/current cancellation so changing bones is visible immediately.
        v-=weaponPivot;
        float ax=attachmentRotation.X*MathF.PI/180f, ay=attachmentRotation.Y*MathF.PI/180f, az=attachmentRotation.Z*MathF.PI/180f;
        if(MathF.Abs(ax)>0.000001f){float c=MathF.Cos(ax),ss=MathF.Sin(ax);v=new NVector3(v.X,v.Y*c-v.Z*ss,v.Y*ss+v.Z*c);}
        if(MathF.Abs(ay)>0.000001f){float c=MathF.Cos(ay),ss=MathF.Sin(ay);v=new NVector3(v.X*c+v.Z*ss,v.Y,-v.X*ss+v.Z*c);}
        if(MathF.Abs(az)>0.000001f){float c=MathF.Cos(az),ss=MathF.Sin(az);v=new NVector3(v.X*c-v.Y*ss,v.X*ss+v.Y*c,v.Z);}
        v+=attachmentOffset;
        v=NVector3.Transform(v,boneRot)+bonePos;
        return TransformEnemyModelVertex(v,origin,rx,ry,rz);
    }

    private void WriteEnemySkinnedTriangle(List<float> values, EnemyModelTriangle tri, NVector3 origin, float rx, float ry, float rz, EnemyModelScene model, FcvSkeletonPose pose, FcvSkeletonPose bindPose,byte enemyType,int animationId)
    {
        if (model.Skeleton == null) { WriteEnemyTriangle(values, tri, origin, rx, ry, rz); return; }
        NVector3 Skin(NVector3 vertex,EnemyVertexSkin skin)
        {
            var key=(enemyType,animationId,vertex,skin);if(enemySkinnedVertexFrameCache.TryGetValue(key,out NVector3 cached))return cached;
            NVector3 result=TransformEnemySkinnedVertex(vertex,skin,model.Skeleton,pose,bindPose);enemySkinnedVertexFrameCache[key]=result;return result;
        }
        NVector3 a = Skin(tri.A, tri.SkinA);
        NVector3 b = Skin(tri.B, tri.SkinB);
        NVector3 c = Skin(tri.C, tri.SkinC);
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

        // Most clips remove their complete gameplay/root motion. Planted idle poses instead
        // provide the frame-zero anchor here, preserving their small cyclic hip movement while
        // keeping the enemy centered on its ESL marker.
        NVector3 delta = pose.RootMotionToRemove / 100f;
        return float.IsFinite(delta.X) && float.IsFinite(delta.Y) && float.IsFinite(delta.Z) ? delta : NVector3.Zero;
    }

    private static void WriteEnemyTriangle(List<float> values, EnemyModelTriangle tri, NVector3 origin, float rx, float ry, float rz, IReadOnlyDictionary<NVector3,NVector3>? smoothNormals=null)
    {
        NVector3 a = TransformEnemyModelVertex(tri.A, origin, rx, ry, rz);
        NVector3 b = TransformEnemyModelVertex(tri.B, origin, rx, ry, rz);
        NVector3 c = TransformEnemyModelVertex(tri.C, origin, rx, ry, rz);
        NVector3 n = NVector3.Cross(b-a,c-a);
        float len = n.Length();
        if (!float.IsFinite(len) || len < 0.000001f) return;
        n /= len;
        NVector3 Normal(NVector3 source) => smoothNormals != null && smoothNormals.TryGetValue(source, out NVector3 smooth) ? RotateEnemyModelVector(smooth, rx, ry, rz) : n;
        WriteEnemyModelVertex(values,a,Normal(tri.A),tri.UvA);
        WriteEnemyModelVertex(values,b,Normal(tri.B),tri.UvB);
        WriteEnemyModelVertex(values,c,Normal(tri.C),tri.UvC);
    }

    private static NVector3 RotateEnemyModelVector(NVector3 v, float rx, float ry, float rz)
    {
        if (MathF.Abs(rx) > 0.000001f) { float c=MathF.Cos(rx),s=MathF.Sin(rx); v=new NVector3(v.X,v.Y*c-v.Z*s,v.Y*s+v.Z*c); }
        if (MathF.Abs(ry) > 0.000001f) { float c=MathF.Cos(ry),s=MathF.Sin(ry); v=new NVector3(v.X*c+v.Z*s,v.Y,-v.X*s+v.Z*c); }
        if (MathF.Abs(rz) > 0.000001f) { float c=MathF.Cos(rz),s=MathF.Sin(rz); v=new NVector3(v.X*c-v.Y*s,v.X*s+v.Y*c,v.Z); }
        float length=v.Length(); return length>0.000001f?v/length:NVector3.UnitY;
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
    }
    private void AddEnemyTransformGizmo(List<float>[] axes,EslEnemyEntry e)
    {
        float x=e.PosX*EslWorldScale,y=e.PosY*EslWorldScale,z=e.PosZ*EslWorldScale,axis=EnemyGizmoLength(e);
        void L(int index,float ax,float ay,float az,float bx,float by,float bz)=>axes[index].AddRange(new[]{ax,ay,az,0f,0f,0f,bx,by,bz,0f,0f,0f});
        if (EnemyTransformMode == EnemyGizmoMode.Move)
        {
            float arrowHead=axis*.13f,wing=axis*.075f;
            L(0,x,y,z,x+axis,y,z); L(1,x,y,z,x,y+axis,z); L(2,x,y,z,x,y,z+axis);
            L(0,x+axis,y,z,x+axis-arrowHead,y+wing,z); L(0,x+axis,y,z,x+axis-arrowHead,y-wing,z);
            L(1,x,y+axis,z,x+wing,y+axis-arrowHead,z); L(1,x,y+axis,z,x-wing,y+axis-arrowHead,z);
            L(2,x,y,z+axis,x,y+wing,z+axis-arrowHead); L(2,x,y,z+axis,x,y-wing,z+axis-arrowHead);
        }
        else
        {
            const int seg=72; float rr=axis;
            for(int i=0;i<seg;i++)
            {
                float a0=(float)(i*Math.PI*2/seg),a1=(float)((i+1)*Math.PI*2/seg);
                // X ring (YZ), Y ring (XZ), Z ring (XY)
                L(0,x,y+MathF.Cos(a0)*rr,z+MathF.Sin(a0)*rr,x,y+MathF.Cos(a1)*rr,z+MathF.Sin(a1)*rr);
                L(1,x+MathF.Cos(a0)*rr,y+0.04f,z+MathF.Sin(a0)*rr,x+MathF.Cos(a1)*rr,y+0.04f,z+MathF.Sin(a1)*rr);
                L(2,x+MathF.Cos(a0)*rr,y+MathF.Sin(a0)*rr,z,x+MathF.Cos(a1)*rr,y+MathF.Sin(a1)*rr,z);
            }
        }
    }
}

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
    private void DrawAevGpu()
    {
        // AEV volumes participate in the scenario depth test, so the floor and walls
        // clip the portion that is physically behind them. Only editing handles remain
        // an overlay, otherwise a partly buried volume could become impossible to edit.
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
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

        GL.Disable(EnableCap.DepthTest);

        if (aevHandleVertexCount > 0)
        {
            GL.Uniform3(uColor, 1.0f, 0.95f, 0.30f);
            GL.BindVertexArray(aevHandleVao);
            GL.LineWidth(5f);
            GL.DrawArrays(PrimitiveType.Lines, 0, aevHandleVertexCount);
        }

        NVector3[] gizmoColors = { new(1f, .18f, .12f), new(.20f, 1f, .24f), new(.16f, .45f, 1f) };
        for (int i = 0; i < 3; i++)
        {
            if (aevGizmoVertexCounts[i] <= 0) continue;
            GL.Uniform3(uColor, gizmoColors[i].X, gizmoColors[i].Y, gizmoColors[i].Z);
            GL.BindVertexArray(aevGizmoVaos[i]);
            GL.LineWidth(6f);
            GL.DrawArrays(PrimitiveType.Lines, 0, aevGizmoVertexCounts[i]);
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

    private static bool UsesVertexAlpha(ScenarioTriangle triangle) =>
        triangle.AlphaA < 0.999f || triangle.AlphaB < 0.999f || triangle.AlphaC < 0.999f;

    private static void WriteTexturedVertex(float[] output, ref int o, NVector3 p, NVector3 n, System.Numerics.Vector2 uv, float alpha = 1f, NVector3? color = null)
    {
        output[o++] = p.X; output[o++] = p.Y; output[o++] = p.Z;
        output[o++] = n.X; output[o++] = n.Y; output[o++] = n.Z;
        output[o++] = uv.X; output[o++] = uv.Y;
        output[o++] = alpha;
        NVector3 c=color??NVector3.One;output[o++]=c.X;output[o++]=c.Y;output[o++]=c.Z;
    }

    private static void WriteVertex(float[] data, ref int o, NVector3 p, NVector3 n)
    {
        data[o++] = p.X; data[o++] = p.Y; data[o++] = p.Z;
        data[o++] = n.X; data[o++] = n.Y; data[o++] = n.Z;
    }

    private float[] BuildGridData(ScenarioScene scene)
    {
        float radius = GridAtWorldOrigin && GridRadiusOverride > 0f ? GridRadiusOverride : scene.Radius;
        float rawStep = Math.Max(1f, radius / 10f);
        float power = (float)Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        float normalized = rawStep / power;
        float step = normalized < 2f ? power : normalized < 5f ? 2f * power : 5f * power;
        float extent = step * 12f;
        float y = GridAtWorldOrigin ? 0f : scene.BoundsMin.Y;
        float cx = GridAtWorldOrigin ? 0f : scene.Center.X;
        float cz = GridAtWorldOrigin ? 0f : scene.Center.Z;

        var values = new List<float>(25 * 4 * 6);
        for (int i = -12; i <= 12; i++)
        {
            float x = cx + i * step;
            float z = cz + i * step;
            AddGridVertex(values, x, y, cz - extent); AddGridVertex(values, x, y, cz + extent);
            AddGridVertex(values, cx - extent, y, z); AddGridVertex(values, cx + extent, y, z);
        }
        if(ShowWorldOriginMarker)
        {
            float len=step*.65f;
            AddGridVertex(values,0,0,0);AddGridVertex(values,len,0,0);
            AddGridVertex(values,0,0,0);AddGridVertex(values,0,len,0);
            AddGridVertex(values,0,0,0);AddGridVertex(values,0,0,len);
            for(int i=0;i<32;i++){float a=i*MathF.Tau/32,b=(i+1)*MathF.Tau/32;AddGridVertex(values,MathF.Cos(a)*len*.2f,0,MathF.Sin(a)*len*.2f);AddGridVertex(values,MathF.Cos(b)*len*.2f,0,MathF.Sin(b)*len*.2f);}
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
        int marker=ShowWorldOriginMarker?70:0;
        GL.DrawArrays(PrimitiveType.Lines, 0, gridVertexCount-marker);
        if(marker>0){GL.Disable(EnableCap.DepthTest);GL.LineWidth(3);int start=gridVertexCount-marker;
            GL.Uniform3(uColor,1f,.25f,.2f);GL.DrawArrays(PrimitiveType.Lines,start,2);
            GL.Uniform3(uColor,.3f,1f,.4f);GL.DrawArrays(PrimitiveType.Lines,start+2,2);
            GL.Uniform3(uColor,.25f,.6f,1f);GL.DrawArrays(PrimitiveType.Lines,start+4,2);
            GL.Uniform3(uColor,1f,.85f,.35f);GL.DrawArrays(PrimitiveType.Lines,start+6,64);GL.LineWidth(1);GL.Enable(EnableCap.DepthTest);
        }
    }

    private void DrawMeshGpu()
    {
        if(smdEntryGpu.Count>0){DrawSmdEntryMeshes();return;}
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
            bool transparent = batch.HasVertexAlpha || glTextureHasTransparency.TryGetValue(batch.TextureIndex, out bool value) && value;
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
        GL.Disable(EnableCap.CullFace);

        foreach (ScenarioDrawBatch batch in meshBatches)
        {
            bool transparent = batch.HasVertexAlpha || glTextureHasTransparency.TryGetValue(batch.TextureIndex, out bool value) && value;
            if (!transparent) continue;

            DrawScenarioBatch(batch);
        }

        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
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
        Rectangle renderViewport=GetRenderViewport();
        float aspect = Math.Max(0.01f, renderViewport.Width / (float)Math.Max(1, renderViewport.Height));
        float radius = scene?.Radius ?? 1000f;
        float distanceToScene = scene == null ? 1000f : NVector3.Distance(cameraPosition, scene.Center);
        float near = Math.Max(0.001f, radius * 0.00005f);
        float far = Math.Max(near + 100f, distanceToScene + radius * 30f);
        float verticalFov=FieldOfViewIsHorizontal?2f*MathF.Atan(MathF.Tan(MathHelper.DegreesToRadians(fieldOfViewDegrees)*.5f)/aspect):MathHelper.DegreesToRadians(fieldOfViewDegrees);
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(verticalFov, aspect, near, far);
        return view * projection;
    }

    private Rectangle GetRenderViewport()
    {
        int width=Math.Max(1,ClientSize.Width),height=Math.Max(1,ClientSize.Height);float? forced=ForcedAspectRatio;
        if(!forced.HasValue||!float.IsFinite(forced.Value)||forced.Value<=0f)return new Rectangle(0,0,width,height);
        float current=width/(float)height;if(current>forced.Value){int contentWidth=Math.Max(1,(int)MathF.Round(height*forced.Value));return new Rectangle((width-contentWidth)/2,0,contentWidth,height);}
        int contentHeight=Math.Max(1,(int)MathF.Round(width/forced.Value));return new Rectangle(0,(height-contentHeight)/2,width,contentHeight);
    }

    private static float DistancePointToSegment(PointF p, PointF a, PointF b)
    {
        float vx=b.X-a.X, vy=b.Y-a.Y, wx=p.X-a.X, wy=p.Y-a.Y;
        float len2=vx*vx+vy*vy; if(len2<0.0001f) return MathF.Sqrt(wx*wx+wy*wy);
        float t=Math.Clamp((wx*vx+wy*vy)/len2,0f,1f); float dx=p.X-(a.X+t*vx),dy=p.Y-(a.Y+t*vy); return MathF.Sqrt(dx*dx+dy*dy);
    }

    private int PickEnemyGizmoHandle(Point mouse, EslEnemyEntry enemy)
    {
        NVector3 o=EslToWorld(enemy); float axis=EnemyGizmoLength(enemy); const float threshold=11f;
        if (EnemyTransformMode == EnemyGizmoMode.Move)
        {
            NVector3[] ends={o+NVector3.UnitX*axis,o+NVector3.UnitY*axis,o+NVector3.UnitZ*axis};
            if(!TryProjectWorldToScreen(o,out PointF po)) return 0;
            for(int i=0;i<3;i++) if(TryProjectWorldToScreen(ends[i],out PointF pe) && DistancePointToSegment(mouse,po,pe)<=threshold) return i+1;
            return 0;
        }
        const int seg=72; float rr=axis; int best=0; float bestD=threshold;
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
    private float EnemyGizmoLength(EslEnemyEntry enemy)
    {
        NVector3 origin=EslToWorld(enemy); float depth=Math.Max(.05f,NVector3.Dot(origin-cameraPosition,GetForward()));
        Rectangle viewport=GetRenderViewport(); float aspect=Math.Max(.01f,viewport.Width/(float)Math.Max(1,viewport.Height));
        float verticalFov=FieldOfViewIsHorizontal?2f*MathF.Atan(MathF.Tan(fieldOfViewDegrees*MathF.PI/360f)/aspect):fieldOfViewDegrees*MathF.PI/180f;
        float worldPerPixel=2f*depth*MathF.Tan(verticalFov*.5f)/Math.Max(1,viewport.Height);
        return Math.Clamp(worldPerPixel*72f,.25f,1000f);
    }
    private bool IsMouseNearEnemy(Point mouse, EslEnemyEntry enemy, float radius=20f)
    {
        if (!TryProjectWorldToScreen(EslToWorld(enemy), out PointF p)) return false;
        float dx=p.X-mouse.X, dy=p.Y-mouse.Y; return dx*dx+dy*dy <= radius*radius;
    }
    private static short ClampShort(float value) => (short)Math.Clamp((int)MathF.Round(value), short.MinValue, short.MaxValue);

    private void UpdateEnemyDragPreviewModel(EslEnemyEntry enemy)
    {
        if(!enemyGpuPreviewActive){enemyGpuPreviewModel=Matrix4.Identity;return;}
        NVector3 pivot=enemyDragStartWorld;
        if(enemyDragMode is 1 or 2 or 3 or 7)
        {
            NVector3 delta=EslToWorld(enemy)-pivot;
            enemyGpuPreviewModel=Matrix4.CreateTranslation(delta.X,delta.Y,delta.Z);
            return;
        }
        float angle=enemyDragMode switch
        {
            4=>(enemy.RotX-enemyDragStartRotX)*(MathF.PI/32768f),
            5=>(enemy.RotY-enemyDragStartRotY)*(MathF.PI/32768f),
            _=>(enemy.RotZ-enemyDragStartRotZ)*(MathF.PI/32768f)
        };
        var p=new OpenTK.Mathematics.Vector3(pivot.X,pivot.Y,pivot.Z);
        var axis=enemyDragMode==4?OpenTK.Mathematics.Vector3.UnitX:enemyDragMode==5?OpenTK.Mathematics.Vector3.UnitY:OpenTK.Mathematics.Vector3.UnitZ;
        enemyGpuPreviewModel=Matrix4.CreateTranslation(-p)*Matrix4.CreateFromAxisAngle(axis,angle)*Matrix4.CreateTranslation(p);
    }

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
                SetEnemyAlphaState(batch.Key);
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
            Matrix4 selectedModel=enemyGpuPreviewActive?enemyGpuPreviewModel:Matrix4.Identity;
            GL.UniformMatrix4(uModel,true,ref selectedModel);
            GL.BindVertexArray(selectedEnemyModelVao);
            GL.Uniform3(uColor,0.72f,0.74f,0.78f);
            foreach (EnemyModelDrawBatch batch in selectedEnemyModelBatches)
            {
                SetEnemyAlphaState(batch.Key);
                if (batch.Key.TextureIndex == -101) GL.Uniform3(uColor,0.15f,0.45f,1f);
                else if (batch.Key.TextureIndex == -102) GL.Uniform3(uColor,1f,0.82f,0.12f);
                else if (batch.Key.TextureIndex == -103) GL.Uniform3(uColor,1f,0.18f,0.08f);
                else GL.Uniform3(uColor,0.72f,0.74f,0.78f);
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
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);

            // Soft selection tint: preserve the original texture/detail and add only a
            // translucent yellow skin, avoiding the noisy per-face wireframe.
            GL.Uniform1(uUseTexture,0);
            GL.Uniform1(uUnlit,1);
            GL.Uniform3(uColor,1f,0.78f,0.08f);
            GL.Uniform1(uOpacity,.28f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha,BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false);
            GL.Enable(EnableCap.PolygonOffsetFill);
            GL.PolygonOffset(-1f,-1f);
            GL.DrawArrays(PrimitiveType.Triangles,0,selectedEnemyModelVertexCount);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            GL.Uniform1(uOpacity,1f);
            GL.Uniform1(uUnlit,0);
        }

        if (EnemyModelEditWireframeVisible && enemyModelVertexCount > 0)
        {
            Matrix4 identity = Matrix4.Identity;
            GL.UniformMatrix4(uModel, true, ref identity);
            GL.BindVertexArray(enemyModelVao);
            GL.Uniform1(uUseTexture, 0);
            GL.Uniform1(uUnlit, 1);
            GL.Uniform3(uColor, 1f, 0.78f, 0.08f);
            GL.Uniform1(uOpacity, 0.55f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            GL.DepthMask(false);
            GL.LineWidth(1f);
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-1f, -1f);
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            GL.DrawArrays(PrimitiveType.Triangles, 0, enemyModelVertexCount);
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
            GL.Disable(EnableCap.PolygonOffsetLine);
            GL.DepthMask(true);
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Uniform1(uOpacity, 1f);
            GL.Uniform1(uUnlit, 0);
        }

        Matrix4 enemyIdentity=Matrix4.Identity;
        GL.UniformMatrix4(uModel,true,ref enemyIdentity);

        // Texture batches may leave alpha blending enabled (for hair cards).
        // Restore the shared viewport state before drawing the editor overlay.
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);

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
            Matrix4 selectedOverlay=enemyGpuPreviewActive?enemyGpuPreviewModel:Matrix4.Identity;
            GL.UniformMatrix4(uModel,true,ref selectedOverlay);
            GL.Uniform3(uColor,1f,0.85f,0.1f);
            GL.BindVertexArray(selectedEnemyVao);
            GL.LineWidth(5f);
            GL.DrawArrays(PrimitiveType.Lines,0,selectedEnemyVertexCount);
        }
        var axisColors=new[]{(1f,.16f,.12f),(.18f,.9f,.28f),(.14f,.48f,1f)};
        for(int i=0;i<3;i++)
        {
            bool active=(enemyDragMode!=0&&((enemyDragMode-1)%3)==i)||enemyFaceGizmoAxis==i+1;
            if(active)GL.Uniform3(uColor,1f,.95f,.3f);else GL.Uniform3(uColor,axisColors[i].Item1,axisColors[i].Item2,axisColors[i].Item3);
            GL.BindVertexArray(enemyGizmoVaos[i]);GL.LineWidth(active?9f:6f);GL.DrawArrays(PrimitiveType.Lines,0,enemyGizmoCounts[i]);
        }
        GL.UniformMatrix4(uModel,true,ref enemyIdentity);
        GL.LineWidth(1f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
    }

    private void SetEnemyAlphaState(EnemyTextureKey key)
    {
        if (glEnemyTextureHasTransparency.TryGetValue(key, out bool transparent) && transparent)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            // Alpha-tested hair cards still have a defined outside face. Keeping
            // backface culling enabled prevents the inner side of the hairstyle
            // from showing through when the camera is outside the model.
            GL.Enable(EnableCap.CullFace);
        }
        else
        {
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
        }
    }

}

using RE4_PS2_MOD_WORKSPACE.Core.Visual;
using OpenTK.Graphics.OpenGL4;

namespace RE4_PS2_MOD_WORKSPACE;

public sealed partial class ScenarioViewport
{
    private const float EtsWorldScale = 0.01f;
    private readonly Stack<Action> etsUndo = new();
    private EtsEntry? draggingEts;
    private EtsTransformState etsDragStart;
    private Point etsDragMouse;
    private System.Numerics.Vector3 etsDragWorld;
    private int etsDragAxis;
    private readonly Dictionary<EtsEntry,EtsTransformState> etsGroupDragStart=new();
    private bool etsTexturesDirty = true;
    private readonly Dictionary<EtsTextureKey,int> glEtsTextures = new();
    private readonly Dictionary<EtsTextureKey,bool> glEtsTextureTransparency = new();
    private readonly List<EtsDrawBatch> etsDrawBatches = new(), selectedEtsDrawBatches = new();
    private readonly int[] etsGizmoVaos=new int[3],etsGizmoVbos=new int[3],etsGizmoVertexCounts=new int[3];
    public bool IsEtsDragging => draggingEts != null;

    public bool UndoEtsEdit() { if (etsUndo.Count == 0) return false; etsUndo.Pop()(); return true; }
    public void RegisterEtsUndo(Action action) { if(action!=null) etsUndo.Push(action); }
    public void FocusEts(EtsEntry? e) { if(e==null)return; var p=EtsWorld(e); target=p; distance=Math.Max(8f,distance); cameraPosition=target-GetForward()*Math.Min(distance,80f); Invalidate(); }

    private void UploadEts()
    {
        etsGpuDirty = false; etsVertexCount = selectedEtsVertexCount = etsModelVertexCount = selectedEtsModelVertexCount = 0;
        if (!glReady || etsScene == null) return;
        EnsureEtsBuffers();
        var normal = new List<float>(etsScene.Entries.Count * 60);
        var selected = new List<float>(60);
        var gizmoAxes=new[]{new List<float>(),new List<float>(),new List<float>()};
        var modelBuckets = new Dictionary<EtsTextureKey,List<float>>();
        var selectedBuckets = new Dictionary<EtsTextureKey,List<float>>();
        foreach (EtsEntry entry in etsScene.Entries)
        {
            bool isSelected = selectedEtsFileOrders.Contains(entry.FileOrder);
            AddEtsMarker(isSelected ? selected : normal, entry, isSelected);
            if(entry.FileOrder==selectedEtsFileOrder)AddEtsGizmo(gizmoAxes,entry);
            if (etmCatalog?.ModelParts.TryGetValue(entry.ObjectId, out IReadOnlyList<EtmModelPart>? parts) == true)
                foreach(EtmModelPart part in parts)
                {
                    foreach(var material in part.Triangles.GroupBy(t=>t.TextureIndex))
                    {
                        var key=new EtsTextureKey(entry.ObjectId,part.Effect?.FileOrder??-1,material.Key);
                        if(!glEtsTextures.ContainsKey(key)&&part.TextureFallback!=null)
                            key=new EtsTextureKey(entry.ObjectId,part.TextureFallback.FileOrder,material.Key);
                        var buckets=isSelected?selectedBuckets:modelBuckets;if(!buckets.TryGetValue(key,out var values)){values=new();buckets[key]=values;}
                        AddEtsModel(values,entry,material);
                    }
                }
        }
        UploadLineBuffer(etsVao, etsVbo, normal, out etsVertexCount);
        UploadLineBuffer(selectedEtsVao, selectedEtsVbo, selected, out selectedEtsVertexCount);
        for(int axis=0;axis<3;axis++)UploadLineBuffer(etsGizmoVaos[axis],etsGizmoVbos[axis],gizmoAxes[axis],out etsGizmoVertexCounts[axis]);
        List<float> model=BuildEtsBatches(modelBuckets,etsDrawBatches);List<float> selectedModel=BuildEtsBatches(selectedBuckets,selectedEtsDrawBatches);
        UploadEnemyModelBuffer(etsModelVao, etsModelVbo, model, out etsModelVertexCount);
        UploadEnemyModelBuffer(selectedEtsModelVao, selectedEtsModelVbo, selectedModel, out selectedEtsModelVertexCount);
    }

    private void EnsureEtsBuffers()
    {
        if (etsVao == 0) etsVao = GL.GenVertexArray();
        if (etsVbo == 0) etsVbo = GL.GenBuffer();
        if (selectedEtsVao == 0) selectedEtsVao = GL.GenVertexArray();
        if (selectedEtsVbo == 0) selectedEtsVbo = GL.GenBuffer();
        if (etsModelVao == 0) etsModelVao = GL.GenVertexArray();
        if (etsModelVbo == 0) etsModelVbo = GL.GenBuffer();
        if (selectedEtsModelVao == 0) selectedEtsModelVao = GL.GenVertexArray();
        if (selectedEtsModelVbo == 0) selectedEtsModelVbo = GL.GenBuffer();
        for(int i=0;i<3;i++){if(etsGizmoVaos[i]==0)etsGizmoVaos[i]=GL.GenVertexArray();if(etsGizmoVbos[i]==0)etsGizmoVbos[i]=GL.GenBuffer();}
    }

    private void AddEtsMarker(List<float> values, EtsEntry e, bool selected)
    {
        float x=e.PositionX*EtsWorldScale,y=e.PositionY*EtsWorldScale,z=e.PositionZ*EtsWorldScale;
        float r=selected?2.4f:1.8f;
        void L(float ax,float ay,float az,float bx,float by,float bz)=>values.AddRange(new[]{ax,ay,az,0f,0f,0f,bx,by,bz,0f,0f,0f});
        // Box-like marker instead of a point: doors/windows/crates remain easy to select at distance.
        L(x-r,y,z-r,x+r,y,z-r); L(x+r,y,z-r,x+r,y,z+r); L(x+r,y,z+r,x-r,y,z+r); L(x-r,y,z+r,x-r,y,z-r);
        L(x,y,z,x,y+r*2,z);
        float dx=MathF.Sin(e.RotationY),dz=MathF.Cos(e.RotationY),len=r*2.2f;
        L(x,y+0.1f,z,x+dx*len,y+0.1f,z+dz*len);
    }

    private void AddEtsGizmo(List<float>[] axes,EtsEntry e)
    {
        float x=e.PositionX*EtsWorldScale,y=e.PositionY*EtsWorldScale,z=e.PositionZ*EtsWorldScale;
        void L(int a,float ax,float ay,float az,float bx,float by,float bz)=>axes[a].AddRange(new[]{ax,ay,az,0f,0f,0f,bx,by,bz,0f,0f,0f});
        if(EtsTransformMode==EtsGizmoMode.Move)
        {
            const float len=5f,head=.65f;L(0,x,y,z,x+len,y,z);L(0,x+len,y,z,x+len-head,y+head*.35f,z);L(0,x+len,y,z,x+len-head,y-head*.35f,z);
            L(1,x,y,z,x,y+len,z);L(1,x,y+len,z,x+head*.35f,y+len-head,z);L(1,x,y+len,z,x-head*.35f,y+len-head,z);
            L(2,x,y,z,x,y,z+len);L(2,x,y,z+len,x,y+head*.35f,z+len-head);L(2,x,y,z+len,x,y-head*.35f,z+len-head);
        }
        else
        {
            const int segments=40;const float r=3.8f;for(int i=0;i<segments;i++){float a=i*MathF.Tau/segments,b=(i+1)*MathF.Tau/segments;
                L(0,x,y+MathF.Cos(a)*r,z+MathF.Sin(a)*r,x,y+MathF.Cos(b)*r,z+MathF.Sin(b)*r);
                L(1,x+MathF.Cos(a)*r,y,z+MathF.Sin(a)*r,x+MathF.Cos(b)*r,y,z+MathF.Sin(b)*r);
                L(2,x+MathF.Cos(a)*r,y+MathF.Sin(a)*r,z,x+MathF.Cos(b)*r,y+MathF.Sin(b)*r,z);}
        }
    }

    private void DrawEtsGpu()
    {
        GL.Enable(EnableCap.DepthTest); GL.Disable(EnableCap.CullFace); GL.Uniform1(uUseTexture,0); GL.Uniform1(uUnlit,0); GL.Uniform1(uOpacity,1f);
        if (etsModelVertexCount > 0) { GL.Uniform3(uColor,0.68f,0.72f,0.76f); GL.BindVertexArray(etsModelVao); DrawEtsBatches(etsDrawBatches); }
        if (selectedEtsModelVertexCount > 0)
        {
            GL.Uniform3(uColor,0.82f,0.84f,0.88f); GL.BindVertexArray(selectedEtsModelVao); DrawEtsBatches(selectedEtsDrawBatches);
            GL.Uniform1(uUnlit,1); GL.Uniform3(uColor,1f,0.82f,0.12f); GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Line); GL.DrawArrays(PrimitiveType.Triangles,0,selectedEtsModelVertexCount); GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);
        }
        GL.Uniform1(uUseTexture,0); GL.Uniform1(uUnlit,1); GL.Disable(EnableCap.CullFace); GL.Disable(EnableCap.DepthTest);
        GL.Uniform3(uColor,0.15f,0.78f,0.95f); GL.BindVertexArray(etsVao); GL.LineWidth(3f); GL.DrawArrays(PrimitiveType.Lines,0,etsVertexCount);
        if(selectedEtsVertexCount>0) { GL.Uniform3(uColor,1f,0.82f,0.12f); GL.BindVertexArray(selectedEtsVao); GL.LineWidth(5f); GL.DrawArrays(PrimitiveType.Lines,0,selectedEtsVertexCount); }
        var colors=new[]{(1f,.16f,.12f),(.2f,.9f,.25f),(.15f,.48f,1f)};for(int i=0;i<3;i++)if(etsGizmoVertexCounts[i]>0){GL.Uniform3(uColor,colors[i].Item1,colors[i].Item2,colors[i].Item3);GL.BindVertexArray(etsGizmoVaos[i]);GL.LineWidth(5f);GL.DrawArrays(PrimitiveType.Lines,0,etsGizmoVertexCounts[i]);}
        GL.LineWidth(1f); GL.Enable(EnableCap.DepthTest); GL.Enable(EnableCap.CullFace); GL.Uniform1(uUnlit,0); GL.Uniform1(uUseTexture,0); GL.Uniform1(uOpacity,1f);
    }

    private static void AddEtsModel(List<float> values, EtsEntry entry, IEnumerable<ScenarioTriangle> triangles)
    {
        foreach (ScenarioTriangle triangle in triangles)
        {
            var a=TransformEtsVertex(triangle.A,entry); var b=TransformEtsVertex(triangle.B,entry); var c=TransformEtsVertex(triangle.C,entry);
            var n=System.Numerics.Vector3.Cross(b-a,c-a); float len=n.Length(); if(!float.IsFinite(len)||len<0.000001f) continue; n/=len;
            WriteEnemyModelVertex(values,a,n,triangle.UvA); WriteEnemyModelVertex(values,b,n,triangle.UvB); WriteEnemyModelVertex(values,c,n,triangle.UvC);
        }
    }

    private static System.Numerics.Vector3 TransformEtsVertex(System.Numerics.Vector3 v,EtsEntry e)
    {
        v*=new System.Numerics.Vector3(SafeScale(e.ScaleX),SafeScale(e.ScaleY),SafeScale(e.ScaleZ));
        if(MathF.Abs(e.RotationX)>0.000001f){float c=MathF.Cos(e.RotationX),s=MathF.Sin(e.RotationX);v=new(v.X,v.Y*c-v.Z*s,v.Y*s+v.Z*c);}
        if(MathF.Abs(e.RotationY)>0.000001f){float c=MathF.Cos(e.RotationY),s=MathF.Sin(e.RotationY);v=new(v.X*c+v.Z*s,v.Y,-v.X*s+v.Z*c);}
        if(MathF.Abs(e.RotationZ)>0.000001f){float c=MathF.Cos(e.RotationZ),s=MathF.Sin(e.RotationZ);v=new(v.X*c-v.Y*s,v.X*s+v.Y*c,v.Z);}
        return v+new System.Numerics.Vector3(e.PositionX,e.PositionY,e.PositionZ)*EtsWorldScale;
    }
    private static float SafeScale(float value)=>float.IsFinite(value)&&MathF.Abs(value)>0.000001f?value:1f;

    private static List<float> BuildEtsBatches(Dictionary<EtsTextureKey,List<float>> buckets,List<EtsDrawBatch> batches)
    {batches.Clear();var all=new List<float>();foreach(var pair in buckets){int first=all.Count/8;all.AddRange(pair.Value);int count=pair.Value.Count/8;if(count>0)batches.Add(new(pair.Key,first,count));}return all;}
    private void DrawEtsBatches(List<EtsDrawBatch> batches)
    {
        if(RenderMode==ScenarioRenderMode.Wireframe)
        {
            GL.Uniform1(uUseTexture,0);GL.Disable(EnableCap.Blend);GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Line);
            foreach(EtsDrawBatch batch in batches)GL.DrawArrays(PrimitiveType.Triangles,batch.First,batch.Count);
            GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);return;
        }
        foreach(EtsDrawBatch batch in batches)
        {
            if(batch.Key.TextureIndex>=0&&glEtsTextures.TryGetValue(batch.Key,out int texture)){GL.Uniform1(uUseTexture,1);GL.BindTexture(TextureTarget.Texture2D,texture);if(glEtsTextureTransparency.TryGetValue(batch.Key,out bool transparent)&&transparent){GL.Enable(EnableCap.Blend);GL.BlendFunc(BlendingFactor.SrcAlpha,BlendingFactor.OneMinusSrcAlpha);}else GL.Disable(EnableCap.Blend);}
            else{GL.Uniform1(uUseTexture,0);GL.BindTexture(TextureTarget.Texture2D,0);GL.Disable(EnableCap.Blend);}
            GL.DrawArrays(PrimitiveType.Triangles,batch.First,batch.Count);
        }
        GL.Disable(EnableCap.Blend);GL.BindTexture(TextureTarget.Texture2D,0);
    }
    private void UploadEtsTextures()
    {
        etsTexturesDirty=false;ReleaseEtsTextures();if(etmCatalog==null)return;var tplReader=new RE4_PS2_MOD_WORKSPACE.Core.Textures.TplReader();var effReader=new RE4_PS2_MOD_WORKSPACE.Core.Textures.EffTextureReader();var decoder=new RE4_PS2_MOD_WORKSPACE.Core.Textures.TextureDecoder();
        foreach(var objectPair in etmCatalog.ModelParts)
            foreach(EtmModelPart part in objectPair.Value)
            {
                EtmResource? package=part.Effect??part.TextureFallback;if(package==null)continue;
                int maxTexture=part.Triangles.Max(t=>t.TextureIndex);if(maxTexture<0)continue;
                IReadOnlyList<RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL>? effTextures=null;
                if(part.Effect!=null)try{effTextures=effReader.ReadTextures(part.Effect.Data);}catch{ }
                for(int i=0;i<=maxTexture;i++)try
                {
                    bool useEffect=effTextures!=null&&i<effTextures.Count;
                    EtmResource source=useEffect?part.Effect!:part.TextureFallback!;
                    if(source==null)continue;
                    using var stream=new MemoryStream(source.Data,false);using var br=new BinaryReader(stream);
                    var tpl=useEffect?effTextures![i]:tplReader.ReadTexture(br,i);stream.Position=0;
                    using var bitmap=decoder.Decode(tpl,br);var key=new EtsTextureKey(objectPair.Key,source.FileOrder,i);
                    glEtsTextures[key]=CreateEtsGlTexture(bitmap);glEtsTextureTransparency[key]=BitmapHasTransparency(bitmap);
                }catch{ }
            }
    }
    private static int CreateEtsGlTexture(Bitmap source)
    {
        using var bitmap=new Bitmap(source.Width,source.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using(Graphics g=Graphics.FromImage(bitmap))g.DrawImageUnscaled(source,0,0);
        var rect=new Rectangle(0,0,bitmap.Width,bitmap.Height);var data=bitmap.LockBits(rect,System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        try{int texture=GL.GenTexture();GL.BindTexture(TextureTarget.Texture2D,texture);GL.PixelStore(PixelStoreParameter.UnpackAlignment,4);GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba8,bitmap.Width,bitmap.Height,0,OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,PixelType.UnsignedByte,data.Scan0);GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureWrapS,(int)TextureWrapMode.Repeat);GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureWrapT,(int)TextureWrapMode.Repeat);GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Linear);GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Linear);GL.BindTexture(TextureTarget.Texture2D,0);return texture;}finally{bitmap.UnlockBits(data);}
    }
    private void ReleaseEtsTextures(){foreach(int texture in glEtsTextures.Values)if(texture!=0)GL.DeleteTexture(texture);glEtsTextures.Clear();glEtsTextureTransparency.Clear();}
    private readonly record struct EtsTextureKey(byte ObjectId,int SourceFileOrder,int TextureIndex);
    private readonly record struct EtsDrawBatch(EtsTextureKey Key,int First,int Count);

    private static System.Numerics.Vector3 EtsWorld(EtsEntry e)=>new System.Numerics.Vector3(e.PositionX,e.PositionY,e.PositionZ)*EtsWorldScale;
    private EtsEntry? SelectedEts()=>etsScene?.Entries.FirstOrDefault(e=>e.FileOrder==selectedEtsFileOrder);
    private EtsEntry? PickEtsEntry(Point mouse)
    {
        EtsEntry? hit=null; float best=24f;
        if(etsScene==null)return null;
        foreach(EtsEntry e in etsScene.Entries){if(!TryProjectWorldToScreen(EtsWorld(e),out PointF p))continue;float dx=p.X-mouse.X,dy=p.Y-mouse.Y,d=MathF.Sqrt(dx*dx+dy*dy);if(d<best){best=d;hit=e;}}
        return hit;
    }
    private int PickEtsAxis(Point mouse,EtsEntry e)
    {
        var o=EtsWorld(e); if(!TryProjectWorldToScreen(o,out PointF po))return 0; float best=11f;int axis=0;
        var ends=new[]{o+System.Numerics.Vector3.UnitX*5f,o+System.Numerics.Vector3.UnitY*5f,o+System.Numerics.Vector3.UnitZ*5f};
        for(int i=0;i<3;i++)if(TryProjectWorldToScreen(ends[i],out PointF pe)){float d=DistancePointToSegment(mouse,po,pe);if(d<best){best=d;axis=i+1;}}
        return axis;
    }
    private bool TryBeginEtsDrag(Point mouse)
    {
        if((ModifierKeys&Keys.Control)!=0)return false;
        EtsEntry? e=SelectedEts(); if(e==null)return false;
        int axis=PickEtsAxis(mouse,e); EtsEntry? nearby=PickEtsEntry(mouse);
        if(axis==0&&!ReferenceEquals(nearby,e))return false;
        draggingEts=e;etsDragStart=EtsTransformState.From(e);etsGroupDragStart.Clear();if(etsScene!=null)foreach(var selected in etsScene.Entries.Where(x=>selectedEtsFileOrders.Contains(x.FileOrder)))etsGroupDragStart[selected]=EtsTransformState.From(selected);etsDragMouse=mouse;etsDragWorld=EtsWorld(e);etsDragAxis=axis;
        if(etsDragAxis==0)etsDragAxis=EtsTransformMode==EtsGizmoMode.Move?7:2;
        return true;
    }
    private void UpdateEtsDrag(Point mouse)
    {
        EtsEntry e=draggingEts!; int dx=mouse.X-etsDragMouse.X,dy=mouse.Y-etsDragMouse.Y;
        if(EtsTransformMode==EtsGizmoMode.Move)
        {
            if(etsDragAxis is 1 or 3 or 7 && TryScreenPointOnHorizontalPlane(etsDragMouse,etsDragWorld.Y,out var a)&&TryScreenPointOnHorizontalPlane(mouse,etsDragWorld.Y,out var b))
            {var d=(b-a)/EtsWorldScale;foreach(var pair in etsGroupDragStart){if(etsDragAxis is 1 or 7)pair.Key.PositionX=SnapEts(pair.Value.Px+d.X,10f);if(etsDragAxis is 3 or 7)pair.Key.PositionZ=SnapEts(pair.Value.Pz+d.Z,10f);}}
            else if(etsDragAxis==2)foreach(var pair in etsGroupDragStart)pair.Key.PositionY=SnapEts(pair.Value.Py-dy*25f,10f);
        }
        else
        {float d=dx*(MathF.PI/360f);foreach(var pair in etsGroupDragStart){if(etsDragAxis==1)pair.Key.RotationX=SnapAngle(pair.Value.Rx+d);else if(etsDragAxis==3)pair.Key.RotationZ=SnapAngle(pair.Value.Rz+d);else pair.Key.RotationY=SnapAngle(pair.Value.Ry+d);}}
        etsGpuDirty=true;foreach(var selected in etsGroupDragStart.Keys)EtsEntryEdited?.Invoke(selected);Invalidate();
    }
    private void EndEtsDrag()
    {
        EtsEntry e=draggingEts!;var before=etsGroupDragStart.ToArray();bool changed=before.Any(x=>!x.Value.Equals(EtsTransformState.From(x.Key)));draggingEts=null;etsDragAxis=0;etsGroupDragStart.Clear();
        if(changed){etsUndo.Push(()=>{foreach(var pair in before)pair.Value.Apply(pair.Key);selectedEtsFileOrders.Clear();foreach(var pair in before)selectedEtsFileOrders.Add(pair.Key.FileOrder);selectedEtsFileOrder=e.FileOrder;etsGpuDirty=true;foreach(var pair in before)EtsEntryEdited?.Invoke(pair.Key);EtsEntryClicked?.Invoke(e);EtsSelectionChanged?.Invoke(before.Select(x=>x.Key).ToArray());Invalidate();});while(etsUndo.Count>100){var keep=etsUndo.Reverse().Take(100).Reverse().ToArray();etsUndo.Clear();foreach(var x in keep)etsUndo.Push(x);}}
    }
    private float SnapEts(float v,float step)=>EtsSnapEnabled?MathF.Round(v/step)*step:v;
    private float SnapAngle(float v)=>EtsSnapEnabled?MathF.Round(v/(MathF.PI/36f))*(MathF.PI/36f):v;
    private readonly record struct EtsTransformState(float Px,float Py,float Pz,float Rx,float Ry,float Rz)
    {public static EtsTransformState From(EtsEntry e)=>new(e.PositionX,e.PositionY,e.PositionZ,e.RotationX,e.RotationY,e.RotationZ);public void Apply(EtsEntry e){e.PositionX=Px;e.PositionY=Py;e.PositionZ=Pz;e.RotationX=Rx;e.RotationY=Ry;e.RotationZ=Rz;}}
}

using OpenTK.Graphics.OpenGL4;
using RE4_PS2_MOD_WORKSPACE.Core.Collision;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public enum CollisionRenderStyle { SolidWireframe, Solid, Wireframe }
public enum CollisionVertexMoveMode { Horizontal, Vertical }

public sealed partial class ScenarioViewport
{
    private EsatFile? satCollision;
    private EsatFile? eatCollision;
    private bool collisionGpuDirty;
    private int satCollisionVao, satCollisionVbo, satCollisionVertexCount;
    private int eatCollisionVao, eatCollisionVbo, eatCollisionVertexCount;
    private int selectedCollisionVao, selectedCollisionVbo, selectedCollisionVertexCount;
    private EsatFaceInspection? selectedCollision;
    private int collisionHandleVao, collisionHandleVbo, collisionHandleVertexCount;
    private int selectedCollisionVertexSlot = -1;
    private bool draggingCollisionVertex;
    private NVector3 collisionDragStartPosition;
    private Point collisionDragStartMouse;
    private float collisionVerticalPixelsPerUnit = 1f;
    private CollisionVertexMoveMode collisionMoveMode;
    private bool collisionMoveWholeFace;
    private bool collisionMoveFaceSide;
    private bool collisionMoveObject;
    private Dictionary<int,NVector3> collisionDragStartVertices = new();
    private int[] selectedCollisionRegionVertices = Array.Empty<int>();
    private HashSet<int> selectedCollisionRegionFaces = new();
    private readonly Stack<Action> collisionUndo = new();

    public bool CollisionVisible { get; set; }
    public bool SatCollisionVisible { get; set; } = true;
    public bool EatCollisionVisible { get; set; } = true;
    public bool CollisionFloorVisible { get; set; } = true;
    public bool CollisionSlopeVisible { get; set; } = true;
    public bool CollisionWallVisible { get; set; } = true;
    public int SatCollisionMeshFilter { get; set; } = -1;
    public int EatCollisionMeshFilter { get; set; } = -1;
    public float CollisionOpacity { get; set; } = 0.22f;
    public CollisionRenderStyle CollisionStyle { get; set; } = CollisionRenderStyle.SolidWireframe;
    public event Action<EsatFaceInspection?>? CollisionFaceClicked;
    public event Action<EsatFaceInspection>? CollisionVertexEdited;
    public EsatFaceInspection? SelectedCollisionFace => selectedCollision;
    public EsatFile? SatCollision => satCollision;
    public EsatFile? EatCollision => eatCollision;

    public void SetCollision(EsatFile? sat, EsatFile? eat)
    {
        satCollision = sat;
        eatCollision = eat;
        selectedCollision = null;
        selectedCollisionRegionVertices=Array.Empty<int>();selectedCollisionRegionFaces.Clear();
        collisionGpuDirty = true;
        Invalidate();
    }

    private void UploadCollision()
    {
        collisionGpuDirty = false;
        if (!glReady) return;
        EnsureCollisionBuffer(ref satCollisionVao, ref satCollisionVbo);
        EnsureCollisionBuffer(ref eatCollisionVao, ref eatCollisionVbo);
        EnsureCollisionBuffer(ref selectedCollisionVao, ref selectedCollisionVbo);
        EnsureCollisionBuffer(ref collisionHandleVao, ref collisionHandleVbo);
        UploadEnemyModelBuffer(satCollisionVao, satCollisionVbo, BuildCollisionVertices(satCollision), out satCollisionVertexCount);
        UploadEnemyModelBuffer(eatCollisionVao, eatCollisionVbo, BuildCollisionVertices(eatCollision), out eatCollisionVertexCount);
        UploadEnemyModelBuffer(selectedCollisionVao, selectedCollisionVbo, BuildSelectedCollisionVertices(), out selectedCollisionVertexCount);
        UploadLineBuffer(collisionHandleVao, collisionHandleVbo, BuildCollisionHandle(), out collisionHandleVertexCount);
    }

    private List<float> BuildCollisionVertices(EsatFile? file)
    {
        var output = new List<float>((file?.FaceCount ?? 0) * 24);
        if (file == null) return output;
        for (int meshIndex = 0; meshIndex < file.Meshes.Count; meshIndex++)
        {
            if (file.Kind == EsatKind.Sat && SatCollisionMeshFilter >= 0 && meshIndex != SatCollisionMeshFilter) continue;
            if (file.Kind == EsatKind.Eat && EatCollisionMeshFilter >= 0 && meshIndex != EatCollisionMeshFilter) continue;
            EsatMesh mesh = file.Meshes[meshIndex];
            for (int faceIndex = 0; faceIndex < mesh.Faces.Count; faceIndex++)
            {
                if (!CategoryVisible(GetCategory(mesh, faceIndex))) continue;
                EsatFace face = mesh.Faces[faceIndex];
                NVector3 normal = mesh.Normals[face.Normal];
                WriteCollisionVertex(output, mesh.Positions[face.Vertex0] / 100f, normal);
                WriteCollisionVertex(output, mesh.Positions[face.Vertex1] / 100f, normal);
                WriteCollisionVertex(output, mesh.Positions[face.Vertex2] / 100f, normal);
            }
        }
        return output;
    }

    private List<float> BuildSelectedCollisionVertices()
    {
        var output = new List<float>(24);
        if (selectedCollision == null) return output;
        if (!CategoryVisible(selectedCollision.Category)) return output;
        if (selectedCollision.File.Kind == EsatKind.Sat && (!SatCollisionVisible || (SatCollisionMeshFilter >= 0 && selectedCollision.MeshIndex != SatCollisionMeshFilter))) return output;
        if (selectedCollision.File.Kind == EsatKind.Eat && (!EatCollisionVisible || (EatCollisionMeshFilter >= 0 && selectedCollision.MeshIndex != EatCollisionMeshFilter))) return output;
        EsatMesh m = selectedCollision.Mesh;
        IEnumerable<int> faceIndices=CollisionRegionActive&&selectedCollisionRegionFaces.Count>0?selectedCollisionRegionFaces:new[]{selectedCollision.FaceIndex};
        foreach(int faceIndex in faceIndices)
        {
            EsatFace f=m.Faces[faceIndex];NVector3 n=m.Normals[f.Normal];
            WriteCollisionVertex(output,m.Positions[f.Vertex0]/100f,n);WriteCollisionVertex(output,m.Positions[f.Vertex1]/100f,n);WriteCollisionVertex(output,m.Positions[f.Vertex2]/100f,n);
        }
        return output;
    }

    public void RefreshCollisionDisplay() { collisionGpuDirty = true; Invalidate(); }
    public void SelectCollisionVertex(int slot) { selectedCollisionVertexSlot = slot is >= 0 and <= 2 ? slot : -1; collisionGpuDirty=true; Invalidate(); }
    public void SetCollisionMoveMode(CollisionVertexMoveMode mode) { collisionMoveMode=mode; Invalidate(); }
    public void SetCollisionMoveWholeFace(bool enabled) { collisionMoveWholeFace=enabled; RefreshCollisionRegionSelection(); collisionGpuDirty=true; Invalidate(); }
    public void SetCollisionMoveFaceSide(bool enabled){collisionMoveFaceSide=enabled;RefreshCollisionRegionSelection();collisionGpuDirty=true;Invalidate();}
    public void SetCollisionMoveObject(bool enabled){collisionMoveObject=enabled;RefreshCollisionRegionSelection();collisionGpuDirty=true;Invalidate();}
    private bool CollisionRegionActive=>collisionMoveWholeFace||collisionMoveFaceSide||collisionMoveObject;
    public (int Faces,int Vertices) GetCollisionRegionSelectionSize()=>CollisionRegionActive?(selectedCollisionRegionFaces.Count,selectedCollisionRegionVertices.Length):(selectedCollision==null?0:1,selectedCollision==null?0:3);
    public bool SelectedCollisionIsBoxPrimitive=>collisionMoveObject&&selectedCollisionRegionFaces.Count==12&&selectedCollisionRegionVertices.Length==8;
    public (NVector3 Center,NVector3 Size)? GetSelectedCollisionObjectBounds()
    {
        if(!SelectedCollisionIsBoxPrimitive||selectedCollision==null)return null;var points=selectedCollisionRegionVertices.Select(i=>selectedCollision.Mesh.Positions[i]).ToArray();
        NVector3 min=points.Aggregate(NVector3.Min),max=points.Aggregate(NVector3.Max);return((min+max)/2f,max-min);
    }
    public bool TransformSelectedCollisionObject(float rotateYDegrees=0,bool invert=false,bool remove=false)
    {
        if(!SelectedCollisionIsBoxPrimitive||selectedCollision==null)return false;EsatMesh mesh=selectedCollision.Mesh;int[] indices=selectedCollisionRegionVertices;
        var before=indices.ToDictionary(i=>i,i=>mesh.Positions[i]);NVector3 center=before.Values.Aggregate(NVector3.Zero,(a,b)=>a+b)/before.Count;
        float radians=rotateYDegrees*MathF.PI/180f;var rotation=System.Numerics.Matrix4x4.CreateRotationY(radians);
        foreach(int index in indices){NVector3 local=before[index]-center;if(invert)local.X=-local.X;if(rotateYDegrees!=0)local=NVector3.Transform(local,rotation);mesh.Positions[index]=remove?before[index]+new NVector3(0,-10000f,0):center+local;}
        collisionUndo.Push(()=>{foreach(var item in before)mesh.Positions[item.Key]=item.Value;});collisionGpuDirty=true;CollisionVertexEdited?.Invoke(selectedCollision);Invalidate();return true;
    }
    public bool UndoCollisionEdit()
    {
        if (collisionUndo.Count == 0) return false;
        collisionUndo.Pop()(); collisionGpuDirty=true; if(selectedCollision!=null)CollisionVertexEdited?.Invoke(selectedCollision); Invalidate(); return true;
    }

    public void SetSelectedCollisionVertexPosition(NVector3 worldPosition, bool registerUndo=true)
    {
        if (selectedCollision == null || selectedCollisionVertexSlot < 0) return;
        EsatMesh mesh=selectedCollision.Mesh;
        if(CollisionRegionActive)
        {
            NVector3 delta=(worldPosition-SelectedVertexWorld())*100f;if(delta==NVector3.Zero)return;
            int[] indices=SelectedFaceVertexIndices();var before=indices.ToDictionary(x=>x,x=>mesh.Positions[x]);
            if(registerUndo)collisionUndo.Push(()=>{foreach(var item in before)mesh.Positions[item.Key]=item.Value;});
            foreach(int index in indices)mesh.Positions[index]+=delta;
        }
        else
        {
            int index=SelectedVertexIndex();NVector3 old=mesh.Positions[index];NVector3 raw=worldPosition*100f;if(old==raw)return;
            if(registerUndo)collisionUndo.Push(()=>mesh.Positions[index]=old);mesh.Positions[index]=raw;
        }
        collisionGpuDirty=true; CollisionVertexEdited?.Invoke(selectedCollision); Invalidate();
    }

    private int SelectedVertexIndex() => selectedCollisionVertexSlot switch { 0 => selectedCollision!.Face.Vertex0, 1 => selectedCollision!.Face.Vertex1, _ => selectedCollision!.Face.Vertex2 };
    private int[] SelectedFaceVertexIndices()=>CollisionRegionActive&&selectedCollisionRegionVertices.Length>0?selectedCollisionRegionVertices:new[]{(int)selectedCollision!.Face.Vertex0,(int)selectedCollision.Face.Vertex1,(int)selectedCollision.Face.Vertex2}.Distinct().ToArray();
    private NVector3 SelectedVertexWorld()
    {
        if(!CollisionRegionActive)return selectedCollision!.Mesh.Positions[SelectedVertexIndex()]/100f;
        int[] indices=SelectedFaceVertexIndices();NVector3 sum=NVector3.Zero;foreach(int index in indices)sum+=selectedCollision!.Mesh.Positions[index];return sum/indices.Length/100f;
    }
    private List<float> BuildCollisionHandle()
    {
        var v=new List<float>(72); if(selectedCollision==null||selectedCollisionVertexSlot<0)return v; NVector3 p=SelectedVertexWorld(); float r=Math.Max((scene?.Radius??100f)*0.008f,0.3f);
        void L(NVector3 a,NVector3 b){v.AddRange(new[]{a.X,a.Y,a.Z,0f,0f,0f,b.X,b.Y,b.Z,0f,0f,0f});}
        L(p-NVector3.UnitX*r,p+NVector3.UnitX*r);L(p-NVector3.UnitY*r,p+NVector3.UnitY*r);L(p-NVector3.UnitZ*r,p+NVector3.UnitZ*r); return v;
    }
    private bool CategoryVisible(EsatFaceCategory c) => c switch { EsatFaceCategory.Floor => CollisionFloorVisible, EsatFaceCategory.Slope => CollisionSlopeVisible, _ => CollisionWallVisible };
    private static EsatFaceCategory GetCategory(EsatMesh mesh, int index) => index < mesh.FloorCount ? EsatFaceCategory.Floor : index < mesh.FloorCount + mesh.SlopeCount ? EsatFaceCategory.Slope : EsatFaceCategory.Wall;

    private static void WriteCollisionVertex(List<float> output, NVector3 position, NVector3 normal)
    {
        output.Add(position.X); output.Add(position.Y); output.Add(position.Z);
        output.Add(normal.X); output.Add(normal.Y); output.Add(normal.Z);
        output.Add(0f); output.Add(0f);
    }

    private static void EnsureCollisionBuffer(ref int vao, ref int vbo)
    {
        if (vao != 0) return;
        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();
    }

    private void DrawCollisionGpu()
    {
        if (!CollisionVisible || (satCollisionVertexCount == 0 && eatCollisionVertexCount == 0)) return;
        GL.Disable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Uniform1(uUnlit, 1);
        GL.Uniform1(uUseTexture, 0);

        if (SatCollisionVisible) DrawCollisionLayer(satCollisionVao, satCollisionVertexCount, 1.00f, 0.34f, 0.06f);
        if (EatCollisionVisible) DrawCollisionLayer(eatCollisionVao, eatCollisionVertexCount, 0.04f, 0.76f, 1.00f);
        if (selectedCollisionVertexCount > 0)
        {
            GL.BindVertexArray(selectedCollisionVao); GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); GL.Uniform3(uColor, 1f, 1f, 0.12f); GL.Uniform1(uOpacity, 0.62f); GL.DrawArrays(PrimitiveType.Triangles, 0, selectedCollisionVertexCount);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line); GL.LineWidth(3f); GL.Uniform1(uOpacity, 1f); GL.DrawArrays(PrimitiveType.Triangles, 0, selectedCollisionVertexCount);
        }
        if(collisionHandleVertexCount>0){GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);GL.Disable(EnableCap.DepthTest);GL.BindVertexArray(collisionHandleVao);GL.Uniform3(uColor,1f,0.92f,0.05f);GL.Uniform1(uOpacity,1f);GL.LineWidth(5f);GL.DrawArrays(PrimitiveType.Lines,0,collisionHandleVertexCount);GL.Enable(EnableCap.DepthTest);}

        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        GL.LineWidth(1f);
        GL.Uniform1(uOpacity, 1f);
        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);
        GL.Enable(EnableCap.CullFace);
    }

    private void DrawCollisionLayer(int vao, int count, float r, float g, float b)
    {
        if (count <= 0) return;
        GL.BindVertexArray(vao);
        GL.Uniform3(uColor, r, g, b);
        if (CollisionStyle != CollisionRenderStyle.Wireframe) { GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); GL.Uniform1(uOpacity, CollisionOpacity); GL.DrawArrays(PrimitiveType.Triangles, 0, count); }
        if (CollisionStyle != CollisionRenderStyle.Solid) { GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line); GL.LineWidth(1.25f); GL.Uniform1(uOpacity, Math.Max(0.65f, CollisionOpacity)); GL.DrawArrays(PrimitiveType.Triangles, 0, count); }
    }

    private bool TryPickCollision(Point screen)
    {
        if (!CollisionVisible || !TryBuildPickRay(screen, out NVector3 origin, out NVector3 direction)) return false;
        EsatFaceInspection? best = null; float bestDistance = float.PositiveInfinity;
        PickCollisionFile(satCollision, SatCollisionVisible, origin, direction, ref best, ref bestDistance);
        PickCollisionFile(eatCollision, EatCollisionVisible, origin, direction, ref best, ref bestDistance);
        selectedCollision = best;RefreshCollisionRegionSelection();collisionGpuDirty = true; CollisionFaceClicked?.Invoke(best); Invalidate(); return best != null;
    }

    private void RefreshCollisionRegionSelection()
    {
        selectedCollisionRegionFaces.Clear();selectedCollisionRegionVertices=Array.Empty<int>();
        if(!CollisionRegionActive||selectedCollision==null)return;
        EsatMesh mesh=selectedCollision.Mesh;var facesByVertex=new Dictionary<int,List<int>>();
        for(int fi=0;fi<mesh.Faces.Count;fi++)
        {
            EsatFace f=mesh.Faces[fi];foreach(int vi in new[]{(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2}.Distinct())
            {if(!facesByVertex.TryGetValue(vi,out List<int>? list))facesByVertex[vi]=list=new();list.Add(fi);}
        }
        var vertices=new HashSet<int>();var queue=new Queue<int>();queue.Enqueue(selectedCollision.FaceIndex);selectedCollisionRegionFaces.Add(selectedCollision.FaceIndex);
        NVector3 selectedNormal=NVector3.Normalize(mesh.Normals[selectedCollision.Face.Normal]);
        while(queue.Count>0)
        {
            int current=queue.Dequeue();EsatFace f=mesh.Faces[current];
            foreach(int vi in new[]{(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2}.Distinct())
            {
                vertices.Add(vi);foreach(int adjacent in facesByVertex[vi])
                {
                    if(selectedCollisionRegionFaces.Contains(adjacent))continue;
                    if(collisionMoveFaceSide)
                    {
                        EsatFace other=mesh.Faces[adjacent];NVector3 otherNormal=NVector3.Normalize(mesh.Normals[other.Normal]);
                        int shared=new[]{(int)other.Vertex0,(int)other.Vertex1,(int)other.Vertex2}.Count(x=>new[]{(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2}.Contains(x));
                        if(shared<2||NVector3.Dot(selectedNormal,otherNormal)<0.999f)continue;
                    }
                    else if(collisionMoveObject)
                    {
                        EsatFace other=mesh.Faces[adjacent];
                        if(other.Connectivity!=0xE0||other.Unknown!=0||other.Blue!=0||other.Green!=0||other.Red!=0)continue;
                    }
                    selectedCollisionRegionFaces.Add(adjacent);queue.Enqueue(adjacent);
                }
            }
        }
        selectedCollisionRegionVertices=vertices.ToArray();
        if(collisionMoveObject&&(selectedCollisionRegionFaces.Count!=12||selectedCollisionRegionVertices.Length!=8)){selectedCollisionRegionFaces.Clear();selectedCollisionRegionVertices=Array.Empty<int>();}
    }

    private bool TryBeginCollisionVertexDrag(Point screen)
    {
        if(!CollisionVisible||selectedCollision==null||selectedCollisionVertexSlot<0||!TryProjectWorldToScreen(SelectedVertexWorld(),out PointF p))return false;
        float dx=p.X-screen.X,dy=p.Y-screen.Y;if(dx*dx+dy*dy>18f*18f)return false;
        draggingCollisionVertex=true;collisionDragStartMouse=screen;collisionDragStartPosition=SelectedVertexWorld();
        collisionDragStartVertices=SelectedFaceVertexIndices().ToDictionary(x=>x,x=>selectedCollision.Mesh.Positions[x]);
        NVector3 a=collisionDragStartPosition,b=a+NVector3.UnitY;if(TryProjectWorldToScreen(a,out PointF pa)&&TryProjectWorldToScreen(b,out PointF pb))collisionVerticalPixelsPerUnit=Math.Max(0.05f,MathF.Abs(pb.Y-pa.Y));
        return true;
    }
    private void UpdateCollisionVertexDrag(Point screen)
    {
        if(!draggingCollisionVertex||selectedCollision==null)return;NVector3 next=collisionDragStartPosition;
        if(collisionMoveMode==CollisionVertexMoveMode.Horizontal&&TryScreenPointOnHorizontalPlane(collisionDragStartMouse,collisionDragStartPosition.Y,out NVector3 a)&&TryScreenPointOnHorizontalPlane(screen,collisionDragStartPosition.Y,out NVector3 b))next+=new NVector3(b.X-a.X,0,b.Z-a.Z);
        else if(collisionMoveMode==CollisionVertexMoveMode.Vertical)next.Y+=-(screen.Y-collisionDragStartMouse.Y)/collisionVerticalPixelsPerUnit;
        SetSelectedCollisionVertexPosition(next,false);
    }
    private void EndCollisionVertexDrag()
    {
        if(!draggingCollisionVertex||selectedCollision==null)return;draggingCollisionVertex=false;EsatMesh mesh=selectedCollision.Mesh;
        var before=new Dictionary<int,NVector3>(collisionDragStartVertices);bool changed=before.Any(x=>mesh.Positions[x.Key]!=x.Value);
        if(changed)collisionUndo.Push(()=>{foreach(var item in before)mesh.Positions[item.Key]=item.Value;});collisionDragStartVertices.Clear();
    }

    private void PickCollisionFile(EsatFile? file, bool visible, NVector3 origin, NVector3 direction, ref EsatFaceInspection? best, ref float bestDistance)
    {
        if (!visible || file == null) return;
        for (int mi = 0; mi < file.Meshes.Count; mi++)
        {
            if (file.Kind == EsatKind.Sat && SatCollisionMeshFilter >= 0 && mi != SatCollisionMeshFilter) continue;
            if (file.Kind == EsatKind.Eat && EatCollisionMeshFilter >= 0 && mi != EatCollisionMeshFilter) continue;
            EsatMesh mesh = file.Meshes[mi];
            for (int fi = 0; fi < mesh.Faces.Count; fi++)
            {
                EsatFaceCategory category = GetCategory(mesh, fi); if (!CategoryVisible(category)) continue;
                EsatFace f = mesh.Faces[fi]; NVector3 a = mesh.Positions[f.Vertex0] / 100f, b = mesh.Positions[f.Vertex1] / 100f, c = mesh.Positions[f.Vertex2] / 100f;
                if (RayTriangle(origin, direction, a, b, c, out float distance) && distance >= 0 && distance < bestDistance) { bestDistance = distance; best = new EsatFaceInspection { File=file, Mesh=mesh, Face=f, MeshIndex=mi, FaceIndex=fi, Category=category }; }
            }
        }
    }

    private void DisposeCollisionGpu()
    {
        if (satCollisionVbo != 0) GL.DeleteBuffer(satCollisionVbo);
        if (satCollisionVao != 0) GL.DeleteVertexArray(satCollisionVao);
        if (eatCollisionVbo != 0) GL.DeleteBuffer(eatCollisionVbo);
        if (eatCollisionVao != 0) GL.DeleteVertexArray(eatCollisionVao);
        if (selectedCollisionVbo != 0) GL.DeleteBuffer(selectedCollisionVbo);
        if (selectedCollisionVao != 0) GL.DeleteVertexArray(selectedCollisionVao);
        if (collisionHandleVbo != 0) GL.DeleteBuffer(collisionHandleVbo);
        if (collisionHandleVao != 0) GL.DeleteVertexArray(collisionHandleVao);
    }
}

using OpenTK.Graphics.OpenGL4;
using RE4_PS2_MOD_WORKSPACE.Core.Collision;
using NVector3 = System.Numerics.Vector3;

namespace RE4_PS2_MOD_WORKSPACE;

public enum CollisionRenderStyle { SolidWireframe, Solid, Wireframe }
public enum CollisionVertexMoveMode { Horizontal, Vertical }
public enum CollisionGizmoMode { Move, Rotate }

public sealed partial class ScenarioViewport
{
    private EsatFile? satCollision;
    private EsatFile? eatCollision;
    private bool collisionGpuDirty;
    private int satCollisionVao, satCollisionVbo, satCollisionVertexCount;
    private int eatCollisionVao, eatCollisionVbo, eatCollisionVertexCount;
    private int disabledCollisionVao,disabledCollisionVbo,disabledCollisionVertexCount;
    private int selectedCollisionVao, selectedCollisionVbo, selectedCollisionVertexCount;
    private int markedCollisionEdgeVao,markedCollisionEdgeVbo,markedCollisionEdgeVertexCount;private readonly List<(int FaceIndex,int EdgeSlot)> markedCollisionEdges=new();
    private EsatFaceInspection? selectedCollision;
    private readonly int[] collisionHandleVaos=new int[3],collisionHandleVbos=new int[3],collisionHandleVertexCounts=new int[3];
    private int selectedCollisionVertexSlot = -1;
    private bool draggingCollisionVertex;
    private int collisionDragAxis;
    private NVector3 collisionDragWorldAxis=NVector3.UnitX;
    private System.Numerics.Vector2 collisionDragScreenAxis;
    private NVector3 collisionDragStartPosition;
    private Point collisionDragStartMouse;
    private float collisionVerticalPixelsPerUnit = 1f;
    private CollisionVertexMoveMode collisionMoveMode;
    private bool collisionMoveWholeFace;
    private bool collisionMoveFaceSide;
    private bool collisionMoveObject;
    private Dictionary<int,NVector3> collisionDragStartVertices = new();
    private Dictionary<int,NVector3> collisionDragStartNormals = new();
    private int[] selectedCollisionRegionVertices = Array.Empty<int>();
    private HashSet<int> selectedCollisionRegionFaces = new();
    private readonly HashSet<int> selectedCollisionManualFaces = new();
    private readonly Stack<Action> collisionUndo = new();

    public bool CollisionVisible { get; set; }
    public bool ShowDisabledCollision { get; set; }
    public bool CollisionEdgeEditingEnabled { get; set; }
    public CollisionGizmoMode CollisionTransformMode { get; private set; }
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
    public event Action<EsatFaceInspection?,int,bool>? CollisionEdgeClicked;
    public event Action<EsatFaceInspection>? CollisionVertexEdited;
    public EsatFaceInspection? SelectedCollisionFace => selectedCollision;
    public EsatFile? SatCollision => satCollision;
    public EsatFile? EatCollision => eatCollision;

    public void SetCollision(EsatFile? sat, EsatFile? eat)
    {
        satCollision = sat;
        eatCollision = eat;
        selectedCollision = null;
        selectedCollisionRegionVertices=Array.Empty<int>();selectedCollisionRegionFaces.Clear();selectedCollisionManualFaces.Clear();markedCollisionEdges.Clear();
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
        EnsureCollisionBuffer(ref markedCollisionEdgeVao,ref markedCollisionEdgeVbo);
        EnsureCollisionBuffer(ref disabledCollisionVao,ref disabledCollisionVbo);
        for(int i=0;i<3;i++)EnsureCollisionBuffer(ref collisionHandleVaos[i],ref collisionHandleVbos[i]);
        UploadEnemyModelBuffer(satCollisionVao, satCollisionVbo, BuildCollisionVertices(satCollision), out satCollisionVertexCount);
        UploadEnemyModelBuffer(eatCollisionVao, eatCollisionVbo, BuildCollisionVertices(eatCollision), out eatCollisionVertexCount);
        UploadEnemyModelBuffer(disabledCollisionVao,disabledCollisionVbo,BuildDisabledCollisionVertices(),out disabledCollisionVertexCount);
        UploadEnemyModelBuffer(selectedCollisionVao, selectedCollisionVbo, BuildSelectedCollisionVertices(), out selectedCollisionVertexCount);
        UploadLineBuffer(markedCollisionEdgeVao,markedCollisionEdgeVbo,BuildMarkedCollisionEdges(),out markedCollisionEdgeVertexCount);
        UploadCollisionHandles();
    }

    private void UploadCollisionHandles()
    {
        if(!glReady)return;for(int i=0;i<3;i++)EnsureCollisionBuffer(ref collisionHandleVaos[i],ref collisionHandleVbos[i]);
        List<float>[] handles=BuildCollisionHandle();for(int i=0;i<3;i++)UploadLineBuffer(collisionHandleVaos[i],collisionHandleVbos[i],handles[i],out collisionHandleVertexCounts[i]);
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
                EsatFace face = mesh.Faces[faceIndex];if(Ps2EsatPrimitiveWriter.IsFaceDisabled(file.Kind,face))continue;
                NVector3 normal = mesh.Normals[face.Normal];
                WriteCollisionVertex(output, mesh.Positions[face.Vertex0] / 100f, normal);
                WriteCollisionVertex(output, mesh.Positions[face.Vertex1] / 100f, normal);
                WriteCollisionVertex(output, mesh.Positions[face.Vertex2] / 100f, normal);
            }
        }
        return output;
    }

    private List<float> BuildDisabledCollisionVertices()
    {
        var output=new List<float>();if(!ShowDisabledCollision)return output;Add(satCollision,SatCollisionVisible,SatCollisionMeshFilter);Add(eatCollision,EatCollisionVisible,EatCollisionMeshFilter);return output;
        void Add(EsatFile? file,bool visible,int filter){if(file==null||!visible)return;for(int mi=0;mi<file.Meshes.Count;mi++){if(filter>=0&&mi!=filter)continue;EsatMesh mesh=file.Meshes[mi];for(int fi=0;fi<mesh.Faces.Count;fi++){EsatFace f=mesh.Faces[fi];if(!Ps2EsatPrimitiveWriter.IsFaceDisabled(file.Kind,f)||!CategoryVisible(GetCategory(mesh,fi)))continue;NVector3 n=mesh.Normals[f.Normal];WriteCollisionVertex(output,mesh.Positions[f.Vertex0]/100f,n);WriteCollisionVertex(output,mesh.Positions[f.Vertex1]/100f,n);WriteCollisionVertex(output,mesh.Positions[f.Vertex2]/100f,n);}}}
    }

    private List<float> BuildSelectedCollisionVertices()
    {
        var output = new List<float>(24);
        if (selectedCollision == null) return output;
        if (!CategoryVisible(selectedCollision.Category)) return output;
        if (selectedCollision.File.Kind == EsatKind.Sat && (!SatCollisionVisible || (SatCollisionMeshFilter >= 0 && selectedCollision.MeshIndex != SatCollisionMeshFilter))) return output;
        if (selectedCollision.File.Kind == EsatKind.Eat && (!EatCollisionVisible || (EatCollisionMeshFilter >= 0 && selectedCollision.MeshIndex != EatCollisionMeshFilter))) return output;
        EsatMesh m = selectedCollision.Mesh;
        IEnumerable<int> faceIndices=CollisionRegionActive&&selectedCollisionRegionFaces.Count>0?selectedCollisionRegionFaces:selectedCollisionManualFaces.Count>0?selectedCollisionManualFaces:new[]{selectedCollision.FaceIndex};
        foreach(int faceIndex in faceIndices)
        {
            EsatFace f=m.Faces[faceIndex];NVector3 n=m.Normals[f.Normal];
            WriteCollisionVertex(output,m.Positions[f.Vertex0]/100f,n);WriteCollisionVertex(output,m.Positions[f.Vertex1]/100f,n);WriteCollisionVertex(output,m.Positions[f.Vertex2]/100f,n);
        }
        return output;
    }

    public NVector3 GetCollisionCreationPosition(float ahead=10f)=>(cameraPosition+GetForward()*ahead)*100f;
    public void SetCollisionMarkedEdges(IEnumerable<(int FaceIndex,int EdgeSlot)> edges)
    {
        markedCollisionEdges.Clear();markedCollisionEdges.AddRange(edges);collisionGpuDirty=true;Invalidate();
    }
    private bool HasMarkedCollisionEdges=>selectedCollision!=null&&markedCollisionEdges.Count>0;
    private int[] MarkedCollisionVertexIndices()
    {
        if(selectedCollision==null)return Array.Empty<int>();var vertices=new HashSet<int>();foreach(var item in markedCollisionEdges){if(item.FaceIndex<0||item.FaceIndex>=selectedCollision.Mesh.Faces.Count)continue;EsatFace face=selectedCollision.Mesh.Faces[item.FaceIndex];int[] ids={(int)face.Vertex0,(int)face.Vertex1,(int)face.Vertex2};int edge=Math.Clamp(item.EdgeSlot,0,2);vertices.Add(ids[edge]);vertices.Add(ids[(edge+1)%3]);}return vertices.ToArray();
    }
    private List<float> BuildMarkedCollisionEdges()
    {
        var output=new List<float>();if(selectedCollision==null)return output;foreach(var item in markedCollisionEdges){if(item.FaceIndex<0||item.FaceIndex>=selectedCollision.Mesh.Faces.Count)continue;EsatFace f=selectedCollision.Mesh.Faces[item.FaceIndex];int[] ids={(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2};NVector3 a=selectedCollision.Mesh.Positions[ids[Math.Clamp(item.EdgeSlot,0,2)]]/100f,b=selectedCollision.Mesh.Positions[ids[(Math.Clamp(item.EdgeSlot,0,2)+1)%3]]/100f;AddCamLine(output,a,b);}return output;
    }
    public void RefreshCollisionDisplay() { collisionGpuDirty = true; Invalidate(); }
    public void SelectCollisionVertex(int slot) { selectedCollisionVertexSlot = slot is >= 0 and <= 2 ? slot : -1; collisionGpuDirty=true; Invalidate(); }
    public void SetCollisionMoveMode(CollisionVertexMoveMode mode) { collisionMoveMode=mode; Invalidate(); }
    public void SetCollisionGizmoMode(CollisionGizmoMode mode){CollisionTransformMode=mode;collisionGpuDirty=true;Invalidate();}
    public void SetCollisionMoveWholeFace(bool enabled) { collisionMoveWholeFace=enabled; RefreshCollisionRegionSelection(); collisionGpuDirty=true; Invalidate(); }
    public void SetCollisionMoveFaceSide(bool enabled){collisionMoveFaceSide=enabled;RefreshCollisionRegionSelection();collisionGpuDirty=true;Invalidate();}
    public void SetCollisionMoveObject(bool enabled){collisionMoveObject=enabled;RefreshCollisionRegionSelection();collisionGpuDirty=true;Invalidate();}
    private bool CollisionRegionActive=>collisionMoveWholeFace||collisionMoveFaceSide||collisionMoveObject;
    public (int Faces,int Vertices) GetCollisionRegionSelectionSize()
    {
        if(CollisionRegionActive)return(selectedCollisionRegionFaces.Count,selectedCollisionRegionVertices.Length);
        if(selectedCollision==null)return(0,0);
        IEnumerable<int> faces=selectedCollisionManualFaces.Count>0?selectedCollisionManualFaces:new[]{selectedCollision.FaceIndex};
        int vertices=faces.SelectMany(i=>{EsatFace f=selectedCollision.Mesh.Faces[i];return new[]{(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2};}).Distinct().Count();
        return(faces.Count(),vertices);
    }
    public bool SelectedCollisionIsBoxPrimitive=>collisionMoveObject&&IsRecognizedCollisionPrimitive(selectedCollisionRegionFaces.Count,selectedCollisionRegionVertices.Length);
    private static bool IsRecognizedCollisionPrimitive(int faces,int vertices)=>(faces==12&&vertices==8)||(faces==8&&vertices==6);
    public (NVector3 Center,NVector3 Size)? GetSelectedCollisionObjectBounds()
    {
        if(!SelectedCollisionIsBoxPrimitive||selectedCollision==null)return null;var points=selectedCollisionRegionVertices.Select(i=>selectedCollision.Mesh.Positions[i]).ToArray();
        NVector3 min=points.Aggregate(NVector3.Min),max=points.Aggregate(NVector3.Max);return((min+max)/2f,max-min);
    }
    public bool TransformSelectedCollisionObject(float rotateYDegrees=0,bool invert=false,bool remove=false)
    {
        if(!SelectedCollisionIsBoxPrimitive||selectedCollision==null)return false;EsatMesh mesh=selectedCollision.Mesh;int[] indices=selectedCollisionRegionVertices;
        var before=indices.ToDictionary(i=>i,i=>mesh.Positions[i]);var beforeNormals=selectedCollisionRegionFaces.Select(i=>(int)mesh.Faces[i].Normal).Distinct().ToDictionary(i=>i,i=>mesh.Normals[i]);NVector3 center=before.Values.Aggregate(NVector3.Zero,(a,b)=>a+b)/before.Count;
        float radians=rotateYDegrees*MathF.PI/180f;var rotation=System.Numerics.Matrix4x4.CreateRotationY(radians);
        foreach(int index in indices){NVector3 local=before[index]-center;if(invert)local.X=-local.X;if(rotateYDegrees!=0)local=NVector3.Transform(local,rotation);mesh.Positions[index]=remove?before[index]+new NVector3(0,-10000f,0):center+local;}if(rotateYDegrees!=0)foreach(var item in beforeNormals)mesh.Normals[item.Key]=NVector3.Normalize(NVector3.TransformNormal(item.Value,rotation));
        collisionUndo.Push(()=>{foreach(var item in before)mesh.Positions[item.Key]=item.Value;foreach(var item in beforeNormals)mesh.Normals[item.Key]=item.Value;});collisionGpuDirty=true;CollisionVertexEdited?.Invoke(selectedCollision);Invalidate();return true;
    }
    public bool UndoCollisionEdit()
    {
        if (collisionUndo.Count == 0) return false;
        collisionUndo.Pop()(); collisionGpuDirty=true; if(selectedCollision!=null)CollisionVertexEdited?.Invoke(selectedCollision); Invalidate(); return true;
    }

    public int DisableSelectedCollisionFaces()
    {
        if(selectedCollision==null)return 0;EsatMesh mesh=selectedCollision.Mesh;EsatKind kind=selectedCollision.File.Kind;IEnumerable<int> selection=ResolveCollisionFacesForDeletion();int[] indices=selection.Distinct().Where(x=>x>=0&&x<mesh.Faces.Count&&!Ps2EsatPrimitiveWriter.IsFaceDisabled(kind,mesh.Faces[x])).ToArray();if(indices.Length==0)return 0;var before=indices.ToDictionary(x=>x,x=>mesh.Faces[x]);foreach(int index in indices)mesh.Faces[index]=Ps2EsatPrimitiveWriter.DisableFaceFlags(kind,mesh.Faces[index]);void RefreshSelected(){if(selectedCollision!=null&&selectedCollision.FaceIndex>=0&&selectedCollision.FaceIndex<mesh.Faces.Count)selectedCollision.Face=mesh.Faces[selectedCollision.FaceIndex];}RefreshSelected();collisionUndo.Push(()=>{foreach(var item in before)mesh.Faces[item.Key]=item.Value;});markedCollisionEdges.Clear();selectedCollision=null;selectedCollisionManualFaces.Clear();selectedCollisionRegionFaces.Clear();selectedCollisionRegionVertices=Array.Empty<int>();selectedCollisionVertexSlot=-1;collisionGpuDirty=true;CollisionFaceClicked?.Invoke(null);Invalidate();return indices.Length;
    }
    private IEnumerable<int> ResolveCollisionFacesForDeletion()
    {
        if(CollisionRegionActive&&selectedCollisionRegionFaces.Count>0)return selectedCollisionRegionFaces;
        if(selectedCollisionManualFaces.Count>1)return selectedCollisionManualFaces;
        int[] edgeFaces=markedCollisionEdges.Select(x=>x.FaceIndex).Distinct().ToArray();
        if(edgeFaces.Length>1)return edgeFaces;
        if(selectedCollisionManualFaces.Count>0)return selectedCollisionManualFaces;
        if(edgeFaces.Length>0)return edgeFaces;
        return selectedCollision==null?Array.Empty<int>():new[]{selectedCollision.FaceIndex};
    }
    public void SetSelectedCollisionVertexPosition(NVector3 worldPosition, bool registerUndo=true)
    {
        if (selectedCollision == null || selectedCollisionVertexSlot < 0) return;
        EsatMesh mesh=selectedCollision.Mesh;
        if(CollisionRegionActive||HasMarkedCollisionEdges)
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
    private int[] SelectedFaceVertexIndices()=>HasMarkedCollisionEdges?MarkedCollisionVertexIndices():CollisionRegionActive&&selectedCollisionRegionVertices.Length>0?selectedCollisionRegionVertices:new[]{(int)selectedCollision!.Face.Vertex0,(int)selectedCollision.Face.Vertex1,(int)selectedCollision.Face.Vertex2}.Distinct().ToArray();
    private NVector3 SelectedVertexWorld()
    {
        if(!CollisionRegionActive&&!HasMarkedCollisionEdges)return selectedCollision!.Mesh.Positions[SelectedVertexIndex()]/100f;
        int[] indices=SelectedFaceVertexIndices();if(collisionMoveObject&&indices.Length>0){NVector3 min=indices.Select(i=>selectedCollision!.Mesh.Positions[i]).Aggregate(NVector3.Min),max=indices.Select(i=>selectedCollision!.Mesh.Positions[i]).Aggregate(NVector3.Max);return(min+max)/200f;}NVector3 sum=NVector3.Zero;foreach(int index in indices)sum+=selectedCollision!.Mesh.Positions[index];return sum/indices.Length/100f;
    }
    private List<float>[] BuildCollisionHandle()
    {
        var axes=new[]{new List<float>(),new List<float>(),new List<float>()};if(selectedCollision==null||(selectedCollisionVertexSlot<0&&!HasMarkedCollisionEdges))return axes;
        NVector3 p=SelectedVertexWorld();float length=CamScreenSize(p,72f);NVector3[] directions=CollisionGizmoAxes();if(CollisionTransformMode==CollisionGizmoMode.Rotate){const int segments=64;for(int ring=0;ring<3;ring++){NVector3 u=directions[(ring+1)%3],v=directions[(ring+2)%3];for(int i=0;i<segments;i++){float a=MathF.Tau*i/segments,b=MathF.Tau*(i+1)/segments;NVector3 x=p+(u*MathF.Cos(a)+v*MathF.Sin(a))*length,y=p+(u*MathF.Cos(b)+v*MathF.Sin(b))*length;AddCamLine(axes[ring],x,y);}}}else for(int i=0;i<3;i++)AddCamArrow(axes[i],p,directions[i],length);return axes;
    }
    private NVector3[] CollisionGizmoAxes()
    {
        NVector3[] world={NVector3.UnitX,NVector3.UnitY,NVector3.UnitZ};if(selectedCollision==null||(!CollisionRegionActive&&!HasMarkedCollisionEdges))return world;if(collisionMoveObject&&selectedCollisionRegionFaces.Count>0)return CollisionObjectGizmoAxes();
        EsatFace f=selectedCollision.Face;NVector3 a=selectedCollision.Mesh.Positions[f.Vertex0],b=selectedCollision.Mesh.Positions[f.Vertex1],c=selectedCollision.Mesh.Positions[f.Vertex2];NVector3[] edges={b-a,c-b,a-c};
        for(int i=0;i<3;i++)if(edges[i].LengthSquared()<0.000001f)return world;else edges[i]=NVector3.Normalize(edges[i]);
        int first=0,second=1;float perpendicular=MathF.Abs(NVector3.Dot(edges[0],edges[1]));for(int i=0;i<3;i++)for(int j=i+1;j<3;j++){float score=MathF.Abs(NVector3.Dot(edges[i],edges[j]));if(score<perpendicular){perpendicular=score;first=i;second=j;}}
        NVector3 x=edges[first],z=NVector3.Cross(x,edges[second]);if(z.LengthSquared()<0.000001f)return world;z=NVector3.Normalize(z);NVector3 y=NVector3.Normalize(NVector3.Cross(z,x));if(NVector3.Dot(y,edges[second])<0)y=-y;NVector3[] local={x,y,z};
        int[][] permutations={new[]{0,1,2},new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}};int[] best=permutations[0];float bestScore=float.NegativeInfinity;foreach(int[] permutation in permutations){float score=0;for(int i=0;i<3;i++)score+=MathF.Abs(NVector3.Dot(world[i],local[permutation[i]]));if(score>bestScore){bestScore=score;best=permutation;}}
        var result=new NVector3[3];for(int i=0;i<3;i++){result[i]=local[best[i]];if(NVector3.Dot(result[i],world[i])<0)result[i]=-result[i];}return result;
    }
    private NVector3[] CollisionObjectGizmoAxes()
    {
        NVector3[] world={NVector3.UnitX,NVector3.UnitY,NVector3.UnitZ};if(selectedCollision==null)return world;EsatMesh mesh=selectedCollision.Mesh;var faceSet=selectedCollisionRegionFaces;var floorNormals=faceSet.Where(i=>GetCategory(mesh,i)==EsatFaceCategory.Floor).Select(i=>mesh.Normals[mesh.Faces[i].Normal]).Where(n=>n.LengthSquared()>0.000001f).ToArray();if(floorNormals.Length==0)return world;NVector3 y=floorNormals.Aggregate(NVector3.Zero,(a,b)=>a+b);if(y.LengthSquared()<0.000001f)y=floorNormals[0];y=NVector3.Normalize(y);if(NVector3.Dot(y,NVector3.UnitY)<0)y=-y;
        var pairs=new HashSet<(int,int)>();foreach(int fi in faceSet){EsatFace f=mesh.Faces[fi];int[] v={(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2};for(int i=0;i<3;i++){int a=Math.Min(v[i],v[(i+1)%3]),b=Math.Max(v[i],v[(i+1)%3]);pairs.Add((a,b));}}
        var clusters=new List<(NVector3 Direction,int Count,float Length)>();foreach(var pair in pairs){NVector3 edge=mesh.Positions[pair.Item2]-mesh.Positions[pair.Item1];float length=edge.Length();if(length<0.001f)continue;NVector3 direction=edge/length;if(MathF.Abs(NVector3.Dot(direction,y))>.01f)continue;int found=clusters.FindIndex(x=>MathF.Abs(NVector3.Dot(x.Direction,direction))>.999f);if(found<0)clusters.Add((direction,1,length));else{var old=clusters[found];clusters[found]=(old.Direction,old.Count+1,old.Length+length);}}if(clusters.Count==0)return world;var best=clusters.OrderByDescending(x=>x.Count).ThenByDescending(x=>MathF.Abs(NVector3.Dot(x.Direction,NVector3.UnitX))).ThenByDescending(x=>x.Length).First();NVector3 x=best.Direction-y*NVector3.Dot(best.Direction,y);if(x.LengthSquared()<0.000001f)return world;x=NVector3.Normalize(x);if(NVector3.Dot(x,NVector3.UnitX)<0)x=-x;NVector3 z=NVector3.Normalize(NVector3.Cross(x,y));x=NVector3.Normalize(NVector3.Cross(y,z));return new[]{x,y,z};
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
        if (EatCollisionVisible) DrawCollisionLayer(eatCollisionVao, eatCollisionVertexCount, 0.04f, 0.76f, 1.00f);if(ShowDisabledCollision)DrawCollisionLayer(disabledCollisionVao,disabledCollisionVertexCount,1f,.05f,.72f);
        if (selectedCollisionVertexCount > 0)
        {
            GL.BindVertexArray(selectedCollisionVao); GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); GL.Uniform3(uColor, 1f, 1f, 0.12f); GL.Uniform1(uOpacity, 0.62f); GL.DrawArrays(PrimitiveType.Triangles, 0, selectedCollisionVertexCount);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line); GL.LineWidth(3f); GL.Uniform1(uOpacity, 1f); GL.DrawArrays(PrimitiveType.Triangles, 0, selectedCollisionVertexCount);
        }
        if(markedCollisionEdgeVertexCount>0){GL.Disable(EnableCap.DepthTest);GL.BindVertexArray(markedCollisionEdgeVao);GL.Uniform3(uColor,1f,.08f,.78f);GL.Uniform1(uOpacity,1f);GL.LineWidth(9f);GL.DrawArrays(PrimitiveType.Lines,0,markedCollisionEdgeVertexCount);GL.Enable(EnableCap.DepthTest);}
        GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);GL.Disable(EnableCap.DepthTest);GL.Uniform1(uOpacity,1f);var axisColors=new[]{(1f,.15f,.12f),(.2f,.95f,.25f),(.12f,.48f,1f)};for(int i=0;i<3;i++){var c=axisColors[i];if(collisionDragAxis==i+1)GL.Uniform3(uColor,1f,.92f,.18f);else GL.Uniform3(uColor,c.Item1,c.Item2,c.Item3);GL.BindVertexArray(collisionHandleVaos[i]);GL.LineWidth(collisionDragAxis==i+1?9f:6f);GL.DrawArrays(PrimitiveType.Lines,0,collisionHandleVertexCounts[i]);}GL.Enable(EnableCap.DepthTest);

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
        bool additive=(ModifierKeys&Keys.Control)!=0;
        if(additive&&best!=null&&selectedCollision!=null&&ReferenceEquals(best.File,selectedCollision.File)&&best.MeshIndex==selectedCollision.MeshIndex)
        {
            if(!selectedCollisionManualFaces.Add(best.FaceIndex))selectedCollisionManualFaces.Remove(best.FaceIndex);
            if(selectedCollisionManualFaces.Count==0)selectedCollision=null;
            else if(!selectedCollisionManualFaces.Contains(selectedCollision.FaceIndex))selectedCollision=CreateCollisionInspection(best.File,best.Mesh,best.MeshIndex,selectedCollisionManualFaces.Last());
            else if(selectedCollisionManualFaces.Contains(best.FaceIndex))selectedCollision=best;
        }
        else
        {
            selectedCollisionManualFaces.Clear();
            if(best!=null)selectedCollisionManualFaces.Add(best.FaceIndex);
            selectedCollision=best;
        }
        RefreshCollisionRegionSelection();collisionGpuDirty = true; CollisionFaceClicked?.Invoke(selectedCollision);if(CollisionEdgeEditingEnabled){if(best!=null&&TryPickCollisionEdge(best,screen,out int edgeSlot))CollisionEdgeClicked?.Invoke(best,edgeSlot,(ModifierKeys&(Keys.Control|Keys.Shift))!=0);else CollisionEdgeClicked?.Invoke(best,-1,false);} Invalidate(); return best != null;
    }

    private static EsatFaceInspection CreateCollisionInspection(EsatFile file,EsatMesh mesh,int meshIndex,int faceIndex)=>new(){File=file,Mesh=mesh,Face=mesh.Faces[faceIndex],MeshIndex=meshIndex,FaceIndex=faceIndex,Category=GetCategory(mesh,faceIndex)};

    private bool TryPickCollisionEdge(EsatFaceInspection face,Point screen,out int edgeSlot)
    {
        edgeSlot=-1;EsatFace f=face.Face;int[] ids={(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2};float best=14f;for(int i=0;i<3;i++){NVector3 a=face.Mesh.Positions[ids[i]]/100f,b=face.Mesh.Positions[ids[(i+1)%3]]/100f;if(!TryProjectWorldToScreen(a,out PointF pa)||!TryProjectWorldToScreen(b,out PointF pb))continue;float distance=DistancePointToSegment(screen,pa,pb);if(distance<best){best=distance;edgeSlot=i;}}return edgeSlot>=0;
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
        var vertices=new HashSet<int>();if(collisionMoveWholeFace){IEnumerable<int> selected=selectedCollisionManualFaces.Count>0?selectedCollisionManualFaces:new[]{selectedCollision.FaceIndex};foreach(int faceIndex in selected.Where(x=>x>=0&&x<mesh.Faces.Count)){EsatFace face=mesh.Faces[faceIndex];selectedCollisionRegionFaces.Add(faceIndex);vertices.Add((int)face.Vertex0);vertices.Add((int)face.Vertex1);vertices.Add((int)face.Vertex2);}selectedCollisionRegionVertices=vertices.ToArray();return;}var queue=new Queue<int>();queue.Enqueue(selectedCollision.FaceIndex);selectedCollisionRegionFaces.Add(selectedCollision.FaceIndex);
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
                        // Uma primitiva continua sendo o mesmo objeto após editar flags ou desativar faces.
                        // Vértices próprios delimitam o componente conectado com segurança.
                    }
                    selectedCollisionRegionFaces.Add(adjacent);queue.Enqueue(adjacent);
                }
            }
        }
        selectedCollisionRegionVertices=vertices.ToArray();
        if(collisionMoveObject&&!IsRecognizedCollisionPrimitive(selectedCollisionRegionFaces.Count,selectedCollisionRegionVertices.Length)){selectedCollisionRegionFaces.Clear();selectedCollisionRegionVertices=Array.Empty<int>();}
    }

    private bool TryBeginCollisionVertexDrag(Point screen)
    {
        if(!CollisionVisible||selectedCollision==null||(selectedCollisionVertexSlot<0&&!HasMarkedCollisionEdges))return false;
        NVector3 p=SelectedVertexWorld();float length=CamScreenSize(p,72f);if(!TryProjectWorldToScreen(p,out PointF origin))return false;float best=12f;collisionDragAxis=0;NVector3[] directions=CollisionGizmoAxes();if(CollisionTransformMode==CollisionGizmoMode.Rotate){const int segments=64;for(int ring=0;ring<3;ring++){NVector3 u=directions[(ring+1)%3],v=directions[(ring+2)%3];PointF? previous=null;for(int i=0;i<=segments;i++){float angle=MathF.Tau*i/segments;NVector3 world=p+(u*MathF.Cos(angle)+v*MathF.Sin(angle))*length;if(!TryProjectWorldToScreen(world,out PointF point)){previous=null;continue;}if(previous.HasValue){float d=DistancePointToSegment(screen,previous.Value,point);if(d<best){best=d;collisionDragAxis=ring+1;}}previous=point;}}}else for(int i=0;i<3;i++)if(TryProjectWorldToScreen(p+directions[i]*length,out PointF end)){float d=DistancePointToSegment(screen,origin,end);if(d<best){best=d;collisionDragAxis=i+1;}}if(collisionDragAxis==0)return false;collisionDragWorldAxis=directions[collisionDragAxis-1];
        draggingCollisionVertex=true;collisionDragStartMouse=screen;collisionDragStartPosition=SelectedVertexWorld();
        collisionDragStartVertices=SelectedFaceVertexIndices().ToDictionary(x=>x,x=>selectedCollision.Mesh.Positions[x]);collisionDragStartNormals.Clear();if(CollisionTransformMode==CollisionGizmoMode.Rotate){IEnumerable<int> faces=CollisionRegionActive&&selectedCollisionRegionFaces.Count>0?selectedCollisionRegionFaces:selectedCollisionManualFaces.Count>0?selectedCollisionManualFaces:new[]{selectedCollision.FaceIndex};collisionDragStartNormals=faces.Select(i=>(int)selectedCollision.Mesh.Faces[i].Normal).Distinct().ToDictionary(i=>i,i=>selectedCollision.Mesh.Normals[i]);var radial=new System.Numerics.Vector2(screen.X-origin.X,screen.Y-origin.Y);if(radial.LengthSquared()>.01f){radial=System.Numerics.Vector2.Normalize(radial);collisionDragScreenAxis=new(-radial.Y,radial.X);}}
        NVector3 a=collisionDragStartPosition,b=a+NVector3.UnitY;if(TryProjectWorldToScreen(a,out PointF pa)&&TryProjectWorldToScreen(b,out PointF pb))collisionVerticalPixelsPerUnit=Math.Max(0.05f,MathF.Abs(pb.Y-pa.Y));
        return true;
    }
    private void UpdateCollisionVertexDrag(Point screen)
    {
        if(!draggingCollisionVertex||selectedCollision==null)return;NVector3 axis=collisionDragWorldAxis;if(CollisionTransformMode==CollisionGizmoMode.Rotate){float pixels=System.Numerics.Vector2.Dot(new(screen.X-collisionDragStartMouse.X,screen.Y-collisionDragStartMouse.Y),collisionDragScreenAxis);float angle=pixels*MathF.PI/360f;var rotation=System.Numerics.Matrix4x4.CreateFromAxisAngle(axis,angle);foreach(var item in collisionDragStartVertices)selectedCollision.Mesh.Positions[item.Key]=(collisionDragStartPosition+NVector3.Transform(item.Value/100f-collisionDragStartPosition,rotation))*100f;foreach(var item in collisionDragStartNormals)selectedCollision.Mesh.Normals[item.Key]=NVector3.Normalize(NVector3.TransformNormal(item.Value,rotation));collisionGpuDirty=true;CollisionVertexEdited?.Invoke(selectedCollision);Invalidate();return;}NVector3 next=collisionDragStartPosition;float length=CamScreenSize(collisionDragStartPosition,72f);if(!TryProjectWorldToScreen(collisionDragStartPosition,out PointF a)||!TryProjectWorldToScreen(collisionDragStartPosition+axis*length,out PointF b))return;var projected=new System.Numerics.Vector2(b.X-a.X,b.Y-a.Y);float axisPixels=projected.Length();if(axisPixels<.1f)return;projected/=axisPixels;float delta=System.Numerics.Vector2.Dot(new(screen.X-collisionDragStartMouse.X,screen.Y-collisionDragStartMouse.Y),projected)*length/axisPixels;next+=axis*delta;SetSelectedCollisionVertexPosition(next,false);
    }
    private void EndCollisionVertexDrag()
    {
        if(!draggingCollisionVertex||selectedCollision==null)return;draggingCollisionVertex=false;collisionDragAxis=0;EsatMesh mesh=selectedCollision.Mesh;
        var before=new Dictionary<int,NVector3>(collisionDragStartVertices);var beforeNormals=new Dictionary<int,NVector3>(collisionDragStartNormals);bool changed=before.Any(x=>mesh.Positions[x.Key]!=x.Value);
        if(changed)collisionUndo.Push(()=>{foreach(var item in before)mesh.Positions[item.Key]=item.Value;foreach(var item in beforeNormals)mesh.Normals[item.Key]=item.Value;});collisionDragStartVertices.Clear();collisionDragStartNormals.Clear();
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
                EsatFace f = mesh.Faces[fi];if(Ps2EsatPrimitiveWriter.IsFaceDisabled(file.Kind,f)&&!ShowDisabledCollision)continue; NVector3 a = mesh.Positions[f.Vertex0] / 100f, b = mesh.Positions[f.Vertex1] / 100f, c = mesh.Positions[f.Vertex2] / 100f;
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
        if(disabledCollisionVbo!=0)GL.DeleteBuffer(disabledCollisionVbo);if(disabledCollisionVao!=0)GL.DeleteVertexArray(disabledCollisionVao);
        if (selectedCollisionVbo != 0) GL.DeleteBuffer(selectedCollisionVbo);
        if (selectedCollisionVao != 0) GL.DeleteVertexArray(selectedCollisionVao);
        for(int i=0;i<3;i++){if(collisionHandleVbos[i]!=0)GL.DeleteBuffer(collisionHandleVbos[i]);if(collisionHandleVaos[i]!=0)GL.DeleteVertexArray(collisionHandleVaos[i]);}
    }
}

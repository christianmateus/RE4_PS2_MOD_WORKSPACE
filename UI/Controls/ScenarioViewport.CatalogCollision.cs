using RE4_PS2_MOD_WORKSPACE.Core.Collision;
namespace RE4_PS2_MOD_WORKSPACE;
public sealed partial class ScenarioViewport
{
    public bool CatalogCollisionPicking {get;set;}
    public EsatKind? CatalogCollisionKind {get;set;}
    private readonly List<EsatFaceInspection> catalogCollisionFaces=new();
    public IReadOnlyList<EsatFaceInspection> CatalogCollisionFaces=>catalogCollisionFaces.ToArray();
    public event Action? CatalogCollisionSelectionChanged;
    public void SetCatalogCollisionFaces(IEnumerable<EsatFaceInspection> faces){catalogCollisionFaces.Clear();catalogCollisionFaces.AddRange(faces);collisionGpuDirty=true;Invalidate();CatalogCollisionSelectionChanged?.Invoke();}
    private void PickCatalogCollision(Point point)
    {
        if(!TryBuildPickRay(point,out var origin,out var direction))return;
        EsatFaceInspection? best=null;float distance=float.PositiveInfinity;
        PickCollisionFile(satCollision,CatalogCollisionKind!=EsatKind.Eat,origin,direction,ref best,ref distance);
        PickCollisionFile(eatCollision,CatalogCollisionKind!=EsatKind.Sat,origin,direction,ref best,ref distance);
        bool additive=(ModifierKeys&(Keys.Control|Keys.Shift))!=0;
        if(best==null)return;
        ToggleCatalogCollisionFace(best,additive);
    }
    public void ToggleCatalogCollisionFace(EsatFaceInspection best,bool additive)
    {
        if(!CatalogCollisionPicking)return;
        if(!additive)catalogCollisionFaces.Clear();
        int i=catalogCollisionFaces.FindIndex(f=>ReferenceEquals(f.File,best.File)&&f.MeshIndex==best.MeshIndex&&f.FaceIndex==best.FaceIndex);
        if(i>=0&&additive)catalogCollisionFaces.RemoveAt(i);else catalogCollisionFaces.Add(best);
        collisionGpuDirty=true;Invalidate();CatalogCollisionSelectionChanged?.Invoke();
    }
    private List<float> BuildCatalogCollisionVertices()
    {
        var output=new List<float>();foreach(var selection in catalogCollisionFaces){var mesh=selection.Mesh;var f=mesh.Faces[selection.FaceIndex];var n=mesh.Normals[f.Normal];WriteCollisionVertex(output,mesh.Positions[f.Vertex0]/100f,n);WriteCollisionVertex(output,mesh.Positions[f.Vertex1]/100f,n);WriteCollisionVertex(output,mesh.Positions[f.Vertex2]/100f,n);}return output;
    }
}

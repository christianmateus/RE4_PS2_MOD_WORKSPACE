using System.Numerics;
using System.Text.Json;
using RE4_PS2_MOD_WORKSPACE.Core.Collision;
namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;
public sealed class SmdCatalogCollisionLink
{
    public EsatKind Kind {get;set;} public int Entry {get;set;} public int BinId {get;set;} public int Mesh {get;set;} public int FirstVertex {get;set;}
    public List<float[]> LocalPoints {get;set;}=new(); public List<float[]> LastPoints {get;set;}=new();
}
public static class SmdCatalogCollisionLinks
{
    public static string PathFor(string smd)=>smd+".catalog-collisions.json";
    public static List<SmdCatalogCollisionLink> Load(string smd)=>File.Exists(PathFor(smd))?JsonSerializer.Deserialize<List<SmdCatalogCollisionLink>>(File.ReadAllText(PathFor(smd)))??new():new();
    public static void Write(string path,List<SmdCatalogCollisionLink> links){string temp=path+".write.tmp";try{File.WriteAllText(temp,JsonSerializer.Serialize(links,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}}
    public static bool Apply(List<SmdCatalogCollisionLink> links,ScenarioEntry entry,EsatFile? sat,EsatFile? eat)
    {
        var targets=links.Where(l=>l.Entry==entry.FileOrder&&l.BinId==entry.BinId).ToArray();bool changed=false;
        if(targets.Length>0&&(!float.IsFinite(entry.ScaleX)||!float.IsFinite(entry.ScaleY)||!float.IsFinite(entry.ScaleZ)||entry.ScaleX==0||entry.ScaleY==0||entry.ScaleZ==0))throw new InvalidOperationException("Uma colisão vinculada precisa de escala diferente de zero. Desvincule-a para ocultar o objeto.");
        foreach(var link in targets)
        {
            var file=link.Kind==EsatKind.Sat?sat:eat;if(file==null||link.Mesh<0||link.Mesh>=file.Meshes.Count)continue;var mesh=file.Meshes[link.Mesh];
            if(link.LocalPoints.Count!=link.LastPoints.Count||link.FirstVertex<0||link.FirstVertex+link.LocalPoints.Count>mesh.Positions.Count)throw new InvalidOperationException("A estrutura da colisão vinculada mudou. Desvincule a colisão antes de transformar o objeto.");
            for(int i=0;i<link.LastPoints.Count;i++){var v=link.LastPoints[i];if(v.Length!=3||Vector3.DistanceSquared(mesh.Positions[link.FirstVertex+i],new Vector3(v[0],v[1],v[2]))>.01f)throw new InvalidOperationException("A colisão vinculada foi editada separadamente. Desvincule-a antes de transformar o objeto.");}
        }
        foreach(var link in targets){var file=link.Kind==EsatKind.Sat?sat:eat;if(file==null||link.Mesh<0||link.Mesh>=file.Meshes.Count)continue;var mesh=file.Meshes[link.Mesh];var helper=new SmdCatalogCollisionFace();var next=link.LocalPoints.Select(p=>helper.Point(p,entry)).ToArray();for(int i=0;i<next.Length;i++){changed|=mesh.Positions[link.FirstVertex+i]!=next[i];mesh.Positions[link.FirstVertex+i]=next[i];}foreach(var face in mesh.Faces.Where(f=>f.Vertex0>=link.FirstVertex&&f.Vertex0<link.FirstVertex+next.Length)){Vector3 a=mesh.Positions[face.Vertex0],b=mesh.Positions[face.Vertex1],c=mesh.Positions[face.Vertex2];var normal=Vector3.Cross(b-a,c-a);if(normal.LengthSquared()>.000001f){var oldCross=Vector3.Cross(mesh.OriginalPositions[face.Vertex1]-mesh.OriginalPositions[face.Vertex0],mesh.OriginalPositions[face.Vertex2]-mesh.OriginalPositions[face.Vertex0]);float sign=Vector3.Dot(oldCross,mesh.OriginalNormals[face.Normal])<0?-1:1;mesh.Normals[face.Normal]=Vector3.Normalize(normal)*sign;}mesh.EdgeVectors[face.Edge0]=b-a;mesh.EdgeVectors[face.Edge1]=c-b;mesh.EdgeVectors[face.Edge2]=a-c;}link.LastPoints=next.Select(v=>new[]{v.X,v.Y,v.Z}).ToList();}return changed;
    }
}

using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Collision;

public static class Ps2EsatVertexWriter
{
    public static bool Save(EsatFile file, string backupPath)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (!file.Meshes.Any(x => x.IsModified)) return false;
        foreach (EsatMesh mesh in file.Meshes)
            foreach (Vector3 p in mesh.Positions)
                if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z)) throw new InvalidDataException("Não é possível salvar vértice com coordenada não finita.");
        foreach (EsatMesh mesh in file.Meshes.Where(x=>x.IsModified)) PrepareFixedSizeEdit(mesh);
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        if (!File.Exists(backupPath)) File.Copy(file.SourcePath, backupPath, false);
        string temp = file.SourcePath + ".vertex-edit.tmp";
        try
        {
            File.Copy(file.SourcePath, temp, true);
            using (var stream = new FileStream(temp, FileMode.Open, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                foreach (EsatMesh mesh in file.Meshes)
                {
                    stream.Position = mesh.PositionsFileOffset;
                    WriteVectors(writer, mesh.Positions);
                    stream.Position = mesh.NormalsFileOffset; WriteVectors(writer, mesh.Normals);
                    stream.Position = mesh.EdgeVectorsFileOffset; WriteVectors(writer, mesh.EdgeVectors);
                    foreach (EsatGroup group in mesh.Groups)
                    {
                        stream.Position = group.FileOffset;
                        WriteVector(writer, group.Position); WriteVector(writer, group.Size);
                    }
                }
            }
            _ = Ps2EsatReader.Read(temp, file.Kind);
            if(new FileInfo(temp).Length!=new FileInfo(file.SourcePath).Length)
                throw new InvalidDataException("A edição alterou indevidamente o tamanho do SAT/EAT.");
            File.Move(temp, file.SourcePath, true);
            foreach (EsatMesh mesh in file.Meshes) { mesh.OriginalPositions.Clear(); mesh.OriginalPositions.AddRange(mesh.Positions); }
            return true;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void PrepareFixedSizeEdit(EsatMesh mesh)
    {
        var changedFaces=new HashSet<int>();
        var normals=new Dictionary<ushort,Vector3>();
        var edges=new Dictionary<ushort,Vector3>();
        for(int i=0;i<mesh.Faces.Count;i++)
        {
            EsatFace face=mesh.Faces[i];
            if(mesh.Positions[face.Vertex0]==mesh.OriginalPositions[face.Vertex0]&&mesh.Positions[face.Vertex1]==mesh.OriginalPositions[face.Vertex1]&&mesh.Positions[face.Vertex2]==mesh.OriginalPositions[face.Vertex2])continue;
            changedFaces.Add(i);
            Vector3 a=mesh.Positions[face.Vertex0],b=mesh.Positions[face.Vertex1],c=mesh.Positions[face.Vertex2];
            Vector3 cross=Vector3.Cross(b-a,c-a);float length=cross.Length();
            if(!float.IsFinite(length)||length<0.0001f)throw new InvalidDataException("A edição criou um triângulo de colisão degenerado.");
            AddDemand(normals,face.Normal,cross/length,"normal");
            AddDemand(edges,face.Edge0,b-a,"aresta 0");AddDemand(edges,face.Edge1,c-b,"aresta 1");AddDemand(edges,face.Edge2,a-c,"aresta 2");
        }

        ValidateSharedIndices(mesh,changedFaces,normals,edges);
        foreach(var item in normals)mesh.Normals[item.Key]=item.Value;
        foreach(var item in edges)mesh.EdgeVectors[item.Key]=item.Value;
        ExpandSpatialGroups(mesh,changedFaces);
    }

    private static void AddDemand(Dictionary<ushort,Vector3> demands,ushort index,Vector3 value,string label)
    {
        if(demands.TryGetValue(index,out Vector3 previous)&&!NearlyEqual(previous,value))
            throw new InvalidDataException($"A edição exige valores incompatíveis para o índice compartilhado de {label} {index}. Inclua mais faces conectadas na transformação ou reduza a alteração.");
        demands[index]=value;
    }

    private static void ValidateSharedIndices(EsatMesh mesh,HashSet<int> changedFaces,Dictionary<ushort,Vector3> normals,Dictionary<ushort,Vector3> edges)
    {
        for(int i=0;i<mesh.Faces.Count;i++)
        {
            if(changedFaces.Contains(i))continue;EsatFace face=mesh.Faces[i];
            if(normals.TryGetValue(face.Normal,out Vector3 normal)&&!NearlyEqual(normal,mesh.Normals[face.Normal]))
                throw new InvalidDataException($"A normal {face.Normal} também é usada pela face não editada {i}. O salvamento foi bloqueado para preservar o SAT.");
            CheckEdge(face.Edge0,i);CheckEdge(face.Edge1,i);CheckEdge(face.Edge2,i);
        }
        void CheckEdge(ushort index,int faceIndex)
        {
            if(edges.TryGetValue(index,out Vector3 value)&&!NearlyEqual(value,mesh.EdgeVectors[index]))
                throw new InvalidDataException($"A aresta {index} também é usada pela face não editada {faceIndex}. O salvamento foi bloqueado para preservar o SAT.");
        }
    }

    private static bool NearlyEqual(Vector3 a,Vector3 b)
    {
        float scale=Math.Max(1f,Math.Max(a.Length(),b.Length()));return Vector3.Distance(a,b)<=scale*0.00001f;
    }

    private static void ExpandSpatialGroups(EsatMesh mesh,HashSet<int> changedFaces)
    {
        var ancestors=BuildAncestorMap(mesh.Groups);
        foreach(int faceIndex in changedFaces)
        {
            EsatFace face=mesh.Faces[faceIndex];Vector3 a=mesh.Positions[face.Vertex0],b=mesh.Positions[face.Vertex1],c=mesh.Positions[face.Vertex2];
            Vector3 min=Vector3.Min(a,Vector3.Min(b,c))-new Vector3(1.11111f);Vector3 max=Vector3.Max(a,Vector3.Max(b,c))+new Vector3(1.11111f);
            for(int gi=0;gi<mesh.Groups.Count;gi++)
            {
                EsatGroup group=mesh.Groups[gi];
                if(!group.FloorFaces.Contains((ushort)faceIndex)&&!group.SlopeFaces.Contains((ushort)faceIndex)&&!group.WallFaces.Contains((ushort)faceIndex))continue;
                Expand(group,min,max);
                if(ancestors.TryGetValue(gi,out List<int>? parents))foreach(int parent in parents)Expand(mesh.Groups[parent],min,max);
            }
        }
    }

    private static Dictionary<int,List<int>> BuildAncestorMap(IReadOnlyList<EsatGroup> groups)
    {
        var result=new Dictionary<int,List<int>>();int index=0;
        try{while(index<groups.Count)Parse(new List<int>());}catch{result.Clear();}
        return result;
        void Parse(List<int> parents)
        {
            if(index>=groups.Count)throw new InvalidDataException();int current=index++;result[current]=new List<int>(parents);
            if(groups[current].Flags!=1)return;var nextParents=new List<int>(parents){current};for(int child=0;child<4;child++)Parse(nextParents);
        }
    }

    private static void Expand(EsatGroup group,Vector3 min,Vector3 max)
    {
        Vector3 oldMin=group.Position,oldMax=group.Position+group.Size;Vector3 newMin=Vector3.Min(oldMin,min),newMax=Vector3.Max(oldMax,max);
        group.Position=newMin;group.Size=newMax-newMin;
    }

    private static void WriteVectors(BinaryWriter writer,IEnumerable<Vector3> vectors){foreach(Vector3 p in vectors)WriteVector(writer,p);}
    private static void WriteVector(BinaryWriter writer,Vector3 p){writer.Write(p.X);writer.Write(p.Y);writer.Write(p.Z);}
}

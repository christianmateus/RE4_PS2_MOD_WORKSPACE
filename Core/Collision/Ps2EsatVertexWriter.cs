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
        bool requiresRebuild=false;foreach (EsatMesh mesh in file.Meshes.Where(x=>x.IsModified)) requiresRebuild|=PrepareFixedSizeEdit(mesh);
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        if (!File.Exists(backupPath)) File.Copy(file.SourcePath, backupPath, false);
        string temp = file.SourcePath + ".vertex-edit.tmp";
        try
        {
            if(requiresRebuild)Ps2EsatPrimitiveWriter.WriteEditedCopy(file,temp);else File.Copy(file.SourcePath,temp,true);
            using (var stream = new FileStream(temp, FileMode.Open, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream))
            {
                foreach (EsatMesh mesh in file.Meshes)
                {
                    if(requiresRebuild)break;
                    stream.Position = mesh.PositionsFileOffset;
                    WriteVectors(writer, mesh.Positions);
                    stream.Position = mesh.NormalsFileOffset; WriteVectors(writer, mesh.Normals);
                    stream.Position = mesh.EdgeVectorsFileOffset; WriteVectors(writer, mesh.EdgeVectors);
                    long facesOffset=mesh.EdgeVectorsFileOffset+mesh.EdgeVectors.Count*12L;for(int i=0;i<mesh.Faces.Count;i++){EsatFace face=mesh.Faces[i];stream.Position=facesOffset+i*20L+16L;writer.Write(face.Blue);writer.Write(face.Green);writer.Write(face.Red);writer.Write(face.Connectivity);}
                    foreach (EsatGroup group in mesh.Groups)
                    {
                        stream.Position = group.FileOffset;
                        WriteVector(writer, group.Position); WriteVector(writer, group.Size);
                    }
                }
            }
            EsatFile check=Ps2EsatReader.Read(temp,file.Kind);
            if(check.Meshes.Count!=file.Meshes.Count||check.FaceCount!=file.FaceCount)throw new InvalidDataException("A validação estrutural da colisão editada falhou.");
            if(!requiresRebuild&&new FileInfo(temp).Length!=new FileInfo(file.SourcePath).Length)throw new InvalidDataException("A edição alterou indevidamente o tamanho do SAT/EAT.");
            File.Move(temp, file.SourcePath, true);
            foreach (EsatMesh mesh in file.Meshes) { mesh.OriginalPositions.Clear(); mesh.OriginalPositions.AddRange(mesh.Positions);mesh.OriginalNormals.Clear();mesh.OriginalNormals.AddRange(mesh.Normals);mesh.OriginalFaces.Clear();mesh.OriginalFaces.AddRange(mesh.Faces); }
            return true;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static bool PrepareFixedSizeEdit(EsatMesh mesh)
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
            Vector3 originalA=mesh.OriginalPositions[face.Vertex0],originalB=mesh.OriginalPositions[face.Vertex1],originalC=mesh.OriginalPositions[face.Vertex2];
            if(NearlyEqual(b-a,originalB-originalA)&&NearlyEqual(c-b,originalC-originalB)&&NearlyEqual(a-c,originalA-originalC))continue;
            Vector3 cross=Vector3.Cross(b-a,c-a);float length=cross.Length();
            if(!float.IsFinite(length)||length<0.0001f)throw new InvalidDataException("A edição criou um triângulo de colisão degenerado.");
            Vector3 normal=face.Normal<mesh.OriginalNormals.Count&&mesh.Normals[face.Normal]!=mesh.OriginalNormals[face.Normal]?Vector3.Normalize(mesh.Normals[face.Normal]):cross/length;
            AddNormalDemand(normals,face.Normal,normal);
            AddDemand(edges,face.Edge0,b-a,"aresta 0");AddDemand(edges,face.Edge1,c-b,"aresta 1");AddDemand(edges,face.Edge2,a-c,"aresta 2");
        }

        bool detached=DetachSharedIndices(mesh,changedFaces,normals,edges);
        ValidateSharedIndices(mesh,changedFaces,normals,edges);
        foreach(var item in normals)mesh.Normals[item.Key]=item.Value;
        foreach(var item in edges)mesh.EdgeVectors[item.Key]=item.Value;
        ExpandSpatialGroups(mesh,changedFaces);return detached;
    }

    private static bool DetachSharedIndices(EsatMesh mesh,HashSet<int> changedFaces,Dictionary<ushort,Vector3> normals,Dictionary<ushort,Vector3> edges)
    {
        bool detached=false;
        foreach(var item in normals.ToArray()){ushort old=item.Key;if(!Enumerable.Range(0,mesh.Faces.Count).Any(i=>!changedFaces.Contains(i)&&mesh.Faces[i].Normal==old)||NearlyEqual(item.Value,mesh.OriginalNormals[old]))continue;ushort replacement=checked((ushort)mesh.Normals.Count);mesh.Normals.Add(item.Value);normals.Remove(old);normals[replacement]=item.Value;foreach(int i in changedFaces){EsatFace face=mesh.Faces[i];if(face.Normal!=old)continue;Vector3 a=mesh.Positions[face.Vertex0],b=mesh.Positions[face.Vertex1],c=mesh.Positions[face.Vertex2],cross=Vector3.Cross(b-a,c-a);if(cross.LengthSquared()>0.000001f&&NearlyEqual(Vector3.Normalize(cross),item.Value))mesh.Faces[i]=face with{Normal=replacement};}detached=true;}
        foreach(var item in edges.ToArray()){ushort old=item.Key;if(!Enumerable.Range(0,mesh.Faces.Count).Any(i=>!changedFaces.Contains(i)&&UsesEdge(mesh.Faces[i],old))||NearlyEqual(item.Value,mesh.EdgeVectors[old]))continue;ushort replacement=checked((ushort)mesh.EdgeVectors.Count);mesh.EdgeVectors.Add(item.Value);edges.Remove(old);edges[replacement]=item.Value;foreach(int i in changedFaces){EsatFace face=mesh.Faces[i];Vector3 a=mesh.Positions[face.Vertex0],b=mesh.Positions[face.Vertex1],c=mesh.Positions[face.Vertex2];if(face.Edge0==old&&NearlyEqual(b-a,item.Value))face=face with{Edge0=replacement};if(face.Edge1==old&&NearlyEqual(c-b,item.Value))face=face with{Edge1=replacement};if(face.Edge2==old&&NearlyEqual(a-c,item.Value))face=face with{Edge2=replacement};mesh.Faces[i]=face;}detached=true;}return detached;
        static bool UsesEdge(EsatFace face,ushort index)=>face.Edge0==index||face.Edge1==index||face.Edge2==index;
    }

    private static void AddNormalDemand(Dictionary<ushort,Vector3> demands,ushort index,Vector3 value)
    {
        value=Vector3.Normalize(value);if(!demands.TryGetValue(index,out Vector3 previous)){demands[index]=value;return;}
        Vector3 combined=previous+value;if(!float.IsFinite(combined.LengthSquared())||combined.LengthSquared()<0.000001f)throw new InvalidDataException($"As faces ligadas à normal compartilhada {index} ficaram com orientações opostas. Reduza a deformação.");
        demands[index]=Vector3.Normalize(combined);
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
        var result=new Dictionary<int,List<int>>();for(int leaf=0;leaf<groups.Count;leaf++){EsatGroup item=groups[leaf];if(item.Flags!=2)continue;Vector3 leafMin=item.Position,leafMax=item.Position+item.Size;var parents=new List<int>();for(int i=0;i<groups.Count;i++){EsatGroup parent=groups[i];if(parent.Flags!=1)continue;Vector3 min=parent.Position,max=min+parent.Size;if(leafMin.X>=min.X-2f&&leafMin.Y>=min.Y-2f&&leafMin.Z>=min.Z-2f&&leafMax.X<=max.X+2f&&leafMax.Y<=max.Y+2f&&leafMax.Z<=max.Z+2f)parents.Add(i);}result[leaf]=parents;}return result;
    }
    private static void Expand(EsatGroup group,Vector3 min,Vector3 max)
    {
        Vector3 oldMin=group.Position,oldMax=group.Position+group.Size;Vector3 newMin=Vector3.Min(oldMin,min),newMax=Vector3.Max(oldMax,max);
        group.Position=newMin;group.Size=newMax-newMin;
    }

    private static void WriteVectors(BinaryWriter writer,IEnumerable<Vector3> vectors){foreach(Vector3 p in vectors)WriteVector(writer,p);}
    private static void WriteVector(BinaryWriter writer,Vector3 p){writer.Write(p.X);writer.Write(p.Y);writer.Write(p.Z);}
}

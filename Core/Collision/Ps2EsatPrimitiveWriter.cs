using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Collision;

public static class Ps2EsatPrimitiveWriter
{
    private sealed record MeshData(EsatMesh Source,List<Vector3> Positions,List<Vector3> Normals,List<Vector3> Edges,List<EsatFace> Faces,List<GroupData> Groups,int Floors,int Slopes,int Walls);
    private sealed record GroupData(Vector3 Position,Vector3 Size,ushort Flags,uint Brother,ushort[] Floors,ushort[] Slopes,ushort[] Walls);

    public static void AddBox(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,string backupPath)
        =>AddBoxInternal(file,meshIndex,anchorFace,dimensions,null,backupPath);

    public static void AddBoxAt(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,Vector3 center,string backupPath)
        =>AddBoxInternal(file,meshIndex,anchorFace,dimensions,center,backupPath);

    public static void DeleteBox(EsatFile file,int meshIndex,int selectedFace,string backupPath)
    {
        if(file.Kind!=EsatKind.Sat)throw new InvalidOperationException("A exclusão de primitivas está disponível apenas para SAT.");
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);
        var all=new List<MeshData>();for(int i=0;i<file.Meshes.Count;i++)all.Add(i==meshIndex?DeletePrimitive(file.Meshes[i],selectedFace):Copy(file.Meshes[i]));
        string temp=file.SourcePath+".delete-primitive.tmp";
        try{Write(file,all,temp);EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(check.FaceCount!=file.FaceCount-12)throw new InvalidDataException("A validação não encontrou a remoção das 12 faces.");File.Move(temp,file.SourcePath,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }

    private static void AddBoxInternal(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,Vector3? explicitCenter,string backupPath)
    {
        if(file.Kind!=EsatKind.Sat)throw new InvalidOperationException("O primitivo experimental está disponível apenas para SAT.");
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        EsatMesh mesh=file.Meshes[meshIndex];if(anchorFace<0||anchorFace>=mesh.Faces.Count)throw new ArgumentOutOfRangeException(nameof(anchorFace));
        if(dimensions.X<=1||dimensions.Y<=1||dimensions.Z<=1||!float.IsFinite(dimensions.X)||!float.IsFinite(dimensions.Y)||!float.IsFinite(dimensions.Z))throw new ArgumentOutOfRangeException(nameof(dimensions));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);
        var all=new List<MeshData>();for(int i=0;i<file.Meshes.Count;i++)all.Add(i==meshIndex?BuildBox(file.Meshes[i],anchorFace,dimensions,explicitCenter):Copy(file.Meshes[i]));
        string temp=file.SourcePath+".cube.tmp";
        try{Write(file,all,temp);EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(check.Meshes.Count!=file.Meshes.Count||check.FaceCount!=file.FaceCount+12)throw new InvalidDataException("A validação do cubo não encontrou as 12 faces novas.");File.Move(temp,file.SourcePath,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }

    private static MeshData Copy(EsatMesh m)=>new(m,new(m.Positions),new(m.Normals),new(m.EdgeVectors),new(m.Faces),m.Groups.Select(CopyGroup).ToList(),m.FloorCount,m.SlopeCount,m.WallCount);
    private static GroupData CopyGroup(EsatGroup g)=>new(g.Position,g.Size,g.Flags,g.BrotherDistance,g.FloorFaces,g.SlopeFaces,g.WallFaces);

    private static MeshData DeletePrimitive(EsatMesh m,int selectedFace)
    {
        if(selectedFace<0||selectedFace>=m.Faces.Count)throw new ArgumentOutOfRangeException(nameof(selectedFace));
        var facesByVertex=new Dictionary<int,List<int>>();for(int fi=0;fi<m.Faces.Count;fi++)foreach(int vi in Vertices(m.Faces[fi]).Distinct()){if(!facesByVertex.TryGetValue(vi,out var list))facesByVertex[vi]=list=new();list.Add(fi);}
        var removeFaces=new HashSet<int>{selectedFace};var queue=new Queue<int>();queue.Enqueue(selectedFace);
        while(queue.Count>0){int fi=queue.Dequeue();foreach(int vi in Vertices(m.Faces[fi]))foreach(int adjacent in facesByVertex[vi])if(!removeFaces.Contains(adjacent)){EsatFace f=m.Faces[adjacent];if(f.Connectivity==0xE0&&f.Unknown==0&&f.Blue==0&&f.Green==0&&f.Red==0){removeFaces.Add(adjacent);queue.Enqueue(adjacent);}}}
        var removeVertices=removeFaces.SelectMany(i=>Vertices(m.Faces[i])).ToHashSet();
        if(removeFaces.Count!=12||removeVertices.Count!=8)throw new InvalidDataException("A seleção não é uma primitiva de caixa reconhecida (12 faces e 8 vértices).");
        return DeleteFaceSet(m,removeFaces);
        static int[] Vertices(EsatFace f)=>new[]{(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2};
    }

    private static MeshData DeleteFaceSet(EsatMesh m,HashSet<int> removeFaces)
    {
        if(removeFaces.Count>=m.Faces.Count)throw new InvalidDataException("Não é permitido excluir todas as faces do submesh.");
        var keptFaces=Enumerable.Range(0,m.Faces.Count).Where(i=>!removeFaces.Contains(i)).Select(i=>m.Faces[i]).ToArray();
        var usedVertices=keptFaces.SelectMany(Vertices).ToHashSet();var usedNormals=keptFaces.Select(f=>(int)f.Normal).ToHashSet();var usedEdges=keptFaces.SelectMany(f=>new[]{(int)f.Edge0,(int)f.Edge1,(int)f.Edge2}).ToHashSet();
        var removeVertices=Enumerable.Range(0,m.Positions.Count).Where(i=>!usedVertices.Contains(i)).ToHashSet();var removeNormals=Enumerable.Range(0,m.Normals.Count).Where(i=>!usedNormals.Contains(i)).ToHashSet();var removeEdges=Enumerable.Range(0,m.EdgeVectors.Count).Where(i=>!usedEdges.Contains(i)).ToHashSet();
        var vertexMap=BuildMap(m.Positions.Count,removeVertices);var normalMap=BuildMap(m.Normals.Count,removeNormals);var edgeMap=BuildMap(m.EdgeVectors.Count,removeEdges);var faceMap=BuildMap(m.Faces.Count,removeFaces);
        var positions=m.Positions.Where((_,i)=>!removeVertices.Contains(i)).ToList();var normals=m.Normals.Where((_,i)=>!removeNormals.Contains(i)).ToList();var edges=m.EdgeVectors.Where((_,i)=>!removeEdges.Contains(i)).ToList();
        var faces=new List<EsatFace>();for(int i=0;i<m.Faces.Count;i++)if(!removeFaces.Contains(i)){EsatFace f=m.Faces[i];faces.Add(f with{Vertex0=vertexMap[f.Vertex0],Vertex1=vertexMap[f.Vertex1],Vertex2=vertexMap[f.Vertex2],Normal=normalMap[f.Normal],Edge0=edgeMap[f.Edge0],Edge1=edgeMap[f.Edge1],Edge2=edgeMap[f.Edge2]});}
        int removedFloors=removeFaces.Count(i=>i<m.FloorCount),removedSlopes=removeFaces.Count(i=>i>=m.FloorCount&&i<m.FloorCount+m.SlopeCount),removedWalls=removeFaces.Count-removedFloors-removedSlopes;
        var groups=m.Groups.Select(g=>new GroupData(g.Position,g.Size,g.Flags,g.BrotherDistance,Remap(g.FloorFaces),Remap(g.SlopeFaces),Remap(g.WallFaces))).ToList();RecalculateBrothers(groups);
        return new(m,positions,normals,edges,faces,groups,m.FloorCount-removedFloors,m.SlopeCount-removedSlopes,m.WallCount-removedWalls);
        ushort[] Remap(IEnumerable<ushort> values)=>values.Where(x=>!removeFaces.Contains(x)).Select(x=>faceMap[x]).ToArray();
        static int[] Vertices(EsatFace f)=>new[]{(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2};
        static ushort[] BuildMap(int count,HashSet<int> removed){var map=new ushort[count];int next=0;for(int i=0;i<count;i++)if(!removed.Contains(i))map[i]=checked((ushort)next++);return map;}
    }

    private static MeshData BuildBox(EsatMesh m,int anchor,Vector3 dimensions,Vector3? explicitCenter)
    {
        if(m.Positions.Count>ushort.MaxValue-8||m.Normals.Count>ushort.MaxValue-6||m.EdgeVectors.Count>ushort.MaxValue-36||m.Faces.Count>ushort.MaxValue-12)throw new InvalidDataException("O submesh não possui capacidade de índices para um cubo.");
        EsatFace selected=m.Faces[anchor];Vector3 baseCenter=(m.Positions[selected.Vertex0]+m.Positions[selected.Vertex1]+m.Positions[selected.Vertex2])/3f;Vector3 h=dimensions/2f;Vector3 center=explicitCenter??baseCenter+new Vector3(0,h.Y,0);
        var p=new List<Vector3>(m.Positions);ushort pv=(ushort)p.Count;
        p.AddRange(new[]{center+new Vector3(-h.X,-h.Y,-h.Z),center+new Vector3(h.X,-h.Y,-h.Z),center+new Vector3(h.X,-h.Y,h.Z),center+new Vector3(-h.X,-h.Y,h.Z),center+new Vector3(-h.X,h.Y,-h.Z),center+new Vector3(h.X,h.Y,-h.Z),center+new Vector3(h.X,h.Y,h.Z),center+new Vector3(-h.X,h.Y,h.Z)});
        var n=new List<Vector3>(m.Normals);ushort nn=(ushort)n.Count;n.AddRange(new[]{-Vector3.UnitY,Vector3.UnitY,-Vector3.UnitZ,Vector3.UnitX,Vector3.UnitZ,-Vector3.UnitX});
        var e=new List<Vector3>(m.EdgeVectors);var cubeFaces=new List<EsatFace>();
        void Face(int a,int b,int c,int normal)
        {Vector3 va=p[pv+a],vb=p[pv+b],vc=p[pv+c];ushort ei=(ushort)e.Count;e.Add(vb-va);e.Add(vc-vb);e.Add(va-vc);cubeFaces.Add(new((ushort)(pv+a),(ushort)(pv+b),(ushort)(pv+c),(ushort)(nn+normal),ei,(ushort)(ei+1),(ushort)(ei+2),0,0,0,0,0xE0));}
        Face(0,1,2,0);Face(0,2,3,0);Face(4,6,5,1);Face(4,7,6,1);
        Face(0,5,1,2);Face(0,4,5,2);Face(1,6,2,3);Face(1,5,6,3);
        Face(2,7,3,4);Face(2,6,7,4);Face(3,4,0,5);Face(3,7,4,5);
        int oldFloors=m.FloorCount,oldSlopes=m.SlopeCount;var faces=new List<EsatFace>(m.Faces.Count+12);
        faces.AddRange(m.Faces.Take(oldFloors));faces.AddRange(cubeFaces.Take(2));faces.AddRange(m.Faces.Skip(oldFloors));faces.AddRange(cubeFaces.Skip(2));
        ushort Map(ushort old)=>old<oldFloors?old:(ushort)(old+2);ushort firstFloor=(ushort)oldFloors,firstWall=(ushort)(m.Faces.Count+2);
        var groups=new List<GroupData>(m.Groups.Count);bool assigned=false;
        foreach(EsatGroup g in m.Groups)
        {
            ushort[] gf=g.FloorFaces.Select(Map).ToArray(),gs=g.SlopeFaces.Select(Map).ToArray(),gw=g.WallFaces.Select(Map).ToArray();
            bool contains=g.FloorFaces.Contains((ushort)anchor)||g.SlopeFaces.Contains((ushort)anchor)||g.WallFaces.Contains((ushort)anchor);
            if(g.Flags==2&&contains){gf=gf.Concat(new[]{firstFloor,(ushort)(firstFloor+1)}).ToArray();gw=gw.Concat(Enumerable.Range(firstWall,10).Select(x=>(ushort)x)).ToArray();assigned=true;}
            groups.Add(new(g.Position,g.Size,g.Flags,g.BrotherDistance,gf,gs,gw));
        }
        if(!assigned)throw new InvalidDataException("A face selecionada não pertence a nenhum grupo folha; não é possível posicionar o cubo com segurança.");
        RecalculateBrothers(groups);
        return new(m,p,n,e,faces,groups,m.FloorCount+2,m.SlopeCount,m.WallCount+10);
    }

    private static void RecalculateBrothers(List<GroupData> groups)
    {
        int index=0;
        Parse();
        if(index!=groups.Count)throw new InvalidDataException("A árvore espacial original não pôde ser percorrida integralmente.");
        int Parse()
        {
            if(index>=groups.Count)throw new InvalidDataException("Árvore espacial truncada.");int current=index++;
            if(groups[current].Flags!=1)return current;
            int[] children=new int[4];for(int i=0;i<4;i++)children[i]=Parse();
            for(int i=0;i<3;i++){uint distance=0;for(int j=children[i];j<children[i+1];j++)distance+=Length(groups[j]);groups[children[i]]=groups[children[i]] with{Brother=distance};}
            groups[children[3]]=groups[children[3]] with{Brother=0};return current;
        }
        static uint Length(GroupData g){int count=g.Floors.Length+g.Slopes.Length+g.Walls.Length;return(uint)(36+count*2+(count&1)*2);}
    }

    private static void Write(EsatFile file,List<MeshData> meshes,string path)
    {
        using var s=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None);using var w=new BinaryWriter(s);
        if(file.ContainerMagic==0x80){w.Write((byte)0x80);w.Write((byte)meshes.Count);w.Write(file.ContainerUnknown);long table=s.Position;foreach(var _ in meshes)w.Write(0u);for(int i=0;i<meshes.Count;i++){long pos=s.Position,longBack=pos;s.Position=table+i*4;w.Write((uint)pos);s.Position=longBack;WriteMesh(w,meshes[i]);}}
        else WriteMesh(w,meshes[0]);int pad=32-(int)(s.Position%32);w.Write(Enumerable.Repeat((byte)0xCD,pad).ToArray());
    }
    private static void WriteMesh(BinaryWriter w,MeshData m)
    {
        w.Write(m.Source.Magic);w.Write(m.Source.Unknown01);w.Write((ushort)m.Positions.Count);w.Write((ushort)m.Normals.Count);w.Write((ushort)m.Edges.Count);w.Write(m.Source.Unknown08);w.Write((ushort)m.Faces.Count);w.Write((ushort)m.Floors);w.Write((ushort)m.Slopes);w.Write((ushort)m.Walls);w.Write((ushort)m.Groups.Count);
        foreach(Vector3 v in m.Positions)V(w,v);foreach(Vector3 v in m.Normals)V(w,v);foreach(Vector3 v in m.Edges)V(w,v);foreach(EsatFace f in m.Faces){w.Write(f.Vertex0);w.Write(f.Vertex1);w.Write(f.Vertex2);w.Write(f.Normal);w.Write(f.Edge0);w.Write(f.Edge1);w.Write(f.Edge2);w.Write(f.Unknown);w.Write(f.Blue);w.Write(f.Green);w.Write(f.Red);w.Write(f.Connectivity);}
        foreach(GroupData g in m.Groups){V(w,g.Position);V(w,g.Size);w.Write((ushort)g.Floors.Length);w.Write((ushort)g.Slopes.Length);w.Write((ushort)g.Walls.Length);w.Write(g.Flags);w.Write(g.Brother);foreach(ushort x in g.Floors)w.Write(x);foreach(ushort x in g.Slopes)w.Write(x);foreach(ushort x in g.Walls)w.Write(x);if(((g.Floors.Length+g.Slopes.Length+g.Walls.Length)&1)!=0)w.Write((ushort)0);}
    }
    private static void V(BinaryWriter w,Vector3 v){w.Write(v.X);w.Write(v.Y);w.Write(v.Z);}
}

using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Collision;

public static class Ps2EsatPrimitiveWriter
{
    private sealed record MeshData(EsatMesh Source,List<Vector3> Positions,List<Vector3> Normals,List<Vector3> Edges,List<EsatFace> Faces,List<GroupData> Groups,int Floors,int Slopes,int Walls);
    private sealed record GroupData(Vector3 Position,Vector3 Size,ushort Flags,uint Brother,ushort[] Floors,ushort[] Slopes,ushort[] Walls,uint OriginalLength);

    public static void AddBox(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,string backupPath)
        =>AddBoxInternal(file,meshIndex,anchorFace,dimensions,null,backupPath);

    public static void AddBoxAt(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,Vector3 center,string backupPath)
        =>AddBoxInternal(file,meshIndex,anchorFace,dimensions,center,backupPath);

    public static void AddTriangularPrismAt(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,Vector3 center,string backupPath)
        =>AddTriangularPrismInternal(file,meshIndex,anchorFace,dimensions,center,backupPath);

    public static void AddFullTriangularPrismAt(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,Vector3 center,string backupPath)
        =>AddFullTriangularPrismInternal(file,meshIndex,anchorFace,dimensions,center,backupPath);

    public static void AddFloorFromEdge(EsatFile file,int meshIndex,int anchorFace,int edgeSlot,float depth,float thickness,string backupPath)
    {
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        EsatMesh mesh=file.Meshes[meshIndex];if(anchorFace<0||anchorFace>=mesh.Faces.Count)throw new ArgumentOutOfRangeException(nameof(anchorFace));
        EsatFace face=mesh.Faces[anchorFace];int[] indices={(int)face.Vertex0,(int)face.Vertex1,(int)face.Vertex2};edgeSlot=Math.Clamp(edgeSlot,0,2);
        Vector3 a=mesh.Positions[indices[edgeSlot]],b=mesh.Positions[indices[(edgeSlot+1)%3]],third=mesh.Positions[indices[(edgeSlot+2)%3]];
        Vector3 edge=b-a;edge.Y=0;float width=edge.Length();if(width<=1f)throw new InvalidDataException("A aresta escolhida é curta demais para encaixar um chão.");
        Vector3 direction=edge/width;Vector3 outward=new(-direction.Z,0,direction.X);Vector3 midpoint=(a+b)/2f;if(Vector3.Dot(outward,third-midpoint)>0)outward=-outward;
        Vector3 center=midpoint+outward*(depth/2f)+new Vector3(0,thickness/2f,0);float rotationY=-MathF.Atan2(direction.Z,direction.X);
        AddBoxInternal(file,meshIndex,anchorFace,new Vector3(width,thickness,depth),center,backupPath,rotationY);
    }

    public static void AddSurfaceFromEdges(EsatFile file,int meshIndex,IReadOnlyList<(int FaceIndex,int EdgeSlot)> selections,string backupPath)
    {
        if(file.Kind!=EsatKind.Sat)throw new InvalidOperationException("O preenchimento de arestas está disponível apenas para SAT.");
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        if(selections.Count<2)throw new InvalidOperationException("Marque pelo menos duas arestas.");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);
        var all=new List<MeshData>();for(int i=0;i<file.Meshes.Count;i++)all.Add(i==meshIndex?BuildSurface(file.Meshes[i],selections):Copy(file.Meshes[i]));
        string temp=file.SourcePath+".fill-surface.tmp";try{Write(file,all,temp);EsatFile check=Ps2EsatReader.Read(temp,file.Kind);int added=selections.SelectMany(x=>EdgeVertices(file.Meshes[meshIndex].Faces[x.FaceIndex],x.EdgeSlot)).Distinct().Count()-2;if(added<1||check.FaceCount!=file.FaceCount+added)throw new InvalidDataException("A validação não confirmou as novas faces.");File.Move(temp,file.SourcePath,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }

    private static int[] EdgeVertices(EsatFace face,int slot)=>Math.Clamp(slot,0,2) switch{0=>new[]{(int)face.Vertex0,(int)face.Vertex1},1=>new[]{(int)face.Vertex1,(int)face.Vertex2},_=>new[]{(int)face.Vertex2,(int)face.Vertex0}};
    public static void DeleteBox(EsatFile file,int meshIndex,int selectedFace,string backupPath)
    {
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);
        var all=new List<MeshData>();for(int i=0;i<file.Meshes.Count;i++)all.Add(i==meshIndex?DeletePrimitive(file.Meshes[i],selectedFace):Copy(file.Meshes[i]));
        string temp=file.SourcePath+".delete-primitive.tmp";
        try{Write(file,all,temp);EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(check.FaceCount!=file.FaceCount-12)throw new InvalidDataException("A validação não encontrou a remoção das 12 faces.");File.Move(temp,file.SourcePath,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }

    public static void DeleteFace(EsatFile file,int meshIndex,int faceIndex,string backupPath)
    {
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        EsatMesh mesh=file.Meshes[meshIndex];if(faceIndex<0||faceIndex>=mesh.Faces.Count)throw new ArgumentOutOfRangeException(nameof(faceIndex));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);
        string temp=file.SourcePath+".disable-face.tmp";
        try
        {
            File.Copy(file.SourcePath,temp,true);long flagsOffset=checked(mesh.EdgeVectorsFileOffset+mesh.EdgeVectors.Count*12L+faceIndex*20L+16L);
            using(var stream=new FileStream(temp,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {
                if(flagsOffset<0||flagsOffset+4>stream.Length)throw new InvalidDataException("A posição dos flags da face está fora do SAT.");
                stream.Position=flagsOffset;int blue=stream.ReadByte(),green=stream.ReadByte(),red=stream.ReadByte(),connectivity=stream.ReadByte();
                if(connectivity<0)throw new EndOfStreamException();stream.Position=flagsOffset;
                EsatFace disabledFlags=DisableFaceFlags(file.Kind,new EsatFace(0,0,0,0,0,0,0,0,(byte)blue,(byte)green,(byte)red,(byte)connectivity));
                stream.WriteByte(disabledFlags.Blue);
                stream.WriteByte(disabledFlags.Green);
                stream.WriteByte(disabledFlags.Red);
                stream.WriteByte((byte)connectivity);     // preserve edge connectivity
            }
            EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(check.FaceCount!=file.FaceCount||new FileInfo(temp).Length!=new FileInfo(file.SourcePath).Length)throw new InvalidDataException("A desativação alterou indevidamente a estrutura do SAT.");
            EsatFace disabled=check.Meshes[meshIndex].Faces[faceIndex];if(!IsFaceDisabled(file.Kind,disabled))throw new InvalidDataException("A validação não confirmou os flags No Hit da face.");
            File.Move(temp,file.SourcePath,true);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }

    public static int RepairFaceOrientation(EsatFile file,int meshIndex,IReadOnlyList<int> faceIndices,Vector3 referenceNormal,string backupPath)
    {
        if(file.Kind!=EsatKind.Sat)throw new InvalidOperationException("O reparo de orientação está disponível apenas para SAT.");if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));EsatMesh mesh=file.Meshes[meshIndex];int[] targets=faceIndices.Distinct().OrderBy(x=>x).ToArray();if(targets.Any(x=>x<0||x>=mesh.Faces.Count))throw new ArgumentOutOfRangeException(nameof(faceIndices));referenceNormal=Vector3.Normalize(referenceNormal);var targetSet=targets.ToHashSet();var normalUsers=mesh.Faces.Select((face,index)=>(face,index)).GroupBy(x=>(int)x.face.Normal).ToDictionary(x=>x.Key,x=>x.Select(v=>v.index).ToArray());var edgeUse=mesh.Faces.SelectMany(x=>new[]{(int)x.Edge0,(int)x.Edge1,(int)x.Edge2}).GroupBy(x=>x).ToDictionary(x=>x.Key,x=>x.Count());foreach(int index in targets){EsatFace f=mesh.Faces[index];if(normalUsers[f.Normal].Any(x=>!targetSet.Contains(x))||edgeUse[f.Edge0]!=1||edgeUse[f.Edge1]!=1||edgeUse[f.Edge2]!=1)throw new InvalidDataException($"A face {index} compartilha dados com uma face fora do reparo; a operação foi cancelada.");}
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);string temp=file.SourcePath+".repair-orientation.tmp";File.Copy(file.SourcePath,temp,true);int repaired=0;try{using(var stream=new FileStream(temp,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){using var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true);foreach(int index in targets){EsatFace f=mesh.Faces[index];Vector3 a=mesh.Positions[f.Vertex0],b=mesh.Positions[f.Vertex1],c=mesh.Positions[f.Vertex2],normal=Vector3.Normalize(Vector3.Cross(b-a,c-a));if(Vector3.Dot(normal,referenceNormal)>=0)continue;ushort v0=f.Vertex0,v1=f.Vertex2,v2=f.Vertex1;Vector3 p0=mesh.Positions[v0],p1=mesh.Positions[v1],p2=mesh.Positions[v2],fixedNormal=Vector3.Normalize(Vector3.Cross(p1-p0,p2-p0));stream.Position=mesh.NormalsFileOffset+f.Normal*12L;V(writer,fixedNormal);stream.Position=mesh.EdgeVectorsFileOffset+f.Edge0*12L;V(writer,p1-p0);stream.Position=mesh.EdgeVectorsFileOffset+f.Edge1*12L;V(writer,p2-p1);stream.Position=mesh.EdgeVectorsFileOffset+f.Edge2*12L;V(writer,p0-p2);long faceOffset=mesh.EdgeVectorsFileOffset+mesh.EdgeVectors.Count*12L+index*20L;stream.Position=faceOffset;writer.Write(v0);writer.Write(v1);writer.Write(v2);stream.Position=faceOffset+19;byte low=(byte)(f.Connectivity&0x1F),e0=(byte)(f.Connectivity&0x20),e1=(byte)(f.Connectivity&0x40),e2=(byte)(f.Connectivity&0x80);writer.Write((byte)(low|(e0<<2)|e1|(e2>>2)));repaired++;}}EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(new FileInfo(temp).Length!=new FileInfo(file.SourcePath).Length)throw new InvalidDataException("O reparo alterou o tamanho do SAT.");foreach(int index in targets){EsatFace f=check.Meshes[meshIndex].Faces[index];if(Vector3.Dot(check.Meshes[meshIndex].Normals[f.Normal],referenceNormal)<0)throw new InvalidDataException($"A validação da face {index} falhou.");}File.Move(temp,file.SourcePath,true);return repaired;}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public static EsatFace DisableFaceFlags(EsatKind kind,EsatFace face)=>kind==EsatKind.Eat?face with{Blue=(byte)(face.Blue|0x40),Green=(byte)(face.Green|0x40),Red=(byte)(face.Red|0x40)}:face with{Blue=(byte)(face.Blue|0x44),Green=(byte)(face.Green|0xC0),Red=(byte)(face.Red|0xC0)};
    public static bool IsFaceDisabled(EsatKind kind,EsatFace face)=>kind==EsatKind.Eat?(face.Blue&0x40)!=0&&(face.Green&0x40)!=0&&(face.Red&0x40)!=0:(face.Blue&0x44)==0x44&&(face.Green&0xC0)==0xC0&&(face.Red&0xC0)==0xC0;

    public static void RestoreFace(EsatFile file,int meshIndex,int faceIndex,string backupPath)
    {
        if(!File.Exists(backupPath))throw new FileNotFoundException("O backup original do SAT não foi encontrado.",backupPath);
        EsatFile original=Ps2EsatReader.Read(backupPath,file.Kind);if(meshIndex<0||meshIndex>=file.Meshes.Count||meshIndex>=original.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        EsatMesh mesh=file.Meshes[meshIndex],source=original.Meshes[meshIndex];if(mesh.Faces.Count!=source.Faces.Count)throw new InvalidDataException("O SAT atual não possui a mesma estrutura do backup. Restaure o arquivo antes de recuperar faces.");
        if(faceIndex<0||faceIndex>=mesh.Faces.Count)throw new ArgumentOutOfRangeException(nameof(faceIndex));EsatFace flags=source.Faces[faceIndex];
        string temp=file.SourcePath+".restore-face.tmp";try{File.Copy(file.SourcePath,temp,true);long offset=checked(mesh.EdgeVectorsFileOffset+mesh.EdgeVectors.Count*12L+faceIndex*20L+16L);using(var stream=new FileStream(temp,FileMode.Open,FileAccess.Write,FileShare.None)){stream.Position=offset;stream.WriteByte(flags.Blue);stream.WriteByte(flags.Green);stream.WriteByte(flags.Red);stream.WriteByte(flags.Connectivity);}EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(new FileInfo(temp).Length!=new FileInfo(file.SourcePath).Length)throw new InvalidDataException("A restauração alterou o tamanho do SAT.");File.Move(temp,file.SourcePath,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }

    private static void AddBoxInternal(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,Vector3? explicitCenter,string backupPath,float rotationY=0f)
    {
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        EsatMesh mesh=file.Meshes[meshIndex];if(anchorFace<0||anchorFace>=mesh.Faces.Count)throw new ArgumentOutOfRangeException(nameof(anchorFace));
        if(dimensions.X<=1||dimensions.Y<=1||dimensions.Z<=1||!float.IsFinite(dimensions.X)||!float.IsFinite(dimensions.Y)||!float.IsFinite(dimensions.Z))throw new ArgumentOutOfRangeException(nameof(dimensions));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);
        var all=new List<MeshData>();for(int i=0;i<file.Meshes.Count;i++)all.Add(i==meshIndex?BuildBox(file.Meshes[i],anchorFace,dimensions,explicitCenter,rotationY):Copy(file.Meshes[i]));
        string temp=file.SourcePath+".cube.tmp";
        try{Write(file,all,temp);EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(check.Meshes.Count!=file.Meshes.Count||check.FaceCount!=file.FaceCount+12)throw new InvalidDataException("A validação do cubo não encontrou as 12 faces novas.");File.Move(temp,file.SourcePath,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }

    private static void AddTriangularPrismInternal(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,Vector3 center,string backupPath)
    {
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        EsatMesh mesh=file.Meshes[meshIndex];if(anchorFace<0||anchorFace>=mesh.Faces.Count)throw new ArgumentOutOfRangeException(nameof(anchorFace));
        if(dimensions.X<=1||dimensions.Y<=1||dimensions.Z<=1||!float.IsFinite(dimensions.X)||!float.IsFinite(dimensions.Y)||!float.IsFinite(dimensions.Z))throw new ArgumentOutOfRangeException(nameof(dimensions));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);
        var all=new List<MeshData>();for(int i=0;i<file.Meshes.Count;i++)all.Add(i==meshIndex?BuildTriangularPrism(file.Meshes[i],anchorFace,dimensions,center):Copy(file.Meshes[i]));
        string temp=file.SourcePath+".triangular-prism.tmp";
        try{Write(file,all,temp);EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(check.Meshes.Count!=file.Meshes.Count||check.FaceCount!=file.FaceCount+8)throw new InvalidDataException("A validação do prisma triangular não encontrou as 8 faces novas.");File.Move(temp,file.SourcePath,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }

    private static void AddFullTriangularPrismInternal(EsatFile file,int meshIndex,int anchorFace,Vector3 dimensions,Vector3 center,string backupPath)
    {
        if(meshIndex<0||meshIndex>=file.Meshes.Count)throw new ArgumentOutOfRangeException(nameof(meshIndex));
        EsatMesh mesh=file.Meshes[meshIndex];if(anchorFace<0||anchorFace>=mesh.Faces.Count)throw new ArgumentOutOfRangeException(nameof(anchorFace));
        if(dimensions.X<=1||dimensions.Y<=1||dimensions.Z<=1||!float.IsFinite(dimensions.X)||!float.IsFinite(dimensions.Y)||!float.IsFinite(dimensions.Z))throw new ArgumentOutOfRangeException(nameof(dimensions));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);if(!File.Exists(backupPath))File.Copy(file.SourcePath,backupPath,false);
        var all=new List<MeshData>();for(int i=0;i<file.Meshes.Count;i++)all.Add(i==meshIndex?BuildFullTriangularPrism(file.Meshes[i],anchorFace,dimensions,center):Copy(file.Meshes[i]));
        string temp=file.SourcePath+".full-triangular-prism.tmp";
        try{Write(file,all,temp);EsatFile check=Ps2EsatReader.Read(temp,file.Kind);if(check.Meshes.Count!=file.Meshes.Count||check.FaceCount!=file.FaceCount+8)throw new InvalidDataException("A validação do prisma triangular completo não encontrou as 8 faces novas.");File.Move(temp,file.SourcePath,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }

    private static MeshData Copy(EsatMesh m)=>new(m,new(m.Positions),new(m.Normals),new(m.EdgeVectors),new(m.Faces),m.Groups.Select(CopyGroup).ToList(),m.FloorCount,m.SlopeCount,m.WallCount);
    private static GroupData CopyGroup(EsatGroup g)=>new(g.Position,g.Size,g.Flags,g.BrotherDistance,g.FloorFaces,g.SlopeFaces,g.WallFaces,GroupLength(g.FloorFaces.Length+g.SlopeFaces.Length+g.WallFaces.Length));

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
        var groups=m.Groups.Select(g=>new GroupData(g.Position,g.Size,g.Flags,g.BrotherDistance,Remap(g.FloorFaces),Remap(g.SlopeFaces),Remap(g.WallFaces),GroupLength(g.FloorFaces.Length+g.SlopeFaces.Length+g.WallFaces.Length))).ToList();RecalculateBrothers(groups);
        return new(m,positions,normals,edges,faces,groups,m.FloorCount-removedFloors,m.SlopeCount-removedSlopes,m.WallCount-removedWalls);
        ushort[] Remap(IEnumerable<ushort> values)=>values.Where(x=>!removeFaces.Contains(x)).Select(x=>faceMap[x]).ToArray();
        static int[] Vertices(EsatFace f)=>new[]{(int)f.Vertex0,(int)f.Vertex1,(int)f.Vertex2};
        static ushort[] BuildMap(int count,HashSet<int> removed){var map=new ushort[count];int next=0;for(int i=0;i<count;i++)if(!removed.Contains(i))map[i]=checked((ushort)next++);return map;}
    }

    private static MeshData BuildBox(EsatMesh m,int anchor,Vector3 dimensions,Vector3? explicitCenter,float rotationY)
    {
        if(m.Positions.Count>ushort.MaxValue-8||m.Normals.Count>ushort.MaxValue-6||m.EdgeVectors.Count>ushort.MaxValue-36||m.Faces.Count>ushort.MaxValue-12)throw new InvalidDataException("O submesh não possui capacidade de índices para um cubo.");
        EsatFace selected=m.Faces[anchor];Vector3 baseCenter=(m.Positions[selected.Vertex0]+m.Positions[selected.Vertex1]+m.Positions[selected.Vertex2])/3f;Vector3 h=dimensions/2f;Vector3 center=explicitCenter??baseCenter+new Vector3(0,h.Y,0);
        var p=new List<Vector3>(m.Positions);ushort pv=(ushort)p.Count;
        var rotation=Matrix4x4.CreateRotationY(rotationY);Vector3 R(Vector3 value)=>Vector3.Transform(value,rotation);
        p.AddRange(new[]{center+R(new Vector3(-h.X,-h.Y,-h.Z)),center+R(new Vector3(h.X,-h.Y,-h.Z)),center+R(new Vector3(h.X,-h.Y,h.Z)),center+R(new Vector3(-h.X,-h.Y,h.Z)),center+R(new Vector3(-h.X,h.Y,-h.Z)),center+R(new Vector3(h.X,h.Y,-h.Z)),center+R(new Vector3(h.X,h.Y,h.Z)),center+R(new Vector3(-h.X,h.Y,h.Z))});
        var n=new List<Vector3>(m.Normals);ushort nn=(ushort)n.Count;n.AddRange(new[]{R(-Vector3.UnitY),R(Vector3.UnitY),R(-Vector3.UnitZ),R(Vector3.UnitX),R(Vector3.UnitZ),R(-Vector3.UnitX)});
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
            groups.Add(new(g.Position,g.Size,g.Flags,g.BrotherDistance,gf,gs,gw,GroupLength(g.FloorFaces.Length+g.SlopeFaces.Length+g.WallFaces.Length)));
        }
        if(!assigned)throw new InvalidDataException("A face selecionada não pertence a nenhum grupo folha; não é possível posicionar o cubo com segurança.");
        ExpandPrimitiveGroups(groups,new[]{firstFloor},p.Skip(pv));
        RecalculateBrothers(groups);
        return new(m,p,n,e,faces,groups,m.FloorCount+2,m.SlopeCount,m.WallCount+10);
    }

    private static MeshData BuildTriangularPrism(EsatMesh m,int anchor,Vector3 dimensions,Vector3 center)
    {
        const int floorAdded=2,slopeAdded=2,wallAdded=4,faceAdded=8;
        if(m.Positions.Count>ushort.MaxValue-6||m.Normals.Count>ushort.MaxValue-faceAdded||m.EdgeVectors.Count>ushort.MaxValue-faceAdded*3||m.Faces.Count>ushort.MaxValue-faceAdded)throw new InvalidDataException("O submesh não possui capacidade de índices para um prisma triangular.");
        Vector3 h=dimensions/2f;var p=new List<Vector3>(m.Positions);ushort pv=(ushort)p.Count;
        p.AddRange(new[]{center+new Vector3(-h.X,-h.Y,-h.Z),center+new Vector3(h.X,-h.Y,-h.Z),center+new Vector3(h.X,-h.Y,h.Z),center+new Vector3(-h.X,-h.Y,h.Z),center+new Vector3(-h.X,h.Y,h.Z),center+new Vector3(h.X,h.Y,h.Z)});
        var n=new List<Vector3>(m.Normals);var e=new List<Vector3>(m.EdgeVectors);var created=new List<EsatFace>();
        void Face(int a,int b,int c){Vector3 va=p[pv+a],vb=p[pv+b],vc=p[pv+c],normal=Vector3.Normalize(Vector3.Cross(vb-va,vc-va));ushort ni=(ushort)n.Count;n.Add(normal);ushort ei=(ushort)e.Count;e.Add(vb-va);e.Add(vc-vb);e.Add(va-vc);created.Add(new((ushort)(pv+a),(ushort)(pv+b),(ushort)(pv+c),ni,ei,(ushort)(ei+1),(ushort)(ei+2),0,0,0,0,0xE0));}
        Face(0,1,2);Face(0,2,3);Face(0,4,5);Face(0,5,1);Face(3,2,5);Face(3,5,4);Face(0,3,4);Face(1,5,2);
        int oldFloors=m.FloorCount,oldSlopes=m.SlopeCount;var faces=new List<EsatFace>(m.Faces.Count+faceAdded);
        faces.AddRange(m.Faces.Take(oldFloors));faces.AddRange(created.Take(floorAdded));faces.AddRange(m.Faces.Skip(oldFloors).Take(oldSlopes));faces.AddRange(created.Skip(floorAdded).Take(slopeAdded));faces.AddRange(m.Faces.Skip(oldFloors+oldSlopes));faces.AddRange(created.Skip(floorAdded+slopeAdded));
        ushort Map(ushort old)=>old<oldFloors?old:old<oldFloors+oldSlopes?(ushort)(old+floorAdded):(ushort)(old+floorAdded+slopeAdded);
        ushort firstFloor=(ushort)oldFloors,firstSlope=(ushort)(oldFloors+floorAdded+oldSlopes),firstWall=(ushort)(m.Faces.Count+floorAdded+slopeAdded);var groups=new List<GroupData>(m.Groups.Count);bool assigned=false;
        foreach(EsatGroup g in m.Groups){ushort[] gf=g.FloorFaces.Select(Map).ToArray(),gs=g.SlopeFaces.Select(Map).ToArray(),gw=g.WallFaces.Select(Map).ToArray();bool contains=g.FloorFaces.Contains((ushort)anchor)||g.SlopeFaces.Contains((ushort)anchor)||g.WallFaces.Contains((ushort)anchor);if(g.Flags==2&&contains){gf=gf.Concat(new[]{firstFloor,(ushort)(firstFloor+1)}).ToArray();gs=gs.Concat(new[]{firstSlope,(ushort)(firstSlope+1)}).ToArray();gw=gw.Concat(Enumerable.Range(firstWall,wallAdded).Select(x=>(ushort)x)).ToArray();assigned=true;}groups.Add(new(g.Position,g.Size,g.Flags,g.BrotherDistance,gf,gs,gw,GroupLength(g.FloorFaces.Length+g.SlopeFaces.Length+g.WallFaces.Length)));}
        if(!assigned)throw new InvalidDataException("A face de referência não pertence a nenhum grupo folha; não é possível posicionar o prisma triangular com segurança.");ExpandPrimitiveGroups(groups,new[]{firstFloor},p.Skip(pv));RecalculateBrothers(groups);return new(m,p,n,e,faces,groups,m.FloorCount+floorAdded,m.SlopeCount+slopeAdded,m.WallCount+wallAdded);
    }

    private static MeshData BuildFullTriangularPrism(EsatMesh m,int anchor,Vector3 dimensions,Vector3 center)
    {
        const int floorAdded=2,slopeAdded=4,wallAdded=2,faceAdded=8;
        if(m.Positions.Count>ushort.MaxValue-6||m.Normals.Count>ushort.MaxValue-faceAdded||m.EdgeVectors.Count>ushort.MaxValue-faceAdded*3||m.Faces.Count>ushort.MaxValue-faceAdded)throw new InvalidDataException("O submesh não possui capacidade de índices para um prisma triangular completo.");
        Vector3 h=dimensions/2f;var p=new List<Vector3>(m.Positions);ushort pv=(ushort)p.Count;
        p.AddRange(new[]{center+new Vector3(-h.X,-h.Y,-h.Z),center+new Vector3(h.X,-h.Y,-h.Z),center+new Vector3(-h.X,-h.Y,h.Z),center+new Vector3(h.X,-h.Y,h.Z),center+new Vector3(-h.X,h.Y,0),center+new Vector3(h.X,h.Y,0)});
        var n=new List<Vector3>(m.Normals);var e=new List<Vector3>(m.EdgeVectors);var created=new List<EsatFace>();
        void Face(int a,int b,int c){Vector3 va=p[pv+a],vb=p[pv+b],vc=p[pv+c],normal=Vector3.Normalize(Vector3.Cross(vb-va,vc-va));ushort ni=(ushort)n.Count;n.Add(normal);ushort ei=(ushort)e.Count;e.Add(vb-va);e.Add(vc-vb);e.Add(va-vc);created.Add(new((ushort)(pv+a),(ushort)(pv+b),(ushort)(pv+c),ni,ei,(ushort)(ei+1),(ushort)(ei+2),0,0,0,0,0xE0));}
        Face(0,1,3);Face(0,3,2);Face(0,4,5);Face(0,5,1);Face(2,3,5);Face(2,5,4);Face(0,2,4);Face(1,5,3);
        int oldFloors=m.FloorCount,oldSlopes=m.SlopeCount;var faces=new List<EsatFace>(m.Faces.Count+faceAdded);faces.AddRange(m.Faces.Take(oldFloors));faces.AddRange(created.Take(floorAdded));faces.AddRange(m.Faces.Skip(oldFloors).Take(oldSlopes));faces.AddRange(created.Skip(floorAdded).Take(slopeAdded));faces.AddRange(m.Faces.Skip(oldFloors+oldSlopes));faces.AddRange(created.Skip(floorAdded+slopeAdded));
        ushort Map(ushort old)=>old<oldFloors?old:old<oldFloors+oldSlopes?(ushort)(old+floorAdded):(ushort)(old+floorAdded+slopeAdded);ushort firstFloor=(ushort)oldFloors,firstSlope=(ushort)(oldFloors+floorAdded+oldSlopes),firstWall=(ushort)(m.Faces.Count+floorAdded+slopeAdded);var groups=new List<GroupData>(m.Groups.Count);bool assigned=false;
        foreach(EsatGroup g in m.Groups){ushort[] gf=g.FloorFaces.Select(Map).ToArray(),gs=g.SlopeFaces.Select(Map).ToArray(),gw=g.WallFaces.Select(Map).ToArray();bool contains=g.FloorFaces.Contains((ushort)anchor)||g.SlopeFaces.Contains((ushort)anchor)||g.WallFaces.Contains((ushort)anchor);if(g.Flags==2&&contains){gf=gf.Concat(new[]{firstFloor,(ushort)(firstFloor+1)}).ToArray();gs=gs.Concat(Enumerable.Range(firstSlope,slopeAdded).Select(x=>(ushort)x)).ToArray();gw=gw.Concat(Enumerable.Range(firstWall,wallAdded).Select(x=>(ushort)x)).ToArray();assigned=true;}groups.Add(new(g.Position,g.Size,g.Flags,g.BrotherDistance,gf,gs,gw,GroupLength(g.FloorFaces.Length+g.SlopeFaces.Length+g.WallFaces.Length)));}
        if(!assigned)throw new InvalidDataException("A face de referência não pertence a nenhum grupo folha; não é possível posicionar o prisma triangular completo com segurança.");ExpandPrimitiveGroups(groups,new[]{firstFloor},p.Skip(pv));RecalculateBrothers(groups);return new(m,p,n,e,faces,groups,m.FloorCount+floorAdded,m.SlopeCount+slopeAdded,m.WallCount+wallAdded);
    }

    private static MeshData BuildSurface(EsatMesh m,IReadOnlyList<(int FaceIndex,int EdgeSlot)> selections)
    {
        foreach(var x in selections)if(x.FaceIndex<0||x.FaceIndex>=m.Faces.Count)throw new ArgumentOutOfRangeException(nameof(selections));
        int[] vertices=selections.SelectMany(x=>EdgeVertices(m.Faces[x.FaceIndex],x.EdgeSlot)).Distinct().ToArray();if(vertices.Length<3)throw new InvalidOperationException("As arestas marcadas não formam uma superfície.");
        Vector3 center=vertices.Select(i=>m.Positions[i]).Aggregate(Vector3.Zero,(a,b)=>a+b)/vertices.Length;vertices=vertices.OrderBy(i=>MathF.Atan2(m.Positions[i].Z-center.Z,m.Positions[i].X-center.X)).ToArray();
        Vector3 reference=selections.Select(x=>m.Normals[m.Faces[x.FaceIndex].Normal]).Aggregate(Vector3.Zero,(a,b)=>a+b);if(reference.LengthSquared()<0.000001f)reference=Vector3.UnitY;else reference=Vector3.Normalize(reference);Vector3 test=Vector3.Cross(m.Positions[vertices[1]]-m.Positions[vertices[0]],m.Positions[vertices[2]]-m.Positions[vertices[0]]);if(Vector3.Dot(test,reference)<0)Array.Reverse(vertices);
        int addCount=vertices.Length-2;if(m.Normals.Count>ushort.MaxValue-addCount||m.EdgeVectors.Count>ushort.MaxValue-addCount*3||m.Faces.Count>ushort.MaxValue-addCount)throw new InvalidDataException("O submesh não possui capacidade para o preenchimento.");
        var normals=new List<Vector3>(m.Normals);var edges=new List<Vector3>(m.EdgeVectors);var created=new List<EsatFace>();var seam=selections.Select(x=>EdgeVertices(m.Faces[x.FaceIndex],x.EdgeSlot)).Select(x=>(Math.Min(x[0],x[1]),Math.Max(x[0],x[1]))).ToHashSet();
        var triangles=new List<(int A,int B,int C)>();for(int i=1;i<vertices.Length-1;i++)triangles.Add((vertices[0],vertices[i],vertices[i+1]));var counts=new Dictionary<(int,int),int>();foreach(var t in triangles)foreach(var pair in new[]{(t.A,t.B),(t.B,t.C),(t.C,t.A)}){var key=(Math.Min(pair.Item1,pair.Item2),Math.Max(pair.Item1,pair.Item2));counts[key]=counts.GetValueOrDefault(key)+1;}
        foreach(var t in triangles){Vector3 a=m.Positions[t.A],b=m.Positions[t.B],c=m.Positions[t.C],normal=Vector3.Normalize(Vector3.Cross(b-a,c-a));ushort ni=(ushort)normals.Count;normals.Add(normal);ushort ei=(ushort)edges.Count;edges.Add(b-a);edges.Add(c-b);edges.Add(a-c);byte connectivity=0;var pairs=new[]{(t.A,t.B),(t.B,t.C),(t.C,t.A)};for(int e=0;e<3;e++){var key=(Math.Min(pairs[e].Item1,pairs[e].Item2),Math.Max(pairs[e].Item1,pairs[e].Item2));if(seam.Contains(key)||counts[key]>1)connectivity|=(byte)(0x20<<e);}created.Add(new((ushort)t.A,(ushort)t.B,(ushort)t.C,ni,ei,(ushort)(ei+1),(ushort)(ei+2),0,0,0,0,connectivity));}
        var sourceFaces=new List<EsatFace>(m.Faces);foreach(var x in selections){EsatFace f=sourceFaces[x.FaceIndex];sourceFaces[x.FaceIndex]=f with{Connectivity=(byte)(f.Connectivity|(0x20<<Math.Clamp(x.EdgeSlot,0,2)))};}
        Vector3 average=created.Select(f=>normals[f.Normal]).Aggregate(Vector3.Zero,(a,b)=>a+b)/created.Count;bool floor=MathF.Abs(Vector3.Normalize(average).Y)>=.98f;int insert=floor?m.FloorCount:m.FloorCount+m.SlopeCount;var faces=new List<EsatFace>(m.Faces.Count+created.Count);faces.AddRange(sourceFaces.Take(insert));faces.AddRange(created);faces.AddRange(sourceFaces.Skip(insert));ushort Map(ushort old)=>old<insert?old:(ushort)(old+created.Count);ushort[] added=Enumerable.Range(insert,created.Count).Select(i=>(ushort)i).ToArray();var anchorFaces=selections.Select(x=>x.FaceIndex).ToHashSet();bool assigned=false;
        var groups=new List<GroupData>();foreach(EsatGroup g in m.Groups){ushort[] gf=g.FloorFaces.Select(Map).ToArray(),gs=g.SlopeFaces.Select(Map).ToArray(),gw=g.WallFaces.Select(Map).ToArray();bool contains=g.Flags==2&&(g.FloorFaces.Any(x=>anchorFaces.Contains(x))||g.SlopeFaces.Any(x=>anchorFaces.Contains(x))||g.WallFaces.Any(x=>anchorFaces.Contains(x)));if(contains){if(floor)gf=gf.Concat(added).ToArray();else gs=gs.Concat(added).ToArray();assigned=true;}groups.Add(new(g.Position,g.Size,g.Flags,g.BrotherDistance,gf,gs,gw,GroupLength(g.FloorFaces.Length+g.SlopeFaces.Length+g.WallFaces.Length)));}if(!assigned)throw new InvalidDataException("As arestas não pertencem a um grupo espacial folha.");RecalculateBrothers(groups);return new(m,new(m.Positions),normals,edges,faces,groups,m.FloorCount+(floor?created.Count:0),m.SlopeCount+(floor?0:created.Count),m.WallCount);
    }
    private static void ExpandPrimitiveGroups(List<GroupData> groups,IReadOnlyCollection<ushort> createdFaces,IEnumerable<Vector3> points)
    {
        Vector3[] vertices=points.ToArray();if(vertices.Length==0)return;Vector3 min=vertices.Aggregate(Vector3.Min)-new Vector3(1.11111f),max=vertices.Aggregate(Vector3.Max)+new Vector3(1.11111f);int[] leaves=Enumerable.Range(0,groups.Count).Where(i=>groups[i].Flags==2&&(groups[i].Floors.Any(createdFaces.Contains)||groups[i].Slopes.Any(createdFaces.Contains)||groups[i].Walls.Any(createdFaces.Contains))).ToArray();var targets=new HashSet<int>(leaves);
        foreach(int leafIndex in leaves){GroupData leaf=groups[leafIndex];Vector3 leafMin=leaf.Position,leafMax=leaf.Position+leaf.Size;for(int i=0;i<groups.Count;i++){if(groups[i].Flags!=1)continue;Vector3 parentMin=groups[i].Position,parentMax=parentMin+groups[i].Size;if(Contains(parentMin,parentMax,leafMin,leafMax))targets.Add(i);}}
        foreach(int index in targets){GroupData group=groups[index];Vector3 oldMin=group.Position,oldMax=group.Position+group.Size,newMin=Vector3.Min(oldMin,min),newMax=Vector3.Max(oldMax,max);groups[index]=group with{Position=newMin,Size=newMax-newMin};}
        static bool Contains(Vector3 outerMin,Vector3 outerMax,Vector3 innerMin,Vector3 innerMax)=>innerMin.X>=outerMin.X-2f&&innerMin.Y>=outerMin.Y-2f&&innerMin.Z>=outerMin.Z-2f&&innerMax.X<=outerMax.X+2f&&innerMax.Y<=outerMax.Y+2f&&innerMax.Z<=outerMax.Z+2f;
    }    private static Dictionary<int,List<int>> BuildGroupAncestorMap(IReadOnlyList<GroupData> groups)
    {
        var result=new Dictionary<int,List<int>>();int index=0;try{while(index<groups.Count)Parse(new List<int>());}catch{result.Clear();}return result;
        void Parse(List<int> parents){if(index>=groups.Count)throw new InvalidDataException();int current=index++;result[current]=new List<int>(parents);if(groups[current].Flags!=1)return;var next=new List<int>(parents){current};for(int child=0;child<4;child++)Parse(next);}
    }
    private static void RecalculateBrothers(List<GroupData> groups)
    {
        var originalOffsets=new uint[groups.Count+1];
        for(int i=0;i<groups.Count;i++)originalOffsets[i+1]=checked(originalOffsets[i]+groups[i].OriginalLength);
        var indexByOffset=new Dictionary<uint,int>();for(int i=0;i<groups.Count;i++)indexByOffset[originalOffsets[i]]=i;
        for(int i=0;i<groups.Count;i++)
        {
            uint oldDistance=groups[i].Brother;if(oldDistance==0)continue;
            uint targetOffset=checked(originalOffsets[i]+oldDistance);
            if(!indexByOffset.TryGetValue(targetOffset,out int target)||target<=i)throw new InvalidDataException($"Grupo espacial {i} possui salto de irmão inválido (0x{oldDistance:X}).");
            uint newDistance=0;for(int j=i;j<target;j++)newDistance=checked(newDistance+Length(groups[j]));
            groups[i]=groups[i] with{Brother=newDistance};
        }
    }

    private static uint GroupLength(int count)=>(uint)(36+count*2+(count&1)*2);
    private static uint Length(GroupData g)=>GroupLength(g.Floors.Length+g.Slopes.Length+g.Walls.Length);
    internal static void WriteEditedCopy(EsatFile file,string path)=>Write(file,file.Meshes.Select(Copy).ToList(),path);
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

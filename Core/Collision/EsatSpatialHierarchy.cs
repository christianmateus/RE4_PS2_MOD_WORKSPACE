using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Collision;

/// <summary>Reads the depth-first child/sibling links without assuming a fixed branching factor.</summary>
internal static class EsatSpatialHierarchy
{
    internal static IReadOnlyList<int[]> Ancestors(IReadOnlyList<uint> lengths,IReadOnlyList<uint> brothers,IReadOnlyList<bool> branches)
    {
        int count=lengths.Count;
        if(brothers.Count!=count||branches.Count!=count)throw new InvalidDataException("Tabelas espaciais incompatíveis.");
        var offsets=new Dictionary<long,int>();long offset=0;
        for(int i=0;i<count;i++){offsets.Add(offset,i);offset=checked(offset+lengths[i]);}
        var result=new int[count][];var stack=new List<(int Index,int End)>();long cursor=0;
        for(int i=0;i<count;i++)
        {
            while(stack.Count>0&&stack[^1].End==i)stack.RemoveAt(stack.Count-1);
            int parentEnd=stack.Count>0?stack[^1].End:count;
            int end=parentEnd;
            if(brothers[i]!=0&&(!offsets.TryGetValue(checked(cursor+brothers[i]),out end)||end<=i||end>parentEnd))
                throw new InvalidDataException($"Grupo espacial {i}: vínculo de irmão inválido.");
            result[i]=stack.Select(x=>x.Index).ToArray();
            if(branches[i])
            {
                if(end<=i+1)throw new InvalidDataException($"Grupo espacial {i}: ramo sem filhos.");
                stack.Add((i,end));
            }
            else if(end!=i+1)throw new InvalidDataException($"Grupo espacial {i}: registros fora da hierarquia.");
            cursor=checked(cursor+lengths[i]);
        }
        return result;
    }

    internal static IReadOnlyList<int[]> Ancestors(IReadOnlyList<EsatGroup> groups)=>Ancestors(
        groups.Select(g=>Length(g)).ToArray(),groups.Select(g=>g.BrotherDistance).ToArray(),groups.Select(g=>(g.Flags&1)!=0).ToArray());

    private static uint Length(EsatGroup g)
    {
        int count=g.FloorFaces.Length+g.SlopeFaces.Length+g.WallFaces.Length;
        return checked((uint)(36+count*2+(count&1)*2));
    }

    /// <summary>Restore conservative XZ bounds along actual ancestor paths; geometry and attributes stay intact.</summary>
    internal static int RepairBounds(EsatMesh mesh,int originalVertexCount)
    {
        var ancestors=Ancestors(mesh.Groups);var changed=new HashSet<int>();
        // An edit may have moved appended geometry beyond the leaf itself, as well as its ancestors.
        for(int fi=0;fi<mesh.Faces.Count;fi++)
        {
            var face=mesh.Faces[fi];
            if(face.Vertex0<originalVertexCount&&face.Vertex1<originalVertexCount&&face.Vertex2<originalVertexCount)continue;
            Vector3 a=mesh.Positions[face.Vertex0],b=mesh.Positions[face.Vertex1],c=mesh.Positions[face.Vertex2];
            Vector3 center=(a+b+c)/3f;
            // Original spatial partitions can legitimately reference triangles crossing their boundaries.
            // Keep those partitions when at least one complete path already reaches the edited face.
            if(mesh.Groups.Select((g,i)=>(g,i)).Any(x=>(x.g.Flags&1)==0&&References(x.g,fi)&&Contains(x.g,center)&&ancestors[x.i].All(p=>Contains(mesh.Groups[p],center))))continue;
            Vector3 min=Vector3.Min(a,Vector3.Min(b,c))-new Vector3(1.11111f),max=Vector3.Max(a,Vector3.Max(b,c))+new Vector3(1.11111f);
            for(int gi=0;gi<mesh.Groups.Count;gi++)
            {
                var leaf=mesh.Groups[gi];if((leaf.Flags&1)!=0)continue;
                if(!leaf.FloorFaces.Contains((ushort)fi)&&!leaf.SlopeFaces.Contains((ushort)fi)&&!leaf.WallFaces.Contains((ushort)fi))continue;
                foreach(int target in ancestors[gi].Append(gi))
                {
                    var g=mesh.Groups[target];Vector3 oldMin=g.Position,oldMax=oldMin+g.Size;
                    var nextMin=new Vector3(Math.Min(oldMin.X,min.X),oldMin.Y,Math.Min(oldMin.Z,min.Z));
                    var nextMax=new Vector3(Math.Max(oldMax.X,max.X),oldMax.Y,Math.Max(oldMax.Z,max.Z));
                    if(Near(nextMin.X,oldMin.X)&&Near(nextMin.Z,oldMin.Z)&&Near(nextMax.X,oldMax.X)&&Near(nextMax.Z,oldMax.Z))continue;
                    g.Position=nextMin;g.Size=nextMax-nextMin;changed.Add(target);
                }
            }
        }
        return changed.Count;
        static bool References(EsatGroup g,int f)=>g.FloorFaces.Contains((ushort)f)||g.SlopeFaces.Contains((ushort)f)||g.WallFaces.Contains((ushort)f);
        static bool Contains(EsatGroup g,Vector3 p)=>p.X>=g.Position.X-.01f&&p.Z>=g.Position.Z-.01f&&p.X<=g.Position.X+g.Size.X+.01f&&p.Z<=g.Position.Z+g.Size.Z+.01f;
        static bool Near(float a,float b)=>Math.Abs(a-b)<=Math.Max(.01f,Math.Max(Math.Abs(a),Math.Abs(b))*1e-6f);
    }
}

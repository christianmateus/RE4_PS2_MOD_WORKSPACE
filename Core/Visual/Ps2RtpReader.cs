using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class RtpNode
{
    public int Index { get; set; }
    public Vector3 Position { get; set; }
    public ushort ConnectionTableIndex { get; set; }
    public ushort ConnectionCount { get; set; }
    public RtpNode(int index, Vector3 position, ushort connectionTableIndex=0, ushort connectionCount=0)
    { Index=index; Position=position; ConnectionTableIndex=connectionTableIndex; ConnectionCount=connectionCount; }
    public override string ToString()=>$"Waypoint #{Index:D3}  •  X {Position.X:0.##}  Y {Position.Y:0.##}  Z {Position.Z:0.##}";
}
public sealed record RtpConnection(int From, int To, ushort Distance);

public sealed class RtpScene
{
    public string SourcePath { get; init; } = string.Empty;
    public List<RtpNode> Nodes { get; init; } = new();
    public List<RtpConnection> Connections { get; init; } = new();
    public int RouteTableSize { get; init; }
    public bool IsModified { get; set; }
}

public static class Ps2RtpWriter
{
    private const float RawScale = 100f;

    public static bool Save(RtpScene scene, string backupPath)
    {
        if (scene.Nodes.Count is <= 0 or > 255) throw new InvalidDataException("O RTP precisa conter entre 1 e 255 waypoints.");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        bool backupCreated=false;
        if (!File.Exists(backupPath)) { File.Copy(scene.SourcePath, backupPath); backupCreated=true; }

        List<RtpConnection> ordered=scene.Connections.OrderBy(x=>x.From).ThenBy(x=>x.To).ToList();
        if(ordered.Any(x=>x.From<0||x.From>=scene.Nodes.Count||x.To<0||x.To>=scene.Nodes.Count))throw new InvalidDataException("RTP possui conexão fora da tabela de nodes.");
        int nodeOffset=0x20, connectionOffset=nodeOffset+scene.Nodes.Count*0x20, routeOffset=connectionOffset+ordered.Count*4;
        byte[,] routes=BuildRoutes(scene.Nodes.Count,ordered);

        string temp=scene.SourcePath+".tmp";
        try
        {
            using(var fs=File.Create(temp))using(var bw=new BinaryWriter(fs))
            {
                bw.Write(0x32525450u);bw.Write((ushort)0);bw.Write((ushort)scene.Nodes.Count);bw.Write((ushort)ordered.Count);bw.Write((ushort)(scene.Nodes.Count*scene.Nodes.Count));
                bw.Write((uint)nodeOffset);bw.Write((uint)connectionOffset);bw.Write((uint)routeOffset);bw.Write(new byte[8]);
                int cursor=0;
                for(int i=0;i<scene.Nodes.Count;i++)
                {
                    RtpNode node=scene.Nodes[i];List<RtpConnection> links=ordered.Where(x=>x.From==i).ToList();node.Index=i;node.ConnectionTableIndex=(ushort)cursor;node.ConnectionCount=(ushort)links.Count;cursor+=links.Count;
                    bw.Write(node.Position.X*RawScale);bw.Write(node.Position.Y*RawScale);bw.Write(node.Position.Z*RawScale);bw.Write(1f);bw.Write(node.ConnectionTableIndex);bw.Write(node.ConnectionCount);bw.Write(new byte[12]);
                }
                foreach(RtpConnection link in ordered){bw.Write((ushort)link.To);bw.Write(link.Distance);}
                for(int row=0;row<scene.Nodes.Count;row++)for(int col=0;col<scene.Nodes.Count;col++)bw.Write(routes[row,col]);
                int padding=(32-(int)(fs.Position%32))%32;if(padding>0)bw.Write(new byte[padding]);
            }
            File.Move(temp,scene.SourcePath,true);scene.IsModified=false;return backupCreated;
        }
        finally { if(File.Exists(temp))File.Delete(temp); }
    }

    public static ushort CalculateDistance(Vector3 a,Vector3 b)=>
        (ushort)Math.Clamp((int)MathF.Round(Vector3.Distance(a,b)*10f),0,ushort.MaxValue);

    private static byte[,] BuildRoutes(int count,IReadOnlyList<RtpConnection> connections)
    {
        const int infinity=int.MaxValue/4;var distances=new int[count,count];var next=new byte[count,count];
        for(int i=0;i<count;i++)for(int j=0;j<count;j++){distances[i,j]=i==j?0:infinity;next[i,j]=i==j?(byte)i:(byte)0xFF;}
        foreach(RtpConnection edge in connections)if(edge.Distance<distances[edge.From,edge.To]){distances[edge.From,edge.To]=edge.Distance;next[edge.From,edge.To]=(byte)edge.To;}
        for(int k=0;k<count;k++)for(int i=0;i<count;i++)if(distances[i,k]<infinity)for(int j=0;j<count;j++)
        {if(distances[k,j]>=infinity)continue;int candidate=distances[i,k]+distances[k,j];if(candidate<distances[i,j]){distances[i,j]=candidate;next[i,j]=next[i,k];}}
        return next;
    }
}

public static class Ps2RtpReader
{
    private const uint Magic = 0x32525450; // "PTR2" in the little-endian file.
    private const float WorldScale = 1f / 100f;

    public static RtpScene Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Caminho RTP inválido.", nameof(path));
        using FileStream stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        if (stream.Length < 0x20) throw new InvalidDataException("RTP menor que o cabeçalho PS2.");
        if (reader.ReadUInt32() != Magic) throw new InvalidDataException("Magic RTP inválido; esperado PTR2.");

        reader.ReadUInt16();
        int nodeCount = reader.ReadUInt16();
        int connectionCount = reader.ReadUInt16();
        int routeTableSize = reader.ReadUInt16();
        uint nodesOffset = reader.ReadUInt32();
        uint connectionsOffset = reader.ReadUInt32();
        uint routesOffset = reader.ReadUInt32();

        if (nodeCount <= 0 || nodeCount > 255) throw new InvalidDataException($"Quantidade de nodes RTP inválida: {nodeCount}.");
        if (routeTableSize != nodeCount * nodeCount) throw new InvalidDataException("A matriz de rotas RTP não corresponde à quantidade de nodes.");
        ValidateRange(nodesOffset, nodeCount * 0x20L, stream.Length, "nodes");
        ValidateRange(connectionsOffset, connectionCount * 4L, stream.Length, "conexões");
        ValidateRange(routesOffset, routeTableSize, stream.Length, "matriz de rotas");

        var nodes = new List<RtpNode>(nodeCount);
        stream.Position = nodesOffset;
        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 position = new(reader.ReadSingle() * WorldScale, reader.ReadSingle() * WorldScale, reader.ReadSingle() * WorldScale);
            reader.ReadSingle(); // Homogeneous W, normally 1.0.
            ushort first = reader.ReadUInt16();
            ushort count = reader.ReadUInt16();
            reader.ReadBytes(12);
            if (!IsFinite(position)) throw new InvalidDataException($"Node RTP {i} possui coordenada inválida.");
            if ((long)first + count > connectionCount) throw new InvalidDataException($"Conexões do node RTP {i} estão fora da tabela.");
            nodes.Add(new RtpNode(i, position, first, count));
        }

        var connections = new List<RtpConnection>(connectionCount);
        foreach (RtpNode node in nodes)
        {
            stream.Position = connectionsOffset + node.ConnectionTableIndex * 4L;
            for (int i = 0; i < node.ConnectionCount; i++)
            {
                ushort destination = reader.ReadUInt16();
                ushort distance = reader.ReadUInt16();
                if (destination >= nodeCount) throw new InvalidDataException($"Node RTP {node.Index} aponta para o node inexistente {destination}.");
                connections.Add(new RtpConnection(node.Index, destination, distance));
            }
        }

        return new RtpScene { SourcePath = path, Nodes = nodes, Connections = connections, RouteTableSize = routeTableSize };
    }

    private static void ValidateRange(long offset, long size, long length, string name)
    {
        if (offset < 0x18 || size < 0 || offset > length || size > length - offset)
            throw new InvalidDataException($"Tabela RTP de {name} fora do arquivo.");
    }

    private static bool IsFinite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}

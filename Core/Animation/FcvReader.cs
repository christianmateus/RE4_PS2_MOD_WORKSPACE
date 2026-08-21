using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;

public sealed class FcvAnimation
{
    public string FilePath { get; init; } = "";
    public ushort FrameCount { get; init; }
    public byte TrackCount { get; init; }
    public uint DeclaredFileSize { get; init; }
    public long ActualFileSize { get; init; }
    public List<FcvTrack> Tracks { get; } = new();
}

public sealed class FcvTrack
{
    public int Index { get; init; }
    public byte NodeId { get; init; }
    public byte Type { get; init; }
    public byte DataType { get; init; }
    public uint Offset { get; init; }
    public int PhysicalOrder { get; set; }
    public FcvAxis X { get; init; } = new();
    public FcvAxis Y { get; init; } = new();
    public FcvAxis Z { get; init; } = new();
    public string TypeName => Type switch { 0x01 => "Movement (Relative)", 0x02 => "Rotation (Relative)", 0x04 => "Translation (Relative)", 0x08 => "Scale (Relative)", 0x10 => "Rotation (Absolute?)", 0x20 => "Translation (Absolute?)", 0x40 => "Scale (Absolute?)", 0x80 => "Flip Bone", _ => $"Unknown 0x{Type:X2}" };
    public override string ToString() => $"#{Index:D2}  Node {NodeId:X2}  {TypeName}  [{Type:X2}/{DataType:X2}]";
}

public sealed class FcvAxis
{
    public List<FcvKey> Keys { get; } = new();
}

public sealed record FcvKey(ushort Frame, double Value, double TangentIn, double TangentOut, double Extra);

public static class FcvReader
{
    public static FcvAnimation Read(string path)
    {
        using var fs = File.OpenRead(path);
        return Read(fs, path);
    }

    public static FcvAnimation Read(byte[] data, string sourceName = "embedded.fcv")
    {
        using var ms = new MemoryStream(data, writable: false);
        return Read(ms, sourceName);
    }

    private static FcvAnimation Read(Stream fs, string sourceName)
    {
        using var br = new BinaryReader(fs, System.Text.Encoding.UTF8, leaveOpen: true);
        if (fs.Length < 8) throw new InvalidDataException("FCV muito pequeno.");
        ushort frames = br.ReadUInt16();
        byte count = br.ReadByte();
        if (count == 0) throw new InvalidDataException("FCV sem tracks.");
        var types = new byte[count]; var data = new byte[count];
        for (int i = 0; i < count; i++) { types[i] = br.ReadByte(); data[i] = br.ReadByte(); }
        var nodes = br.ReadBytes(count);
        if (nodes.Length != count) throw new EndOfStreamException();
        while ((fs.Position & 3) != 0) br.ReadByte();
        Span<byte> sizeBytes = stackalloc byte[4];
        if (fs.Read(sizeBytes) != 4) throw new EndOfStreamException();
        uint declaredSize = BinaryPrimitives.ReadUInt32BigEndian(sizeBytes);
        var offsets = new uint[count];
        for (int i = 0; i < count; i++) offsets[i] = br.ReadUInt32();
        long dataStart = fs.Position;
        for (int i = 0; i < count; i++) if (offsets[i] < dataStart || offsets[i] >= fs.Length) throw new InvalidDataException($"Offset inválido no track {i}: 0x{offsets[i]:X8}.");
        var order = offsets.Select((v, i) => (v, i)).OrderBy(x => x.v).Select((x, n) => (x.i, n: n + 1)).ToDictionary(x => x.i, x => x.n);
        var anim = new FcvAnimation { FilePath = sourceName, FrameCount = frames, TrackCount = count, DeclaredFileSize = declaredSize, ActualFileSize = fs.Length };
        for (int i = 0; i < count; i++)
        {
            fs.Position = offsets[i];
            int encoding = data[i] >> 4;
            var track = new FcvTrack { Index = i, NodeId = nodes[i], Type = types[i], DataType = data[i], Offset = offsets[i], PhysicalOrder = order[i], X = ReadAxis(br, encoding), Y = ReadAxis(br, encoding), Z = ReadAxis(br, encoding) };
            anim.Tracks.Add(track);
        }
        return anim;
    }

    private static FcvAxis ReadAxis(BinaryReader br, int encoding)
    {
        var axis = new FcvAxis();
        ushort count = br.ReadUInt16();
        var frames = new ushort[count];
        for (int i = 0; i < count; i++) frames[i] = br.ReadUInt16();
        for (int i = 0; i < count; i++)
        {
            (double v, double tin, double tout, double extra) = encoding switch
            {
                0x0 => (br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), 0),
                0x1 => (br.ReadSingle(), br.ReadInt16(), br.ReadInt16(), 0),
                0x2 => (br.ReadInt16(), br.ReadInt16(), br.ReadInt16(), 0),
                0x4 => (br.ReadInt16(), br.ReadSingle(), br.ReadSingle(), 0),
                0x5 => (br.ReadInt16(), br.ReadInt16(), br.ReadUInt16(), 0),
                0x6 => (br.ReadInt16(), unchecked((sbyte)br.ReadByte()), unchecked((sbyte)br.ReadByte()), 0),
                0x8 => (unchecked((sbyte)br.ReadByte()), br.ReadSingle(), br.ReadSingle(), 0),
                0x9 => (unchecked((sbyte)br.ReadByte()), br.ReadInt16(), br.ReadInt16(), 0),
                0xA => (unchecked((sbyte)br.ReadByte()), unchecked((sbyte)br.ReadByte()), unchecked((sbyte)br.ReadByte()), 0),
                0xF => (unchecked((sbyte)br.ReadByte()), br.ReadByte(), br.ReadByte(), br.ReadByte()),
                _ => throw new InvalidDataException($"Encoding FCV desconhecido: 0x{encoding:X1}0")
            };
            axis.Keys.Add(new FcvKey(frames[i], v, tin, tout, extra));
        }
        return axis;
    }
}

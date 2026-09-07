using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class EtmCatalog
{
    public string SourcePath { get; init; } = string.Empty;
    public IReadOnlyList<EtmResource> Resources { get; init; } = Array.Empty<EtmResource>();
    public IReadOnlyDictionary<byte, EtmObjectDefinition> Objects { get; init; } = new Dictionary<byte, EtmObjectDefinition>();
    public IReadOnlyDictionary<byte, IReadOnlyList<ScenarioTriangle>> Models { get; init; } = new Dictionary<byte, IReadOnlyList<ScenarioTriangle>>();
    public IReadOnlyDictionary<byte, IReadOnlyList<EtmModelPart>> ModelParts { get; init; } = new Dictionary<byte, IReadOnlyList<EtmModelPart>>();
}

public sealed record EtmResource(int FileOrder, uint Type, string Name, byte[] Data);
public sealed record EtmModelPart(EtmResource Bin, EtmResource? Effect, EtmResource? TextureFallback, IReadOnlyList<ScenarioTriangle> Triangles);
public sealed record EtmObjectDefinition(byte Id, IReadOnlyList<EtmResource> Resources)
{
    public string DisplayName => EtsObjectNames.Get(Id);
    public override string ToString() => $"{DisplayName} ({Resources.Count} resources)";
}

public sealed class EtsScene
{
    public string SourcePath { get; init; } = string.Empty;
    public byte[] Header { get; init; } = new byte[0x10];
    public byte[] Footer { get; init; } = Enumerable.Repeat((byte)0xCD, 0x10).ToArray();
    public List<EtsEntry> Entries { get; } = new();
    public bool IsModified { get; set; }
}

[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class EtsEntry
{
    internal byte[] RawData { get; set; } = new byte[0x40];
    [Browsable(false)] public int FileOrder { get; set; }

    [Category("Object"), DisplayName("Object ID")]
    public byte ObjectId { get; set; }
    [Category("Object"), DisplayName("Instance Index")]
    public ushort InstanceIndex { get; set; }

    [Category("Position")] public float PositionX { get; set; }
    [Category("Position")] public float PositionY { get; set; }
    [Category("Position")] public float PositionZ { get; set; }
    [Category("Rotation (radians)")] public float RotationX { get; set; }
    [Category("Rotation (radians)")] public float RotationY { get; set; }
    [Category("Rotation (radians)")] public float RotationZ { get; set; }
    // Preserved for binary round-tripping, but ETS does not expose a functional scale control.
    [Browsable(false)] public float ScaleX { get; set; } = 1f;
    [Browsable(false)] public float ScaleY { get; set; } = 1f;
    [Browsable(false)] public float ScaleZ { get; set; } = 1f;

    public override string ToString() => $"#{InstanceIndex:D3} {EtsObjectNames.Get(ObjectId)}";

    public EtsEntry Clone() => new()
    {
        RawData = (byte[])RawData.Clone(), FileOrder = FileOrder, ObjectId = ObjectId, InstanceIndex = InstanceIndex,
        PositionX = PositionX, PositionY = PositionY, PositionZ = PositionZ,
        RotationX = RotationX, RotationY = RotationY, RotationZ = RotationZ,
        ScaleX = ScaleX, ScaleY = ScaleY, ScaleZ = ScaleZ
    };
}

public static class Ps2EtmReader
{
    public static EtmCatalog Read(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 0x20) throw new InvalidDataException("ETM is too small.");
        uint count = BitConverter.ToUInt32(data, 0);
        int offset = 0x20;
        var resources = new List<EtmResource>();
        for (int i = 0; i < count; i++)
        {
            if (offset + 0x40 > data.Length) throw new InvalidDataException($"ETM entry {i} header is outside the file.");
            uint size = BitConverter.ToUInt32(data, offset);
            uint type = BitConverter.ToUInt32(data, offset + 4);
            if (size < 0x40 || offset + size > data.Length) throw new InvalidDataException($"ETM entry {i} has an invalid size.");
            int nameEnd = Array.IndexOf(data, (byte)0, offset + 0x20, 0x20);
            if (nameEnd < 0) nameEnd = offset + 0x40;
            string name = System.Text.Encoding.ASCII.GetString(data, offset + 0x20, nameEnd - offset - 0x20);
            byte[] payload = data.AsSpan(offset + 0x40, checked((int)size - 0x40)).ToArray();
            resources.Add(new EtmResource(i, type, name, payload));
            offset += checked((int)size);
        }
        if (offset != data.Length) throw new InvalidDataException($"ETM has {data.Length - offset} unexpected trailing bytes.");

        var objects = resources.Select(r => (Resource: r, Id: ParseObjectId(r.Name)))
            .Where(x => x.Id.HasValue).GroupBy(x => x.Id!.Value)
            .ToDictionary(g => g.Key, g => new EtmObjectDefinition(g.Key, g.Select(x => x.Resource).ToArray()));
        var models = new Dictionary<byte, IReadOnlyList<ScenarioTriangle>>();
        var modelParts = new Dictionary<byte, IReadOnlyList<EtmModelPart>>();
        foreach ((byte id, EtmObjectDefinition definition) in objects)
        {
            var triangles = new List<ScenarioTriangle>();
            var parts = new List<EtmModelPart>();
            foreach (EtmResource resource in definition.Resources.Where(r => r.Name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    IReadOnlyList<ScenarioTriangle> mesh = Ps2ScenarioReader.ReadStandaloneBin(resource.Data);
                    triangles.AddRange(mesh);
                    if (mesh.Count > 0) parts.Add(new EtmModelPart(resource, FindEffect(definition.Resources, id), FindTexturePackage(definition.Resources, resource), mesh));
                }
                catch (InvalidDataException) { /* Some ETM BIN resources are control data rather than meshes. */ }
                catch (EndOfStreamException) { }
            }
            if (triangles.Count > 0) models[id] = triangles;
            if (parts.Count > 0) modelParts[id] = parts;
        }
        return new EtmCatalog { SourcePath = path, Resources = resources, Objects = objects, Models = models, ModelParts = modelParts };
    }

    private static byte? ParseObjectId(string name)
    {
        if (!name.StartsWith("et", StringComparison.OrdinalIgnoreCase) || name.Length < 4) return null;
        return byte.TryParse(name.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte id) ? id : null;
    }

    private static EtmResource? FindTexturePackage(IReadOnlyList<EtmResource> resources, EtmResource bin)
    {
        string stem = Path.GetFileNameWithoutExtension(bin.Name);
        EtmResource? exact = resources.FirstOrDefault(r => r.Name.Equals(stem + ".tpl", StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;
        // Additional BINs commonly reuse the only/nearest texture package in etXX.
        return resources.Where(r => r.Name.EndsWith(".tpl", StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => Math.Abs(r.FileOrder - bin.FileOrder)).FirstOrDefault();
    }

    private static EtmResource? FindEffect(IReadOnlyList<EtmResource> resources, byte id) =>
        resources.FirstOrDefault(r => r.Name.Equals($"et{id:X2}.eff", StringComparison.OrdinalIgnoreCase));
}

public static class Ps2EtsReader
{
    public static EtsScene Read(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 0x20) throw new InvalidDataException("ETS is too small.");
        uint count = BitConverter.ToUInt32(data, 0);
        long expected = 0x10L + count * 0x40L + 0x10L;
        if (expected != data.Length) throw new InvalidDataException($"ETS size mismatch: expected {expected}, found {data.Length}.");
        var scene = new EtsScene { SourcePath = path, Header = data[..0x10], Footer = data[^0x10..] };
        for (int i = 0; i < count; i++)
        {
            int o = 0x10 + i * 0x40;
            byte[] raw = data.AsSpan(o, 0x40).ToArray();
            scene.Entries.Add(new EtsEntry
            {
                FileOrder = i, RawData = raw,
                ScaleX = BitConverter.ToSingle(raw, 0), ScaleY = BitConverter.ToSingle(raw, 4), ScaleZ = BitConverter.ToSingle(raw, 8),
                RotationX = BitConverter.ToSingle(raw, 0x10), RotationY = BitConverter.ToSingle(raw, 0x14), RotationZ = BitConverter.ToSingle(raw, 0x18),
                PositionX = BitConverter.ToSingle(raw, 0x20), PositionY = BitConverter.ToSingle(raw, 0x24), PositionZ = BitConverter.ToSingle(raw, 0x28),
                ObjectId = raw[0x30], InstanceIndex = BitConverter.ToUInt16(raw, 0x32)
            });
        }
        return scene;
    }
}

public static class Ps2EtsWriter
{
    public static void Write(EtsScene scene, string path)
    {
        byte[] output = new byte[0x10 + scene.Entries.Count * 0x40 + 0x10];
        scene.Header.AsSpan(0, Math.Min(0x10, scene.Header.Length)).CopyTo(output);
        BitConverter.GetBytes((uint)scene.Entries.Count).CopyTo(output, 0);
        for (int i = 0; i < scene.Entries.Count; i++)
        {
            EtsEntry e = scene.Entries[i]; int o = 0x10 + i * 0x40;
            e.RawData.AsSpan(0, Math.Min(0x40, e.RawData.Length)).CopyTo(output.AsSpan(o, 0x40));
            Write(output, o, e.ScaleX, e.ScaleY, e.ScaleZ); Write(output, o + 0x10, e.RotationX, e.RotationY, e.RotationZ);
            Write(output, o + 0x20, e.PositionX, e.PositionY, e.PositionZ);
            output[o + 0x30] = e.ObjectId; BitConverter.GetBytes(e.InstanceIndex).CopyTo(output, o + 0x32);
            e.FileOrder = i;
        }
        scene.Footer.AsSpan(0, Math.Min(0x10, scene.Footer.Length)).CopyTo(output.AsSpan(output.Length - 0x10));
        File.WriteAllBytes(path, output); scene.IsModified = false;
    }
    private static void Write(byte[] b, int o, float x, float y, float z) { BitConverter.GetBytes(x).CopyTo(b, o); BitConverter.GetBytes(y).CopyTo(b, o + 4); BitConverter.GetBytes(z).CopyTo(b, o + 8); }
}

using System.ComponentModel;
using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public sealed class EmiEntry
{
    internal byte[] RawData { get; }
    internal Vector3 OriginalPosition { get; set; }
    internal float OriginalDirectionDegrees { get; set; }
    internal bool HasOriginalValues { get; set; }

    [Browsable(false)] public int Index { get; internal set; }
    [Category("EMI"), DisplayName("Tipo"), Description("Tipo de ponto usado pela lógica do cenário (1–19 no editor original).")]
    public byte Type { get; set; }
    [Category("EMI"), DisplayName("Work 0"), Description("Parâmetro específico do tipo. Em rotas, normalmente identifica o grupo ou a variante.")]
    public byte Work0 { get; set; }
    [Category("EMI"), DisplayName("Work 1"), Description("Parâmetro específico do tipo.")]
    public byte Work1 { get; set; }
    [Category("EMI"), DisplayName("Work 2"), Description("Parâmetro específico do tipo.")]
    public byte Work2 { get; set; }
    [Browsable(false)] public Vector3 Position { get; set; }
    [Category("Transform"), DisplayName("Posição X")]
    public float PositionX { get => Position.X; set => Position = new Vector3(value, Position.Y, Position.Z); }
    [Category("Transform"), DisplayName("Posição Y")]
    public float PositionY { get => Position.Y; set => Position = new Vector3(Position.X, value, Position.Z); }
    [Category("Transform"), DisplayName("Posição Z")]
    public float PositionZ { get => Position.Z; set => Position = new Vector3(Position.X, Position.Y, value); }
    [Category("Transform"), DisplayName("Direção Y (graus)"), Description("Direção horizontal do ponto, editada em graus e gravada em radianos.")]
    public float DirectionDegrees { get; set; }
    [Category("EMI"), DisplayName("Uso conhecido"), ReadOnly(true)]
    public string TypeName => EmiTypeNames.Get(Type);

    internal EmiEntry(int index, byte[] raw)
    {
        Index = index;
        RawData = raw;
    }

    internal EmiEntry Clone(int index)
    {
        return new EmiEntry(index, (byte[])RawData.Clone())
        {
            Type = Type, Work0 = Work0, Work1 = Work1, Work2 = Work2,
            Position = Position, DirectionDegrees = DirectionDegrees,
            OriginalPosition = Position, OriginalDirectionDegrees = DirectionDegrees, HasOriginalValues = true
        };
    }

    public override string ToString() => $"#{Index:D3}  T{Type:D2}  {TypeName}";
}

public sealed class EmiScene
{
    public string SourcePath { get; init; } = string.Empty;
    public List<EmiEntry> Entries { get; } = new();
    public bool IsModified { get; set; }
    public bool WasEmptyFile { get; init; }
    internal int OriginalEntryCount { get; init; }
    internal byte[] HeaderReserved { get; init; } = new byte[4];
    internal byte[] TrailingData { get; init; } = Array.Empty<byte>();
}

public static class EmiTypeNames
{
    private static readonly string[] Names =
    {
        "Desconhecido / livre", "EM10 • esconderijo", "EM2F • rota", "EM2B • casa",
        "EM2B • pedra", "Ashley • rota de retirada", "Pedra/barril • rota", "Pedra • fuga",
        "Pedra • início", "Ashley • salto", "Cão • latido", "Ashley • rota alternativa",
        "Retorno • início", "EM2B • direção de captura", "No. 3 • sniper",
        "EM10 • destino", "EM2D • sem parede", "EM32 • salto", "EM32 • teto", "EM3A • rota"
    };

    public static string Get(byte type) => type < Names.Length ? Names[type] : $"Tipo desconhecido 0x{type:X2}";
}

public static class Ps2EmiReader
{
    private const int HeaderSize = 8;
    private const int EntrySize = 0x40;
    private const float WorldScale = 1f / 100f;

    public static EmiScene Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Caminho EMI inválido.", nameof(path));
        byte[] data = File.ReadAllBytes(path);
        if (data.Length == 0) return new EmiScene { SourcePath = path, WasEmptyFile = true };
        if (data.Length < HeaderSize) throw new InvalidDataException("EMI menor que o cabeçalho PS2.");

        int count = BitConverter.ToInt32(data, 0);
        if (count < 0 || count > 256) throw new InvalidDataException($"Quantidade de pontos EMI inválida: {count}.");
        long required = HeaderSize + count * (long)EntrySize;
        if (required > data.Length) throw new InvalidDataException("A tabela de pontos EMI está truncada.");
        var scene = new EmiScene
        {
            SourcePath = path,
            OriginalEntryCount = count,
            HeaderReserved = data.AsSpan(4, 4).ToArray(),
            TrailingData = data.AsSpan((int)required).ToArray()
        };

        for (int i = 0; i < count; i++)
        {
            int offset = HeaderSize + i * EntrySize;
            byte[] raw = data.AsSpan(offset, EntrySize).ToArray();
            Vector3 position = new(
                BitConverter.ToSingle(raw, 0x08) * WorldScale,
                BitConverter.ToSingle(raw, 0x0C) * WorldScale,
                BitConverter.ToSingle(raw, 0x10) * WorldScale);
            float direction = BitConverter.ToSingle(raw, 0x18);
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z) || !float.IsFinite(direction))
                throw new InvalidDataException($"Ponto EMI {i} contém valores não finitos.");
            scene.Entries.Add(new EmiEntry(i, raw)
            {
                Position = position,
                DirectionDegrees = direction * (180f / MathF.PI),
                Type = raw[0x1C], Work0 = raw[0x1D], Work1 = raw[0x1E], Work2 = raw[0x1F],
                OriginalPosition = position, OriginalDirectionDegrees = direction * (180f / MathF.PI), HasOriginalValues = true
            });
        }
        return scene;
    }
}

public static class Ps2EmiWriter
{
    private const int EntrySize = 0x40;
    private const float RawScale = 100f;

    public static bool Save(EmiScene scene, string backupPath)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.Entries.Count > 256) throw new InvalidDataException("O EMI aceita no máximo 256 pontos.");
        bool backupCreated = false;
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        if (File.Exists(scene.SourcePath) && !File.Exists(backupPath)) { File.Copy(scene.SourcePath, backupPath); backupCreated = true; }

        string temp = scene.SourcePath + ".tmp";
        try
        {
            using (var stream = File.Create(temp))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(scene.Entries.Count);
                writer.Write(scene.HeaderReserved.Length == 4 ? scene.HeaderReserved : new byte[4]);
                for (int i = 0; i < scene.Entries.Count; i++)
                {
                    EmiEntry entry = scene.Entries[i];
                    if (!float.IsFinite(entry.Position.X) || !float.IsFinite(entry.Position.Y) || !float.IsFinite(entry.Position.Z) || !float.IsFinite(entry.DirectionDegrees))
                        throw new InvalidDataException($"Ponto EMI {i} contém valores inválidos.");
                    byte[] raw = entry.RawData.Length == EntrySize ? (byte[])entry.RawData.Clone() : new byte[EntrySize];
                    if (!entry.HasOriginalValues || entry.Position.X != entry.OriginalPosition.X) WriteSingle(raw, 0x08, entry.Position.X * RawScale);
                    if (!entry.HasOriginalValues || entry.Position.Y != entry.OriginalPosition.Y) WriteSingle(raw, 0x0C, entry.Position.Y * RawScale);
                    if (!entry.HasOriginalValues || entry.Position.Z != entry.OriginalPosition.Z) WriteSingle(raw, 0x10, entry.Position.Z * RawScale);
                    if (!entry.HasOriginalValues) WriteSingle(raw, 0x14, 1f);
                    if (!entry.HasOriginalValues || entry.DirectionDegrees != entry.OriginalDirectionDegrees)
                        WriteSingle(raw, 0x18, entry.DirectionDegrees * (MathF.PI / 180f));
                    raw[0x1C] = entry.Type; raw[0x1D] = entry.Work0; raw[0x1E] = entry.Work1; raw[0x1F] = entry.Work2;
                    writer.Write(raw);
                    entry.Index = i;
                }
                if (scene.Entries.Count == scene.OriginalEntryCount && scene.TrailingData.Length > 0) writer.Write(scene.TrailingData);
                else { int padding = (32 - (int)(stream.Position % 32)) % 32; if (padding > 0) writer.Write(new byte[padding]); }
            }
            File.Move(temp, scene.SourcePath, true);
            scene.IsModified = false;
            return backupCreated;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    private static void WriteSingle(byte[] target, int offset, float value) => BitConverter.GetBytes(value).CopyTo(target, offset);
}

using System.Numerics;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2AevReader
{
    public const int HeaderSize = 0x10;
    private const int EventSizePs2 = 0xA0;
    private const int EventSizeLegacy = 0x98;
    private const int EventSizeWithRadius = 0x9C;
    private const float WorldScale = 1f / 100f;

    public static AevScene Read(string path)
    {
        byte[] file = File.ReadAllBytes(path);
        if (file.Length < HeaderSize) throw new InvalidDataException("AEV muito pequeno.");
        if (file[0] != (byte)'A' || file[1] != (byte)'E' || file[2] != (byte)'V' || file[3] != 0)
            throw new InvalidDataException("Assinatura AEV inválida. Esperado: AEV\\0.");

        byte unknown1 = file[4];
        byte unknown2 = file[5];
        int count16 = BitConverter.ToUInt16(file, 6);
        int count8 = file[6];
        if (count16 <= 0) return new AevScene { SourcePath = path, HeaderUnknown1 = unknown1, HeaderUnknown2 = unknown2, EntrySize = 0, LayoutName = "Empty", Entries = new List<AevEntry>() };

        // RE4 2007 / PS2: header count is ushort and each AEV line is exactly 160 bytes (0xA0).
        // Prefer this layout whenever the file can contain the declared number of PS2 entries.
        long ps2Required = HeaderSize + (long)count16 * EventSizePs2;
        if (ps2Required <= file.Length)
        {
            Candidate ps2 = TryParseCandidate(file, count16, EventSizePs2, hasRadiusField: true, ps2Layout: true);
            if (ps2.Valid)
            {
                return new AevScene
                {
                    SourcePath = path, HeaderUnknown1 = unknown1, HeaderUnknown2 = unknown2,
                    EntrySize = EventSizePs2, LayoutName = "RE4 2007 PS2 • AEV 0xA0",
                    Entries = ps2.Entries
                };
            }
        }

        // Fallback for files originating from PC/UHD tools.
        Candidate c98 = TryParseCandidate(file, count8, EventSizeLegacy, hasRadiusField: false, ps2Layout: false);
        Candidate c9c = TryParseCandidate(file, count8, EventSizeWithRadius, hasRadiusField: true, ps2Layout: false);
        Candidate best = ChooseBest(file.Length, count8, c98, c9c);
        if (!best.Valid) throw new InvalidDataException("Não foi possível reconhecer a estrutura das entries do AEV.");

        return new AevScene
        {
            SourcePath = path, HeaderUnknown1 = unknown1, HeaderUnknown2 = unknown2,
            EntrySize = best.EntrySize,
            LayoutName = best.HasRadiusField ? "AEV 0x9C (Radius fallback)" : "AEV 0x98 (Classic fallback)",
            Entries = best.Entries
        };
    }

    private sealed class Candidate
    {
        public bool Valid;
        public int EntrySize;
        public bool HasRadiusField;
        public int Score;
        public List<AevEntry> Entries = new();
    }

    private static Candidate TryParseCandidate(byte[] file, int count, int entrySize, bool hasRadiusField, bool ps2Layout)
    {
        var result = new Candidate { EntrySize = entrySize, HasRadiusField = hasRadiusField };
        if (count <= 0) return result;
        long required = HeaderSize + (long)count * entrySize;
        if (required > file.Length) return result;

        try
        {
            for (int i = 0; i < count; i++)
            {
                int offset = HeaderSize + i * entrySize;
                byte[] raw = new byte[entrySize];
                Buffer.BlockCopy(file, offset, raw, 0, entrySize);
                AevEntry entry = ParseEntry(raw, i, hasRadiusField, ps2Layout);
                result.Entries.Add(entry);
                result.Score += ScoreEntry(entry, i);
            }
            result.Valid = true;
        }
        catch
        {
            result.Valid = false;
            result.Entries.Clear();
        }
        return result;
    }

    private static Candidate ChooseBest(int fileLength, int count, Candidate a, Candidate b)
    {
        if (!a.Valid) return b;
        if (!b.Valid) return a;
        int remA = Math.Abs(fileLength - (HeaderSize + count * a.EntrySize));
        int remB = Math.Abs(fileLength - (HeaderSize + count * b.EntrySize));
        if (remA == 0 && remB != 0) return a;
        if (remB == 0 && remA != 0) return b;
        if (Math.Abs(remA - remB) >= 4) return remA < remB ? a : b;
        return a.Score >= b.Score ? a : b;
    }

    private static int ScoreEntry(AevEntry e, int fileOrder)
    {
        int score = 0;
        if (AevNames.IsKnownEventType(e.Type)) score += 10; else if (e.Type <= 0x30) score += 1; else score -= 6;
        if (e.AreaHitType <= 2) score += 5; else score -= 5;
        if (e.Index == fileOrder || e.Index == (byte)fileOrder) score += 5;
        else if (Math.Abs(e.Index - fileOrder) <= 2) score += 1;
        if (Reasonable(e.Y) && Reasonable(e.Height) && Reasonable(e.Position1.X) && Reasonable(e.Position1.Y)) score += 4; else score -= 8;
        return score;
    }

    private static bool Reasonable(float v) => float.IsFinite(v) && MathF.Abs(v) < 100000f;

    private static AevEntry ParseEntry(byte[] raw, int fileOrder, bool hasRadiusField, bool ps2Layout)
    {
        using MemoryStream ms = new(raw, writable: false);
        using BinaryReader br = new(ms, Encoding.ASCII, leaveOpen: false);

        br.ReadUInt32(); // runtime pointer / next
        br.ReadByte();  // unknown GH
        byte areaHitType = br.ReadByte();
        br.ReadUInt16();

        float y = ReadWorldFloat(br);
        float height = ReadWorldFloat(br);
        float circleRadius = hasRadiusField ? ReadWorldFloat(br) : 0f;
        Vector2 p1 = ReadWorldXZ(br);
        Vector2 p2 = ReadWorldXZ(br);
        Vector2 p3 = ReadWorldXZ(br);
        Vector2 p4 = ReadWorldXZ(br);

        // In PS2 0xA0 this starts at 0x34: active/unknown, type @0x35, index @0x36.
        byte active = br.ReadByte();
        byte type = br.ReadByte();
        byte index = br.ReadByte();
        byte hitType = br.ReadByte();
        byte triggerType = br.ReadByte();
        byte targetType = br.ReadByte();
        br.ReadByte();
        br.ReadBytes(5);
        uint functionPointer = br.ReadUInt32();
        byte priority = br.ReadByte();           // entry +0x44 / JS definition byte 1
        byte definitionByte2 = br.ReadByte();    // entry +0x45
        byte definitionByte3 = br.ReadByte();    // entry +0x46
        byte definitionByte4 = br.ReadByte();    // entry +0x47
        byte hitAngle = br.ReadByte();
        byte openAngle = br.ReadByte();
        byte actionType = br.ReadByte();
        br.ReadBytes(8);
        br.ReadByte();
        br.ReadBytes(8);
        byte[] parameters = br.ReadBytes(64);
        // PS2 has four additional trailing bytes in its 0xA0 line. Preserve them via RawData.

        ValidateFinite(y, nameof(y)); ValidateFinite(height, nameof(height)); ValidateFinite(circleRadius, nameof(circleRadius));
        ValidateFinite(p1.X, "Position1.X"); ValidateFinite(p1.Y, "Position1.Z");
        ValidateFinite(p2.X, "Position2.X"); ValidateFinite(p2.Y, "Position2.Z");
        ValidateFinite(p3.X, "Position3.X"); ValidateFinite(p3.Y, "Position3.Z");
        ValidateFinite(p4.X, "Position4.X"); ValidateFinite(p4.Y, "Position4.Z");

        return new AevEntry
        {
            FileOrder = fileOrder, RawData = raw, ParameterBuffer = parameters, Index = index, Type = type,
            Active = active, Priority = priority, DefinitionByte2 = definitionByte2, DefinitionByte3 = definitionByte3,
            DefinitionByte4 = definitionByte4, FunctionPointer = functionPointer, AreaHitType = areaHitType,
            HitType = hitType, TriggerType = triggerType, TargetType = targetType, HitAngle = hitAngle,
            OpenAngle = openAngle, ActionType = actionType, Y = y, Height = height, CircleRadius = circleRadius,
            Position1 = p1, Position2 = p2, Position3 = p3, Position4 = p4, HasExplicitRadius = hasRadiusField,
            IsPs2Layout = ps2Layout
        };
    }

    private static float ReadWorldFloat(BinaryReader br) => br.ReadSingle() * WorldScale;
    private static Vector2 ReadWorldXZ(BinaryReader br) => new(br.ReadSingle() * WorldScale, br.ReadSingle() * WorldScale);
    private static void ValidateFinite(float value, string field)
    {
        if (!float.IsFinite(value)) throw new InvalidDataException($"Campo AEV inválido: {field} não é finito.");
    }
}

using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Collision;

public static class Ps2EsatReader
{
    private const int MaxItems = ushort.MaxValue;

    public static EsatFile Read(string path, EsatKind? kind = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream);
        if (stream.Length < 20) throw new InvalidDataException("SAT/EAT menor que o cabeçalho mínimo de 20 bytes.");

        EsatKind resolvedKind = kind ?? (Path.GetExtension(path).Equals(".EAT", StringComparison.OrdinalIgnoreCase) ? EsatKind.Eat : EsatKind.Sat);
        byte magic = reader.ReadByte();
        var result = new EsatFile { SourcePath = path, Kind = resolvedKind, ContainerMagic = magic };
        var offsets = new List<uint>();

        if (magic == 0x80)
        {
            byte count = reader.ReadByte();
            if (count == 0) throw new InvalidDataException("Contêiner SAT/EAT 0x80 sem blocos.");
            result.ContainerUnknown = reader.ReadUInt16();
            Require(stream, count * 4L, "tabela de offsets");
            for (int i = 0; i < count; i++) offsets.Add(reader.ReadUInt32());
        }
        else if (magic == 0x20)
        {
            offsets.Add(0);
        }
        else
        {
            throw new InvalidDataException($"Magic SAT/EAT PS2 não suportado: 0x{magic:X2}.");
        }

        for (int i = 0; i < offsets.Count; i++)
        {
            uint offset = offsets[i];
            long end = i + 1 < offsets.Count ? offsets[i + 1] : stream.Length;
            if (offset >= stream.Length || end <= offset || end > stream.Length)
                throw new InvalidDataException($"Intervalo inválido para o bloco SAT/EAT #{i}: 0x{offset:X}-0x{end:X}.");
            result.Meshes.Add(ReadMesh(reader, offset, end, i));
        }
        return result;
    }

    private static EsatMesh ReadMesh(BinaryReader reader, long offset, long end, int index)
    {
        Stream stream = reader.BaseStream;
        stream.Position = offset;
        RequireUntil(stream, end, 20, $"cabeçalho do bloco #{index}");
        byte magic = reader.ReadByte();
        if (magic != 0x20) throw new InvalidDataException($"Bloco SAT/EAT #{index} em 0x{offset:X} possui magic 0x{magic:X2}, esperado 0x20.");
        byte unknown01 = reader.ReadByte();
        ushort positionCount = reader.ReadUInt16();
        ushort normalCount = reader.ReadUInt16();
        ushort edgeCount = reader.ReadUInt16();
        ushort unknown08 = reader.ReadUInt16();
        ushort faceCount = reader.ReadUInt16();
        ushort floorCount = reader.ReadUInt16();
        ushort slopeCount = reader.ReadUInt16();
        ushort wallCount = reader.ReadUInt16();
        ushort groupCount = reader.ReadUInt16();
        if (floorCount + slopeCount + wallCount != faceCount)
            throw new InvalidDataException($"Bloco #{index}: floor+slope+wall não corresponde à quantidade de faces.");

        long fixedBytes = checked(12L * (positionCount + normalCount + edgeCount) + 20L * faceCount);
        RequireUntil(stream, end, fixedBytes, $"geometria do bloco #{index}");
        long positionsOffset=stream.Position;
        var mesh = new EsatMesh { FileOffset = offset, PositionsFileOffset = positionsOffset, NormalsFileOffset = positionsOffset + positionCount*12L, EdgeVectorsFileOffset = positionsOffset + (positionCount+normalCount)*12L, Magic = magic, Unknown01 = unknown01, Unknown08 = unknown08, FloorCount = floorCount, SlopeCount = slopeCount, WallCount = wallCount };
        ReadVectors(reader, mesh.Positions, positionCount);
        mesh.OriginalPositions.AddRange(mesh.Positions);
        ReadVectors(reader, mesh.Normals, normalCount);
        ReadVectors(reader, mesh.EdgeVectors, edgeCount);

        for (int i = 0; i < faceCount; i++)
        {
            ushort v0 = reader.ReadUInt16(), v1 = reader.ReadUInt16(), v2 = reader.ReadUInt16();
            ushort normal = reader.ReadUInt16(), e0 = reader.ReadUInt16(), e1 = reader.ReadUInt16(), e2 = reader.ReadUInt16(), unknown = reader.ReadUInt16();
            byte blue = reader.ReadByte(), green = reader.ReadByte(), red = reader.ReadByte(), connectivity = reader.ReadByte();
            if (v0 >= positionCount || v1 >= positionCount || v2 >= positionCount || normal >= normalCount || e0 >= edgeCount || e1 >= edgeCount || e2 >= edgeCount)
                throw new InvalidDataException($"Bloco #{index}, face #{i}: índice fora dos limites.");
            mesh.Faces.Add(new EsatFace(v0, v1, v2, normal, e0, e1, e2, unknown, blue, green, red, connectivity));
        }

        for (int i = 0; i < groupCount; i++) mesh.Groups.Add(ReadGroup(reader, end, faceCount, index, i));
        return mesh;
    }

    private static EsatGroup ReadGroup(BinaryReader reader, long end, ushort faceCount, int meshIndex, int groupIndex)
    {
        Stream stream = reader.BaseStream;
        long groupOffset = stream.Position;
        RequireUntil(stream, end, 36, $"grupo #{groupIndex} do bloco #{meshIndex}");
        Vector3 pos = ReadVector(reader), size = ReadVector(reader);
        ushort floors = reader.ReadUInt16(), slopes = reader.ReadUInt16(), walls = reader.ReadUInt16(), flags = reader.ReadUInt16();
        uint brother = reader.ReadUInt32();
        int count = checked(floors + slopes + walls);
        if (count > MaxItems) throw new InvalidDataException($"Grupo #{groupIndex} possui índices demais.");
        RequireUntil(stream, end, count * 2L, $"índices do grupo #{groupIndex}");
        ushort[] f = ReadIndices(reader, floors, faceCount, meshIndex, groupIndex);
        ushort[] s = ReadIndices(reader, slopes, faceCount, meshIndex, groupIndex);
        ushort[] w = ReadIndices(reader, walls, faceCount, meshIndex, groupIndex);
        // PS2 aligns every variable-length group record to four bytes.
        if ((count & 1) != 0)
        {
            RequireUntil(stream, end, 2, $"padding do grupo #{groupIndex}");
            reader.ReadUInt16();
        }
        return new EsatGroup { FileOffset = groupOffset, Position = pos, Size = size, FloorCount = floors, SlopeCount = slopes, WallCount = walls, Flags = flags, BrotherDistance = brother, FloorFaces = f, SlopeFaces = s, WallFaces = w };
    }

    private static ushort[] ReadIndices(BinaryReader reader, int count, ushort faceCount, int mesh, int group)
    {
        var values = new ushort[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = reader.ReadUInt16();
            if (values[i] >= faceCount) throw new InvalidDataException($"Bloco #{mesh}, grupo #{group}: índice de face {values[i]} fora dos limites.");
        }
        return values;
    }

    private static void ReadVectors(BinaryReader reader, List<Vector3> output, int count)
    {
        output.Capacity = count;
        for (int i = 0; i < count; i++) output.Add(ReadVector(reader));
    }

    private static Vector3 ReadVector(BinaryReader reader)
    {
        var value = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z))
            throw new InvalidDataException("SAT/EAT contém coordenada não finita.");
        return value;
    }

    private static void Require(Stream stream, long bytes, string section) => RequireUntil(stream, stream.Length, bytes, section);
    private static void RequireUntil(Stream stream, long end, long bytes, string section)
    {
        if (bytes < 0 || stream.Position > end - bytes) throw new InvalidDataException($"Fim inesperado ao ler {section}.");
    }
}

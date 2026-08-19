using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2ScenarioReader
{
    private sealed class SmdEntry
    {
        public Vector3 Position;
        public Vector3 Angle;
        public Vector3 Scale;
        public int BinId;
    }

    private readonly struct LocalTriangle
    {
        public readonly Vector3 A, B, C;
        public readonly Vector2 UvA, UvB, UvC;
        public readonly int TextureIndex;

        public LocalTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC, int textureIndex)
        {
            A = a; B = b; C = c;
            UvA = uvA; UvB = uvB; UvC = uvC;
            TextureIndex = textureIndex;
        }
    }

    private sealed class SegmentVertex
    {
        public Vector3 Position;
        public Vector2 Uv;
        public ushort IndexComplement;
    }

    private readonly struct MaterialInfo
    {
        public readonly int TextureIndex;
        public readonly uint NodeOffset;
        public MaterialInfo(int textureIndex, uint nodeOffset) { TextureIndex = textureIndex; NodeOffset = nodeOffset; }
    }

    public static ScenarioScene Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Caminho do SMD inválido.", nameof(path));
        if (!File.Exists(path)) throw new FileNotFoundException("Arquivo SMD não encontrado.", path);

        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        if (fs.Length < 0x10) throw new InvalidDataException("SMD muito pequeno.");

        ushort magic = br.ReadUInt16();
        if (magic != 0x0040 && magic != 0x0031)
            throw new InvalidDataException($"Magic SMD não suportado: 0x{magic:X4}.");

        int entryCount = br.ReadUInt16();
        uint binTableOffset = br.ReadUInt32();
        uint tplTableOffset = br.ReadUInt32();
        br.ReadUInt32();

        if (entryCount <= 0) throw new InvalidDataException("SMD sem entries.");
        if (0x10L + entryCount * 0x40L > fs.Length) throw new InvalidDataException("Tabela de entries do SMD está fora do arquivo.");

        var entries = new List<SmdEntry>(entryCount);
        int maxBin = -1;

        for (int i = 0; i < entryCount; i++)
        {
            fs.Position = 0x10L + i * 0x40L;
            var e = new SmdEntry
            {
                Position = new Vector3(br.ReadSingle() / 100f, br.ReadSingle() / 100f, br.ReadSingle() / 100f),
            };
            br.ReadSingle();
            e.Angle = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.ReadSingle();
            e.Scale = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            br.ReadSingle();
            e.BinId = br.ReadByte();
            br.ReadByte(); br.ReadByte(); br.ReadByte();
            br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32();
            entries.Add(e);
            if (e.BinId > maxBin) maxBin = e.BinId;
        }

        int binCount = maxBin + 1;
        if (binCount <= 0) throw new InvalidDataException("Nenhum BIN referenciado pelo cenário.");
        if (binTableOffset == 0 || binTableOffset + binCount * 4L > fs.Length)
            throw new InvalidDataException("Tabela de offsets BIN inválida.");

        fs.Position = binTableOffset;
        uint[] offsets = new uint[binCount];
        for (int i = 0; i < binCount; i++) offsets[i] = br.ReadUInt32();

        var usedBins = entries.Select(e => e.BinId).Distinct().Where(id => id >= 0 && id < binCount).OrderBy(id => id).ToArray();
        var cache = new Dictionary<int, List<LocalTriangle>>();
        var warnings = new List<string>();
        int loadedBins = 0;

        foreach (int binId in usedBins)
        {
            if (offsets[binId] == 0)
            {
                warnings.Add($"BIN {binId}: offset 0, ignorado.");
                continue;
            }

            long start = (long)binTableOffset + offsets[binId];
            long end = FindBinEnd(binId, offsets, binTableOffset, tplTableOffset, fs.Length);
            try
            {
                cache[binId] = ReadBinTriangles(br, start, end, binId);
                loadedBins++;
            }
            catch (Exception ex)
            {
                warnings.Add($"BIN {binId}: {ex.Message}");
            }
        }

        var worldTriangles = new List<ScenarioTriangle>();
        Vector3 min = new(float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity);

        foreach (var e in entries)
        {
            if (!cache.TryGetValue(e.BinId, out var local)) continue;

            Vector3 scale = new(
                Math.Abs(e.Scale.X) < 0.000001f ? 1f : e.Scale.X,
                Math.Abs(e.Scale.Y) < 0.000001f ? 1f : e.Scale.Y,
                Math.Abs(e.Scale.Z) < 0.000001f ? 1f : e.Scale.Z);

            bool mirrored = scale.X * scale.Y * scale.Z < 0f;
            foreach (var t in local)
            {
                // Apply exactly the same PS2 entry transform that was validated
                // by the repeated-BIN transform fix:
                // LOCAL -> Scale -> Rz -> Ry -> Rx -> Position.
                Vector3 a = TransformPs2EntryVertex(t.A, scale, e.Angle, e.Position);
                Vector3 b = TransformPs2EntryVertex(t.B, scale, e.Angle, e.Position);
                Vector3 c = TransformPs2EntryVertex(t.C, scale, e.Angle, e.Position);
                Vector2 uvA = t.UvA, uvB = t.UvB, uvC = t.UvC;
                if (!IsFinite(a) || !IsFinite(b) || !IsFinite(c)) continue;

                if (mirrored)
                {
                    (b, c) = (c, b);
                    (uvB, uvC) = (uvC, uvB);
                }

                Vector3 cross = Vector3.Cross(b - a, c - a);
                float area2 = cross.LengthSquared();
                if (!float.IsFinite(area2) || area2 < 0.000001f) continue;

                worldTriangles.Add(new ScenarioTriangle(a, b, c, uvA, uvB, uvC, t.TextureIndex));
                min = Vector3.Min(min, Vector3.Min(a, Vector3.Min(b, c)));
                max = Vector3.Max(max, Vector3.Max(a, Vector3.Max(b, c)));
            }
        }

        if (worldTriangles.Count == 0)
            throw new InvalidDataException("O SMD foi lido, mas nenhuma geometria renderizável foi encontrada." +
                (warnings.Count > 0 ? Environment.NewLine + string.Join(Environment.NewLine, warnings.Take(5)) : string.Empty));

        return new ScenarioScene
        {
            SourcePath = path,
            EntryCount = entryCount,
            BinCount = binCount,
            LoadedBinCount = loadedBins,
            SkippedBinCount = usedBins.Length - loadedBins,
            Warnings = warnings,
            Triangles = worldTriangles,
            BoundsMin = min,
            BoundsMax = max
        };
    }

    private static long FindBinEnd(int binId, uint[] offsets, uint binTableOffset, uint tplTableOffset, long fileLength)
    {
        for (int i = binId + 1; i < offsets.Length; i++)
            if (offsets[i] != 0) return Math.Min(fileLength, (long)binTableOffset + offsets[i]);
        if (tplTableOffset > binTableOffset && tplTableOffset < fileLength) return tplTableOffset;
        return fileLength;
    }

    private static List<LocalTriangle> ReadBinTriangles(BinaryReader br, long start, long end, int binId)
    {
        Stream s = br.BaseStream;
        if (start < 0 || start + 0x50 > s.Length) throw new InvalidDataException("offset fora do arquivo.");
        s.Position = start;

        ushort magic = br.ReadUInt16();
        br.ReadUInt16(); // nTex
        br.ReadUInt32();
        br.ReadByte(); br.ReadByte();
        ushort materialCount = br.ReadUInt16();
        uint materialOffset = br.ReadUInt32();
        uint padding1 = br.ReadUInt32();
        br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32();
        for (int i = 0; i < 8; i++) br.ReadSingle();

        if (magic != 0x0030 && padding1 != 0xCDCDCDCD)
            throw new InvalidDataException($"formato BIN não suportado (magic 0x{magic:X4}).");
        if (materialCount == 0) return new List<LocalTriangle>();
        if (materialOffset == 0 || start + materialOffset + materialCount * 16L > s.Length)
            throw new InvalidDataException("tabela de materiais inválida.");

        var materials = new List<MaterialInfo>(materialCount);
        s.Position = start + materialOffset;
        for (int i = 0; i < materialCount; i++)
        {
            byte[] data = br.ReadBytes(16);
            if (data.Length != 16) throw new EndOfStreamException("material incompleto.");

            // Material PS2 validado pelo Materials Handler:
            // byte 0 = material flag, byte 1 = diffuse_map, byte 2 = bump_map,
            // byte 3 = opacity_map, ... uint node offset em +0x0C.
            int diffuseMap = data[1];
            if (diffuseMap == 0xFF) diffuseMap = -1;
            materials.Add(new MaterialInfo(diffuseMap, BitConverter.ToUInt32(data, 12)));
        }

        var result = new List<LocalTriangle>();
        for (int materialIndex = 0; materialIndex < materials.Count; materialIndex++)
        {
            MaterialInfo material = materials[materialIndex];
            uint nodeOffset = material.NodeOffset;
            if (nodeOffset == 0 || start + nodeOffset + 4 > s.Length) continue;

            s.Position = start + nodeOffset;
            br.ReadUInt16();
            int segmentCount = br.ReadByte() + 1;
            int boneIdCount = br.ReadByte();

            int calculation = 4 + boneIdCount;
            int parts = calculation / 16;
            if (calculation % 16 != 0) parts++;
            int boneListSize = parts * 16 - 4;
            if (boneListSize > 0) br.ReadBytes(boneListSize);

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                if (s.Position + 0x30 > s.Length) throw new EndOfStreamException("segmento incompleto.");
                byte[] header1 = br.ReadBytes(0x10);
                bool scenarioWithColors = true;

                if (header1[12] == 0x00 && header1[14] > 1)
                {
                    scenarioWithColors = false;
                    int weightBytes = header1[0] * 0x10;
                    if (weightBytes > 0) br.ReadBytes(weightBytes);
                    header1 = br.ReadBytes(0x10);
                    if (header1.Length != 0x10) throw new EndOfStreamException("header VIF incompleto.");
                }

                byte[] header2 = br.ReadBytes(0x10);
                byte[] header3 = br.ReadBytes(0x10);
                if (header2.Length != 0x10 || header3.Length != 0x10) throw new EndOfStreamException("headers do segmento incompletos.");

                float factor = BitConverter.ToSingle(header2, 0x0C);
                if (!float.IsFinite(factor) || Math.Abs(factor) < 0.0000001f) factor = 1f;
                int vertexCount = header2[0];
                int chunkBytes = header3[0] * 0x10;
                byte[] vertexData = br.ReadBytes(chunkBytes);
                if (vertexData.Length != chunkBytes) throw new EndOfStreamException("bloco de vértices incompleto.");

                var vertices = new List<SegmentVertex>(vertexCount);
                for (int i = 0; i < vertexCount; i++)
                {
                    int o = i * 24;
                    if (o + 24 > vertexData.Length) break;

                    // Re4QuadX PS2 loader normalizes TextureU/TextureV by 255.
                    // ScenarioWithColors stores UV at +08/+0A; normal BIN vertices
                    // store UV at +10/+12.
                    int uvOffset = scenarioWithColors ? 8 : 16;
                    short textureU = BitConverter.ToInt16(vertexData, o + uvOffset);
                    short textureV = BitConverter.ToInt16(vertexData, o + uvOffset + 2);
                    vertices.Add(new SegmentVertex
                    {
                        Position = new Vector3(
                            BitConverter.ToInt16(vertexData, o + 0) * factor / 100f,
                            BitConverter.ToInt16(vertexData, o + 2) * factor / 100f,
                            BitConverter.ToInt16(vertexData, o + 4) * factor / 100f),
                        IndexComplement = BitConverter.ToUInt16(vertexData, o + 14),
                        // ScenarioWithColors is used heavily by foliage/decal-style
                        // scenario meshes on PS2. Its V orientation is opposite to the
                        // regular BIN path in our OpenGL preview.
                        Uv = scenarioWithColors
                            ? new Vector2(textureU / 255f, 1f - (textureV / 255f))
                            : new Vector2(textureU / 255f, textureV / 255f)
                    });
                }

                BuildStrip(vertices, result, material.TextureIndex);

                // Alinhamento observado no decoder PS2 usado como referência.
                if (segmentIndex > 0 && s.Position + 0x10 <= s.Length) br.ReadBytes(0x10);
            }
        }

        return result;
    }

    private static void BuildStrip(List<SegmentVertex> vertices, List<LocalTriangle> output, int textureIndex)
    {
        bool invertFace = false;

        for (int i = 0; i < vertices.Count; i++)
        {
            if (i >= 2 && vertices[i].IndexComplement == 0)
            {
                SegmentVertex va = vertices[i - 2];
                SegmentVertex vb = vertices[i - 1];
                SegmentVertex vc = vertices[i];

                Vector3 a = va.Position, b = vb.Position, c = vc.Position;
                Vector2 uvA = va.Uv, uvB = vb.Uv, uvC = vc.Uv;

                if (invertFace)
                {
                    (a, c) = (c, a);
                    (uvA, uvC) = (uvC, uvA);
                }

                invertFace = !invertFace;

                if (Vector3.DistanceSquared(a, b) < 0.0000000001f ||
                    Vector3.DistanceSquared(b, c) < 0.0000000001f ||
                    Vector3.DistanceSquared(c, a) < 0.0000000001f)
                    continue;

                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (!float.IsFinite(cross.LengthSquared()) || cross.LengthSquared() < 0.0000000001f)
                    continue;

                output.Add(new LocalTriangle(a, b, c, uvA, uvB, uvC, textureIndex));
            }
            else invertFace = false;
        }
    }

    private static Vector3 TransformPs2EntryVertex(Vector3 local, Vector3 scale, Vector3 angle, Vector3 position)
    {
        // The SMD stores AngleX/Y/Z in radians. The scenario bounding boxes confirm
        // the effective vertex order used by the original Matrix4x4 path:
        // Scale -> Rx -> Ry -> Rz -> Translation.
        Vector3 v = new Vector3(local.X * scale.X, local.Y * scale.Y, local.Z * scale.Z);
        v = RotateX(v, angle.X);
        v = RotateY(v, angle.Y);
        v = RotateZ(v, angle.Z);
        return v + position;
    }

    private static Vector3 RotateX(Vector3 v, float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);
        return new Vector3(
            v.X,
            v.Y * c - v.Z * s,
            v.Y * s + v.Z * c);
    }

    private static Vector3 RotateY(Vector3 v, float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);
        return new Vector3(
            v.X * c + v.Z * s,
            v.Y,
            -v.X * s + v.Z * c);
    }

    private static Vector3 RotateZ(Vector3 v, float angle)
    {
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);
        return new Vector3(
            v.X * c - v.Y * s,
            v.X * s + v.Y * c,
            v.Z);
    }

    private static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
}

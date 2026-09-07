using System.Numerics;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static class Ps2ScenarioReader
{
    public static IReadOnlyList<ScenarioTriangle> ReadStandaloneBin(byte[] data, int textureIndexBase = 0, bool useRegularUvLayout = true)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 0x50) return Array.Empty<ScenarioTriangle>();
        using var stream = new MemoryStream(data, writable: false);
        using var reader = new BinaryReader(stream);
        List<LocalTriangle> local = ReadBinTriangles(reader, 0, data.Length, 0, useRegularUvLayout);
        return local.Select(t => new ScenarioTriangle(t.A, t.B, t.C, t.UvA, t.UvB, t.UvC,
            t.TextureIndex < 0 ? -1 : t.TextureIndex + textureIndexBase)).ToArray();
    }

    private sealed class SmdEntry
    {
        public int FileOrder;
        public byte[] RawData = new byte[0x40];
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
        public readonly int OffsetA,OffsetB,OffsetC,StripFlagOffset;public readonly float Factor;

        public LocalTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC, int textureIndex,int offsetA=-1,int offsetB=-1,int offsetC=-1,float factor=1f,int stripFlagOffset=-1)
        {
            A = a; B = b; C = c;
            UvA = uvA; UvB = uvB; UvC = uvC;
            TextureIndex = textureIndex;
            OffsetA=offsetA;OffsetB=offsetB;OffsetC=offsetC;Factor=factor;StripFlagOffset=stripFlagOffset;
        }
    }

    private sealed class SegmentVertex
    {
        public Vector3 Position;
        public Vector2 Uv;
        public ushort IndexComplement;
        public int SourceOffset;
        public float Factor;
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
            byte[] raw = br.ReadBytes(0x40);
            if (raw.Length != 0x40) throw new EndOfStreamException("Entry SMD incompleta.");
            var e = new SmdEntry
            {
                FileOrder = i,
                RawData = raw,
                Position = new Vector3(BitConverter.ToSingle(raw, 0) / 100f, BitConverter.ToSingle(raw, 4) / 100f, BitConverter.ToSingle(raw, 8) / 100f),
            };
            e.Angle = new Vector3(BitConverter.ToSingle(raw, 0x10), BitConverter.ToSingle(raw, 0x14), BitConverter.ToSingle(raw, 0x18));
            e.Scale = new Vector3(BitConverter.ToSingle(raw, 0x20), BitConverter.ToSingle(raw, 0x24), BitConverter.ToSingle(raw, 0x28));
            e.BinId = raw[0x30];
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
                cache[binId] = ReadBinTriangles(br, start, end, binId, useRegularUvLayout: false);
                loadedBins++;
            }
            catch (Exception ex)
            {
                warnings.Add($"BIN {binId}: {ex.Message}");
            }
        }

        var worldTriangles = new List<ScenarioTriangle>();
        var sceneEntries = new List<ScenarioEntry>(entries.Count);
        Vector3 min = new(float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity);

        foreach (var e in entries)
        {
            cache.TryGetValue(e.BinId, out var local);
            sceneEntries.Add(new ScenarioEntry
            {
                FileOrder = e.FileOrder, RawData = e.RawData, BinId = (byte)e.BinId,
                PositionX = e.Position.X, PositionY = e.Position.Y, PositionZ = e.Position.Z,
                RotationX = e.Angle.X, RotationY = e.Angle.Y, RotationZ = e.Angle.Z,
                ScaleX = e.Scale.X, ScaleY = e.Scale.Y, ScaleZ = e.Scale.Z,
                LocalTriangles = local == null ? Array.Empty<ScenarioTriangle>() : local.Select(t => new ScenarioTriangle(t.A, t.B, t.C, t.UvA, t.UvB, t.UvC, t.TextureIndex,t.OffsetA,t.OffsetB,t.OffsetC,t.Factor,t.StripFlagOffset)).ToArray(),
                TextureIndex = local?.Select(t=>t.TextureIndex).Where(x=>x>=0).DefaultIfEmpty(-1).First() ?? -1
            });
            if (local == null) continue;

            Vector3 scale = new(
                float.IsFinite(e.Scale.X) ? e.Scale.X : 1f,
                float.IsFinite(e.Scale.Y) ? e.Scale.Y : 1f,
                float.IsFinite(e.Scale.Z) ? e.Scale.Z : 1f);

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
            Entries = sceneEntries,
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

    private static List<LocalTriangle> ReadBinTriangles(BinaryReader br, long start, long end, int binId, bool useRegularUvLayout)
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
                int vertexDataOffset=checked((int)(s.Position-start-chunkBytes));
                if (vertexData.Length != chunkBytes) throw new EndOfStreamException("bloco de vértices incompleto.");

                var vertices = new List<SegmentVertex>(vertexCount);
                for (int i = 0; i < vertexCount; i++)
                {
                    int o = i * 24;
                    if (o + 24 > vertexData.Length) break;

                    // Re4QuadX PS2 loader normalizes TextureU/TextureV by 255.
                    // ScenarioWithColors stores UV at +08/+0A; normal BIN vertices
                    // store UV at +10/+12.
                    // Standalone ETM BINs use the regular PMD/ITM vertex layout even
                    // when their VIF header resembles a colored scenario segment.
                    int uvOffset = useRegularUvLayout || !scenarioWithColors ? 16 : 8;
                    short textureU = BitConverter.ToInt16(vertexData, o + uvOffset);
                    short textureV = BitConverter.ToInt16(vertexData, o + uvOffset + 2);
                    float normalizedU = useRegularUvLayout
                        ? (textureU & 0xFF) / 255f
                        : textureU / 255f;
                    vertices.Add(new SegmentVertex
                    {
                        Position = new Vector3(
                            BitConverter.ToInt16(vertexData, o + 0) * factor / 100f,
                            BitConverter.ToInt16(vertexData, o + 2) * factor / 100f,
                            BitConverter.ToInt16(vertexData, o + 4) * factor / 100f),
                        IndexComplement = BitConverter.ToUInt16(vertexData, o + 14),
                        SourceOffset=vertexDataOffset+o,
                        Factor=factor,
                        // ScenarioWithColors is used heavily by foliage/decal-style
                        // scenario meshes on PS2. Its V orientation is opposite to the
                        // regular BIN path in our OpenGL preview.
                        Uv = scenarioWithColors && !useRegularUvLayout
                            ? new Vector2(normalizedU, 1f - (textureV / 255f))
                            : new Vector2(normalizedU, textureV / 255f)
                    });
                }

                BuildStrip(vertices, result, material.TextureIndex, useRegularUvLayout);

                // Alinhamento observado no decoder PS2 usado como referência.
                if (segmentIndex > 0 && s.Position + 0x10 <= s.Length) br.ReadBytes(0x10);
            }
        }

        return result;
    }

    private static void BuildStrip(List<SegmentVertex> vertices, List<LocalTriangle> output, int textureIndex, bool orientEtmUvs)
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
                int offsetA=va.SourceOffset,offsetB=vb.SourceOffset,offsetC=vc.SourceOffset;

                if (invertFace)
                {
                    (a, c) = (c, a);
                    (uvA, uvC) = (uvC, uvA);
                    (offsetA,offsetC)=(offsetC,offsetA);
                }

                if (orientEtmUvs)
                    OrientVerticalFaceUvs(a, b, c, ref uvA, ref uvB, ref uvC);

                invertFace = !invertFace;

                if (Vector3.DistanceSquared(a, b) < 0.0000000001f ||
                    Vector3.DistanceSquared(b, c) < 0.0000000001f ||
                    Vector3.DistanceSquared(c, a) < 0.0000000001f)
                    continue;

                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (!float.IsFinite(cross.LengthSquared()) || cross.LengthSquared() < 0.0000000001f)
                    continue;

                output.Add(new LocalTriangle(a, b, c, uvA, uvB, uvC, textureIndex,offsetA,offsetB,offsetC,va.Factor,vertices[i].SourceOffset+14));
            }
            else invertFace = false;
        }
    }

    private static void OrientVerticalFaceUvs(
        Vector3 a, Vector3 b, Vector3 c,
        ref Vector2 uvA, ref Vector2 uvB, ref Vector2 uvC)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a);
        if (normal.LengthSquared() < 0.0000000001f ||
            MathF.Abs(normal.Y) >= MathF.Max(MathF.Abs(normal.X), MathF.Abs(normal.Z)))
            return;

        float meanY = (a.Y + b.Y + c.Y) / 3f;
        float meanU = (uvA.X + uvB.X + uvC.X) / 3f;
        float meanV = (uvA.Y + uvB.Y + uvC.Y) / 3f;
        float covarianceU = MathF.Abs(
            (a.Y - meanY) * (uvA.X - meanU) +
            (b.Y - meanY) * (uvB.X - meanU) +
            (c.Y - meanY) * (uvC.X - meanU));
        float covarianceV = MathF.Abs(
            (a.Y - meanY) * (uvA.Y - meanV) +
            (b.Y - meanY) * (uvB.Y - meanV) +
            (c.Y - meanY) * (uvC.Y - meanV));

        if (covarianceU > covarianceV * 1.25f)
        {
            uvA = new Vector2(uvA.Y, uvA.X);
            uvB = new Vector2(uvB.Y, uvB.X);
            uvC = new Vector2(uvC.Y, uvC.X);
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

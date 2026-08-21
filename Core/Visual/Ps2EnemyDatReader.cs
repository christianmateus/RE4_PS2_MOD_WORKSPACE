using System.Numerics;
using System.Text;
using RE4_PS2_MOD_WORKSPACE.Core.Animation;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>
/// RE4 PS2 enemy DAT reader used by the Visual Editor.
/// Enemy DAT layout observed in em12.dat:
///   +00 uint entry count
///   +10 uint[count] data offsets
///   +10+count*4 uint[count] 3-char type tags (BIN/TPL/SEQ/FCV/...)
/// BIN payloads use the same PS2 mesh encoding used by Ps2ScenarioReader.
/// v0.4.6 also keeps UV/material references and embedded TPL packages for textured preview.
/// Texture mapping test: prefer an immediately following TPL as the BIN texture container.
/// If the BIN has no direct TPL pair, reuse the most recent preceding TPL container so adjacent
/// body/head/neck/hand pieces can share one package. Materials still select the image through diffuse_map.
/// </summary>
public static class Ps2EnemyDatReader
{
    private sealed class SegmentVertex
    {
        public Vector3 Position;
        public Vector2 Uv;
        public ushort IndexComplement;
        public ushort WeightMapRawIndex;
        public EnemyVertexSkin Skin;
    }

    private readonly record struct RawWeightMap(uint Bone1, uint Bone2, uint Bone3, int Count, float Weight1, float Weight2, float Weight3);

    private readonly record struct MaterialInfo(int TextureIndex, uint NodeOffset);

    public static EnemyModelScene Read(string path, byte enemyType)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Enemy DAT não encontrado.", path);
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);
        if (fs.Length < 0x20) throw new InvalidDataException("Enemy DAT muito pequeno.");

        uint rawCount = br.ReadUInt32();
        if (rawCount == 0 || rawCount > 100000) throw new InvalidDataException($"Quantidade de entries inválida: {rawCount}.");
        int count = checked((int)rawCount);
        long offsetsStart = 0x10;
        long tagsStart = offsetsStart + count * 4L;
        long dataStart = tagsStart + count * 4L;
        if (dataStart > fs.Length) throw new InvalidDataException("Tabelas do enemy DAT ultrapassam o arquivo.");

        fs.Position = offsetsStart;
        uint[] offsets = new uint[count];
        for (int i = 0; i < count; i++) offsets[i] = br.ReadUInt32();
        fs.Position = tagsStart;
        uint[] tags = new uint[count];
        for (int i = 0; i < count; i++) tags[i] = br.ReadUInt32();

        int[] tplEntries = Enumerable.Range(0, count).Where(i => IsTag(tags[i], "TPL")).ToArray();
        var texturePackages = new Dictionary<int, EnemyTexturePackage>();
        foreach (int tplEntry in tplEntries)
        {
            long start = offsets[tplEntry];
            long end = FindEntryEnd(tplEntry, offsets, fs.Length);
            if (start <= 0 || start >= fs.Length || end <= start) continue;
            try
            {
                int length = checked((int)Math.Min(int.MaxValue, end - start));
                fs.Position = start;
                byte[] bytes = br.ReadBytes(length);
                if (bytes.Length >= 0x10)
                    texturePackages[tplEntry] = new EnemyTexturePackage { DatEntryIndex = tplEntry, Data = bytes };
            }
            catch { /* A broken TPL should not block model geometry. */ }
        }

        var triangles = new List<EnemyModelTriangle>();
        var parts = new List<EnemyModelPart>();
        var warnings = new List<string>();
        Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
        int binCount = 0, loaded = 0;

        for (int i = 0; i < count; i++)
        {
            if (!IsTag(tags[i], "BIN")) continue;
            int binIndex = binCount++;
            long start = offsets[i];
            if (start <= 0 || start >= fs.Length) { warnings.Add($"BIN DAT #{i}: offset inválido 0x{start:X}."); continue; }
            long end = FindEntryEnd(i, offsets, fs.Length);
            var tplResolution = ResolveTplEntry(i, tags);
            int tplEntryIndex = tplResolution.EntryIndex;
            try
            {
                List<EnemyModelTriangle> local = ReadBinTriangles(br, start, end, tplEntryIndex);
                if (local.Count == 0) continue;
                loaded++;
                Vector3 partMin = new(float.PositiveInfinity), partMax = new(float.NegativeInfinity);
                foreach (EnemyModelTriangle tri in local)
                {
                    triangles.Add(tri);
                    Vector3 triMin = Vector3.Min(tri.A, Vector3.Min(tri.B, tri.C));
                    Vector3 triMax = Vector3.Max(tri.A, Vector3.Max(tri.B, tri.C));
                    partMin = Vector3.Min(partMin, triMin); partMax = Vector3.Max(partMax, triMax);
                    min = Vector3.Min(min, triMin); max = Vector3.Max(max, triMax);
                }
                parts.Add(new EnemyModelPart
                {
                    BinIndex = binIndex, DatEntryIndex = i, TplEntryIndex = tplEntryIndex, TplResolution = tplResolution.Kind, Triangles = local,
                    DiffuseMaps = local.Select(x => x.TextureIndex).Distinct().OrderBy(x => x).ToArray(), BoundsMin = partMin, BoundsMax = partMax
                });
            }
            catch (Exception ex) { warnings.Add($"BIN DAT #{i}: {ex.Message}"); }
        }

        if (triangles.Count == 0)
            throw new InvalidDataException($"{Path.GetFileName(path)} contém {binCount} BIN(s), mas nenhuma geometria renderizável foi encontrada.");

        FcvAnimation? idleAnimation = null;
        int idleAnimationEntry = -1;
        // em12: FCV 001 is the known idle animation. In the DAT table this is entry #1.
        // Read it directly from the enemy DAT so the Visual Editor does not depend on the Animations page.
        if (enemyType == 0x12 && count > 1 && IsTag(tags[1], "FCV"))
        {
            try
            {
                byte[] idleBytes = ReadEntryBytes(br, 1, offsets, fs.Length);
                if (idleBytes.Length > 8)
                {
                    idleAnimation = FcvReader.Read(idleBytes, $"{Path.GetFileName(path)}#001.fcv");
                    idleAnimationEntry = 1;
                }
            }
            catch (Exception ex) { warnings.Add($"FCV 001 idle: {ex.Message}"); }
        }

        Ps2BinSkeleton? skeleton = null;
        int skeletonSource = -1;
        // Experimental attachment support: em12 body 440 is the known village Ganado base.
        // For other models, fall back to the first geometry BIN that exposes a valid skeleton.
        IEnumerable<int> skeletonCandidates = enemyType == 0x12
            ? new[] { 440 }.Concat(parts.Select(x => x.DatEntryIndex).Where(x => x != 440))
            : parts.Select(x => x.DatEntryIndex);
        foreach (int candidate in skeletonCandidates.Distinct())
        {
            if (candidate < 0 || candidate >= count || !IsTag(tags[candidate], "BIN")) continue;
            try
            {
                byte[] payload = ReadEntryBytes(br, candidate, offsets, fs.Length);
                Ps2BinSkeleton parsed = Ps2BinSkeletonReader.Read(payload, $"{Path.GetFileName(path)}#{candidate:D3}.bin");
                if (parsed.Bones.Count > 0) { skeleton = parsed; skeletonSource = candidate; break; }
            }
            catch { }
        }

        return new EnemyModelScene
        {
            Skeleton = skeleton,
            SkeletonSourceDatEntryIndex = skeletonSource,
            IdleAnimation = idleAnimation,
            IdleAnimationDatEntryIndex = idleAnimationEntry,
            EnemyType = enemyType,
            SourcePath = path,
            DatEntryCount = count,
            BinCount = binCount,
            LoadedBinCount = loaded,
            Warnings = warnings,
            Parts = parts,
            TexturePackages = texturePackages,
            Triangles = triangles,
            BoundsMin = min,
            BoundsMax = max
        };
    }

    /// <summary>
    /// Resolves the texture CONTAINER for a BIN. First preference is the common direct pair
    /// BIN N -> TPL N+1. When a body set shares one TPL across later head/neck/hand BINs, those
    /// BINs have no direct TPL and reuse the most recent preceding TPL. The material diffuse_map
    /// still selects the actual texture inside that package, so this fallback does not guess an
    /// image index.
    /// </summary>
    private readonly record struct TplResolution(int EntryIndex, EnemyTplResolutionKind Kind);

    private static TplResolution ResolveTplEntry(int binEntry, uint[] tags)
    {
        int next = binEntry + 1;
        if (next < tags.Length && IsTag(tags[next], "TPL")) return new TplResolution(next, EnemyTplResolutionKind.DirectNext);

        for (int i = binEntry - 1; i >= 0; i--)
            if (IsTag(tags[i], "TPL")) return new TplResolution(i, EnemyTplResolutionKind.SharedPrevious);

        return new TplResolution(-1, EnemyTplResolutionKind.None);
    }


    private static byte[] ReadEntryBytes(BinaryReader br, int index, uint[] offsets, long fileLength)
    {
        Stream s = br.BaseStream; long start = offsets[index]; long end = FindEntryEnd(index, offsets, fileLength);
        if (start <= 0 || start >= fileLength || end <= start) return Array.Empty<byte>();
        int length = checked((int)Math.Min(int.MaxValue, end - start));
        s.Position = start; return br.ReadBytes(length);
    }

    private static bool IsTag(uint value, string tag)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        string text = Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        return text.Equals(tag, StringComparison.OrdinalIgnoreCase);
    }

    private static long FindEntryEnd(int index, uint[] offsets, long fileLength)
    {
        uint current = offsets[index];
        for (int i = index + 1; i < offsets.Length; i++)
            if (offsets[i] > current && offsets[i] <= fileLength) return offsets[i];
        return fileLength;
    }

    private static List<EnemyModelTriangle> ReadBinTriangles(BinaryReader br, long start, long end, int tplEntryIndex)
    {
        Stream s = br.BaseStream;
        if (start < 0 || start + 0x50 > s.Length) throw new InvalidDataException("offset fora do arquivo.");
        s.Position = start;

        ushort magic = br.ReadUInt16();
        br.ReadUInt16();
        br.ReadUInt32();
        br.ReadByte(); br.ReadByte();
        ushort materialCount = br.ReadUInt16();
        uint materialOffset = br.ReadUInt32();
        uint padding1 = br.ReadUInt32();
        br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32();
        for (int i = 0; i < 8; i++) br.ReadSingle();

        if (magic != 0x0030 && padding1 != 0xCDCDCDCD)
            throw new InvalidDataException($"formato BIN não suportado (magic 0x{magic:X4}).");
        if (materialCount == 0) return new List<EnemyModelTriangle>();
        if (materialOffset == 0 || start + materialOffset + materialCount * 16L > s.Length)
            throw new InvalidDataException("tabela de materiais inválida.");

        var materials = new List<MaterialInfo>(materialCount);
        s.Position = start + materialOffset;
        for (int i = 0; i < materialCount; i++)
        {
            byte[] mat = br.ReadBytes(16);
            if (mat.Length != 16) throw new EndOfStreamException("material incompleto.");
            int diffuseMap = mat[1];
            if (diffuseMap == 0xFF) diffuseMap = -1;
            materials.Add(new MaterialInfo(diffuseMap, BitConverter.ToUInt32(mat, 12)));
        }

        var result = new List<EnemyModelTriangle>();
        foreach (MaterialInfo material in materials)
        {
            uint nodeOffset = material.NodeOffset;
            if (nodeOffset == 0 || start + nodeOffset + 4 > s.Length) continue;
            s.Position = start + nodeOffset;
            br.ReadUInt16();
            int segmentCount = br.ReadByte() + 1;
            int boneIdCount = br.ReadByte();
            int calculation = 4 + boneIdCount;
            int parts = (calculation + 15) / 16;
            int boneListSize = parts * 16 - 4;
            byte[] nodeBoneStorage = boneListSize > 0 ? br.ReadBytes(boneListSize) : Array.Empty<byte>();
            byte[] nodeBoneList = nodeBoneStorage.Take(boneIdCount).ToArray();

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                if (s.Position + 0x30 > s.Length || s.Position >= end) break;
                byte[] header1 = br.ReadBytes(0x10);
                bool scenarioWithColors = true;
                if (header1.Length != 0x10) break;

                var weightMaps = new List<RawWeightMap>();
                if (header1[12] == 0x00 && header1[14] > 1)
                {
                    scenarioWithColors = false;
                    int weightBytes = header1[0] * 0x10;
                    byte[] weightData = weightBytes > 0 ? br.ReadBytes(weightBytes) : Array.Empty<byte>();
                    for (int wo = 0; wo + 32 <= weightData.Length; wo += 32)
                    {
                        weightMaps.Add(new RawWeightMap(
                            BitConverter.ToUInt32(weightData, wo + 0x00),
                            BitConverter.ToUInt32(weightData, wo + 0x04),
                            BitConverter.ToUInt32(weightData, wo + 0x08),
                            BitConverter.ToInt32(weightData, wo + 0x0C),
                            BitConverter.ToSingle(weightData, wo + 0x10),
                            BitConverter.ToSingle(weightData, wo + 0x14),
                            BitConverter.ToSingle(weightData, wo + 0x18)));
                    }
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
                    int uvOffset = scenarioWithColors ? 8 : 16;
                    short textureU = BitConverter.ToInt16(vertexData, o + uvOffset);
                    short textureV = BitConverter.ToInt16(vertexData, o + uvOffset + 2);
                    ushort rawWeightIndex = scenarioWithColors ? (ushort)0 : BitConverter.ToUInt16(vertexData, o + 6);
                    EnemyVertexSkin skin = scenarioWithColors ? EnemyVertexSkin.None : ResolveVertexSkin(rawWeightIndex, weightMaps, nodeBoneList);
                    vertices.Add(new SegmentVertex
                    {
                        Position = new Vector3(
                            BitConverter.ToInt16(vertexData, o) * factor / 100f,
                            BitConverter.ToInt16(vertexData, o + 2) * factor / 100f,
                            BitConverter.ToInt16(vertexData, o + 4) * factor / 100f),
                        IndexComplement = BitConverter.ToUInt16(vertexData, o + 14),
                        WeightMapRawIndex = rawWeightIndex,
                        Skin = skin,
                        // Enemy TPLs are decoded to the bitmap orientation used by the GL uploader.
                        Uv = new Vector2(textureU / 255f, 1f - (textureV / 255f))
                    });
                }

                BuildStrip(vertices, result, material.TextureIndex, tplEntryIndex);
                if (segmentIndex > 0 && s.Position + 0x10 <= s.Length && s.Position + 0x10 <= end) br.ReadBytes(0x10);
            }
        }
        return result;
    }

    private static EnemyVertexSkin ResolveVertexSkin(ushort rawIndex, List<RawWeightMap> maps, byte[] nodeBoneList)
    {
        // RE4 PS2 BIN: vertex UnknownB is WeightMap ID * 2. Each 32-byte WeightMap line
        // references entries in the node bone list through (boneId / 4). This matches the
        // SMD exporter used by the public RE4 PS2 BIN Tool.
        int wi = rawIndex / 2;
        if (wi < 0 || wi >= maps.Count) return EnemyVertexSkin.None;
        RawWeightMap m = maps[wi];
        int count = Math.Clamp(m.Count, 0, 3);
        EnemySkinInfluence Influence(uint encoded, float weight)
        {
            uint li = encoded / 4;
            byte bone = li < nodeBoneList.Length ? nodeBoneList[li] : (byte)0;
            return new EnemySkinInfluence(bone, float.IsFinite(weight) ? weight : 0f);
        }
        EnemySkinInfluence a = count > 0 ? Influence(m.Bone1, m.Weight1) : default;
        EnemySkinInfluence b = count > 1 ? Influence(m.Bone2, m.Weight2) : default;
        EnemySkinInfluence c = count > 2 ? Influence(m.Bone3, m.Weight3) : default;
        float sum = a.Weight + b.Weight + c.Weight;
        if (sum > 0.000001f && Math.Abs(sum - 1f) > 0.0001f)
        {
            a = a with { Weight = a.Weight / sum };
            b = b with { Weight = b.Weight / sum };
            c = c with { Weight = c.Weight / sum };
        }
        return new EnemyVertexSkin(a, b, c, count);
    }

    private static void BuildStrip(List<SegmentVertex> vertices, List<EnemyModelTriangle> output, int textureIndex, int tplEntryIndex)
    {
        bool invertFace = false;
        for (int i = 0; i < vertices.Count; i++)
        {
            if (i >= 2 && vertices[i].IndexComplement == 0)
            {
                SegmentVertex va = vertices[i - 2], vb = vertices[i - 1], vc = vertices[i];
                Vector3 a = va.Position, b = vb.Position, c = vc.Position;
                Vector2 uvA = va.Uv, uvB = vb.Uv, uvC = vc.Uv;
                EnemyVertexSkin skinA = va.Skin, skinB = vb.Skin, skinC = vc.Skin;
                if (invertFace) { (a, c) = (c, a); (uvA, uvC) = (uvC, uvA); (skinA, skinC) = (skinC, skinA); }
                invertFace = !invertFace;
                if (Vector3.DistanceSquared(a, b) < 1e-10f || Vector3.DistanceSquared(b, c) < 1e-10f || Vector3.DistanceSquared(c, a) < 1e-10f) continue;
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (!float.IsFinite(cross.LengthSquared()) || cross.LengthSquared() < 1e-10f) continue;
                output.Add(new EnemyModelTriangle(a, b, c, uvA, uvB, uvC, textureIndex, tplEntryIndex, skinA, skinB, skinC));
            }
            else invertFace = false;
        }
    }
}

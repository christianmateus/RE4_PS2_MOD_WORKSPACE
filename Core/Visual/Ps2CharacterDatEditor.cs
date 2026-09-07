using System.Text;
using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

/// <summary>Safe, size-preserving texture edits inside character/enemy DAT packages.</summary>
public static class Ps2CharacterDatEditor
{
    public enum TextureTransform { Rotate90, FlipX, FlipY }
    public enum SkeletonAdaptMode { Automatic, Rigid, Distributed, KeepDonor }

    public static byte[] CaptureTplEntry(string datPath, int tplEntryIndex)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");
        return dat.AsSpan(start, length).ToArray();
    }

    public static byte[] CaptureDatEntry(string datPath, int entryIndex)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, _) = GetEntry(dat, entryIndex);
        return dat.AsSpan(start, length).ToArray();
    }

    public static void RestoreDatEntry(string datPath, int entryIndex, byte[] entryData)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, _) = GetEntry(dat, entryIndex);
        CommitTpl(datPath, dat, start, length, entryData);
    }

    public static void ReplaceBinEntry(string targetDatPath, int targetEntryIndex, string sourceDatPath, int sourceEntryIndex, SkeletonAdaptMode mode = SkeletonAdaptMode.Automatic)
    {
        byte[] target = File.ReadAllBytes(targetDatPath);
        (int targetStart, int targetLength, string targetTag) = GetEntry(target, targetEntryIndex);
        if (!targetTag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada de destino não é um BIN.");
        byte[] source = File.ReadAllBytes(sourceDatPath);
        (int sourceStart, int sourceLength, string sourceTag) = GetEntry(source, sourceEntryIndex);
        if (!sourceTag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada de origem não é um BIN.");
        byte[] targetBin = target.AsSpan(targetStart, targetLength).ToArray();
        // Reapplying a donor over an already swapped slot must still adapt to the
        // receiver's original rig, not to the previously adapted donor subset.
        string targetBackup = targetDatPath + ".bak";
        if (File.Exists(targetBackup))
        {
            try { targetBin = CaptureDatEntry(targetBackup, targetEntryIndex); } catch { }
        }
        byte[] replacement = source.AsSpan(sourceStart, sourceLength).ToArray();
        if (mode != SkeletonAdaptMode.KeepDonor) AdaptMeshToTargetSkeleton(targetBin, replacement, mode);
        CommitTpl(targetDatPath, target, targetStart, targetLength, replacement);
    }

    public static void ReplaceBinEntryFromFile(string targetDatPath, int targetEntryIndex, string sourceBinPath, SkeletonAdaptMode mode = SkeletonAdaptMode.Automatic)
    {
        byte[] target = File.ReadAllBytes(targetDatPath);
        (int targetStart, int targetLength, string targetTag) = GetEntry(target, targetEntryIndex);
        if (!targetTag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada de destino não é um BIN.");
        byte[] replacement = File.ReadAllBytes(sourceBinPath);
        if (replacement.Length < 0x10 || BitConverter.ToUInt16(replacement, 0) != 0x30) throw new InvalidDataException("O conversor não produziu um BIN PS2 válido.");
        byte[] targetBin = target.AsSpan(targetStart, targetLength).ToArray();
        string targetBackup = targetDatPath + ".bak";
        if (File.Exists(targetBackup)) try { targetBin = CaptureDatEntry(targetBackup, targetEntryIndex); } catch { }
        if (mode != SkeletonAdaptMode.KeepDonor) AdaptMeshToTargetSkeleton(targetBin, replacement, mode);
        CommitTpl(targetDatPath, target, targetStart, targetLength, replacement);
    }

    public static (bool Adjusted, float Scale) AutoFitBinFileToTemplate(string sourceBinPath, byte[] templateBin)
    {
        byte[] source = File.ReadAllBytes(sourceBinPath);
        var sourceVertices = FindBinVertices(source);
        var targetVertices = FindBinVertices(templateBin);
        if (sourceVertices.Count == 0 || targetVertices.Count == 0) return (false, 1f);
        (Vector3 Min, Vector3 Max) Bounds(byte[] data, List<(int Offset, float Factor)> vertices)
        {
            Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
            foreach (var vertex in vertices)
            {
                Vector3 p = new(BitConverter.ToInt16(data, vertex.Offset) * vertex.Factor / 100f, BitConverter.ToInt16(data, vertex.Offset + 2) * vertex.Factor / 100f, BitConverter.ToInt16(data, vertex.Offset + 4) * vertex.Factor / 100f);
                min = Vector3.Min(min, p); max = Vector3.Max(max, p);
            }
            return (min, max);
        }
        var sb = Bounds(source, sourceVertices); var tb = Bounds(templateBin, targetVertices);
        Vector3 sourceSize = sb.Max - sb.Min, targetSize = tb.Max - tb.Min;
        float sourceExtent = Math.Max(sourceSize.X, Math.Max(sourceSize.Y, sourceSize.Z));
        float targetExtent = Math.Max(targetSize.X, Math.Max(targetSize.Y, targetSize.Z));
        if (sourceExtent < 0.0001f || targetExtent < 0.0001f) return (false, 1f);
        float ratio = targetExtent / sourceExtent;
        Vector3 sourceCenter = (sb.Min + sb.Max) * 0.5f, targetCenter = (tb.Min + tb.Max) * 0.5f;
        bool extremeScale = ratio < 0.5f || ratio > 2f;
        bool misplaced = Vector3.Distance(sourceCenter, targetCenter) > targetExtent * 0.75f;
        if (!extremeScale && !misplaced) return (false, 1f);
        float scale = extremeScale ? Math.Clamp(ratio, 0.01f, 100f) : 1f;
        foreach (var vertex in sourceVertices)
        {
            Vector3 p = new(BitConverter.ToInt16(source, vertex.Offset) * vertex.Factor / 100f, BitConverter.ToInt16(source, vertex.Offset + 2) * vertex.Factor / 100f, BitConverter.ToInt16(source, vertex.Offset + 4) * vertex.Factor / 100f);
            p = (p - sourceCenter) * scale + targetCenter;
            float[] values = { p.X, p.Y, p.Z };
            for (int axis = 0; axis < 3; axis++)
            {
                short raw = (short)Math.Clamp((int)MathF.Round(values[axis] * 100f / vertex.Factor), short.MinValue, short.MaxValue);
                Buffer.BlockCopy(BitConverter.GetBytes(raw), 0, source, vertex.Offset + axis * 2, 2);
            }
        }
        File.WriteAllBytes(sourceBinPath, source); return (true, scale);
    }

    public static (Vector3 Min, Vector3 Max) GetBinBounds(byte[] bin)
    {
        List<(int Offset, float Factor)> vertices = FindBinVertices(bin);
        if (vertices.Count == 0) throw new InvalidDataException("BIN sem geometria para calcular o encaixe.");
        Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
        foreach (var vertex in vertices)
        {
            Vector3 p = new(BitConverter.ToInt16(bin, vertex.Offset) * vertex.Factor / 100f, BitConverter.ToInt16(bin, vertex.Offset + 2) * vertex.Factor / 100f, BitConverter.ToInt16(bin, vertex.Offset + 4) * vertex.Factor / 100f);
            min = Vector3.Min(min, p); max = Vector3.Max(max, p);
        }
        return (min, max);
    }

    private readonly record struct RawBone(byte Id, byte ParentId, Vector3 Local, int Offset);
    private sealed record BinVertexSegment(int FactorOffset, float Factor, List<int> VertexOffsets);

    private static void AdaptMeshToTargetSkeleton(byte[] targetBin, byte[] replacementBin, SkeletonAdaptMode mode)
    {
        if (targetBin.Length < 0x10 || replacementBin.Length < 0x10) throw new InvalidDataException("BIN sem cabeçalho suficiente para validar o esqueleto.");
        int targetBonesOffset = checked((int)BitConverter.ToUInt32(targetBin, 4));
        int sourceBonesOffset = checked((int)BitConverter.ToUInt32(replacementBin, 4));
        int targetCount = targetBin[9], sourceCount = replacementBin[9];
        if (targetCount <= 0 || sourceCount <= 0) throw new InvalidOperationException("Um dos BINs não possui esqueleto adaptável.");
        if (targetBonesOffset < 0 || targetBonesOffset + targetCount * 16 > targetBin.Length || sourceBonesOffset < 0 || sourceBonesOffset + sourceCount * 16 > replacementBin.Length)
            throw new InvalidDataException("Tabela de ossos inválida em um dos BINs.");

        RawBone[] ReadBones(byte[] data, int offset, int count) => Enumerable.Range(0, count).Select(i =>
        {
            int p = offset + i * 16;
            return new RawBone(data[p], data[p + 1], new Vector3(BitConverter.ToSingle(data, p + 4), BitConverter.ToSingle(data, p + 8), BitConverter.ToSingle(data, p + 12)), p);
        }).ToArray();
        RawBone[] targetBones = ReadBones(targetBin, targetBonesOffset, targetCount);
        RawBone[] sourceBones = ReadBones(replacementBin, sourceBonesOffset, sourceCount);

        Vector3[] Globals(RawBone[] bones)
        {
            var result = new Vector3[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                int parent = -1;
                for (int p = i - 1; p >= 0; p--) if (bones[p].Id == bones[i].ParentId) { parent = p; break; }
                result[i] = bones[i].Local + (parent >= 0 ? result[parent] : Vector3.Zero);
            }
            return result;
        }
        Vector3[] targetGlobal = Globals(targetBones), sourceGlobal = Globals(sourceBones);
        (Vector3 Center, float Scale) Bounds(Vector3[] points)
        {
            Vector3 min = points.Aggregate(new Vector3(float.PositiveInfinity), Vector3.Min), max = points.Aggregate(new Vector3(float.NegativeInfinity), Vector3.Max);
            return ((min + max) * 0.5f, Math.Max(0.0001f, (max - min).Length()));
        }
        var tb = Bounds(targetGlobal); var sb = Bounds(sourceGlobal);
        var mapping = new Dictionary<byte, RawBone>();
        var usedTargetIds = new HashSet<byte>();
        RawBone? rigidAnchor = null;
        // A donor with more bones than the receiver slot cannot be mapped one to
        // one. Mapping the surplus by proximity commonly reaches Leon's secondary
        // hair chain (wind physics), making an entire transplanted head wobble.
        // Attach such meshes rigidly to the receiver bone with the largest direct
        // subtree -- for pl00 head slots this is the stable head anchor (bone 4).
        bool rigidAttachment = mode == SkeletonAdaptMode.Rigid || mode == SkeletonAdaptMode.Automatic && sourceBones.Length > targetBones.Length;
        if (rigidAttachment)
        {
            RawBone anchor = targetBones
                .OrderByDescending(candidate => targetBones.Count(other => other.ParentId == candidate.Id))
                .ThenBy(candidate => candidate.Id)
                .First();
            rigidAnchor = anchor;
            foreach (RawBone sourceBone in sourceBones)
            {
                mapping[sourceBone.Id] = anchor;
                Buffer.BlockCopy(targetBin, anchor.Offset, replacementBin, sourceBone.Offset, 16);
                replacementBin[sourceBone.Offset + 1] = 0xFF;
            }
        }
        else for (int i = 0; i < sourceBones.Length; i++)
        {
            RawBone sourceBone = sourceBones[i];
            Vector3 normalizedSource = (sourceGlobal[i] - sb.Center) / sb.Scale;
            mapping.TryGetValue(sourceBone.ParentId, out RawBone mappedParent);
            bool hasMappedParent = sourceBone.ParentId != 0xFF && mapping.ContainsKey(sourceBone.ParentId);
            int best = Enumerable.Range(0, targetBones.Length).OrderBy(t =>
            {
                RawBone candidate = targetBones[t];
                float score = Vector3.DistanceSquared(normalizedSource, (targetGlobal[t] - tb.Center) / tb.Scale);
                if ((sourceBone.ParentId == 0xFF) != (candidate.ParentId == 0xFF)) score += 4f;
                // Parent-chain continuity matters more than coincidental numeric IDs
                // when adapting meshes between different character families.
                if (hasMappedParent && candidate.ParentId != mappedParent.Id) score += 0.75f;
                // Duplicate IDs make two distinct donor pivots receive the same matrix.
                // Prefer a one-to-one assignment while the receiver has spare bones.
                if (usedTargetIds.Contains(candidate.Id) && usedTargetIds.Count < targetBones.Length) score += 2f;
                if (candidate.Id == sourceBone.Id) score -= 0.04f;
                return score;
            }).First();
            mapping[sourceBone.Id] = targetBones[best];
            usedTargetIds.Add(targetBones[best].Id);
        }

        // Keep the donor bind pose (XYZ) intact. Copying the receiver's complete
        // 16-byte bone record changes the donor's pivots and produces long spikes
        // around the jaw/neck once the game applies an animation. Only IDs and the
        // parent chain belong to the receiver animation rig.
        if (!rigidAttachment) foreach (RawBone sourceBone in sourceBones)
        {
            if (!mapping.TryGetValue(sourceBone.Id, out RawBone mapped)) continue;
            replacementBin[sourceBone.Offset] = mapped.Id;
            replacementBin[sourceBone.Offset + 1] = sourceBone.ParentId != 0xFF && mapping.TryGetValue(sourceBone.ParentId, out RawBone mappedParentBone)
                ? mappedParentBone.Id
                : (byte)0xFF;
        }

        // Material nodes carry compact lists of bone IDs used by their weight maps.
        int materialCount = BitConverter.ToUInt16(replacementBin, 0x0A);
        int materialOffset = checked((int)BitConverter.ToUInt32(replacementBin, 0x0C));
        if (materialOffset > 0 && materialOffset + materialCount * 16 <= replacementBin.Length)
            for (int i = 0; i < materialCount; i++)
            {
                int node = checked((int)BitConverter.ToUInt32(replacementBin, materialOffset + i * 16 + 12));
                if (node <= 0 || node + 4 > replacementBin.Length) continue;
                int boneIds = replacementBin[node + 3];
                for (int b = 0; b < boneIds && node + 4 + b < replacementBin.Length; b++)
                    if (rigidAnchor.HasValue) replacementBin[node + 4 + b] = rigidAnchor.Value.Id;
                    else if (mapping.TryGetValue(replacementBin[node + 4 + b], out RawBone mapped)) replacementBin[node + 4 + b] = mapped.Id;
            }
    }

    public static void TransformBinEntry(string datPath, int entryIndex, Vector3 translation, Vector3 rotationDegrees, Vector3 scale)
    {
        if (!float.IsFinite(translation.X + translation.Y + translation.Z + rotationDegrees.X + rotationDegrees.Y + rotationDegrees.Z + scale.X + scale.Y + scale.Z))
            throw new ArgumentException("Transformação contém valores inválidos.");
        if (Math.Abs(scale.X) < 0.0001f || Math.Abs(scale.Y) < 0.0001f || Math.Abs(scale.Z) < 0.0001f)
            throw new ArgumentException("A escala não pode ser zero.");
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, entryIndex);
        if (!tag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada selecionada não é um BIN.");
        byte[] bin = dat.AsSpan(start, length).ToArray();
        List<BinVertexSegment> segments = FindBinVertexSegments(bin);
        List<(int Offset, float Factor)> vertices = segments.SelectMany(segment => segment.VertexOffsets.Select(offset => (offset, segment.Factor))).ToList();
        if (vertices.Count == 0) throw new InvalidDataException("Nenhum vértice editável foi encontrado no BIN.");
        Vector3 Read((int Offset, float Factor) vertex) => new(
            BitConverter.ToInt16(bin, vertex.Offset) * vertex.Factor / 100f,
            BitConverter.ToInt16(bin, vertex.Offset + 2) * vertex.Factor / 100f,
            BitConverter.ToInt16(bin, vertex.Offset + 4) * vertex.Factor / 100f);
        Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
        foreach (var vertex in vertices) { Vector3 p = Read(vertex); min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        Vector3 pivot = (min + max) * 0.5f;
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(rotationDegrees.Y * MathF.PI / 180f, rotationDegrees.X * MathF.PI / 180f, rotationDegrees.Z * MathF.PI / 180f);
        var transformed = new Dictionary<int, Vector3>(vertices.Count);
        foreach (var vertex in vertices)
        {
            Vector3 p = Read(vertex) - pivot;
            p = Vector3.Transform(p * scale, rotation) + pivot + translation;
            transformed[vertex.Offset] = p;
        }
        WriteVerticesWithAdaptiveFactors(bin, segments, transformed);
        CommitTpl(datPath, dat, start, length, bin);
    }

    public static void TransformBinVertices(string datPath, int entryIndex, IReadOnlyCollection<int> sourceVertexOffsets, Vector3 translation, Vector3 rotationDegrees, Vector3 scale)
    {
        if (sourceVertexOffsets.Count == 0) throw new InvalidOperationException("Nenhum vértice de face foi selecionado.");
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, entryIndex);
        if (!tag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada selecionada não é um BIN.");
        byte[] bin = dat.AsSpan(start, length).ToArray();
        List<BinVertexSegment> segments = FindBinVertexSegments(bin);
        Dictionary<int, float> available = segments.SelectMany(segment => segment.VertexOffsets.Select(offset => (offset, segment.Factor))).ToDictionary(x => x.offset, x => x.Factor);
        var vertices = sourceVertexOffsets.Distinct().Where(available.ContainsKey).Select(offset => (Offset: offset, Factor: available[offset])).ToList();
        if (vertices.Count == 0) throw new InvalidDataException("As faces selecionadas não correspondem aos vértices deste BIN.");
        Vector3 Read((int Offset, float Factor) vertex) => new(BitConverter.ToInt16(bin, vertex.Offset) * vertex.Factor / 100f, BitConverter.ToInt16(bin, vertex.Offset + 2) * vertex.Factor / 100f, BitConverter.ToInt16(bin, vertex.Offset + 4) * vertex.Factor / 100f);
        Vector3 pivot = vertices.Select(Read).Aggregate(Vector3.Zero, (sum, value) => sum + value) / vertices.Count;
        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(rotationDegrees.Y * MathF.PI / 180f, rotationDegrees.X * MathF.PI / 180f, rotationDegrees.Z * MathF.PI / 180f);
        var transformed = new Dictionary<int, Vector3>(vertices.Count);
        foreach (var vertex in vertices)
        {
            Vector3 p = Vector3.Transform((Read(vertex) - pivot) * scale, rotation) + pivot + translation;
            transformed[vertex.Offset] = p;
        }
        WriteVerticesWithAdaptiveFactors(bin, segments, transformed);
        CommitTpl(datPath, dat, start, length, bin);
    }

    public static int DeleteBinFaces(string datPath, int entryIndex, IReadOnlyCollection<int> stripFlagOffsets)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, entryIndex);
        if (!tag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada selecionada não é um BIN.");
        byte[] bin = dat.AsSpan(start, length).ToArray(); int changed = 0;
        foreach (int offset in stripFlagOffsets.Distinct())
        {
            if (offset < 0 || offset + 2 > bin.Length) continue;
            if (BitConverter.ToUInt16(bin, offset) != 0) continue;
            bin[offset] = 1; bin[offset + 1] = 0; changed++;
        }
        if (changed == 0) throw new InvalidOperationException("Nenhuma face válida foi encontrada para excluir.");
        CommitTpl(datPath, dat, start, length, bin); return changed;
    }

    public static void CenterBinOnOriginal(string datPath, int entryIndex)
    {
        string backup = datPath + ".bak";
        if (!File.Exists(backup)) throw new FileNotFoundException("Não existe backup original para calcular o encaixe.", backup);
        Vector3 current = GetBinCenter(CaptureDatEntry(datPath, entryIndex));
        Vector3 original = GetBinCenter(CaptureDatEntry(backup, entryIndex));
        TransformBinEntry(datPath, entryIndex, original - current, Vector3.Zero, Vector3.One);
    }

    private static Vector3 GetBinCenter(byte[] bin)
    {
        List<(int Offset, float Factor)> vertices = FindBinVertices(bin);
        if (vertices.Count == 0) throw new InvalidDataException("BIN sem vértices editáveis.");
        Vector3 min = new(float.PositiveInfinity), max = new(float.NegativeInfinity);
        foreach (var vertex in vertices)
        {
            Vector3 p = new(BitConverter.ToInt16(bin, vertex.Offset) * vertex.Factor / 100f, BitConverter.ToInt16(bin, vertex.Offset + 2) * vertex.Factor / 100f, BitConverter.ToInt16(bin, vertex.Offset + 4) * vertex.Factor / 100f);
            min = Vector3.Min(min, p); max = Vector3.Max(max, p);
        }
        return (min + max) * 0.5f;
    }

    private static List<(int Offset, float Factor)> FindBinVertices(byte[] bin)
        => FindBinVertexSegments(bin).SelectMany(segment => segment.VertexOffsets.Select(offset => (offset, segment.Factor))).ToList();

    private static List<BinVertexSegment> FindBinVertexSegments(byte[] bin)
    {
        var result = new Dictionary<int, BinVertexSegment>();
        if (bin.Length < 0x10) return new();
        int materialCount = BitConverter.ToUInt16(bin, 0x0A);
        int materialOffset = checked((int)BitConverter.ToUInt32(bin, 0x0C));
        if (materialOffset <= 0 || materialOffset + materialCount * 16 > bin.Length) return new();
        for (int material = 0; material < materialCount; material++)
        {
            int node = checked((int)BitConverter.ToUInt32(bin, materialOffset + material * 16 + 12));
            if (node <= 0 || node + 4 > bin.Length) continue;
            int segmentCount = bin[node + 2] + 1, boneCount = bin[node + 3];
            int position = node + 4 + ((4 + boneCount + 15) / 16 * 16 - 4);
            for (int segment = 0; segment < segmentCount; segment++)
            {
                if (position + 0x30 > bin.Length) break;
                int header1 = position; position += 0x10;
                if (bin[header1 + 12] == 0 && bin[header1 + 14] > 1)
                {
                    position += bin[header1] * 0x10;
                    if (position + 0x10 > bin.Length) break;
                    position += 0x10;
                }
                if (position + 0x20 > bin.Length) break;
                int header2 = position, header3 = position + 0x10; position += 0x20;
                float factor = BitConverter.ToSingle(bin, header2 + 0x0C);
                if (!float.IsFinite(factor) || Math.Abs(factor) < 0.0000001f) factor = 1f;
                int vertexCount = bin[header2], chunkBytes = bin[header3] * 0x10;
                if (position + chunkBytes > bin.Length) break;
                if (!result.TryGetValue(header2 + 0x0C, out BinVertexSegment? vertexSegment))
                {
                    vertexSegment = new BinVertexSegment(header2 + 0x0C, factor, new List<int>());
                    result.Add(vertexSegment.FactorOffset, vertexSegment);
                }
                for (int i = 0; i < vertexCount && i * 24 + 24 <= chunkBytes; i++)
                {
                    int vertexOffset = position + i * 24;
                    if (!vertexSegment.VertexOffsets.Contains(vertexOffset)) vertexSegment.VertexOffsets.Add(vertexOffset);
                }
                position += chunkBytes;
                if (segment > 0 && position + 0x10 <= bin.Length) position += 0x10;
            }
        }
        return result.Values.ToList();
    }

    private static void WriteVerticesWithAdaptiveFactors(byte[] bin, IReadOnlyList<BinVertexSegment> segments, IReadOnlyDictionary<int, Vector3> transformed)
    {
        const float SafeRawMaximum = 32760f;
        foreach (BinVertexSegment segment in segments)
        {
            float oldFactor = segment.Factor;
            float oldMagnitude = Math.Abs(oldFactor);
            if (!float.IsFinite(oldMagnitude) || oldMagnitude < 0.0000001f) oldMagnitude = 1f;
            var positions = new Dictionary<int, Vector3>(segment.VertexOffsets.Count);
            float maximumCoordinate = 0f;
            foreach (int offset in segment.VertexOffsets)
            {
                Vector3 position = transformed.TryGetValue(offset, out Vector3 changed)
                    ? changed
                    : new Vector3(BitConverter.ToInt16(bin, offset) * oldFactor / 100f,
                        BitConverter.ToInt16(bin, offset + 2) * oldFactor / 100f,
                        BitConverter.ToInt16(bin, offset + 4) * oldFactor / 100f);
                if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                    throw new InvalidDataException("A transformação produziu uma coordenada inválida.");
                positions[offset] = position;
                maximumCoordinate = Math.Max(maximumCoordinate, Math.Max(Math.Abs(position.X), Math.Max(Math.Abs(position.Y), Math.Abs(position.Z))));
            }

            float requiredMagnitude = maximumCoordinate * 100f / SafeRawMaximum;
            float newMagnitude = Math.Max(oldMagnitude, requiredMagnitude);
            if (!float.IsFinite(newMagnitude) || newMagnitude > float.MaxValue / 2f)
                throw new InvalidOperationException("A posição solicitada excede a faixa representável pelo BIN do PS2.");
            // Preserve the (unusual, but valid) sign of a segment factor.
            float newFactor = oldFactor < 0f ? -newMagnitude : newMagnitude;
            Buffer.BlockCopy(BitConverter.GetBytes(newFactor), 0, bin, segment.FactorOffset, sizeof(float));

            foreach ((int offset, Vector3 position) in positions)
                for (int axis = 0; axis < 3; axis++)
                {
                    int raw = checked((int)MathF.Round(position[axis] * 100f / newFactor));
                    if (raw < short.MinValue || raw > short.MaxValue)
                        throw new InvalidOperationException("Não foi possível requantizar um segmento do mesh sem exceder 16 bits.");
                    Buffer.BlockCopy(BitConverter.GetBytes((short)raw), 0, bin, offset + axis * 2, sizeof(short));
                }
        }
    }

    public static int ReplaceBinTextureIndex(string datPath, int binEntryIndex, int oldTextureIndex, int newTextureIndex)
    {
        if (oldTextureIndex is < -1 or > 254) throw new ArgumentOutOfRangeException(nameof(oldTextureIndex));
        if (newTextureIndex is < -1 or > 254) throw new ArgumentOutOfRangeException(nameof(newTextureIndex));
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, binEntryIndex);
        if (!tag.Equals("BIN", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{binEntryIndex:D3} não é um BIN.");
        if (length < 0x10) throw new InvalidDataException("BIN muito pequeno.");
        ushort materialCount = BitConverter.ToUInt16(dat, start + 0x0A);
        uint materialOffset = BitConverter.ToUInt32(dat, start + 0x0C);
        long table = start + materialOffset;
        if (materialCount == 0 || materialOffset == 0 || table < start || table + materialCount * 16L > start + length)
            throw new InvalidDataException("Tabela de materiais do BIN inválida.");
        byte oldRaw = oldTextureIndex < 0 ? (byte)0xFF : checked((byte)oldTextureIndex);
        byte newRaw = newTextureIndex < 0 ? (byte)0xFF : checked((byte)newTextureIndex);
        int changed = 0;
        for (int i = 0; i < materialCount; i++)
        {
            int diffuseOffset = checked((int)table + i * 16 + 1);
            if (dat[diffuseOffset] != oldRaw) continue;
            dat[diffuseOffset] = newRaw; changed++;
        }
        if (changed == 0) throw new InvalidOperationException($"Nenhum material usando o índice {oldTextureIndex} foi encontrado neste BIN.");
        string backup = datPath + ".bak";
        if (!File.Exists(backup)) File.Copy(datPath, backup, false);
        string staged = datPath + ".tmp"; File.WriteAllBytes(staged, dat); File.Move(staged, datPath, true);
        return changed;
    }

    public static void DisableBinGeometry(string datPath, int binEntryIndex)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, binEntryIndex);
        if (!tag.Equals("BIN", StringComparison.OrdinalIgnoreCase) || length < 0x10) throw new InvalidDataException("A entrada selecionada não é um BIN válido.");
        // Keeping the BIN and its skeleton in place avoids shifting DAT indices;
        // zero materials means the game/parser has no geometry nodes to draw.
        dat[start + 0x0A] = 0;
        dat[start + 0x0B] = 0;
        string backup = datPath + ".bak";
        if (!File.Exists(backup)) File.Copy(datPath, backup, false);
        string staged = datPath + ".tmp"; File.WriteAllBytes(staged, dat); File.Move(staged, datPath, true);
    }

    public static void RestoreTplEntry(string datPath, int tplEntryIndex, byte[] tplData)
    {
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");
        CommitTpl(datPath, dat, start, length, tplData);
    }

    public static void ReplaceTextureFromPng(string datPath, int tplEntryIndex, int textureIndex, string pngPath)
    {
        if (!File.Exists(datPath)) throw new FileNotFoundException("DAT não encontrado.", datPath);
        if (!File.Exists(pngPath)) throw new FileNotFoundException("PNG não encontrado.", pngPath);

        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");

        string temp = Path.Combine(Path.GetTempPath(), $"re4_character_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(temp, dat.AsSpan(start, length).ToArray());
            new TextureWorkspaceService().ReplaceFromImage(temp, textureIndex, pngPath);
            byte[] replacement = File.ReadAllBytes(temp);
            if (replacement.Length != length)
                throw new InvalidDataException($"A textura alterou o tamanho do TPL ({length:N0} → {replacement.Length:N0} bytes). A gravação no DAT foi cancelada.");

            string backup = datPath + ".bak";
            if (!File.Exists(backup)) File.Copy(datPath, backup, false);
            Buffer.BlockCopy(replacement, 0, dat, start, length);
            string staged = datPath + ".tmp";
            File.WriteAllBytes(staged, dat);
            File.Move(staged, datPath, true);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }

    public static void CopyTextureFromDat(string targetDatPath, int targetTplEntry, int targetTextureIndex, string sourceDatPath, int sourceTplEntry, int sourceTextureIndex)
    {
        byte[] targetDat = File.ReadAllBytes(targetDatPath);
        (int targetStart, int targetLength, string targetTag) = GetEntry(targetDat, targetTplEntry);
        if (!targetTag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A entrada TPL de destino é inválida.");
        byte[] sourceTpl = CaptureTplEntry(sourceDatPath, sourceTplEntry);
        string targetTemp = Path.Combine(Path.GetTempPath(), $"re4_mesh_tex_target_{Guid.NewGuid():N}.tpl");
        string sourceTemp = Path.Combine(Path.GetTempPath(), $"re4_mesh_tex_source_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(targetTemp, targetDat.AsSpan(targetStart, targetLength).ToArray());
            File.WriteAllBytes(sourceTemp, sourceTpl);
            var reader = new TplReader();
            new TplWriter(reader).ReplaceTexture(targetTemp, targetTextureIndex, sourceTemp, sourceTextureIndex);
            CommitTpl(targetDatPath, targetDat, targetStart, targetLength, File.ReadAllBytes(targetTemp));
        }
        finally
        {
            try { if (File.Exists(targetTemp)) File.Delete(targetTemp); } catch { }
            try { if (File.Exists(sourceTemp)) File.Delete(sourceTemp); } catch { }
        }
    }

    public static void TransformTexture(string datPath, int tplEntryIndex, int textureIndex, TextureTransform transform)
    {
        if (!File.Exists(datPath)) throw new FileNotFoundException("DAT não encontrado.", datPath);
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");

        string temp = Path.Combine(Path.GetTempPath(), $"re4_character_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(temp, dat.AsSpan(start, length).ToArray());
            var service = new TextureWorkspaceService();
            using Bitmap bitmap = service.Decode(temp, textureIndex);
            bitmap.RotateFlip(transform switch
            {
                TextureTransform.Rotate90 => RotateFlipType.Rotate90FlipNone,
                TextureTransform.FlipX => RotateFlipType.RotateNoneFlipX,
                TextureTransform.FlipY => RotateFlipType.RotateNoneFlipY,
                _ => RotateFlipType.RotateNoneFlipNone
            });
            service.ReplaceFromBitmap(temp, textureIndex, bitmap, preserveDimensions: transform != TextureTransform.Rotate90);
            CommitTpl(datPath, dat, start, length, File.ReadAllBytes(temp));
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }

    public static void ResizeTexture(string datPath, int tplEntryIndex, int textureIndex, int width, int height, TextureResizeResampling resampling)
    {
        EditEmbeddedTpl(datPath, tplEntryIndex, (temp, service) =>
        {
            using Bitmap source = service.Decode(temp, textureIndex);
            using var resized = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = resampling switch
                {
                    TextureResizeResampling.NearestNeighbor => System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor,
                    TextureResizeResampling.Bilinear => System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear,
                    _ => System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                };
                graphics.PixelOffsetMode = resampling == TextureResizeResampling.NearestNeighbor
                    ? System.Drawing.Drawing2D.PixelOffsetMode.Half
                    : System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.DrawImage(source, new Rectangle(0, 0, width, height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
            }
            service.ReplaceFromBitmap(temp, textureIndex, resized, preserveDimensions: false);
        });
    }

    public static void ConvertTextureBitDepth(string datPath, int tplEntryIndex, int textureIndex, int colors) =>
        EditEmbeddedTpl(datPath, tplEntryIndex, (temp, service) => service.ConvertBitDepth(temp, textureIndex, colors));

    private static void EditEmbeddedTpl(string datPath, int tplEntryIndex, Action<string, TextureWorkspaceService> edit)
    {
        if (!File.Exists(datPath)) throw new FileNotFoundException("DAT não encontrado.", datPath);
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");
        string temp = Path.Combine(Path.GetTempPath(), $"re4_character_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(temp, dat.AsSpan(start, length).ToArray());
            edit(temp, new TextureWorkspaceService());
            CommitTpl(datPath, dat, start, length, File.ReadAllBytes(temp));
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }

    public static void RestoreTextureFromBackup(string datPath, int tplEntryIndex, int textureIndex)
    {
        string backupPath = datPath + ".bak";
        if (!File.Exists(backupPath)) throw new FileNotFoundException("O backup original do DAT ainda não existe.", backupPath);
        byte[] dat = File.ReadAllBytes(datPath);
        (int start, int length, string tag) = GetEntry(dat, tplEntryIndex);
        if (!tag.Equals("TPL", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"A entrada #{tplEntryIndex:D3} não é um TPL.");
        byte[] originalTpl = CaptureTplEntry(backupPath, tplEntryIndex);
        string targetTemp = Path.Combine(Path.GetTempPath(), $"re4_restore_target_{Guid.NewGuid():N}.tpl");
        string sourceTemp = Path.Combine(Path.GetTempPath(), $"re4_restore_source_{Guid.NewGuid():N}.tpl");
        try
        {
            File.WriteAllBytes(targetTemp, dat.AsSpan(start, length).ToArray());
            File.WriteAllBytes(sourceTemp, originalTpl);
            var reader = new TplReader();
            new TplWriter(reader).ReplaceTexture(targetTemp, textureIndex, sourceTemp, textureIndex);
            CommitTpl(datPath, dat, start, length, File.ReadAllBytes(targetTemp));
        }
        finally
        {
            try { if (File.Exists(targetTemp)) File.Delete(targetTemp); } catch { }
            try { if (File.Exists(sourceTemp)) File.Delete(sourceTemp); } catch { }
        }
    }

    private static void CommitTpl(string datPath, byte[] dat, int start, int length, byte[] replacement)
    {
        // Preserve the original entry alignment when an edit changes the payload
        // size (resize and 4/8-bit conversion both legitimately do this).
        int alignmentPad = (length - replacement.Length) & 0x0F;
        if (alignmentPad != 0) Array.Resize(ref replacement, checked(replacement.Length + alignmentPad));
        string backup = datPath + ".bak";
        if (!File.Exists(backup)) File.Copy(datPath, backup, false);
        int delta = replacement.Length - length;
        byte[] output = new byte[checked(dat.Length + delta)];
        Buffer.BlockCopy(dat, 0, output, 0, start);
        Buffer.BlockCopy(replacement, 0, output, start, replacement.Length);
        Buffer.BlockCopy(dat, start + length, output, start + replacement.Length, dat.Length - start - length);
        if (delta != 0)
        {
            int count = checked((int)BitConverter.ToUInt32(output, 0));
            for (int i = 0; i < count; i++)
            {
                int offsetPosition = 0x10 + i * 4;
                int offset = checked((int)BitConverter.ToUInt32(output, offsetPosition));
                if (offset > start) Buffer.BlockCopy(BitConverter.GetBytes(checked(offset + delta)), 0, output, offsetPosition, 4);
            }
        }
        string staged = datPath + ".tmp";
        File.WriteAllBytes(staged, output);
        File.Move(staged, datPath, true);
    }

    private static (int Start, int Length, string Tag) GetEntry(byte[] dat, int entryIndex)
    {
        if (dat.Length < 0x20) throw new InvalidDataException("DAT muito pequeno.");
        int count = checked((int)BitConverter.ToUInt32(dat, 0));
        if (count <= 0 || entryIndex < 0 || entryIndex >= count) throw new ArgumentOutOfRangeException(nameof(entryIndex));
        int tableEnd = checked(0x10 + count * 8);
        if (tableEnd > dat.Length) throw new InvalidDataException("Tabelas do DAT inválidas.");
        int start = checked((int)BitConverter.ToUInt32(dat, 0x10 + entryIndex * 4));
        int end = dat.Length;
        for (int i = entryIndex + 1; i < count; i++)
        {
            int candidate = checked((int)BitConverter.ToUInt32(dat, 0x10 + i * 4));
            if (candidate > start && candidate <= dat.Length) { end = candidate; break; }
        }
        if (start < tableEnd || start >= dat.Length || end <= start) throw new InvalidDataException("Offset da entrada inválido.");
        int tagOffset = checked(0x10 + count * 4 + entryIndex * 4);
        string tag = Encoding.ASCII.GetString(dat, tagOffset, 4).TrimEnd('\0');
        return (start, end - start, tag);
    }
}

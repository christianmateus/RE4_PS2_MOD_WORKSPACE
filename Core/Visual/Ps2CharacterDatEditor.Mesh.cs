using System.Text;
using System.Numerics;
using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Visual;

public static partial class Ps2CharacterDatEditor
{
    private sealed record BinVertexSegment(int FactorOffset, float Factor, List<int> VertexOffsets);
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

    /// <summary>Transforms referenced vertices in a standalone BIN using the same
    /// segment-wide adaptive-factor protection as the character editor.</summary>
    public static byte[] TransformStandaloneBinVertices(byte[] source, IReadOnlyCollection<int> sourceVertexOffsets, Vector3 translation, Vector3 rotationDegrees, Vector3 scale)
    {
        ArgumentNullException.ThrowIfNull(source);ArgumentNullException.ThrowIfNull(sourceVertexOffsets);
        if(sourceVertexOffsets.Count==0)throw new InvalidOperationException("Nenhum vértice foi selecionado.");
        byte[] bin=(byte[])source.Clone();List<BinVertexSegment> segments=FindBinVertexSegments(bin);
        Dictionary<int,float> available=segments.SelectMany(segment=>segment.VertexOffsets.Select(offset=>(offset,segment.Factor))).GroupBy(x=>x.offset).ToDictionary(x=>x.Key,x=>x.First().Factor);
        var vertices=sourceVertexOffsets.Distinct().Where(available.ContainsKey).Select(offset=>(Offset:offset,Factor:available[offset])).ToList();
        if(vertices.Count==0)throw new InvalidDataException("Os componentes selecionados não possuem referências estruturais válidas neste BIN.");
        Vector3 Read((int Offset,float Factor) v)=>new(BitConverter.ToInt16(bin,v.Offset)*v.Factor/100f,BitConverter.ToInt16(bin,v.Offset+2)*v.Factor/100f,BitConverter.ToInt16(bin,v.Offset+4)*v.Factor/100f);
        Vector3 pivot=vertices.Select(Read).Aggregate(Vector3.Zero,(sum,value)=>sum+value)/vertices.Count;
        Quaternion rotation=Quaternion.CreateFromYawPitchRoll(rotationDegrees.Y*MathF.PI/180f,rotationDegrees.X*MathF.PI/180f,rotationDegrees.Z*MathF.PI/180f);
        var transformed=new Dictionary<int,Vector3>();foreach(var vertex in vertices)transformed[vertex.Offset]=Vector3.Transform((Read(vertex)-pivot)*scale,rotation)+pivot+translation;
        WriteVerticesWithAdaptiveFactors(bin,segments,transformed);return bin;
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
}

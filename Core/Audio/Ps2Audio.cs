using System.Buffers.Binary;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.Audio;

public sealed class SndDocument
{
    public string SourcePath { get; init; } = "";
    public byte[] OriginalBytes { get; init; } = [];
    public List<SndGroup> Groups { get; } = [];
    public bool IsModified => Groups.SelectMany(x => x.Samples).Any(x => x.ReplacementData != null);
}

public sealed class SndGroup
{
    internal int DescriptorOffset;
    public int Index { get; init; }
    public uint MetadataOffset { get; internal set; }
    public uint MetadataLength { get; internal set; }
    public uint HeaderOffset { get; internal set; }
    public uint HeaderLength { get; internal set; }
    public uint AudioOffset { get; internal set; }
    public uint AudioLength { get; internal set; }
    public uint VagiOffset { get; internal set; }
    public List<SndSample> Samples { get; } = [];
}

public sealed class SndSample
{
    public int Index { get; init; }
    public uint RelativeOffset { get; internal set; }
    public uint Length { get; internal set; }
    public uint LoopFlag { get; set; }
    public uint SampleRate { get; set; }
    public byte[] Data { get; init; } = [];
    public byte[]? ReplacementData { get; set; }
    public byte[] EffectiveData => ReplacementData ?? Data;
    public TimeSpan Duration => TimeSpan.FromSeconds(EffectiveData.Length / 16d * 28d / Math.Max(1, SampleRate));
}

public static class SndCodec
{
    public static SndDocument Read(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 0x80) throw new InvalidDataException("Arquivo SND pequeno demais.");
        var doc = new SndDocument { SourcePath = path, OriginalBytes = data };
        for (int groupIndex = 0; groupIndex < 16; groupIndex++)
        {
            int d = 0x20 + groupIndex * 0x60;
            if (d + 0x50 > data.Length) break;
            uint metadataLength = U32(data, d + 4), metadataOffset = U32(data, d + 12);
            uint headerLength = U32(data, d + 0x24), headerOffset = U32(data, d + 0x2C);
            uint audioLength = U32(data, d + 0x44), audioOffset = U32(data, d + 0x4C);
            if (metadataLength == 0 && headerLength == 0 && audioLength == 0) break;
            CheckRange(data, headerOffset, headerLength, $"header do grupo {groupIndex + 1}");
            CheckRange(data, audioOffset, audioLength, $"áudio do grupo {groupIndex + 1}");
            var group = new SndGroup { Index = groupIndex, DescriptorOffset = d, MetadataOffset = metadataOffset, MetadataLength = metadataLength, HeaderOffset = headerOffset, HeaderLength = headerLength, AudioOffset = audioOffset, AudioLength = audioLength };
            group.VagiOffset = FindChunk(data, headerOffset, headerLength, "Vagi");
            int v = checked((int)group.VagiOffset);
            uint storedCount = U32(data, v + 8);
            int count = checked((int)storedCount + 1); // O jogo armazena o maior índice, não a quantidade.
            if (count <= 0 || v + 16L + count * 16L > data.Length) throw new InvalidDataException($"Tabela VAGI inválida no grupo {groupIndex + 1}.");
            for (int i = 0; i < count; i++)
            {
                int p = v + 16 + i * 16;
                uint off = U32(data, p), len = U32(data, p + 4);
                if ((ulong)off + len > audioLength) throw new InvalidDataException($"Amostra {i + 1} do grupo {groupIndex + 1} ultrapassa o bloco de áudio.");
                group.Samples.Add(new SndSample { Index = i, RelativeOffset = off, Length = len, LoopFlag = U32(data, p + 8), SampleRate = U32(data, p + 12), Data = data.AsSpan(checked((int)(audioOffset + off)), checked((int)len)).ToArray() });
            }
            doc.Groups.Add(group);
        }
        if (doc.Groups.Count == 0) throw new InvalidDataException("Nenhum grupo de áudio SND reconhecido.");
        return doc;
    }

    public static void Save(SndDocument doc, string path)
    {
        byte[] result = (byte[])doc.OriginalBytes.Clone();
        foreach (SndGroup group in doc.Groups.OrderByDescending(x => x.AudioOffset))
        {
            using var audio = new MemoryStream();
            foreach (SndSample sample in group.Samples)
            {
                while ((audio.Position & 0xF) != 0) audio.WriteByte(0);
                sample.RelativeOffset = checked((uint)audio.Position);
                byte[] bytes = sample.EffectiveData;
                sample.Length = checked((uint)bytes.Length);
                audio.Write(bytes);
            }
            byte[] rebuilt = audio.ToArray();
            int oldStart = checked((int)group.AudioOffset), oldLength = checked((int)group.AudioLength);
            result = ReplaceRange(result, oldStart, oldLength, rebuilt);
            int delta = rebuilt.Length - oldLength;
            group.AudioLength = checked((uint)rebuilt.Length);
            Put32(result, group.DescriptorOffset + 0x44, group.AudioLength);
            int table = checked((int)group.VagiOffset);
            for (int i = 0; i < group.Samples.Count; i++)
            {
                SndSample s = group.Samples[i]; int p = table + 16 + i * 16;
                Put32(result, p, s.RelativeOffset); Put32(result, p + 4, s.Length); Put32(result, p + 8, s.LoopFlag); Put32(result, p + 12, s.SampleRate);
            }
            if (delta != 0)
            {
                foreach (SndGroup later in doc.Groups.Where(x => x.AudioOffset > group.AudioOffset))
                {
                    later.MetadataOffset = Shift(later.MetadataOffset, delta); later.HeaderOffset = Shift(later.HeaderOffset, delta); later.AudioOffset = Shift(later.AudioOffset, delta); later.VagiOffset = Shift(later.VagiOffset, delta);
                    Put32(result, later.DescriptorOffset + 12, later.MetadataOffset); Put32(result, later.DescriptorOffset + 0x2C, later.HeaderOffset); Put32(result, later.DescriptorOffset + 0x4C, later.AudioOffset);
                }
            }
        }
        string full = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        string temp = full + ".tmp";
        try { File.WriteAllBytes(temp, result); _ = Read(temp); File.Move(temp, full, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public static byte[] ExportVag(SndSample sample, string? name = null) => VagCodec.Wrap(sample.EffectiveData, sample.SampleRate, name ?? $"sample_{sample.Index + 1:000}");
    public static void ImportVag(SndSample sample, byte[] vag)
    {
        VagFile f = VagCodec.Read(vag); sample.ReplacementData = f.AdpcmData; sample.SampleRate = f.SampleRate;
    }
    public static void ImportWave(SndSample sample, byte[] wave)
    {
        WavePcm pcm = WaveCodec.Read(wave); sample.ReplacementData = PsxAdpcm.Encode(pcm.Samples, sample.LoopFlag != 0); sample.SampleRate = checked((uint)pcm.SampleRate);
    }
    private static uint FindChunk(byte[] data, uint start, uint length, string magic)
    {
        int p = checked((int)start), end = checked((int)Math.Min(data.Length, (long)start + length));
        while (p + 8 <= end)
        {
            string id = Encoding.ASCII.GetString(data, p, 4); uint len = U32(data, p + 4);
            if (id.Equals(magic, StringComparison.OrdinalIgnoreCase)) return checked((uint)p);
            if (len < 8 || p + (long)len > end) break; p += checked((int)len);
        }
        throw new InvalidDataException($"Seção {magic} não encontrada.");
    }
    private static byte[] ReplaceRange(byte[] source, int start, int length, byte[] replacement) { byte[] r = new byte[source.Length - length + replacement.Length]; Buffer.BlockCopy(source, 0, r, 0, start); Buffer.BlockCopy(replacement, 0, r, start, replacement.Length); Buffer.BlockCopy(source, start + length, r, start + replacement.Length, source.Length - start - length); return r; }
    private static void CheckRange(byte[] d, uint o, uint l, string label) { if ((ulong)o + l > (ulong)d.Length) throw new InvalidDataException($"Intervalo inválido para {label}."); }
    private static uint U32(byte[] d, int p) => BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(p, 4));
    private static void Put32(byte[] d, int p, uint v) => BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(p, 4), v);
    private static uint Shift(uint value, int delta) => checked((uint)(value + delta));
}

public sealed record VagFile(uint SampleRate, byte[] AdpcmData, string Name);

public static class VagCodec
{
    public static VagFile Read(byte[] data)
    {
        if (data.Length < 0x40 || !data.AsSpan(0, 4).SequenceEqual("VAGp"u8)) throw new InvalidDataException("Cabeçalho VAGp inválido.");
        uint length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12, 4)); uint rate = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16, 4));
        if (length > data.Length - 0x40) throw new InvalidDataException("VAG truncado.");
        string name = Encoding.ASCII.GetString(data, 0x20, 0x20).TrimEnd('\0');
        return new VagFile(rate, data.AsSpan(0x40, checked((int)length)).ToArray(), name);
    }
    public static byte[] Wrap(byte[] adpcm, uint rate, string name)
    {
        byte[] r = new byte[0x40 + adpcm.Length]; "VAGp"u8.CopyTo(r); BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(4, 4), 6); BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(12, 4), checked((uint)adpcm.Length)); BinaryPrimitives.WriteUInt32BigEndian(r.AsSpan(16, 4), rate);
        Encoding.ASCII.GetBytes(name.Length > 31 ? name[..31] : name).CopyTo(r, 0x20); adpcm.CopyTo(r, 0x40); return r;
    }
}

public static class PsxAdpcm
{
    private static readonly int[,] Coefficients = { { 0, 0 }, { 60, 0 }, { 115, -52 }, { 98, -55 }, { 122, -60 } };
    public static short[] Decode(byte[] data) => Decode((ReadOnlySpan<byte>)data);
    public static short[] Decode(ReadOnlySpan<byte> data)
    {
        var samples = new List<short>(data.Length / 16 * 28); int h1 = 0, h2 = 0;
        for (int p = 0; p + 16 <= data.Length; p += 16)
        {
            int filter = Math.Min(4, data[p] >> 4), shift = Math.Min(12, data[p] & 15);
            for (int i = 0; i < 28; i++) { int n = (data[p + 2 + i / 2] >> ((i & 1) * 4)) & 15; if (n >= 8) n -= 16; int value = (n << 12) >> shift; value += (h1 * Coefficients[filter, 0] + h2 * Coefficients[filter, 1] + 32) >> 6; value = Math.Clamp(value, short.MinValue, short.MaxValue); samples.Add((short)value); h2 = h1; h1 = value; }
            if ((data[p + 1] & 1) != 0) break;
        }
        return samples.ToArray();
    }
    public static byte[] Encode(ReadOnlySpan<short> pcm, bool loop)
    {
        int blocks = Math.Max(1, (pcm.Length + 27) / 28); byte[] output = new byte[blocks * 16]; int h1 = 0, h2 = 0;
        for (int block = 0; block < blocks; block++)
        {
            long bestError = long.MaxValue; int bestFilter = 0, bestShift = 0, bestH1 = 0, bestH2 = 0; int[] bestNibbles = new int[28];
            for (int filter = 0; filter < 5; filter++) for (int shift = 0; shift <= 12; shift++)
            {
                int th1 = h1, th2 = h2; long error = 0; int[] ns = new int[28];
                for (int i = 0; i < 28; i++) { int target = block * 28 + i < pcm.Length ? pcm[block * 28 + i] : 0; int predicted = (th1 * Coefficients[filter, 0] + th2 * Coefficients[filter, 1] + 32) >> 6; int step = 1 << (12 - shift); int n = Math.Clamp((int)Math.Round((target - predicted) / (double)step), -8, 7); int decoded = Math.Clamp(predicted + n * step, short.MinValue, short.MaxValue); long e = target - decoded; error += e * e; ns[i] = n & 15; th2 = th1; th1 = decoded; }
                if (error < bestError) { bestError = error; bestFilter = filter; bestShift = shift; bestH1 = th1; bestH2 = th2; bestNibbles = ns; }
            }
            int p = block * 16; output[p] = (byte)(bestFilter << 4 | bestShift); output[p + 1] = block == blocks - 1 ? (byte)(loop ? 3 : 1) : (byte)(loop && block == 0 ? 6 : 0);
            for (int i = 0; i < 28; i += 2) output[p + 2 + i / 2] = (byte)(bestNibbles[i] | bestNibbles[i + 1] << 4); h1 = bestH1; h2 = bestH2;
        }
        return output;
    }
    public static byte[] Encode(short[] pcm, bool loop) => Encode((ReadOnlySpan<short>)pcm, loop);
}

public sealed record WavePcm(int SampleRate, short[] Samples);
public static class WaveCodec
{
    public static WavePcm Read(byte[] data)
    {
        if (data.Length < 44 || !data.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !data.AsSpan(8, 4).SequenceEqual("WAVE"u8)) throw new InvalidDataException("Use WAV PCM de 16 bits.");
        int p = 12, channels = 0, rate = 0, bits = 0; ushort format = 0; ReadOnlySpan<byte> payload = default;
        while (p + 8 <= data.Length) { int len = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(p + 4, 4)); if (len < 0 || p + 8L + len > data.Length) break; var chunk = data.AsSpan(p + 8, len); string id = Encoding.ASCII.GetString(data, p, 4); if (id == "fmt " && len >= 16) { format = BinaryPrimitives.ReadUInt16LittleEndian(chunk); channels = BinaryPrimitives.ReadUInt16LittleEndian(chunk[2..]); rate = BinaryPrimitives.ReadInt32LittleEndian(chunk[4..]); bits = BinaryPrimitives.ReadUInt16LittleEndian(chunk[14..]); } else if (id == "data") payload = chunk; p += 8 + len + (len & 1); }
        if (format != 1 || bits != 16 || channels is < 1 or > 2 || payload.IsEmpty) throw new InvalidDataException("O WAV deve ser PCM 16-bit mono ou estéreo.");
        short[] result = new short[payload.Length / 2 / channels]; for (int i = 0; i < result.Length; i++) { int sum = 0; for (int c = 0; c < channels; c++) sum += BinaryPrimitives.ReadInt16LittleEndian(payload.Slice((i * channels + c) * 2, 2)); result[i] = (short)(sum / channels); } return new WavePcm(rate, result);
    }
    public static byte[] Write(short[] samples, int sampleRate)
    {
        byte[] r = new byte[44 + samples.Length * 2]; "RIFF"u8.CopyTo(r); BinaryPrimitives.WriteInt32LittleEndian(r.AsSpan(4), r.Length - 8); "WAVEfmt "u8.CopyTo(r.AsSpan(8)); BinaryPrimitives.WriteInt32LittleEndian(r.AsSpan(16), 16); BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(20), 1); BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(22), 1); BinaryPrimitives.WriteInt32LittleEndian(r.AsSpan(24), sampleRate); BinaryPrimitives.WriteInt32LittleEndian(r.AsSpan(28), sampleRate * 2); BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(32), 2); BinaryPrimitives.WriteUInt16LittleEndian(r.AsSpan(34), 16); "data"u8.CopyTo(r.AsSpan(36)); BinaryPrimitives.WriteInt32LittleEndian(r.AsSpan(40), samples.Length * 2); for (int i = 0; i < samples.Length; i++) BinaryPrimitives.WriteInt16LittleEndian(r.AsSpan(44 + i * 2), samples[i]); return r;
    }
}

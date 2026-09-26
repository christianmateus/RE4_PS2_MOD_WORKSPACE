namespace RE4_PS2_MOD_WORKSPACE.Core.Video;

public sealed record SfdVideoInfo(int Width, int Height, double FramesPerSecond, int BitRate);

public sealed record SfdAudioInfo(
    int EncodingType,
    int BlockSize,
    int SampleBitDepth,
    int Channels,
    int SampleRate,
    uint SampleCount,
    int HighPassFrequency,
    int Version)
{
    public TimeSpan Duration => SampleRate > 0 ? TimeSpan.FromSeconds((double)SampleCount / SampleRate) : TimeSpan.Zero;
}

public sealed record SfdInfo(
    string StreamName,
    long FileSize,
    int SectorCount,
    int VideoPacketCount,
    int AudioPacketCount,
    SfdVideoInfo Video,
    SfdAudioInfo Audio);

public sealed record SfdBuildResult(SfdInfo Info, TimeSpan VideoDuration, TimeSpan AudioDuration);

/// <summary>Reader/demultiplexer for the sector-based CRI Sofdec v1 format used by RE4 PS2.</summary>
public static class SfdService
{
    private delegate void PacketVisitor(byte streamId, ReadOnlySpan<byte> payload);

    public const int SectorSize = 0x800;

    private static readonly double[] FrameRates =
    {
        0, 24000d / 1001d, 24, 25, 30000d / 1001d, 30, 50, 60000d / 1001d, 60
    };

    public static SfdInfo Read(string path)
    {
        using FileStream input = File.OpenRead(path);
        if (input.Length == 0 || input.Length % SectorSize != 0)
            throw new InvalidDataException("SFD inválido: o tamanho não está alinhado a setores de 0x800 bytes.");

        int videoPackets = 0, audioPackets = 0;
        SfdVideoInfo? video = null;
        SfdAudioInfo? audio = null;
        string streamName = Path.GetFileNameWithoutExtension(path);

        WalkPackets(input, (streamId, payload) =>
        {
            if (streamId == 0xE0)
            {
                videoPackets++;
                video ??= TryReadVideo(payload);
            }
            else if (streamId == 0xC0)
            {
                audioPackets++;
                audio ??= TryReadAdx(payload);
            }
            else if (streamId == 0xBF)
            {
                streamName = TryReadStreamName(payload) ?? streamName;
            }
        });

        if (video == null) throw new InvalidDataException("SFD sem sequence header MPEG-1 válido.");
        if (audio == null) throw new InvalidDataException("SFD sem cabeçalho CRI ADX válido.");
        return new SfdInfo(streamName, input.Length, checked((int)(input.Length / SectorSize)), videoPackets, audioPackets, video, audio);
    }

    public static SfdInfo Demux(string path, string videoPath, string audioPath)
    {
        SfdInfo info = Read(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(videoPath))!);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(audioPath))!);
        using FileStream video = File.Create(videoPath);
        using FileStream audio = File.Create(audioPath);
        using FileStream input = File.OpenRead(path);
        WalkPackets(input, (streamId, payload) =>
        {
            if (streamId == 0xE0) video.Write(payload);
            else if (streamId == 0xC0) audio.Write(payload);
        });
        return info;
    }

    public static SfdInfo ExportAudioWave(string path, string wavePath)
    {
        SfdInfo info = Read(path);
        using var adx = new MemoryStream();
        using (FileStream input = File.OpenRead(path))
            WalkPackets(input, (streamId, payload) => { if (streamId == 0xC0) adx.Write(payload); });
        short[] samples = DecodeAdx(adx.ToArray(), info.Audio);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(wavePath))!);
        File.WriteAllBytes(wavePath, Core.Audio.WaveCodec.Write(samples, info.Audio.SampleRate, info.Audio.Channels));
        return info;
    }

    public static SfdBuildResult BuildFromMpegAndWave(string templateSfdPath, string mpegPath, string wavePath, string outputPath)
    {
        SfdInfo templateInfo = Read(templateSfdPath);
        byte[] video = File.ReadAllBytes(mpegPath);
        if (video.Length >= 4 && HasStartCode(video, 0, 0xBA))
            throw new InvalidDataException("Use um stream MPEG-1 elementar (.m1v), sem contêiner MPG. Você pode obtê-lo pela opção VÍDEO + WAV.");
        SfdVideoInfo videoInfo = TryReadVideo(video) ?? throw new InvalidDataException("O arquivo selecionado não contém um sequence header MPEG-1 válido.");
        if (videoInfo.Width != templateInfo.Video.Width || videoInfo.Height != templateInfo.Video.Height || Math.Abs(videoInfo.FramesPerSecond - templateInfo.Video.FramesPerSecond) > 0.01)
            throw new InvalidDataException($"O MPEG-1 deve usar {templateInfo.Video.Width} × {templateInfo.Video.Height} a {templateInfo.Video.FramesPerSecond:0.###} fps para substituir esta cutscene.");

        WaveInput wave = ReadWave(File.ReadAllBytes(wavePath));
        if (wave.SampleRate != templateInfo.Audio.SampleRate)
            throw new InvalidDataException($"O WAV deve usar {templateInfo.Audio.SampleRate:N0} Hz para substituir esta cutscene.");
        short[] samples = ConvertChannels(wave.Samples, wave.Channels, templateInfo.Audio.Channels);
        byte[] templateAdx = CollectStream(templateSfdPath, 0xC0);
        byte[] adx = EncodeAdx(samples, wave.SampleRate, templateInfo.Audio.Channels, templateInfo.Audio.HighPassFrequency, templateAdx);

        int pictureCount = CountStartCodes(video, 0x00);
        TimeSpan videoDuration = pictureCount > 0 ? TimeSpan.FromSeconds(pictureCount / videoInfo.FramesPerSecond) : TimeSpan.FromSeconds((double)(samples.Length / templateInfo.Audio.Channels) / wave.SampleRate);
        TimeSpan audioDuration = TimeSpan.FromSeconds((double)(samples.Length / templateInfo.Audio.Channels) / wave.SampleRate);
        WriteSfd(templateSfdPath, video, adx, videoDuration, audioDuration, outputPath);
        SfdInfo result = Read(outputPath);
        return new SfdBuildResult(result, videoDuration, audioDuration);
    }

    public static short[] DecodeAdx(ReadOnlySpan<byte> adx, SfdAudioInfo info)
    {
        if (adx.Length < 20 || adx[0] != 0x80 || adx[1] != 0) throw new InvalidDataException("Cabeçalho ADX inválido.");
        int dataOffset = ReadUInt16BE(adx, 2) + 4;
        int samplesPerBlock = (info.BlockSize - 2) * 2;
        if (dataOffset < 0 || dataOffset > adx.Length || samplesPerBlock <= 0) throw new InvalidDataException("Layout ADX inválido.");

        double z = Math.Cos(2.0 * Math.PI * info.HighPassFrequency / info.SampleRate);
        double a = Math.Sqrt(2.0) - z, b = Math.Sqrt(2.0) - 1.0;
        double c = (a - Math.Sqrt((a + b) * (a - b))) / b;
        const int coefficientBits = 12;
        const int coefficientScale = 1 << coefficientBits;
        int coefficient1 = (int)Math.Round(c * 2.0 * coefficientScale);
        int coefficient2 = (int)Math.Round(-(c * c) * coefficientScale);
        int totalFrames = checked((int)info.SampleCount);
        short[] output = new short[checked(totalFrames * info.Channels)];
        int[] history1 = new int[info.Channels], history2 = new int[info.Channels];
        int source = dataOffset, frame = 0;
        while (frame < totalFrames && source + info.BlockSize * info.Channels <= adx.Length)
        {
            for (int channel = 0; channel < info.Channels; channel++)
            {
                int block = source + channel * info.BlockSize;
                int scale = ReadUInt16BE(adx, block);
                for (int i = 0; i < samplesPerBlock && frame + i < totalFrames; i++)
                {
                    int packed = adx[block + 2 + i / 2];
                    int nibble = (i & 1) == 0 ? packed >> 4 : packed & 0x0F;
                    if (nibble >= 8) nibble -= 16;
                    long prediction = (long)coefficient1 * history1[channel] + (long)coefficient2 * history2[channel];
                    int sample = (int)(((long)nibble * scale * coefficientScale + prediction) >> coefficientBits);
                    sample = Math.Clamp(sample, short.MinValue, short.MaxValue);
                    history2[channel] = history1[channel]; history1[channel] = sample;
                    output[(frame + i) * info.Channels + channel] = (short)sample;
                }
            }
            source += info.BlockSize * info.Channels;
            frame += samplesPerBlock;
        }
        if (frame < totalFrames) throw new InvalidDataException("Stream ADX terminou antes da quantidade de amostras declarada.");
        return output;
    }

    private sealed record WaveInput(int SampleRate, int Channels, short[] Samples);

    private static WaveInput ReadWave(byte[] data)
    {
        if (data.Length < 44 || !data.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !data.AsSpan(8, 4).SequenceEqual("WAVE"u8))
            throw new InvalidDataException("Use um arquivo WAV PCM de 16 bits.");
        int position = 12, channels = 0, sampleRate = 0, bits = 0; ushort format = 0; ReadOnlySpan<byte> pcm = default;
        while (position + 8 <= data.Length)
        {
            int length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(position + 4, 4));
            if (length < 0 || position + 8L + length > data.Length) throw new InvalidDataException("WAV truncado ou com chunk inválido.");
            ReadOnlySpan<byte> chunk = data.AsSpan(position + 8, length);
            if (data.AsSpan(position, 4).SequenceEqual("fmt "u8) && length >= 16)
            {
                format = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(chunk);
                channels = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(chunk[2..]);
                sampleRate = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(chunk[4..]);
                bits = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(chunk[14..]);
            }
            else if (data.AsSpan(position, 4).SequenceEqual("data"u8)) pcm = chunk;
            position += 8 + length + (length & 1);
        }
        if (format != 1 || bits != 16 || channels is < 1 or > 2 || sampleRate <= 0 || pcm.IsEmpty || pcm.Length % (channels * 2) != 0)
            throw new InvalidDataException("O WAV deve ser PCM 16-bit mono ou estéreo.");
        short[] samples = new short[pcm.Length / 2];
        for (int i = 0; i < samples.Length; i++) samples[i] = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm.Slice(i * 2, 2));
        return new WaveInput(sampleRate, channels, samples);
    }

    private static short[] ConvertChannels(short[] source, int sourceChannels, int targetChannels)
    {
        if (sourceChannels == targetChannels) return source;
        int frames = source.Length / sourceChannels;
        short[] result = new short[frames * targetChannels];
        if (sourceChannels == 1 && targetChannels == 2)
            for (int i = 0; i < frames; i++) result[i * 2] = result[i * 2 + 1] = source[i];
        else if (sourceChannels == 2 && targetChannels == 1)
            for (int i = 0; i < frames; i++) result[i] = (short)(((int)source[i * 2] + source[i * 2 + 1]) / 2);
        else throw new InvalidDataException("Quantidade de canais não suportada pelo ADX desta cutscene.");
        return result;
    }

    private static byte[] EncodeAdx(short[] pcm, int sampleRate, int channels, int highPassFrequency, byte[] templateAdx)
    {
        int dataOffset = templateAdx.Length >= 4 ? ReadUInt16BE(templateAdx, 2) + 4 : 0x120;
        if (dataOffset < 20 || dataOffset > templateAdx.Length) dataOffset = 0x120;
        int frames = pcm.Length / channels, blocks = (frames + 31) / 32;
        byte[] result = new byte[dataOffset + blocks * 18 * channels + 18];
        templateAdx.AsSpan(0, Math.Min(dataOffset, templateAdx.Length)).CopyTo(result);
        result[0] = 0x80; result[1] = 0; PutUInt16BE(result, 2, dataOffset - 4);
        result[4] = 3; result[5] = 18; result[6] = 4; result[7] = checked((byte)channels);
        PutUInt32BE(result, 8, checked((uint)sampleRate)); PutUInt32BE(result, 12, checked((uint)frames)); PutUInt16BE(result, 16, highPassFrequency); result[18] = 4;

        double a = Math.Sqrt(2.0) - Math.Cos(2.0 * Math.PI * highPassFrequency / sampleRate), b = Math.Sqrt(2.0) - 1.0;
        double c = (a - Math.Sqrt((a + b) * (a - b))) / b;
        int coefficient1 = (int)Math.Round(c * 8192.0), coefficient2 = (int)Math.Round(-(c * c) * 4096.0);
        int[] history1 = new int[channels], history2 = new int[channels]; int destination = dataOffset;
        for (int blockIndex = 0; blockIndex < blocks; blockIndex++)
        {
            for (int channel = 0; channel < channels; channel++)
            {
                int probe1 = history1[channel], probe2 = history2[channel], largest = 0;
                for (int i = 0; i < 32; i++)
                {
                    int frame = blockIndex * 32 + i, target = frame < frames ? pcm[frame * channels + channel] : 0;
                    int predicted = (coefficient1 * probe1 + coefficient2 * probe2) >> 12;
                    largest = Math.Max(largest, Math.Abs(target - predicted)); probe2 = probe1; probe1 = target;
                }
                int scale = Math.Clamp((largest + 6) / 7, 1, 0x7FFF); PutUInt16BE(result, destination, scale);
                for (int i = 0; i < 32; i++)
                {
                    int frame = blockIndex * 32 + i, target = frame < frames ? pcm[frame * channels + channel] : 0;
                    int predicted = (coefficient1 * history1[channel] + coefficient2 * history2[channel]) >> 12;
                    int nibble = Math.Clamp((int)Math.Round((target - predicted) / (double)scale, MidpointRounding.AwayFromZero), -8, 7);
                    int decoded = Math.Clamp(predicted + nibble * scale, short.MinValue, short.MaxValue);
                    history2[channel] = history1[channel]; history1[channel] = decoded;
                    int packedAt = destination + 2 + i / 2;
                    if ((i & 1) == 0) result[packedAt] = (byte)((nibble & 15) << 4); else result[packedAt] |= (byte)(nibble & 15);
                }
                destination += 18;
            }
        }
        result[destination] = 0x80; result[destination + 1] = 0x01;
        return result;
    }

    private static byte[] CollectStream(string path, byte streamId)
    {
        using var output = new MemoryStream(); using FileStream input = File.OpenRead(path);
        WalkPackets(input, (id, payload) => { if (id == streamId) output.Write(payload); }); return output.ToArray();
    }

    private static int CountStartCodes(ReadOnlySpan<byte> data, byte id)
    {
        int count = 0; for (int i = 0; i + 3 < data.Length; i++) if (HasStartCode(data, i, id)) { count++; i += 3; } return count;
    }

    private static void WriteSfd(string templatePath, byte[] video, byte[] audio, TimeSpan videoDuration, TimeSpan audioDuration, string outputPath)
    {
        byte[] template = File.ReadAllBytes(templatePath); int prefixSectors = FindMediaStartSector(template);
        string full = Path.GetFullPath(outputPath); Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        using FileStream output = File.Create(full);
        output.Write(template, 0, prefixSectors * SectorSize);
        const int payloadCapacity = 2017; int videoOffset = 0, audioOffset = 0, videoPacket = 0, audioPacket = 0;
        int videoPackets = (video.Length + payloadCapacity - 1) / payloadCapacity, audioPackets = (audio.Length + payloadCapacity - 1) / payloadCapacity;
        long scrStep = prefixSectors > 1 ? Math.Max(1, ReadScr(template, SectorSize + 4) - ReadScr(template, 4)) : 399;
        long scr = prefixSectors > 0 ? ReadScr(template, (prefixSectors - 1) * SectorSize + 4) + scrStep : 0;
        while (videoOffset < video.Length || audioOffset < audio.Length)
        {
            double videoTime = videoOffset < video.Length ? videoDuration.TotalSeconds * videoPacket / Math.Max(1, videoPackets) : double.PositiveInfinity;
            double audioTime = audioOffset < audio.Length ? audioDuration.TotalSeconds * audioPacket / Math.Max(1, audioPackets) : double.PositiveInfinity;
            bool useVideo = videoTime <= audioTime; byte id = useVideo ? (byte)0xE0 : (byte)0xC0;
            byte[] source = useVideo ? video : audio; int offset = useVideo ? videoOffset : audioOffset;
            int count = Math.Min(payloadCapacity, source.Length - offset); double seconds = useVideo ? videoTime : audioTime;
            WriteMediaSector(output, id, source.AsSpan(offset, count), scr, checked((long)Math.Round(seconds * 90000.0)), template.AsSpan(0, SectorSize)); scr += scrStep;
            if (useVideo) { videoOffset += count; videoPacket++; } else { audioOffset += count; audioPacket++; }
        }
        byte[] end = new byte[SectorSize]; Array.Fill(end, (byte)0xFF); end[0] = 0; end[1] = 0; end[2] = 1; end[3] = 0xB9; output.Write(end);
    }

    private static int FindMediaStartSector(byte[] template)
    {
        int sectors = template.Length / SectorSize;
        for (int i = 0; i < sectors; i++)
        {
            int p = i * SectorSize + 12;
            if (p + 4 <= template.Length && template[p] == 0 && template[p + 1] == 0 && template[p + 2] == 1 && template[p + 3] is 0xC0 or 0xE0) return i;
        }
        throw new InvalidDataException("O SFD modelo não contém setores de mídia.");
    }

    private static void WriteMediaSector(Stream output, byte streamId, ReadOnlySpan<byte> payload, long scr, long pts, ReadOnlySpan<byte> templateSector)
    {
        byte[] sector = new byte[SectorSize]; Array.Fill(sector, (byte)0xFF);
        sector[0] = 0; sector[1] = 0; sector[2] = 1; sector[3] = 0xBA; WriteScr(sector, 4, scr);
        templateSector.Slice(9, 3).CopyTo(sector.AsSpan(9));
        int p = 12; sector[p] = 0; sector[p + 1] = 0; sector[p + 2] = 1; sector[p + 3] = streamId;
        PutUInt16BE(sector, p + 4, 7 + payload.Length); sector[p + 6] = streamId == 0xE0 ? (byte)0x60 : (byte)0x40; sector[p + 7] = streamId == 0xE0 ? (byte)0x2E : (byte)0x04;
        WritePts(sector, p + 8, pts); payload.CopyTo(sector.AsSpan(p + 13)); p += 13 + payload.Length;
        int remaining = SectorSize - p;
        if (remaining >= 6) { sector[p] = 0; sector[p + 1] = 0; sector[p + 2] = 1; sector[p + 3] = 0xBE; PutUInt16BE(sector, p + 4, remaining - 6); }
        output.Write(sector);
    }

    private static void WriteScr(byte[] data, int p, long value)
    {
        ulong v = (ulong)Math.Max(0, value) & 0x1FFFFFFFFUL;
        data[p] = (byte)(0x20 | ((v >> 29) & 0x0E) | 1); data[p + 1] = (byte)(v >> 22); data[p + 2] = (byte)(((v >> 14) & 0xFE) | 1); data[p + 3] = (byte)(v >> 7); data[p + 4] = (byte)(((v << 1) & 0xFE) | 1);
    }

    private static long ReadScr(ReadOnlySpan<byte> data, int p) =>
        ((long)((data[p] >> 1) & 7) << 30) | ((long)data[p + 1] << 22) | ((long)((data[p + 2] >> 1) & 0x7F) << 15) | ((long)data[p + 3] << 7) | ((data[p + 4] >> 1) & 0x7F);

    private static void WritePts(byte[] data, int p, long value)
    {
        ulong v = (ulong)Math.Max(0, value) & 0x1FFFFFFFFUL;
        data[p] = (byte)(0x20 | ((v >> 29) & 0x0E) | 1); data[p + 1] = (byte)(v >> 22); data[p + 2] = (byte)(((v >> 14) & 0xFE) | 1); data[p + 3] = (byte)(v >> 7); data[p + 4] = (byte)(((v << 1) & 0xFE) | 1);
    }

    private static void WalkPackets(Stream input, PacketVisitor visit)
    {
        byte[] sector = new byte[SectorSize];
        while (ReadExactlyOrEnd(input, sector))
        {
            if (HasStartCode(sector, 0, 0xB9)) continue;
            if (!HasStartCode(sector, 0, 0xBA))
                throw new InvalidDataException($"SFD inválido: pack header ausente no setor {(input.Position / SectorSize) - 1}.");

            int position = 12; // MPEG-1 pack header is 12 bytes including its start code.
            while (position + 6 <= sector.Length && sector[position] == 0 && sector[position + 1] == 0 && sector[position + 2] == 1)
            {
                byte streamId = sector[position + 3];
                if (streamId == 0xB9) break;
                int packetLength = ReadUInt16BE(sector, position + 4);
                int packetEnd = position + 6 + packetLength;
                if (packetEnd > sector.Length) throw new InvalidDataException("SFD inválido: pacote PES ultrapassa o setor.");
                int payloadStart = GetPayloadStart(sector, position, packetEnd, streamId);
                if (payloadStart <= packetEnd) visit(streamId, sector.AsSpan(payloadStart, packetEnd - payloadStart));
                position = packetEnd;
            }
        }
    }

    private static int GetPayloadStart(byte[] data, int packetStart, int packetEnd, byte streamId)
    {
        int p = packetStart + 6;
        if (streamId is 0xBB or 0xBC or 0xBE or 0xBF) return p;
        while (p < packetEnd && data[p] == 0xFF) p++;
        if (p + 1 < packetEnd && (data[p] & 0xC0) == 0x40) p += 2; // STD buffer field.
        if (p >= packetEnd) return packetEnd;
        int marker = data[p] & 0xF0;
        if (marker == 0x20) p += 5;       // PTS
        else if (marker == 0x30) p += 10; // PTS + DTS
        else if (data[p] == 0x0F) p++;
        else throw new InvalidDataException("SFD inválido: cabeçalho PES MPEG-1 desconhecido.");
        return Math.Min(p, packetEnd);
    }

    private static SfdVideoInfo? TryReadVideo(ReadOnlySpan<byte> payload)
    {
        for (int i = 0; i + 11 < payload.Length; i++)
        {
            if (!HasStartCode(payload, i, 0xB3)) continue;
            int width = (payload[i + 4] << 4) | (payload[i + 5] >> 4);
            int height = ((payload[i + 5] & 0x0F) << 8) | payload[i + 6];
            int frameRateCode = payload[i + 7] & 0x0F;
            int bitRateValue = (payload[i + 8] << 10) | (payload[i + 9] << 2) | (payload[i + 10] >> 6);
            double fps = frameRateCode < FrameRates.Length ? FrameRates[frameRateCode] : 0;
            if (width > 0 && height > 0 && fps > 0)
                return new SfdVideoInfo(width, height, fps, bitRateValue == 0x3FFFF ? 0 : bitRateValue * 400);
        }
        return null;
    }

    private static SfdAudioInfo? TryReadAdx(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 20 || payload[0] != 0x80 || payload[1] != 0x00) return null;
        int encoding = payload[4], blockSize = payload[5], bits = payload[6], channels = payload[7];
        int sampleRate = checked((int)ReadUInt32BE(payload, 8));
        uint sampleCount = ReadUInt32BE(payload, 12);
        int highPass = ReadUInt16BE(payload, 16);
        int version = payload[18];
        if (blockSize <= 0 || channels <= 0 || sampleRate <= 0) return null;
        return new SfdAudioInfo(encoding, blockSize, bits, channels, sampleRate, sampleCount, highPass, version);
    }

    private static string? TryReadStreamName(ReadOnlySpan<byte> payload)
    {
        ReadOnlySpan<byte> signature = "SofdecStream"u8;
        int signatureOffset = payload.IndexOf(signature);
        if (signatureOffset < 0) return null;
        int nameOffset = signatureOffset + 32;
        if (nameOffset >= payload.Length) return null;
        int length = 0;
        while (nameOffset + length < payload.Length && length < 16 && payload[nameOffset + length] is >= 0x20 and <= 0x7E) length++;
        string value = System.Text.Encoding.ASCII.GetString(payload.Slice(nameOffset, length)).Trim();
        int separator = value.IndexOf(' ');
        if (separator >= 0) value = value[..separator];
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool ReadExactlyOrEnd(Stream input, byte[] buffer)
    {
        int read = 0;
        while (read < buffer.Length)
        {
            int count = input.Read(buffer, read, buffer.Length - read);
            if (count == 0)
            {
                if (read == 0) return false;
                throw new EndOfStreamException("Último setor SFD incompleto.");
            }
            read += count;
        }
        return true;
    }

    private static bool HasStartCode(ReadOnlySpan<byte> data, int offset, byte id) =>
        offset + 3 < data.Length && data[offset] == 0 && data[offset + 1] == 0 && data[offset + 2] == 1 && data[offset + 3] == id;

    private static int ReadUInt16BE(ReadOnlySpan<byte> data, int offset) => (data[offset] << 8) | data[offset + 1];
    private static uint ReadUInt32BE(ReadOnlySpan<byte> data, int offset) =>
        ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    private static void PutUInt16BE(Span<byte> data, int offset, int value)
    {
        data[offset] = (byte)(value >> 8); data[offset + 1] = (byte)value;
    }
    private static void PutUInt32BE(Span<byte> data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24); data[offset + 1] = (byte)(value >> 16); data[offset + 2] = (byte)(value >> 8); data[offset + 3] = (byte)value;
    }
}

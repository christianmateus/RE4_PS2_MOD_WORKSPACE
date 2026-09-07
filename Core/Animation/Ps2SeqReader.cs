using System.Buffers.Binary;

namespace RE4_PS2_MOD_WORKSPACE.Core.Animation;

/// <summary>Minimal SEQ metadata used by the experimental FCV preview.</summary>
public static class Ps2SeqReader
{
    public static int ReadFrameCount(string path)
    {
        using FileStream stream=File.OpenRead(path);
        Span<byte> header=stackalloc byte[4];
        if(stream.Read(header)!=header.Length) throw new InvalidDataException("SEQ muito pequeno.");
        uint count=BinaryPrimitives.ReadUInt32LittleEndian(header);
        if(count==0 || count>1_000_000) throw new InvalidDataException($"Quantidade de frames inválida no SEQ: {count}.");
        return checked((int)count);
    }

    public static string? FindFollowingSeq(string fcvPath)
    {
        string? directory=Path.GetDirectoryName(fcvPath);
        string stem=Path.GetFileNameWithoutExtension(fcvPath);
        int separator=stem.LastIndexOf('_');
        if(string.IsNullOrWhiteSpace(directory) || separator<0 || !int.TryParse(stem[(separator+1)..],out int index)) return null;
        string prefix=stem[..(separator+1)];
        foreach(string extension in new[]{".SEQ",".seq"})
        {
            string candidate=Path.Combine(directory,$"{prefix}{index+1:D3}{extension}");
            if(File.Exists(candidate)) return candidate;
        }
        return null;
    }
}

using RE4_PS2_MOD_WORKSPACE.Core.Textures;

namespace RE4_PS2_MOD_WORKSPACE.Core.Messages;

public sealed class FntFont : IDisposable
{
    public const int HeaderSize = 0x20;
    public string SourcePath { get; }
    public Bitmap Atlas { get; }
    public int CellSize { get; } = 32;

    private FntFont(string path, Bitmap atlas) { SourcePath = path; Atlas = atlas; }

    public static FntFont Load(string path)
    {
        byte[] file = File.ReadAllBytes(path);
        if (file.Length < HeaderSize + 0x40) throw new InvalidDataException("Arquivo FNT incompleto.");
        uint tplOffset = BitConverter.ToUInt32(file, 0);
        if (tplOffset != HeaderSize) throw new InvalidDataException($"Offset TPL inesperado no FNT: 0x{tplOffset:X}.");
        byte[] tplData = file.AsSpan(HeaderSize).ToArray();
        using var stream = new MemoryStream(tplData, writable: false);
        using var reader = new BinaryReader(stream);
        var texture = new TplReader().ReadTexture(reader, 0);
        if (texture.width != 1024 || texture.height != 256) throw new InvalidDataException($"Atlas FNT inesperado: {texture.width}×{texture.height}.");
        stream.Position = 0;
        Bitmap atlas = new TextureDecoder().Decode(texture, reader);
        // TPL decoding follows the regular texture convention. Font UVs use the opposite Y origin.
        atlas.RotateFlip(RotateFlipType.RotateNoneFlipY);
        return new FntFont(path, atlas);
    }

    public Rectangle GetGlyphRectangle(ushort code)
    {
        int index = code - 0x80;
        if (index < 0 || index >= 256) return Rectangle.Empty;
        return new Rectangle((index % 32) * CellSize, (index / 32) * CellSize, CellSize, CellSize);
    }

    public void Dispose() => Atlas.Dispose();
}

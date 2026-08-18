using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures;

public sealed class TextureWorkspaceService
{
    private readonly TplReader reader = new();
    private readonly TextureDecoder decoder = new();
    private readonly TextureEncoder encoder = new();
    private readonly TplWriter writer;
    private readonly MipmapService mipmaps;

    public TextureWorkspaceService()
    {
        writer = new TplWriter(reader);
        mipmaps = new MipmapService(reader, writer, encoder);
    }

    public IReadOnlyList<TextureInfo> ReadCatalog(string tplPath)
    {
        uint count = reader.ReadTextureCount(tplPath);
        var result = new List<TextureInfo>(checked((int)count));
        for (int i = 0; i < count; i++) result.Add(ToInfo(i, reader.ReadTexture(tplPath, i)));
        return result;
    }

    public TextureInfo ReadInfo(string tplPath, int index) => ToInfo(index, reader.ReadTexture(tplPath, index));

    public Bitmap Decode(string tplPath, int index)
    {
        var texture = reader.ReadTexture(tplPath, index);
        using var stream = File.Open(tplPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var binary = new BinaryReader(stream);
        return decoder.Decode(texture, binary);
    }

    public Bitmap DecodeMip(string tplPath, int index, int mipIndex) => mipmaps.DecodeMip(tplPath, index, mipIndex);

    public Bitmap CreateThumbnail(string tplPath, int index, int size)
    {
        using Bitmap source = Decode(tplPath, index);
        var target = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(target);
        g.Clear(Color.FromArgb(24, 26, 31));
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        float scale = Math.Min((float)size / source.Width, (float)size / source.Height);
        int w = Math.Max(1, (int)Math.Round(source.Width * scale));
        int h = Math.Max(1, (int)Math.Round(source.Height * scale));
        int x = (size - w) / 2;
        int y = (size - h) / 2;
        g.DrawImage(source, new Rectangle(x, y, w, h));
        return target;
    }

    public void ExportPng(string tplPath, int index, string outputPath)
    {
        using Bitmap bitmap = Decode(tplPath, index);
        string? dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        bitmap.Save(outputPath, ImageFormat.Png);
    }

    public TextureInfo ReplaceFromImage(string tplPath, int index, string imagePath)
    {
        using Image source = Image.FromFile(imagePath);
        using Bitmap bitmap = new Bitmap(source);
        return ReplaceFromBitmap(tplPath, index, bitmap, true);
    }

    public TextureInfo ReplaceFromBitmap(string tplPath, int index, Bitmap source, bool preserveDimensions)
    {
        var target = reader.ReadTexture(tplPath, index);
        using Bitmap prepared = preserveDimensions && (source.Width != target.width || source.Height != target.height)
            ? Resize(source, target.width, target.height)
            : new Bitmap(source);
        TPLDefinition.TPL replacement;

        if (target.bitDepth == 0x08 || target.bitDepth == 0x09)
        {
            int colors = target.bitDepth == 0x08 ? 16 : 256;
            replacement = encoder.EncodeImage(prepared, colors, target.interlace);
        }
        else if (target.bitDepth == 0x06)
        {
            if (target.mipmapCount > 0) throw new NotSupportedException("Substituição nativa de textura 32-bit com mipmaps ainda não é suportada.");
            replacement = target;
            replacement.header = (byte[])target.header.Clone();
            replacement.width = checked((ushort)prepared.Width);
            replacement.height = checked((ushort)prepared.Height);
            replacement.pixels = Encode32Bit(prepared);
            replacement.palette = Array.Empty<byte>();
            PatchHeaderDimensions(replacement.header, replacement.width, replacement.height);
        }
        else throw new NotSupportedException($"Bit depth 0x{target.bitDepth:X} ainda não é suportado para substituição nativa.");

        writer.ReplaceTexture(tplPath, index, replacement);
        if (target.mipmapCount > 0 && (target.bitDepth == 0x08 || target.bitDepth == 0x09)) mipmaps.Regenerate(tplPath, index);
        return ReadInfo(tplPath, index);
    }

    public TextureInfo ConvertBitDepth(string tplPath, int index, int colorCount)
    {
        if (colorCount != 16 && colorCount != 256) throw new ArgumentOutOfRangeException(nameof(colorCount));
        var target = reader.ReadTexture(tplPath, index);
        using Bitmap source = Decode(tplPath, index);
        var replacement = encoder.EncodeImage(source, colorCount, target.interlace);
        writer.ReplaceTexture(tplPath, index, replacement);
        if (target.mipmapCount > 0) mipmaps.Regenerate(tplPath, index);
        return ReadInfo(tplPath, index);
    }

    public void RegenerateMipmaps(string tplPath, int index) => mipmaps.Regenerate(tplPath, index);
    public void AddMipmaps(string tplPath, int index) => mipmaps.AddMipmaps(tplPath, index);
    public void RemoveMipmaps(string tplPath, int index) => mipmaps.RemoveMipmaps(tplPath, index);
    public void ReplaceMip(string tplPath, int index, int mipIndex, Image image) => mipmaps.ReplaceMip(tplPath, index, mipIndex, image);
    public void ReplaceMainAndRegenerate(string tplPath, int index, Image image) => mipmaps.ReplaceMainAndRegenerate(tplPath, index, image);

    private static Bitmap Resize(Image source, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(bitmap);
        g.CompositingMode = CompositingMode.SourceCopy;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(source, new Rectangle(0, 0, width, height));
        return bitmap;
    }

    private static byte[] Encode32Bit(Bitmap bitmap)
    {
        byte[] output = new byte[checked(bitmap.Width * bitmap.Height * 4)];
        int p = 0;
        for (int y = bitmap.Height - 1; y >= 0; y--)
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color c = bitmap.GetPixel(x, y);
                output[p++] = c.R; output[p++] = c.G; output[p++] = c.B; output[p++] = c.A;
            }
        return output;
    }

    private static void PatchHeaderDimensions(byte[] header, ushort width, ushort height)
    {
        Buffer.BlockCopy(BitConverter.GetBytes(width), 0, header, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(height), 0, header, 2, 2);
    }

    private static TextureInfo ToInfo(int index, TPLDefinition.TPL texture) => new(index, texture.width, texture.height, texture.bitDepth, texture.interlace, texture.mipmapCount, BitDepthName(texture.bitDepth), InterlaceName(texture.interlace));
    private static string BitDepthName(ushort value) => value switch { 0x08 => "4-bit", 0x09 => "8-bit", 0x06 => "32-bit", _ => $"0x{value:X}" };
    private static string InterlaceName(ushort value) => value switch { 0 => "BGRA", 1 => "BGRA Inverted", 2 => "PS2", 3 => "PS2 Inverted", _ => $"0x{value:X}" };
}

public sealed record TextureInfo(int Index, int Width, int Height, ushort BitDepth, ushort Interlace, ushort MipmapCount, string BitDepthName, string InterlaceName);

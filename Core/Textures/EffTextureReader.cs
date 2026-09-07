using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures;

/// <summary>Reads the TPL-compatible texture records embedded in an ETM EFF resource.</summary>
public sealed class EffTextureReader
{
    private const int WrapperSize = 0x10;
    private const int TextureHeaderSize = 0x30;

    public IReadOnlyList<TplModel> ReadTextures(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var textures = new List<(int Offset, TplModel Texture)>();

        // EFF records are 16-byte aligned. The location of the containing section varies
        // between EFF variants, so recognize the strongly-typed texture record itself.
        for (int recordOffset = 0; recordOffset + WrapperSize + TextureHeaderSize <= data.Length; recordOffset += 0x10)
        {
            int headerOffset = recordOffset + WrapperSize;
            ushort width = ReadUInt16(data, headerOffset);
            ushort height = ReadUInt16(data, headerOffset + 2);
            ushort bitDepth = ReadUInt16(data, headerOffset + 4);
            ushort interlace = ReadUInt16(data, headerOffset + 6);
            uint relativePixels = ReadUInt32(data, headerOffset + 0x20);
            uint relativePalette = ReadUInt32(data, headerOffset + 0x24);

            if (!IsDimension(width) || !IsDimension(height) || bitDepth is not (0x06 or 0x08 or 0x09) || interlace > 3)
                continue;

            int pixelLength;
            int paletteLength;
            try
            {
                pixelLength = TplReader.GetPixelDataLength(width, height, bitDepth);
                paletteLength = TplReader.GetPaletteLength(bitDepth);
            }
            catch (OverflowException) { continue; }

            if (!Fits(data, recordOffset, relativePixels, pixelLength) ||
                (paletteLength > 0 && !Fits(data, recordOffset, relativePalette, paletteLength)))
                continue;

            var texture = new TplModel
            {
                tplCount = 1,
                width = width,
                height = height,
                bitDepth = bitDepth,
                interlace = interlace,
                zPriority = ReadUInt16(data, headerOffset + 8),
                mipmapCount = ReadUInt16(data, headerOffset + 0x0A),
                scale = ReadUInt16(data, headerOffset + 0x0C),
                unused2 = ReadUInt16(data, headerOffset + 0x0E),
                mipmapOffset1 = ToAbsolute(recordOffset, ReadUInt32(data, headerOffset + 0x10)),
                mipmapOffset2 = ToAbsolute(recordOffset, ReadUInt32(data, headerOffset + 0x14)),
                unknown1 = ReadUInt32(data, headerOffset + 0x18),
                unknown2 = ReadUInt32(data, headerOffset + 0x1C),
                pixelsOffset = ToAbsolute(recordOffset, relativePixels),
                paletteOffset = ToAbsolute(recordOffset, relativePalette),
                unused3 = data[headerOffset + 0x28],
                config1 = data[headerOffset + 0x29],
                config2 = data[headerOffset + 0x2A],
                config3 = ReadUInt16(data, headerOffset + 0x2B),
                unused4 = data[headerOffset + 0x2D],
                unused5 = data[headerOffset + 0x2E],
                endTag = data[headerOffset + 0x2F],
                header = data.AsSpan(headerOffset, TextureHeaderSize).ToArray(),
                pixels = data.AsSpan(checked(recordOffset + (int)relativePixels), pixelLength).ToArray(),
                palette = paletteLength == 0 ? Array.Empty<byte>() : data.AsSpan(checked(recordOffset + (int)relativePalette), paletteLength).ToArray(),
                mipmapHeader1 = Array.Empty<byte>(), mipmapHeader2 = Array.Empty<byte>(),
                mipmapPixels1 = Array.Empty<byte>(), mipmapPixels2 = Array.Empty<byte>()
            };
            textures.Add((recordOffset, texture));
        }

        return textures.OrderBy(item => item.Offset).Select(item => item.Texture).ToArray();
    }

    private static bool IsDimension(ushort value) => value is >= 4 and <= 4096 && (value & (value - 1)) == 0;
    private static bool Fits(byte[] data, int recordOffset, uint relativeOffset, int length) =>
        relativeOffset >= WrapperSize + TextureHeaderSize &&
        (long)recordOffset + relativeOffset + length <= data.Length;
    private static uint ToAbsolute(int recordOffset, uint relativeOffset) => relativeOffset == 0 ? 0 : checked((uint)recordOffset + relativeOffset);
    private static ushort ReadUInt16(byte[] data, int offset) => BitConverter.ToUInt16(data, offset);
    private static uint ReadUInt32(byte[] data, int offset) => BitConverter.ToUInt32(data, offset);
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    /// <summary>
    /// Encodes regular images into RE4 PS2 indexed TPL textures entirely in memory.
    /// v1.1.4 encodes palette indices and the PS2 CLUT directly from ARGB pixels so
    /// transparency is preserved when changing between 4-bit and 8-bit color depth.
    /// </summary>
    public sealed partial class TextureEncoder
    {
        public TplModel EncodeImage(Image image, int colorCount)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (colorCount != 16 && colorCount != 256)
                throw new ArgumentOutOfRangeException(nameof(colorCount), "TPL indexed textures support 16 or 256 colors.");
            if (image.Width <= 0 || image.Height <= 0 || image.Width > UInt16.MaxValue || image.Height > UInt16.MaxValue)
                throw new InvalidDataException("Image dimensions are not supported by the TPL format.");

            using (Bitmap bitmap = ToArgbBitmap(image))
            {
                int[] argbPixels = ReadArgbPixels(bitmap);
                PaletteQuantizationResult quantized = QuantizeArgb(argbPixels, colorCount);
                ushort bitDepth = colorCount == 16 ? (ushort)0x08 : (ushort)0x09;
                byte[] pixels = BuildTplIndices(quantized.Indices, bitmap.Width, bitmap.Height, bitDepth);
                byte[] palette = BuildTplPalette(quantized.Palette, bitDepth);
                return CreateTpl((ushort)bitmap.Width, (ushort)bitmap.Height, bitDepth, pixels, palette);
            }
        }

        /// <summary>
        /// Encodes an indexed TPL texture and preserves the requested RE4 interlace layout.
        /// Values 0/1 use the linear BGRA family; values 2/3 use the PS2 swizzled family.
        /// The Normal/Inverted flag is preserved exactly in the texture header.
        /// </summary>
        public TplModel EncodeImage(Image image, int colorCount, ushort interlace)
        {
            if (interlace > 3)
                throw new ArgumentOutOfRangeException(nameof(interlace), "Supported interlace values are 0, 1, 2 and 3.");

            TplModel encoded = EncodeImage(image, colorCount);
            if (interlace >= 2)
            {
                encoded = new InterlaceConverter().ConvertFamily(encoded, true);
            }

            // ConvertFamily preserves the source normal/inverted bit. Since a newly encoded
            // texture starts at 0, explicitly restore the exact destination flag (0..3).
            encoded.interlace = interlace;
            UpdateInterlaceInHeader(encoded);
            return encoded;
        }

        private static void UpdateInterlaceInHeader(TplModel tpl)
        {
            if (tpl.header == null || tpl.header.Length != 0x30)
                tpl.header = BuildHeader(tpl);
            tpl.header[0x06] = (byte)(tpl.interlace & 0xFF);
            tpl.header[0x07] = (byte)(tpl.interlace >> 8);
        }

        /// <summary>
        /// Encodes an image using an existing RE4 PS2 CLUT. No new palette is generated.
        /// This is required by mipmaps because RE4 stores the CLUT only on the parent texture.
        /// </summary>
        public TplModel EncodeImageWithPalette(Image image, ushort bitDepth, ushort interlace, byte[] tplPalette)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (bitDepth != 0x08 && bitDepth != 0x09)
                throw new NotSupportedException("Shared-palette encoding supports only 4-bit and 8-bit indexed textures.");
            if (interlace > 3)
                throw new ArgumentOutOfRangeException(nameof(interlace), "Supported interlace values are 0, 1, 2 and 3.");

            int expectedPaletteLength = TplReader.GetPaletteLength(bitDepth);
            if (tplPalette == null || tplPalette.Length < expectedPaletteLength)
                throw new InvalidDataException("The parent texture CLUT is missing or has an invalid size.");

            using (Bitmap bitmap = ToArgbBitmap(image))
            {
                Color[] palette = ReadLogicalTplPalette(tplPalette, bitDepth);
                int[] argbPixels = ReadArgbPixels(bitmap);
                byte[] indices = new byte[argbPixels.Length];
                for (int i = 0; i < argbPixels.Length; i++)
                    indices[i] = FindNearestPaletteIndex(Color.FromArgb(argbPixels[i]), palette);

                byte[] pixels = BuildTplIndices(indices, bitmap.Width, bitmap.Height, bitDepth);
                byte[] paletteCopy = new byte[expectedPaletteLength];
                Buffer.BlockCopy(tplPalette, 0, paletteCopy, 0, expectedPaletteLength);
                TplModel encoded = CreateTpl((ushort)bitmap.Width, (ushort)bitmap.Height, bitDepth, pixels, paletteCopy);

                if (interlace >= 2)
                    encoded = new InterlaceConverter().ConvertFamily(encoded, true);

                encoded.interlace = interlace;
                UpdateInterlaceInHeader(encoded);
                return encoded;
            }
        }

        private static Color[] ReadLogicalTplPalette(byte[] tplPalette, ushort bitDepth)
        {
            int colorCount = bitDepth == 0x08 ? 16 : 256;
            Color[] result = new Color[colorCount];
            for (int logicalIndex = 0; logicalIndex < colorCount; logicalIndex++)
            {
                int physicalIndex;
                if (bitDepth == 0x08)
                {
                    physicalIndex = logicalIndex < 8 ? logicalIndex : 16 + (logicalIndex - 8);
                }
                else
                {
                    int position = logicalIndex & 0x1F;
                    physicalIndex = logicalIndex;
                    if (position >= 8 && position < 16) physicalIndex += 8;
                    else if (position >= 16 && position < 24) physicalIndex -= 8;
                }

                int offset = physicalIndex * 4;
                int alpha = tplPalette[offset + 3] * 0xFF / 0x80;
                if (alpha > 255) alpha = 255;
                result[logicalIndex] = Color.FromArgb(alpha, tplPalette[offset], tplPalette[offset + 1], tplPalette[offset + 2]);
            }
            return result;
        }

        private static byte FindNearestPaletteIndex(Color color, Color[] palette)
        {
            int bestIndex = 0;
            long bestDistance = Int64.MaxValue;
            for (int i = 0; i < palette.Length; i++)
            {
                Color candidate = palette[i];
                long da = color.A - candidate.A;
                long dr = color.R - candidate.R;
                long dg = color.G - candidate.G;
                long db = color.B - candidate.B;
                // Alpha receives extra weight so cutout/translucent edges survive mip generation.
                long distance = da * da * 2L + dr * dr + dg * dg + db * db;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                    if (distance == 0) break;
                }
            }
            return checked((byte)bestIndex);
        }

        public TplModel EncodeIndexedBmp(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("BMP path is empty.", nameof(path));
            using (Image image = Image.FromFile(path))
            {
                int colorCount = Image.GetPixelFormatSize(image.PixelFormat) <= 4 ? 16 : 256;
                return EncodeImage(image, colorCount);
            }
        }

        public TplModel EncodeIndexedBmp(Stream bmpStream)
        {
            if (bmpStream == null) throw new ArgumentNullException(nameof(bmpStream));
            using (Image image = Image.FromStream(bmpStream, true, true))
            {
                int colorCount = Image.GetPixelFormatSize(image.PixelFormat) <= 4 ? 16 : 256;
                return EncodeImage(image, colorCount);
            }
        }

    }
}

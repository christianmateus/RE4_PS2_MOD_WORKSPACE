using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    public static class TplHelper
    {
        private static readonly int[,] Deinterlace4Pattern = new int[32, 2]
        {
            { 4, 0 }, { 12, 8 }, { 20, 16 }, { 28, 24 }, { 5, 1 }, { 13, 9 }, { 21, 17 }, { 29, 25 },
            { 6, 2 }, { 14, 10 }, { 22, 18 }, { 30, 26 }, { 7, 3 }, { 15, 11 }, { 23, 19 }, { 31, 27 },
            { 0, 4 }, { 8, 12 }, { 16, 20 }, { 24, 28 }, { 1, 5 }, { 9, 13 }, { 17, 21 }, { 25, 29 },
            { 2, 6 }, { 10, 14 }, { 18, 22 }, { 26, 30 }, { 3, 7 }, { 11, 15 }, { 19, 23 }, { 27, 31 }
        };

        private static readonly int[] Deinterlace8MapA =
        {
            0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15,
            4, 0, 12, 8, 5, 1, 13, 9, 6, 2, 14, 10, 7, 3, 15, 11
        };

        private static readonly int[] Deinterlace8MapB =
        {
            4, 0, 12, 8, 5, 1, 13, 9, 6, 2, 14, 10, 7, 3, 15, 11,
            0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15
        };

        public static Bitmap DecodeTextureToBitmap(TPLDefinition.TPL tpl, BinaryReader br)
        {
            if (br == null)
                throw new ArgumentNullException(nameof(br));
            if (tpl.width == 0 || tpl.height == 0)
                throw new InvalidDataException("Texture has an invalid zero width or height.");

            if (tpl.bitDepth == 0x8 && (tpl.interlace == 0x0 || tpl.interlace == 0x1))
                return Decode4BitLinear(tpl, br);

            if (tpl.bitDepth == 0x8 && (tpl.interlace == 0x2 || tpl.interlace == 0x3))
                return Decode4BitPs2(tpl, br);

            if (tpl.bitDepth == 0x9 && (tpl.interlace == 0x0 || tpl.interlace == 0x1))
                return Decode8BitLinear(tpl, br);

            if (tpl.bitDepth == 0x9 && (tpl.interlace == 0x2 || tpl.interlace == 0x3))
                return Decode8BitPs2(tpl, br);

            if (tpl.bitDepth == 0x6 && (tpl.interlace == 0x0 || tpl.interlace == 0x1))
                return Decode32BitLinear(tpl, br);

            throw new NotSupportedException($"BitDepth {tpl.bitDepth:X} and Interlace {tpl.interlace:X} not supported.");
        }

        private static Bitmap Decode4BitLinear(TPLDefinition.TPL tpl, BinaryReader br)
        {
            int pixelCount = checked(tpl.width * tpl.height);
            byte[] indices = ReadExact(br, tpl.pixelsOffset, (pixelCount + 1) / 2, "4-bit pixel data");
            Color[] colors = ReadPalette16(tpl, br);
            int[] pixels = new int[pixelCount];

            int dst = 0;
            for (int i = 0; i < indices.Length && dst < pixelCount; i++)
            {
                byte value = indices[i];
                int high = value & 0x0F;
                int low = value >> 4;

                // Preserve the original TPL nibble order: high nibble is the left pixel.
                pixels[dst++] = colors[high].ToArgb();
                if (dst < pixelCount)
                    pixels[dst++] = colors[low].ToArgb();
            }

            return CreateBitmapFlippedY(tpl.width, tpl.height, pixels);
        }

        private static Bitmap Decode8BitLinear(TPLDefinition.TPL tpl, BinaryReader br)
        {
            int pixelCount = checked(tpl.width * tpl.height);
            byte[] indices = ReadExact(br, tpl.pixelsOffset, pixelCount, "8-bit pixel data");
            Color[] colors = ReadPalette256(tpl, br);
            int[] pixels = new int[pixelCount];

            for (int i = 0; i < pixelCount; i++)
                pixels[i] = colors[indices[i]].ToArgb();

            return CreateBitmapFlippedY(tpl.width, tpl.height, pixels);
        }

        private static Bitmap Decode32BitLinear(TPLDefinition.TPL tpl, BinaryReader br)
        {
            int pixelCount = checked(tpl.width * tpl.height);
            byte[] colorBytes = ReadExact(br, tpl.pixelsOffset, checked(pixelCount * 4), "32-bit pixel data");
            int[] pixels = new int[pixelCount];

            int src = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                int r = colorBytes[src + 0];
                int g = colorBytes[src + 1];
                int b = colorBytes[src + 2];
                int a = colorBytes[src + 3];
                pixels[i] = unchecked((int)((uint)(a << 24) | (uint)(r << 16) | (uint)(g << 8) | (uint)b));
                src += 4;
            }

            return CreateBitmapFlippedY(tpl.width, tpl.height, pixels);
        }

        private static Bitmap Decode8BitPs2(TPLDefinition.TPL tpl, BinaryReader br)
        {
            int pixelCount = checked(tpl.width * tpl.height);
            byte[] indices = ReadExact(br, tpl.pixelsOffset, pixelCount, "8-bit PS2 pixel data");
            Color[] colors = ReadPalette256(tpl, br);
            int[] pixels = new int[pixelCount];

            int xCont = 0;
            int yCont = 0;
            bool flipX = false;

            for (int input = 0; input < indices.Length; input += 32)
            {
                int chunkLength = Math.Min(32, indices.Length - input);
                int[] map = flipX ? Deinterlace8MapB : Deinterlace8MapA;

                for (int i = 0; i < chunkLength; i++)
                {
                    int x = xCont + map[i];
                    int y = yCont + ((i & 1) == 0 ? 0 : 2);
                    SetPixelSafe(pixels, tpl.width, tpl.height, x, y, colors[indices[input + i]].ToArgb());
                }

                xCont += 16;
                if (xCont >= tpl.width)
                {
                    xCont = 0;
                    yCont++;
                    if ((yCont & 1) == 0)
                    {
                        yCont += 2;
                        flipX = !flipX;
                    }
                }
            }

            return CreateBitmapFlippedY(tpl.width, tpl.height, pixels);
        }

        private static Bitmap Decode4BitPs2(TPLDefinition.TPL tpl, BinaryReader br)
        {
            int pixelCount = checked(tpl.width * tpl.height);
            byte[] indices = ReadExact(br, tpl.pixelsOffset, (pixelCount + 1) / 2, "4-bit PS2 pixel data");
            Color[] colors = ReadPalette16(tpl, br);

            // The original PS2 4-bit deinterlacer writes to a transposed working surface.
            // Keep that layout for compatibility, but do it in memory instead of SetPixel().
            int workWidth = tpl.height;
            int workHeight = tpl.width;
            int[] workPixels = new int[checked(workWidth * workHeight)];

            int xCont = 0;
            int yCont = 0;
            int altCont = 0;
            int levels = 0;
            int maxLevel = Math.Max(1, tpl.width / 16);
            bool flipX = false;

            for (int input = 0; input < indices.Length; input += 32)
            {
                int chunkLength = Math.Min(32, indices.Length - input);
                for (int i = 0; i < chunkLength; i++)
                {
                    int value = indices[input + i];
                    int nibble1 = value >> 4;
                    int nibble2 = value & 0x0F;
                    int x1 = flipX ? Deinterlace4Pattern[i, 1] : Deinterlace4Pattern[i, 0];
                    int x2 = flipX ? Deinterlace4Pattern[i, 0] : Deinterlace4Pattern[i, 1];

                    SetPixelSafe(workPixels, workWidth, workHeight, xCont + x1, yCont + 2, colors[nibble1].ToArgb());
                    SetPixelSafe(workPixels, workWidth, workHeight, xCont + x2, yCont, colors[nibble2].ToArgb());
                }

                yCont += 16;
                levels++;
                if (levels == maxLevel)
                {
                    levels = 0;
                    altCont++;
                    if ((altCont & 1) == 0)
                    {
                        altCont += 2;
                        flipX = !flipX;
                    }

                    yCont = altCont;
                    if (altCont == 16)
                    {
                        flipX = false;
                        yCont = 0;
                        altCont = 0;
                        xCont += 32;
                    }
                }
            }

            int outputWidth = workWidth;
            int outputHeight = workHeight;
            int[] outputPixels = workPixels;

            // Preserve the existing 128x128 page reordering, but copy the blocks in memory.
            // This removes the old GetPixel/SetPixel nested loop bottleneck.
            if (tpl.width > 128 || tpl.height > 128)
            {
                outputWidth = tpl.width;
                outputHeight = tpl.height;
                outputPixels = Reorder4BitPages(workPixels, workWidth, workHeight, outputWidth, outputHeight);
            }

            return CreateBitmapFlippedY(outputWidth, outputHeight, outputPixels);
        }

        private static int[] Reorder4BitPages(int[] source, int sourceWidth, int sourceHeight, int destinationWidth, int destinationHeight)
        {
            int[] destination = new int[checked(destinationWidth * destinationHeight)];
            int blockColumns = Math.Max(1, (destinationWidth + 127) / 128);
            int blockRows = Math.Max(1, (destinationHeight + 127) / 128);
            int blockCount = checked(blockColumns * blockRows);

            for (int block = 0; block < blockCount; block++)
            {
                int destinationBlockX = block % blockColumns;
                int destinationBlockY = block / blockColumns;
                int sourceBlockX = destinationBlockY;
                int sourceBlockY = destinationBlockX;

                int destinationX = destinationBlockX * 128;
                int destinationY = destinationBlockY * 128;
                int sourceX = sourceBlockX * 128;
                int sourceY = sourceBlockY * 128;

                int copyWidth = Math.Min(128, Math.Min(destinationWidth - destinationX, sourceWidth - sourceX));
                int copyHeight = Math.Min(128, Math.Min(destinationHeight - destinationY, sourceHeight - sourceY));
                if (copyWidth <= 0 || copyHeight <= 0)
                    continue;

                for (int y = 0; y < copyHeight; y++)
                {
                    Array.Copy(
                        source, (sourceY + y) * sourceWidth + sourceX,
                        destination, (destinationY + y) * destinationWidth + destinationX,
                        copyWidth);
                }
            }

            return destination;
        }

        private static Color[] ReadPalette16(TPLDefinition.TPL tpl, BinaryReader br)
        {
            byte[] palette = ReadExact(br, tpl.paletteOffset, 0x80, "16-color palette");
            Color[] colors = new Color[16];
            int offset = 0;

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = ReadPs2Color(palette, offset);
                offset += 4;
                if (i == 7)
                    offset = 0x40;
            }

            return colors;
        }

        private static Color[] ReadPalette256(TPLDefinition.TPL tpl, BinaryReader br)
        {
            byte[] palette = ReadExact(br, tpl.paletteOffset, 0x400, "256-color palette");
            Color[] colors = new Color[256];

            for (int logicalIndex = 0; logicalIndex < colors.Length; logicalIndex++)
            {
                int physicalIndex = UnswizzleClut8Index(logicalIndex);
                colors[logicalIndex] = ReadPs2Color(palette, physicalIndex * 4);
            }

            return colors;
        }

        private static int UnswizzleClut8Index(int index)
        {
            // PS2 8-bit CLUT order swaps the middle two groups of eight entries in each 32-color block.
            int positionInBlock = index & 0x1F;
            if (positionInBlock >= 8 && positionInBlock < 16)
                return index + 8;
            if (positionInBlock >= 16 && positionInBlock < 24)
                return index - 8;
            return index;
        }

        private static Color ReadPs2Color(byte[] palette, int offset)
        {
            int alpha = palette[offset + 3] * 0xFF / 0x80;
            if (alpha > 255)
                alpha = 255;

            return Color.FromArgb(alpha, palette[offset], palette[offset + 1], palette[offset + 2]);
        }

        private static byte[] ReadExact(BinaryReader br, long offset, int count, string description)
        {
            if (offset < 0 || offset > br.BaseStream.Length)
                throw new InvalidDataException($"Invalid {description} offset: 0x{offset:X}.");
            if (count < 0 || offset + count > br.BaseStream.Length)
                throw new InvalidDataException($"{description} extends beyond the end of the TPL file.");

            br.BaseStream.Position = offset;
            byte[] data = br.ReadBytes(count);
            if (data.Length != count)
                throw new EndOfStreamException($"Could not read the complete {description}.");
            return data;
        }

        private static void SetPixelSafe(int[] pixels, int width, int height, int x, int y, int argb)
        {
            if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                return;
            pixels[y * width + x] = argb;
        }

        private static Bitmap CreateBitmapFlippedY(int width, int height, int[] sourcePixels)
        {
            int[] flipped = new int[sourcePixels.Length];
            for (int y = 0; y < height; y++)
            {
                Array.Copy(sourcePixels, y * width, flipped, (height - 1 - y) * width, width);
            }
            return CreateBitmap(width, height, flipped);
        }

        private static Bitmap CreateBitmap(int width, int height, int[] pixels)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Rectangle rectangle = new Rectangle(0, 0, width, height);
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                if (bitmapData.Stride == width * 4)
                {
                    Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                }
                else
                {
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr destination = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                        Marshal.Copy(pixels, y * width, destination, width);
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        public static byte FindPaletteIndex(Color color, Color[] palette)
        {
            for (byte i = 0; i < palette.Length; i++)
            {
                if (palette[i].ToArgb() == color.ToArgb())
                    return i;
            }
            throw new Exception("Cor do pixel não encontrada na paleta!");
        }
    }
}

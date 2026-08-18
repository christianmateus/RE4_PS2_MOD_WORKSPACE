using System;
using System.IO;
using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    /// <summary>
    /// Lossless pixel-layout converter for indexed RE4 PS2 TPL textures.
    /// It preserves the palette and changes only the pixel-index layout + interlace header.
    /// </summary>
    public sealed class InterlaceConverter
    {
        private static readonly int[,] Pattern4 = new int[32, 2]
        {
            { 4, 0 }, { 12, 8 }, { 20, 16 }, { 28, 24 }, { 5, 1 }, { 13, 9 }, { 21, 17 }, { 29, 25 },
            { 6, 2 }, { 14, 10 }, { 22, 18 }, { 30, 26 }, { 7, 3 }, { 15, 11 }, { 23, 19 }, { 31, 27 },
            { 0, 4 }, { 8, 12 }, { 16, 20 }, { 24, 28 }, { 1, 5 }, { 9, 13 }, { 17, 21 }, { 25, 29 },
            { 2, 6 }, { 10, 14 }, { 18, 22 }, { 26, 30 }, { 3, 7 }, { 11, 15 }, { 19, 23 }, { 27, 31 }
        };

        private static readonly int[] Map8A =
        {
            0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15,
            4, 0, 12, 8, 5, 1, 13, 9, 6, 2, 14, 10, 7, 3, 15, 11
        };

        private static readonly int[] Map8B =
        {
            4, 0, 12, 8, 5, 1, 13, 9, 6, 2, 14, 10, 7, 3, 15, 11,
            0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15
        };

        public TplModel ConvertFamily(TplModel source, bool targetPs2)
        {
            if (source.bitDepth != 0x08 && source.bitDepth != 0x09)
                throw new NotSupportedException("Interlace conversion currently supports only indexed 4-bit and 8-bit textures.");
            if (source.interlace > 3)
                throw new InvalidDataException("Unknown interlace value: " + source.interlace + ".");

            bool sourcePs2 = source.interlace >= 2;
            if (sourcePs2 == targetPs2)
                throw new InvalidOperationException(targetPs2 ? "The selected texture is already in the PS2 interlace family." : "The selected texture is already in the BGRA interlace family.");

            // Do not pre-reject rectangular 4-bit PS2 textures. RE4 uses valid layouts such as 256x128.
            // The encoder/decoder below already validate bounds and complete pixel coverage before any
            // converted payload can be written, so unsupported dimensions still fail safely in memory.

            byte[] canonical;
            byte[] convertedPixels;

            if (source.bitDepth == 0x09)
            {
                canonical = sourcePs2
                    ? DecodePs2Indices8(source.pixels, source.width, source.height)
                    : DecodeLinearIndices8(source.pixels, source.width, source.height);
                convertedPixels = targetPs2
                    ? EncodePs2Indices8(canonical, source.width, source.height)
                    : EncodeLinearIndices8(canonical, source.width, source.height);
            }
            else
            {
                canonical = sourcePs2
                    ? DecodePs2Indices4(source.pixels, source.width, source.height)
                    : DecodeLinearIndices4(source.pixels, source.width, source.height);
                convertedPixels = targetPs2
                    ? EncodePs2Indices4(canonical, source.width, source.height)
                    : EncodeLinearIndices4(canonical, source.width, source.height);
            }

            TplModel result = source;
            result.pixels = convertedPixels;
            result.palette = source.palette == null ? null : (byte[])source.palette.Clone();
            result.header = source.header == null ? null : (byte[])source.header.Clone();

            // Preserve Normal/Inverted bit while changing only the family:
            // 0 <-> 2 and 1 <-> 3.
            result.interlace = (ushort)((targetPs2 ? 2 : 0) | (source.interlace & 1));
            if (result.header == null || result.header.Length != 0x30)
                throw new InvalidDataException("Texture header is missing or invalid.");
            result.header[0x06] = (byte)(result.interlace & 0xFF);
            result.header[0x07] = (byte)(result.interlace >> 8);

            return result;
        }

        private static byte[] DecodeLinearIndices8(byte[] raw, int width, int height)
        {
            int count = checked(width * height);
            RequireLength(raw, count);
            byte[] result = new byte[count];
            Buffer.BlockCopy(raw, 0, result, 0, count);
            return result;
        }

        private static byte[] EncodeLinearIndices8(byte[] canonical, int width, int height)
        {
            return DecodeLinearIndices8(canonical, width, height);
        }

        private static byte[] DecodePs2Indices8(byte[] raw, int width, int height)
        {
            int count = checked(width * height);
            RequireLength(raw, count);
            byte[] canonical = new byte[count];
            bool[] written = new bool[count];
            int xCont = 0;
            int yCont = 0;
            bool flipX = false;

            for (int input = 0; input < count; input += 32)
            {
                int chunkLength = Math.Min(32, count - input);
                int[] map = flipX ? Map8B : Map8A;
                for (int i = 0; i < chunkLength; i++)
                {
                    int x = xCont + map[i];
                    int y = yCont + ((i & 1) == 0 ? 0 : 2);
                    if ((uint)x < (uint)width && (uint)y < (uint)height)
                    {
                        int dst = y * width + x;
                        canonical[dst] = raw[input + i];
                        written[dst] = true;
                    }
                }
                Advance8(width, ref xCont, ref yCont, ref flipX);
            }

            EnsureComplete(written, "8-bit PS2 deinterlace mapping");
            return canonical;
        }

        private static byte[] EncodePs2Indices8(byte[] canonical, int width, int height)
        {
            int count = checked(width * height);
            RequireLength(canonical, count);
            byte[] raw = new byte[count];
            bool[] read = new bool[count];
            int xCont = 0;
            int yCont = 0;
            bool flipX = false;

            for (int output = 0; output < count; output += 32)
            {
                int chunkLength = Math.Min(32, count - output);
                int[] map = flipX ? Map8B : Map8A;
                for (int i = 0; i < chunkLength; i++)
                {
                    int x = xCont + map[i];
                    int y = yCont + ((i & 1) == 0 ? 0 : 2);
                    if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                        throw new InvalidDataException("8-bit PS2 interlace mapping falls outside the texture dimensions.");
                    int src = y * width + x;
                    raw[output + i] = canonical[src];
                    read[src] = true;
                }
                Advance8(width, ref xCont, ref yCont, ref flipX);
            }

            EnsureComplete(read, "8-bit PS2 interlace mapping");
            return raw;
        }

        private static void Advance8(int width, ref int xCont, ref int yCont, ref bool flipX)
        {
            xCont += 16;
            if (xCont >= width)
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

        private static byte[] DecodeLinearIndices4(byte[] raw, int width, int height)
        {
            int count = checked(width * height);
            RequireLength(raw, (count + 1) / 2);
            byte[] canonical = new byte[count];
            int dst = 0;
            for (int i = 0; i < raw.Length && dst < count; i++)
            {
                canonical[dst++] = (byte)(raw[i] & 0x0F);
                if (dst < count) canonical[dst++] = (byte)(raw[i] >> 4);
            }
            return canonical;
        }

        private static byte[] EncodeLinearIndices4(byte[] canonical, int width, int height)
        {
            int count = checked(width * height);
            RequireLength(canonical, count);
            byte[] raw = new byte[(count + 1) / 2];
            int src = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                byte first = (byte)(canonical[src++] & 0x0F);
                byte second = src < count ? (byte)(canonical[src++] & 0x0F) : (byte)0;
                raw[i] = (byte)(first | (second << 4));
            }
            return raw;
        }

        private static byte[] DecodePs2Indices4(byte[] raw, int width, int height)
        {
            int count = checked(width * height);
            RequireLength(raw, (count + 1) / 2);
            int workWidth = height;
            int workHeight = width;
            byte[] work = new byte[checked(workWidth * workHeight)];
            bool[] written = new bool[work.Length];

            int xCont = 0, yCont = 0, altCont = 0, levels = 0;
            int maxLevel = Math.Max(1, width / 16);
            bool flipX = false;

            for (int input = 0; input < raw.Length; input += 32)
            {
                int chunkLength = Math.Min(32, raw.Length - input);
                for (int i = 0; i < chunkLength; i++)
                {
                    byte value = raw[input + i];
                    byte indexAtY2 = (byte)(value >> 4);
                    byte indexAtY0 = (byte)(value & 0x0F);
                    int x1 = flipX ? Pattern4[i, 1] : Pattern4[i, 0];
                    int x2 = flipX ? Pattern4[i, 0] : Pattern4[i, 1];
                    SetIndex(work, written, workWidth, workHeight, xCont + x1, yCont + 2, indexAtY2);
                    SetIndex(work, written, workWidth, workHeight, xCont + x2, yCont, indexAtY0);
                }
                Advance4(width, ref xCont, ref yCont, ref altCont, ref levels, maxLevel, ref flipX);
            }

            EnsureComplete(written, "4-bit PS2 deinterlace mapping");
            if (width > 128 || height > 128)
                return Reorder4BitPages(work, workWidth, workHeight, width, height);
            return work;
        }

        private static byte[] EncodePs2Indices4(byte[] canonical, int width, int height)
        {
            int count = checked(width * height);
            RequireLength(canonical, count);
            int workWidth = height;
            int workHeight = width;
            byte[] work = (width > 128 || height > 128)
                ? UndoReorder4BitPages(canonical, width, height, workWidth, workHeight)
                : (byte[])canonical.Clone();

            byte[] raw = new byte[(count + 1) / 2];
            bool[] read = new bool[work.Length];
            int xCont = 0, yCont = 0, altCont = 0, levels = 0;
            int maxLevel = Math.Max(1, width / 16);
            bool flipX = false;

            for (int output = 0; output < raw.Length; output += 32)
            {
                int chunkLength = Math.Min(32, raw.Length - output);
                for (int i = 0; i < chunkLength; i++)
                {
                    int x1 = flipX ? Pattern4[i, 1] : Pattern4[i, 0];
                    int x2 = flipX ? Pattern4[i, 0] : Pattern4[i, 1];
                    byte highNibble = GetIndex(work, read, workWidth, workHeight, xCont + x1, yCont + 2);
                    byte lowNibble = GetIndex(work, read, workWidth, workHeight, xCont + x2, yCont);
                    raw[output + i] = (byte)((highNibble << 4) | lowNibble);
                }
                Advance4(width, ref xCont, ref yCont, ref altCont, ref levels, maxLevel, ref flipX);
            }

            EnsureComplete(read, "4-bit PS2 interlace mapping");
            return raw;
        }

        private static void Advance4(int width, ref int xCont, ref int yCont, ref int altCont, ref int levels, int maxLevel, ref bool flipX)
        {
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

        private static byte[] Reorder4BitPages(byte[] source, int sourceWidth, int sourceHeight, int destinationWidth, int destinationHeight)
        {
            byte[] destination = new byte[checked(destinationWidth * destinationHeight)];
            int blockColumns = Math.Max(1, (destinationWidth + 127) / 128);
            int blockRows = Math.Max(1, (destinationHeight + 127) / 128);
            int blockCount = checked(blockColumns * blockRows);
            for (int block = 0; block < blockCount; block++)
            {
                int destinationBlockX = block % blockColumns;
                int destinationBlockY = block / blockColumns;
                int sourceBlockX = destinationBlockY;
                int sourceBlockY = destinationBlockX;
                CopyBlock(source, sourceWidth, sourceHeight, sourceBlockX * 128, sourceBlockY * 128,
                          destination, destinationWidth, destinationHeight, destinationBlockX * 128, destinationBlockY * 128);
            }
            return destination;
        }

        private static byte[] UndoReorder4BitPages(byte[] destination, int destinationWidth, int destinationHeight, int sourceWidth, int sourceHeight)
        {
            byte[] source = new byte[checked(sourceWidth * sourceHeight)];
            int blockColumns = Math.Max(1, (destinationWidth + 127) / 128);
            int blockRows = Math.Max(1, (destinationHeight + 127) / 128);
            int blockCount = checked(blockColumns * blockRows);
            for (int block = 0; block < blockCount; block++)
            {
                int destinationBlockX = block % blockColumns;
                int destinationBlockY = block / blockColumns;
                int sourceBlockX = destinationBlockY;
                int sourceBlockY = destinationBlockX;
                CopyBlock(destination, destinationWidth, destinationHeight, destinationBlockX * 128, destinationBlockY * 128,
                          source, sourceWidth, sourceHeight, sourceBlockX * 128, sourceBlockY * 128);
            }
            return source;
        }

        private static void CopyBlock(byte[] from, int fromWidth, int fromHeight, int fromX, int fromY,
                                      byte[] to, int toWidth, int toHeight, int toX, int toY)
        {
            int copyWidth = Math.Min(128, Math.Min(fromWidth - fromX, toWidth - toX));
            int copyHeight = Math.Min(128, Math.Min(fromHeight - fromY, toHeight - toY));
            if (copyWidth <= 0 || copyHeight <= 0) return;
            for (int y = 0; y < copyHeight; y++)
                Buffer.BlockCopy(from, (fromY + y) * fromWidth + fromX, to, (toY + y) * toWidth + toX, copyWidth);
        }

        private static void SetIndex(byte[] data, bool[] touched, int width, int height, int x, int y, byte value)
        {
            if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                throw new InvalidDataException("PS2 interlace mapping falls outside the texture dimensions.");
            int index = y * width + x;
            data[index] = value;
            touched[index] = true;
        }

        private static byte GetIndex(byte[] data, bool[] touched, int width, int height, int x, int y)
        {
            if ((uint)x >= (uint)width || (uint)y >= (uint)height)
                throw new InvalidDataException("PS2 interlace mapping falls outside the texture dimensions.");
            int index = y * width + x;
            touched[index] = true;
            return (byte)(data[index] & 0x0F);
        }

        private static void EnsureComplete(bool[] touched, string description)
        {
            for (int i = 0; i < touched.Length; i++)
                if (!touched[i])
                    throw new InvalidDataException(description + " did not cover every pixel. This texture size is not safe for the validated converter.");
        }

        private static void RequireLength(byte[] data, int expected)
        {
            if (data == null || data.Length < expected)
                throw new InvalidDataException("Texture pixel data is incomplete.");
        }
    }
}

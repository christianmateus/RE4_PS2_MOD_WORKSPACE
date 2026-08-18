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
    public sealed class TextureEncoder
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

        private static Bitmap ToArgbBitmap(Image image)
        {
            Bitmap bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            bitmap.SetResolution(image.HorizontalResolution > 0 ? image.HorizontalResolution : 96f,
                                 image.VerticalResolution > 0 ? image.VerticalResolution : 96f);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.DrawImageUnscaled(image, 0, 0);
            }
            return bitmap;
        }

        private static int[] ReadArgbPixels(Bitmap bitmap)
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                int[] result = new int[checked(width * height)];
                byte[] row = new byte[checked(width * 4)];

                for (int y = 0; y < height; y++)
                {
                    IntPtr rowPtr = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(rowPtr, row, 0, row.Length);
                    int dst = y * width;
                    int src = 0;
                    for (int x = 0; x < width; x++)
                    {
                        int b = row[src++];
                        int g = row[src++];
                        int r = row[src++];
                        int a = row[src++];

                        // RGB is irrelevant for a fully transparent texel. Collapsing these values
                        // prevents invisible colors from consuming scarce 4/8-bit palette entries.
                        if (a == 0) r = g = b = 0;
                        result[dst + x] = unchecked((int)((uint)(a << 24) | (uint)(r << 16) | (uint)(g << 8) | (uint)b));
                    }
                }
                return result;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static PaletteQuantizationResult QuantizeArgb(int[] pixels, int maxColors)
        {
            Dictionary<int, ColorSample> samplesByColor = new Dictionary<int, ColorSample>();
            for (int i = 0; i < pixels.Length; i++)
            {
                int argb = pixels[i];
                ColorSample sample;
                if (!samplesByColor.TryGetValue(argb, out sample))
                {
                    sample = new ColorSample(argb);
                    samplesByColor.Add(argb, sample);
                }
                sample.Count++;
            }

            List<ColorSample> samples = new List<ColorSample>(samplesByColor.Values);
            List<ColorBox> boxes = new List<ColorBox>();
            boxes.Add(new ColorBox(samples));

            while (boxes.Count < maxColors)
            {
                int splitIndex = FindBoxToSplit(boxes);
                if (splitIndex < 0) break;

                ColorBox box = boxes[splitIndex];
                ColorBox left;
                ColorBox right;
                if (!box.TrySplit(out left, out right)) break;
                boxes[splitIndex] = left;
                boxes.Add(right);
            }

            Color[] palette = new Color[maxColors];
            Dictionary<int, byte> colorToIndex = new Dictionary<int, byte>(samplesByColor.Count);
            for (int i = 0; i < boxes.Count; i++)
            {
                Color color = boxes[i].AverageColor();
                palette[i] = color;
                byte paletteIndex = checked((byte)i);
                foreach (ColorSample sample in boxes[i].Samples)
                    colorToIndex[sample.Argb] = paletteIndex;
            }

            // Unused CLUT entries remain fully transparent rather than opaque black.
            for (int i = boxes.Count; i < palette.Length; i++) palette[i] = Color.FromArgb(0, 0, 0, 0);

            byte[] indices = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) indices[i] = colorToIndex[pixels[i]];
            return new PaletteQuantizationResult(palette, indices);
        }

        private static int FindBoxToSplit(List<ColorBox> boxes)
        {
            int bestIndex = -1;
            long bestScore = -1;
            for (int i = 0; i < boxes.Count; i++)
            {
                ColorBox box = boxes[i];
                if (box.Samples.Count <= 1) continue;
                long score = box.SplitScore;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private static byte[] BuildTplIndices(byte[] topDownIndices, int width, int height, ushort bitDepth)
        {
            if (bitDepth == 0x09)
            {
                byte[] result = new byte[checked(width * height)];
                int dst = 0;
                // TPL linear payload follows the same bottom-up orientation used by the legacy BMP path.
                for (int y = height - 1; y >= 0; y--)
                {
                    Buffer.BlockCopy(topDownIndices, y * width, result, dst, width);
                    dst += width;
                }
                return result;
            }

            int pixelCount = checked(width * height);
            byte[] packed = new byte[(pixelCount + 1) / 2];
            int output = 0;
            bool lowNibble = true;
            byte current = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    byte index = (byte)(topDownIndices[row + x] & 0x0F);
                    if (lowNibble)
                    {
                        current = index;
                        lowNibble = false;
                    }
                    else
                    {
                        current |= (byte)(index << 4);
                        packed[output++] = current;
                        current = 0;
                        lowNibble = true;
                    }
                }
            }
            if (!lowNibble) packed[output] = current;
            return packed;
        }

        private static byte[] BuildTplPalette(Color[] logicalPalette, ushort tplBitDepth)
        {
            int expectedColors = tplBitDepth == 0x08 ? 16 : 256;
            byte[] rgba = new byte[expectedColors * 4];
            int availableColors = Math.Min(expectedColors, logicalPalette.Length);
            for (int i = 0; i < availableColors; i++)
            {
                Color color = logicalPalette[i];
                int dst = i * 4;
                rgba[dst] = color.R;
                rgba[dst + 1] = color.G;
                rgba[dst + 2] = color.B;
                rgba[dst + 3] = ToPs2Alpha(color.A);
            }

            if (tplBitDepth == 0x08)
            {
                // RE4's 4-bit CLUT occupies 0x80 bytes; colors 8-15 begin at offset 0x40.
                byte[] result = new byte[0x80];
                Buffer.BlockCopy(rgba, 0, result, 0, 8 * 4);
                Buffer.BlockCopy(rgba, 8 * 4, result, 0x40, 8 * 4);
                return result;
            }

            // PS2 8-bit CLUT order: groups of 32 colors swap their middle two blocks of 8.
            byte[] swizzled = new byte[0x400];
            for (int group = 0; group < 8; group++)
            {
                int baseColor = group * 32;
                int[] order = { 0, 16, 8, 24 };
                for (int block = 0; block < 4; block++)
                    Buffer.BlockCopy(rgba, (baseColor + order[block]) * 4, swizzled, (baseColor + block * 8) * 4, 8 * 4);
            }
            return swizzled;
        }

        private static byte ToPs2Alpha(byte alpha)
        {
            // PS2 GS uses 0x00..0x80 where 0x80 represents the usual PC alpha 0xFF.
            int converted = (alpha * 0x80 + 127) / 0xFF;
            if (converted < 0) converted = 0;
            if (converted > 0x80) converted = 0x80;
            return (byte)converted;
        }

        private static TplModel CreateTpl(ushort width, ushort height, ushort tplBitDepth, byte[] pixels, byte[] palette)
        {
            int pixelLength = pixels.Length;
            TplModel tpl = new TplModel
            {
                magic = 0x00001000,
                tplCount = 1,
                startOffset = 0x10,
                unused1 = 0,
                width = width,
                height = height,
                bitDepth = tplBitDepth,
                interlace = 0,
                zPriority = (ushort)((width > 256 || height > 256) ? 512 : 256),
                mipmapCount = 0,
                scale = tplBitDepth == 0x09
                    ? (ushort)(width * height / 16)
                    : (ushort)((width * height / 16) / 2),
                unused2 = 0,
                mipmapOffset1 = 0,
                mipmapOffset2 = 0,
                unknown1 = 0,
                unknown2 = 0,
                pixelsOffset = 0x40,
                paletteOffset = (uint)(0x40 + pixelLength),
                unused3 = 0,
                config1 = (byte)(width > 128 ? 0x00 : 0x80),
                config2 = BuildConfig2(width, tplBitDepth),
                config3 = BuildConfig3(width, height),
                unused4 = 0,
                unused5 = 0,
                endTag = 0x40,
                pixels = pixels,
                palette = palette,
                mipmapHeader1 = new byte[0],
                mipmapHeader2 = new byte[0],
                mipmapPixels1 = new byte[0],
                mipmapPixels2 = new byte[0]
            };
            tpl.header = BuildHeader(tpl);
            return tpl;
        }

        private static byte BuildConfig2(ushort width, ushort bitDepth)
        {
            byte high = bitDepth == 0x08 ? (byte)0x40 : (byte)0x30;
            return width > 128 ? (byte)(high + BitConverter.GetBytes(width)[1]) : high;
        }

        private static ushort BuildConfig3(ushort width, ushort height)
        {
            ushort value = 1229;
            for (int m = 0; m < 8; m++) { if (width == Math.Pow(2, 3 + m)) break; value += 4; }
            for (int m = 0; m < 8; m++) { if (height == Math.Pow(2, 3 + m)) break; value += 0x40; }
            return value;
        }

        private static byte[] BuildHeader(TplModel tpl)
        {
            using (MemoryStream ms = new MemoryStream(0x30))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(tpl.width); bw.Write(tpl.height); bw.Write(tpl.bitDepth); bw.Write(tpl.interlace);
                bw.Write(tpl.zPriority); bw.Write(tpl.mipmapCount); bw.Write(tpl.scale); bw.Write(tpl.unused2);
                bw.Write(tpl.mipmapOffset1); bw.Write(tpl.mipmapOffset2); bw.Write(tpl.unknown1); bw.Write(tpl.unknown2);
                bw.Write(tpl.pixelsOffset); bw.Write(tpl.paletteOffset); bw.Write(tpl.unused3); bw.Write(tpl.config1);
                bw.Write(tpl.config2); bw.Write(tpl.config3); bw.Write(tpl.unused4); bw.Write(tpl.unused5); bw.Write(tpl.endTag);
                return ms.ToArray();
            }
        }

        private sealed class PaletteQuantizationResult
        {
            public PaletteQuantizationResult(Color[] palette, byte[] indices)
            {
                Palette = palette;
                Indices = indices;
            }
            public Color[] Palette { get; private set; }
            public byte[] Indices { get; private set; }
        }

        private sealed class ColorSample
        {
            public ColorSample(int argb)
            {
                Argb = argb;
                A = (argb >> 24) & 0xFF;
                R = (argb >> 16) & 0xFF;
                G = (argb >> 8) & 0xFF;
                B = argb & 0xFF;
            }
            public int Argb { get; private set; }
            public int A { get; private set; }
            public int R { get; private set; }
            public int G { get; private set; }
            public int B { get; private set; }
            public int Count { get; set; }
        }

        private sealed class ColorBox
        {
            public ColorBox(List<ColorSample> samples)
            {
                Samples = samples;
                Recalculate();
            }

            public List<ColorSample> Samples { get; private set; }
            public int SplitComponent { get; private set; }
            public long SplitScore { get; private set; }

            public bool TrySplit(out ColorBox left, out ColorBox right)
            {
                left = null;
                right = null;
                if (Samples.Count <= 1) return false;

                int component = SplitComponent;
                Samples.Sort(delegate(ColorSample x, ColorSample y) { return GetComponent(x, component).CompareTo(GetComponent(y, component)); });

                long totalWeight = 0;
                for (int i = 0; i < Samples.Count; i++) totalWeight += Samples[i].Count;
                long half = totalWeight / 2;
                long accumulated = 0;
                int splitAt = 1;
                for (int i = 0; i < Samples.Count - 1; i++)
                {
                    accumulated += Samples[i].Count;
                    if (accumulated >= half)
                    {
                        splitAt = i + 1;
                        break;
                    }
                }
                if (splitAt <= 0 || splitAt >= Samples.Count) splitAt = Samples.Count / 2;
                if (splitAt <= 0 || splitAt >= Samples.Count) return false;

                left = new ColorBox(Samples.GetRange(0, splitAt));
                right = new ColorBox(Samples.GetRange(splitAt, Samples.Count - splitAt));
                return true;
            }

            public Color AverageColor()
            {
                long total = 0;
                long a = 0, r = 0, g = 0, b = 0;
                for (int i = 0; i < Samples.Count; i++)
                {
                    ColorSample sample = Samples[i];
                    long count = sample.Count;
                    total += count;
                    a += sample.A * count;
                    r += sample.R * count;
                    g += sample.G * count;
                    b += sample.B * count;
                }
                if (total == 0) return Color.FromArgb(0, 0, 0, 0);
                return Color.FromArgb(
                    ClampByte((int)((a + total / 2) / total)),
                    ClampByte((int)((r + total / 2) / total)),
                    ClampByte((int)((g + total / 2) / total)),
                    ClampByte((int)((b + total / 2) / total)));
            }

            private void Recalculate()
            {
                int minA = 255, minR = 255, minG = 255, minB = 255;
                int maxA = 0, maxR = 0, maxG = 0, maxB = 0;
                long population = 0;
                for (int i = 0; i < Samples.Count; i++)
                {
                    ColorSample s = Samples[i];
                    if (s.A < minA) minA = s.A; if (s.A > maxA) maxA = s.A;
                    if (s.R < minR) minR = s.R; if (s.R > maxR) maxR = s.R;
                    if (s.G < minG) minG = s.G; if (s.G > maxG) maxG = s.G;
                    if (s.B < minB) minB = s.B; if (s.B > maxB) maxB = s.B;
                    population += s.Count;
                }

                int rangeA = (maxA - minA) * 2; // transparency differences deserve extra weight
                int rangeR = maxR - minR;
                int rangeG = maxG - minG;
                int rangeB = maxB - minB;
                SplitComponent = 0;
                int range = rangeA;
                if (rangeR > range) { range = rangeR; SplitComponent = 1; }
                if (rangeG > range) { range = rangeG; SplitComponent = 2; }
                if (rangeB > range) { range = rangeB; SplitComponent = 3; }
                SplitScore = (long)Math.Max(1, range) * Math.Max(1, population);
            }

            private static int GetComponent(ColorSample sample, int component)
            {
                switch (component)
                {
                    case 0: return sample.A;
                    case 1: return sample.R;
                    case 2: return sample.G;
                    default: return sample.B;
                }
            }

            private static int ClampByte(int value)
            {
                if (value < 0) return 0;
                if (value > 255) return 255;
                return value;
            }
        }
    }
}

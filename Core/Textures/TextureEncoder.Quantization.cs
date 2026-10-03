using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    public sealed partial class TextureEncoder
    {
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

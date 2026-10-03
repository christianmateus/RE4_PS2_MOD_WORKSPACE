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

    }
}

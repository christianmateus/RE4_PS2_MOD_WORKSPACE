using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    /// <summary>
    /// RE4 PS2 mipmap operations. Mipmaps share the parent texture CLUT, so mipmap
    /// replacement/regeneration maps pixels against the existing parent palette instead
    /// of creating independent palettes.
    /// </summary>
    public sealed class MipmapService
    {
        private readonly TplReader reader;
        private readonly TplWriter writer;
        private readonly TextureEncoder encoder;

        public MipmapService(TplReader reader, TplWriter writer, TextureEncoder encoder)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        }

        public Bitmap DecodeMain(string path, int textureIndex)
        {
            TplModel texture = reader.ReadTexture(path, textureIndex);
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader br = new BinaryReader(stream))
                return TplHelper.DecodeTextureToBitmap(texture, br);
        }

        public Bitmap DecodeMip(string path, int textureIndex, int mipIndex)
        {
            TplModel parent = reader.ReadTexture(path, textureIndex);
            byte[] header = GetMipHeader(parent, mipIndex);
            byte[] pixels = GetMipPixels(parent, mipIndex);
            if (header == null || header.Length != 0x30 || pixels == null || pixels.Length == 0)
                throw new InvalidOperationException("The selected mipmap does not exist.");

            TplModel mip = BuildMipModel(header, pixels, parent.palette);
            byte[] streamData = new byte[pixels.Length + parent.palette.Length];
            Buffer.BlockCopy(pixels, 0, streamData, 0, pixels.Length);
            Buffer.BlockCopy(parent.palette, 0, streamData, pixels.Length, parent.palette.Length);
            mip.pixelsOffset = 0;
            mip.paletteOffset = (uint)pixels.Length;

            using (MemoryStream ms = new MemoryStream(streamData, false))
            using (BinaryReader br = new BinaryReader(ms))
                return TplHelper.DecodeTextureToBitmap(mip, br);
        }

        public void ReplaceMip(string path, int textureIndex, int mipIndex, Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            TplModel parent = reader.ReadTexture(path, textureIndex);
            byte[] header = GetMipHeader(parent, mipIndex);
            if (header == null || header.Length != 0x30)
                throw new InvalidOperationException("The selected mipmap does not exist.");

            TplModel mip = BuildMipModel(header, GetMipPixels(parent, mipIndex), parent.palette);
            using (Bitmap resized = ResizeForMip(image, mip.width, mip.height))
            {
                TplModel encoded = encoder.EncodeImageWithPalette(resized, parent.bitDepth, mip.interlace, parent.palette);
                SetMipPixels(ref parent, mipIndex, encoded.pixels);
            }
            ReplaceTextureModel(path, textureIndex, parent);
        }

        public void Regenerate(string path, int textureIndex)
        {
            TplModel parent = reader.ReadTexture(path, textureIndex);
            if (parent.mipmapCount == 0)
                throw new InvalidOperationException("This texture does not contain mipmaps.");

            using (Bitmap main = DecodeMain(path, textureIndex))
            {
                int count = Math.Min((ushort)2, parent.mipmapCount);
                for (int mipIndex = 0; mipIndex < count; mipIndex++)
                {
                    byte[] header = GetMipHeader(parent, mipIndex);
                    TplModel mip = BuildMipModel(header, GetMipPixels(parent, mipIndex), parent.palette);
                    using (Bitmap resized = ResizeForMip(main, mip.width, mip.height))
                    {
                        TplModel encoded = encoder.EncodeImageWithPalette(resized, parent.bitDepth, mip.interlace, parent.palette);
                        SetMipPixels(ref parent, mipIndex, encoded.pixels);
                        UpdateMipHeaderFormat(ref parent, mipIndex, parent.bitDepth, mip.interlace, mip.width, mip.height);
                    }
                }
            }
            ReplaceTextureModel(path, textureIndex, parent);
        }

        public void ReplaceMainAndRegenerate(string path, int textureIndex, Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            TplModel current = reader.ReadTexture(path, textureIndex);
            int colorCount = current.bitDepth == 0x08 ? 16 : current.bitDepth == 0x09 ? 256 : 0;
            if (colorCount == 0) throw new NotSupportedException("The mipmap editor currently supports 4-bit and 8-bit indexed textures.");

            using (Bitmap normalized = ResizeForMip(image, current.width, current.height))
            {
                TplModel replacement = encoder.EncodeImage(normalized, colorCount, current.interlace);
                writer.ReplaceTexture(path, textureIndex, replacement);
            }
            if (current.mipmapCount > 0) Regenerate(path, textureIndex);
        }

        public void AddMipmaps(string path, int textureIndex)
        {
            List<TplModel> textures = ReadAll(path);
            if (textureIndex < 0 || textureIndex >= textures.Count) throw new ArgumentOutOfRangeException(nameof(textureIndex));
            TplModel parent = textures[textureIndex];
            if (parent.mipmapCount > 0) throw new InvalidOperationException("This texture already contains mipmaps.");
            if (parent.bitDepth != 0x08 && parent.bitDepth != 0x09)
                throw new NotSupportedException("Mipmaps can currently be generated only for 4-bit and 8-bit indexed textures.");
            if (parent.width < 4 || parent.height < 4)
                throw new NotSupportedException("The texture is too small to create two mipmap levels.");

            using (Bitmap main = DecodeMain(path, textureIndex))
            {
                ushort w1 = (ushort)Math.Max(1, parent.width / 2);
                ushort h1 = (ushort)Math.Max(1, parent.height / 2);
                ushort w2 = (ushort)Math.Max(1, parent.width / 4);
                ushort h2 = (ushort)Math.Max(1, parent.height / 4);
                ushort i1 = ChooseGeneratedMipInterlace(parent, w1, h1);
                ushort i2 = ChooseGeneratedMipInterlace(parent, w2, h2);

                using (Bitmap b1 = ResizeForMip(main, w1, h1))
                using (Bitmap b2 = ResizeForMip(main, w2, h2))
                {
                    TplModel e1 = encoder.EncodeImageWithPalette(b1, parent.bitDepth, i1, parent.palette);
                    TplModel e2 = encoder.EncodeImageWithPalette(b2, parent.bitDepth, i2, parent.palette);
                    parent.mipmapCount = 2;
                    parent.mipmapHeader1 = BuildGeneratedMipHeader(parent, w1, h1, i1);
                    parent.mipmapHeader2 = BuildGeneratedMipHeader(parent, w2, h2, i2);
                    parent.mipmapPixels1 = e1.pixels;
                    parent.mipmapPixels2 = e2.pixels;
                    parent.mipmapOffset1 = 0;
                    parent.mipmapOffset2 = 0;
                    PatchUInt16(parent.header, 0x0A, 2);
                }
            }

            textures[textureIndex] = parent;
            writer.RebuildFile(path, textures);
        }

        public void RemoveMipmaps(string path, int textureIndex)
        {
            List<TplModel> textures = ReadAll(path);
            if (textureIndex < 0 || textureIndex >= textures.Count) throw new ArgumentOutOfRangeException(nameof(textureIndex));
            TplModel parent = textures[textureIndex];
            if (parent.mipmapCount == 0) return;
            parent.mipmapCount = 0;
            parent.mipmapOffset1 = 0;
            parent.mipmapOffset2 = 0;
            parent.mipmapHeader1 = new byte[0];
            parent.mipmapHeader2 = new byte[0];
            parent.mipmapPixels1 = new byte[0];
            parent.mipmapPixels2 = new byte[0];
            PatchUInt16(parent.header, 0x0A, 0);
            PatchUInt32(parent.header, 0x10, 0);
            PatchUInt32(parent.header, 0x14, 0);
            textures[textureIndex] = parent;
            writer.RebuildFile(path, textures);
        }

        private void ReplaceTextureModel(string path, int textureIndex, TplModel model)
        {
            List<TplModel> textures = ReadAll(path);
            textures[textureIndex] = model;
            writer.RebuildFile(path, textures);
        }

        private List<TplModel> ReadAll(string path)
        {
            uint count = reader.ReadTextureCount(path);
            List<TplModel> textures = new List<TplModel>(checked((int)count));
            for (int i = 0; i < count; i++) textures.Add(reader.ReadTexture(path, i));
            return textures;
        }

        private static Bitmap ResizeForMip(Image source, int width, int height)
        {
            Bitmap result = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, width, height));
            }
            return result;
        }

        private static ushort ChooseGeneratedMipInterlace(TplModel parent, ushort width, ushort height)
        {
            // Matches the layout observed in RE4 PS2 assets: PS2 swizzle is retained for large
            // compatible levels; smaller mip levels fall back to the linear BGRA family.
            if (parent.interlace >= 2)
            {
                bool supported = parent.bitDepth == 0x09
                    ? width >= 128 && height >= 128
                    : width == height && width >= 128 && (width % 32) == 0;
                if (supported) return (ushort)(2 | (parent.interlace & 1));
            }
            return 0;
        }

        private static TplModel BuildMipModel(byte[] header, byte[] pixels, byte[] palette)
        {
            if (header == null || header.Length != 0x30) throw new InvalidDataException("Invalid mipmap header.");
            TplModel mip = new TplModel();
            using (MemoryStream ms = new MemoryStream(header, false))
            using (BinaryReader br = new BinaryReader(ms))
            {
                mip.width = br.ReadUInt16(); mip.height = br.ReadUInt16(); mip.bitDepth = br.ReadUInt16(); mip.interlace = br.ReadUInt16();
                mip.zPriority = br.ReadUInt16(); mip.mipmapCount = br.ReadUInt16(); mip.scale = br.ReadUInt16(); mip.unused2 = br.ReadUInt16();
                mip.mipmapOffset1 = br.ReadUInt32(); mip.mipmapOffset2 = br.ReadUInt32(); mip.unknown1 = br.ReadUInt32(); mip.unknown2 = br.ReadUInt32();
                mip.pixelsOffset = br.ReadUInt32(); mip.paletteOffset = br.ReadUInt32(); mip.unused3 = br.ReadByte(); mip.config1 = br.ReadByte(); mip.config2 = br.ReadByte();
                mip.config3 = br.ReadUInt16(); mip.unused4 = br.ReadByte(); mip.unused5 = br.ReadByte(); mip.endTag = br.ReadByte();
            }
            mip.header = (byte[])header.Clone();
            mip.pixels = pixels == null ? new byte[0] : (byte[])pixels.Clone();
            mip.palette = palette == null ? new byte[0] : (byte[])palette.Clone();
            return mip;
        }

        private static byte[] BuildGeneratedMipHeader(TplModel parent, ushort width, ushort height, ushort interlace)
        {
            byte[] header = parent.header == null || parent.header.Length != 0x30 ? new byte[0x30] : (byte[])parent.header.Clone();
            PatchUInt16(header, 0x00, width);
            PatchUInt16(header, 0x02, height);
            PatchUInt16(header, 0x04, parent.bitDepth);
            PatchUInt16(header, 0x06, interlace);
            PatchUInt16(header, 0x08, 0x20);
            PatchUInt16(header, 0x0A, 2); // mirrors RE4 mip headers observed in game TPLs
            ushort scale = parent.bitDepth == 0x09
                ? (ushort)Math.Max(1, width * height / 16)
                : (ushort)Math.Max(1, (width * height / 16) / 2);
            PatchUInt16(header, 0x0C, scale);
            PatchUInt16(header, 0x0E, 0);
            PatchUInt32(header, 0x10, 0); PatchUInt32(header, 0x14, 0); PatchUInt32(header, 0x18, parent.unknown1); PatchUInt32(header, 0x1C, parent.unknown2);
            PatchUInt32(header, 0x20, 0); PatchUInt32(header, 0x24, 0);
            header[0x28] = 0;
            header[0x29] = 0x80;
            header[0x2A] = BuildConfig2(width, parent.bitDepth);
            PatchUInt16(header, 0x2B, BuildConfig3(width, height));
            header[0x2D] = 0; header[0x2E] = 0; header[0x2F] = 0x40;
            return header;
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

        private static void UpdateMipHeaderFormat(ref TplModel model, int index, ushort bitDepth, ushort interlace, ushort width, ushort height)
        {
            byte[] header = index == 0 ? model.mipmapHeader1 : model.mipmapHeader2;
            if (header == null || header.Length != 0x30) throw new InvalidDataException("Invalid mipmap header.");
            header = (byte[])header.Clone();
            PatchUInt16(header, 0x00, width);
            PatchUInt16(header, 0x02, height);
            PatchUInt16(header, 0x04, bitDepth);
            PatchUInt16(header, 0x06, interlace);
            ushort scale = bitDepth == 0x09
                ? (ushort)Math.Max(1, width * height / 16)
                : (ushort)Math.Max(1, (width * height / 16) / 2);
            PatchUInt16(header, 0x0C, scale);
            header[0x2A] = BuildConfig2(width, bitDepth);
            PatchUInt16(header, 0x2B, BuildConfig3(width, height));
            if (index == 0) model.mipmapHeader1 = header; else model.mipmapHeader2 = header;
        }

        private static byte[] GetMipHeader(TplModel model, int index)
        {
            if (index == 0) return model.mipmapHeader1;
            if (index == 1) return model.mipmapHeader2;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        private static byte[] GetMipPixels(TplModel model, int index)
        {
            if (index == 0) return model.mipmapPixels1;
            if (index == 1) return model.mipmapPixels2;
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        private static void SetMipPixels(ref TplModel model, int index, byte[] pixels)
        {
            if (index == 0) model.mipmapPixels1 = pixels;
            else if (index == 1) model.mipmapPixels2 = pixels;
            else throw new ArgumentOutOfRangeException(nameof(index));
        }

        private static void PatchUInt16(byte[] data, int offset, ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value); Buffer.BlockCopy(bytes, 0, data, offset, 2);
        }
        private static void PatchUInt32(byte[] data, int offset, uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value); Buffer.BlockCopy(bytes, 0, data, offset, 4);
        }
    }
}

using System;
using System.IO;
using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    /// <summary>
    /// Single source of truth for reading RE4 PS2 TPL metadata and texture payloads.
    /// UI code should not parse TPL headers directly when the same information can be obtained here.
    /// </summary>
    public sealed class TplReader
    {
        public TplModel ReadTexture(string path, int textureIndex)
        {
            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentException("TPL path is empty.", nameof(path));

            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                return ReadTexture(reader, textureIndex);
            }
        }

        public TplModel ReadTexture(BinaryReader reader, int textureIndex)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (!reader.BaseStream.CanSeek)
                throw new InvalidOperationException("TPL stream must support seeking.");
            if (reader.BaseStream.Length < 0x10)
                throw new InvalidDataException("TPL file is smaller than the global header.");

            TplModel tpl = new TplModel();
            reader.BaseStream.Position = 0;
            tpl.magic = reader.ReadUInt32();
            tpl.tplCount = reader.ReadUInt32();
            tpl.startOffset = reader.ReadUInt32();
            tpl.unused1 = reader.ReadUInt32();

            if (textureIndex < 0 || textureIndex >= tpl.tplCount)
                throw new ArgumentOutOfRangeException(nameof(textureIndex), "Texture index is outside the TPL texture table.");

            long headerOffset = 0x10L + (0x30L * textureIndex);
            EnsureRange(reader.BaseStream, headerOffset, 0x30, "texture header");
            reader.BaseStream.Position = headerOffset;

            tpl.width = reader.ReadUInt16();
            tpl.height = reader.ReadUInt16();
            tpl.bitDepth = reader.ReadUInt16();
            tpl.interlace = reader.ReadUInt16();
            tpl.zPriority = reader.ReadUInt16();
            tpl.mipmapCount = reader.ReadUInt16();
            tpl.scale = reader.ReadUInt16();
            tpl.unused2 = reader.ReadUInt16();
            tpl.mipmapOffset1 = reader.ReadUInt32();
            tpl.mipmapOffset2 = reader.ReadUInt32();
            tpl.unknown1 = reader.ReadUInt32();
            tpl.unknown2 = reader.ReadUInt32();
            tpl.pixelsOffset = reader.ReadUInt32();
            tpl.paletteOffset = reader.ReadUInt32();
            tpl.unused3 = reader.ReadByte();
            tpl.config1 = reader.ReadByte();
            tpl.config2 = reader.ReadByte();
            tpl.config3 = reader.ReadUInt16();
            tpl.unused4 = reader.ReadByte();
            tpl.unused5 = reader.ReadByte();
            tpl.endTag = reader.ReadByte();

            reader.BaseStream.Position = headerOffset;
            tpl.header = ReadExact(reader, 0x30, "texture header");

            int pixelLength = GetPixelDataLength(tpl.width, tpl.height, tpl.bitDepth);
            if (pixelLength > 0)
            {
                EnsureRange(reader.BaseStream, tpl.pixelsOffset, pixelLength, "texture pixels");
                reader.BaseStream.Position = tpl.pixelsOffset;
                tpl.pixels = ReadExact(reader, pixelLength, "texture pixels");
            }
            else
            {
                tpl.pixels = new byte[0];
            }

            int paletteLength = GetPaletteLength(tpl.bitDepth);
            if (paletteLength > 0)
            {
                EnsureRange(reader.BaseStream, tpl.paletteOffset, paletteLength, "texture palette");
                reader.BaseStream.Position = tpl.paletteOffset;
                tpl.palette = ReadExact(reader, paletteLength, "texture palette");
            }
            else
            {
                tpl.palette = new byte[0];
            }

            // The legacy structure exposes two mipmap slots. Parse those through the same helper
            // instead of duplicating the binary layout in FrmMain.
            if (tpl.mipmapCount > 0 && tpl.mipmapOffset1 != 0)
                ReadMipMap(reader, tpl.mipmapOffset1, out tpl.mipmapHeader1, out tpl.mipmapPixels1);
            else
            {
                tpl.mipmapHeader1 = new byte[0];
                tpl.mipmapPixels1 = new byte[0];
            }

            if (tpl.mipmapCount > 1 && tpl.mipmapOffset2 != 0)
                ReadMipMap(reader, tpl.mipmapOffset2, out tpl.mipmapHeader2, out tpl.mipmapPixels2);
            else
            {
                tpl.mipmapHeader2 = new byte[0];
                tpl.mipmapPixels2 = new byte[0];
            }

            return tpl;
        }

        public uint ReadTextureCount(string path)
        {
            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                EnsureRange(stream, 4, 4, "TPL texture count");
                stream.Position = 4;
                return reader.ReadUInt32();
            }
        }

        public static int GetPixelDataLength(ushort width, ushort height, ushort bitDepth)
        {
            checked
            {
                int pixels = width * height;
                switch (bitDepth)
                {
                    case 0x08: return pixels / 2; // 4-bit indexed
                    case 0x09: return pixels;     // 8-bit indexed
                    case 0x06: return pixels * 4; // 32-bit RGBA
                    default: return pixels;
                }
            }
        }

        public static int GetPaletteLength(ushort bitDepth)
        {
            switch (bitDepth)
            {
                case 0x08: return 0x80;
                case 0x09: return 0x400;
                default: return 0;
            }
        }

        private static void ReadMipMap(BinaryReader reader, uint headerOffset, out byte[] header, out byte[] pixels)
        {
            EnsureRange(reader.BaseStream, headerOffset, 0x30, "mipmap header");
            reader.BaseStream.Position = headerOffset;
            header = ReadExact(reader, 0x30, "mipmap header");

            reader.BaseStream.Position = headerOffset;
            ushort width = reader.ReadUInt16();
            ushort height = reader.ReadUInt16();
            ushort bitDepth = reader.ReadUInt16();
            reader.BaseStream.Position = headerOffset + 0x20;
            uint pixelsOffset = reader.ReadUInt32();

            int pixelLength = GetPixelDataLength(width, height, bitDepth);
            EnsureRange(reader.BaseStream, pixelsOffset, pixelLength, "mipmap pixels");
            reader.BaseStream.Position = pixelsOffset;
            pixels = ReadExact(reader, pixelLength, "mipmap pixels");
        }

        private static byte[] ReadExact(BinaryReader reader, int count, string section)
        {
            byte[] data = reader.ReadBytes(count);
            if (data.Length != count)
                throw new EndOfStreamException("Unexpected end of file while reading " + section + ".");
            return data;
        }

        private static void EnsureRange(Stream stream, long offset, long length, string section)
        {
            if (offset < 0 || length < 0 || offset > stream.Length || offset + length > stream.Length)
                throw new InvalidDataException("Invalid " + section + " offset/length in TPL file.");
        }
    }
}

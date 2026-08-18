using System.Collections.Generic;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    public class TPLDefinition
    {
        public struct TPL
        {
            // 0x00
            public uint magic;
            public uint tplCount;
            public uint startOffset;
            public uint unused1;
            // 0x10
            public ushort width;
            public ushort height;
            public ushort bitDepth;
            public ushort interlace;
            public ushort zPriority;
            public ushort mipmapCount;
            public ushort scale;
            public ushort unused2;
            // 0x20
            public uint mipmapOffset1;
            public uint mipmapOffset2;
            public uint unknown1;
            public uint unknown2;
            // 0x30
            public uint pixelsOffset;
            public uint paletteOffset;
            public byte unused3;
            public byte config1;
            public byte config2;
            public ushort config3;
            public byte unused4;
            public byte unused5;
            public byte endTag;
            // Chunk of data
            public byte[] header;
            public byte[] pixels;
            public byte[] palette;
            public byte[] mipmapHeader1;
            public byte[] mipmapHeader2;
            public byte[] mipmapPixels1;
            public byte[] mipmapPixels2;
        }
        public struct MipMap
        {
            // 0x00
            public ushort width;
            public ushort height;
            public ushort bitDepth;
            public ushort interlace;
            public ushort baseResolution;
            public ushort mipmapCount;
            public ushort multipliedResolution;
            public ushort unused2;
            // 0x10
            public uint mipmapOffset1;
            public uint mipmapOffset2;
            public uint unknown1;
            public uint unknown2;
            // 0x20
            public uint pixelsOffset;
            public uint paletteOffset;
            public byte unused3;
            public byte config1;
            public byte config2;
            public ushort config3;
            public byte unused4;
            public byte unused5;
            public byte endTag;
        }

        public Dictionary<string[], ushort[]> BitDepthDict = new Dictionary<string[], ushort[]>
        {
            { new string []{"4-bit","8-bit","32-bit"}, new ushort []{0x08, 0x09, 0x06} }
        };

        public Dictionary<string[], ushort[]> InterlaceDict = new Dictionary<string[], ushort[]>
        {
            { new string[]{"BGRA","BGRA Inverted", "PS2","PS2 Inverted" }, new ushort[] { 0x00, 0x01, 0x02, 0x03 } },
        };
    }
}

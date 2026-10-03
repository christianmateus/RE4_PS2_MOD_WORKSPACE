// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Materials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Encoding
{
    public struct BinWriteOptions
    {
        public (ushort b1, ushort b2, ushort b3, ushort b4)[] BonePairs;

        public bool EnableBonepairTag;
        public bool EnableAdjacentBoneTag;
        public bool EnableUnkFlag1;
        public bool EnableUnkFlag2;
        public bool EnableUnkFlag4;
        public bool IsScenarioBin;

        public BoundingBox BoundingBox;
    }
}

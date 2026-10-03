// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Decoding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Materials
{
    public static class BinHeaderFlags
    {
        public static bool ReturnsHasEnableBonepairTag(this DecodedModel header)
        {
            return ((BinFlags)header.Bin_flags).HasFlag(BinFlags.EnableBonepairTag);
        }

        public static bool ReturnsHasEnableAdjacentBoneTag(this DecodedModel header)
        {
            return ((BinFlags)header.Bin_flags).HasFlag(BinFlags.EnableAdjacentBoneTag);
        }

        public static bool ReturnsHasEnableUnkFlag1(this DecodedModel header)
        {
            return ((BinFlags)header.Bin_flags).HasFlag(BinFlags.EnableUnkFlag1);
        }

        public static bool ReturnsHasEnableUnkFlag2(this DecodedModel header)
        {
            return ((BinFlags)header.Bin_flags).HasFlag(BinFlags.EnableUnkFlag2);
        }

        public static bool ReturnsHasEnableUnkFlag4(this DecodedModel header)
        {
            return ((BinFlags)header.Bin_flags).HasFlag(BinFlags.EnableUnkFlag4);
        }

    }
}

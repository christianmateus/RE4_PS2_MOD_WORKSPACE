// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Decoding;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Common;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Materials
{
    public static class DecodedMaterialMapper
    {
        public static MaterialTable Parser(DecodedModel bin)
        {
            MaterialTable idx = new MaterialTable();
            idx.MaterialDic = new Dictionary<string, MaterialPart>();

            for (int i = 0; i < bin.materials.Length; i++)
            {
                idx.MaterialDic.Add(ModelEncodingConstants.MATERIAL + i.ToString("D3"), new MaterialPart(bin.materials[i].materialLine));
            }

            return idx;
        }

    }
}

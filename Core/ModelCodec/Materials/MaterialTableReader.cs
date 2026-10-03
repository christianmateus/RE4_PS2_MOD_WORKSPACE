// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Common;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Materials
{
    public static class MaterialTableReader
    {
        public static MaterialTable Load(Stream stream)
        {
            StreamReader reader = new StreamReader(stream, System.Text.Encoding.ASCII);

            MaterialTable idx = new MaterialTable();
            idx.MaterialDic = new Dictionary<string, MaterialPart>();

            MaterialPart temp = new MaterialPart();

            while (!reader.EndOfStream)
            {
                string line = reader?.ReadLine()?.Trim()?.ToUpperInvariant();

                if (line == null || line.Length == 0 || line.StartsWith("\\") || line.StartsWith("/") || line.StartsWith("#") || line.StartsWith(":"))
                {
                    continue;
                }
                else if (line.StartsWith("USEMATERIAL"))
                {
                    temp = new MaterialPart();

                    var split = line.Split(':');
                    if (split.Length >= 2)
                    {
                        string name = split[1].Trim();

                        if (!idx.MaterialDic.ContainsKey(name))
                        {
                            idx.MaterialDic.Add(name, temp);
                        }
                    }
                }
                else
                {
                    _ = Utils.SetByteHex(ref line, "MATERIAL_FLAG", ref temp.material_flag) //HEX
                     || Utils.SetByteDec(ref line, "DIFFUSE_MAP", ref temp.diffuse_map)
                     || Utils.SetByteDec(ref line, "BUMP_MAP", ref temp.bump_map)
                     || Utils.SetByteDec(ref line, "OPACITY_MAP", ref temp.opacity_map)
                     || Utils.SetByteDec(ref line, "GENERIC_SPECULAR_MAP", ref temp.generic_specular_map)
                     || Utils.SetByteDec(ref line, "INTENSITY_SPECULAR_R", ref temp.intensity_specular_r)
                     || Utils.SetByteDec(ref line, "INTENSITY_SPECULAR_G", ref temp.intensity_specular_g)
                     || Utils.SetByteDec(ref line, "INTENSITY_SPECULAR_B", ref temp.intensity_specular_b)
                     || Utils.SetByteDec(ref line, "UNK_08", ref temp.unk_08)
                     || Utils.SetByteDec(ref line, "UNK_09", ref temp.unk_09)
                     || Utils.SetByteHex(ref line, "SPECULAR_SCALE", ref temp.specular_scale) //HEX
                     || Utils.SetByteDec(ref line, "CUSTOM_SPECULAR_MAP", ref temp.custom_specular_map)
                     ;
                }

            }

            return idx;
        }

    }
}

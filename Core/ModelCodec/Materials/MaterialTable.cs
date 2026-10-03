// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.Materials
{
    /// <summary>
    /// representa o conjunto de materiais do BIN
    /// </summary>
    public class MaterialTable
    {
        /// <summary>
        /// material name, MaterialPart
        /// </summary>
        public Dictionary<string, MaterialPart> MaterialDic;

    }

}

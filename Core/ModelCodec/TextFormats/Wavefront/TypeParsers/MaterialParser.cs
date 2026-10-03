// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers.Interfaces;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers
{
    public class MaterialParser : IMaterialParser
    {
        private readonly IMaterialDataStore _materialDataStore;

        public MaterialParser(IMaterialDataStore materialDataStore)
        {
            _materialDataStore = materialDataStore;
        }

        public void AddMaterial(Material material)
        {
            _materialDataStore.Push(material);
        }
    }
}

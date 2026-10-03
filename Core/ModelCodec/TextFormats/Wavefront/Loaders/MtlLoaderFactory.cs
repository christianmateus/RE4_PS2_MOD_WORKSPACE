// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Loaders;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Loaders
{

    public class MtlLoaderFactory : IMtlLoaderFactory
    {
        public IMtlLoader Create()
        {
            var dataStore = new DataStoreMtl();
            var materialParser = new MaterialParser(dataStore);
            return new MtlLoader(dataStore, materialParser);
        }
    }
}

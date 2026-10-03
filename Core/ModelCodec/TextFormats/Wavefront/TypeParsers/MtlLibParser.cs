// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers.Interfaces;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers
{
    public class MtlLibParser : TypeParserBase, IMtlLibParser
    {
        private readonly IMtlLibDataStore _materialLibraryDataStore;

        public MtlLibParser(IMtlLibDataStore materialLibraryDataStore)
        {
            _materialLibraryDataStore = materialLibraryDataStore;
        }

        protected override string Keyword
        {
            get { return "mtllib"; }
        }

        public override void Parse(string line)
        {
            _materialLibraryDataStore.AddMtlLib(line);
        }
    }
}
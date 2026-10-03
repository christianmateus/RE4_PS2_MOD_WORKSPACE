// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers.Interfaces;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers
{
    public class MaterialNameParser : TypeParserBase, IMaterialNameParser
    {
        private readonly IMaterialNameDataStore _materialNameDataStore;

        public MaterialNameParser(IMaterialNameDataStore materialNameDataStore)
        {
            _materialNameDataStore = materialNameDataStore;
        }

        protected override string Keyword
        {
            get { return "usemtl"; }
        }

        public override void Parse(string line)
        {
            _materialNameDataStore.PushMaterial(line);
        }
    }
}
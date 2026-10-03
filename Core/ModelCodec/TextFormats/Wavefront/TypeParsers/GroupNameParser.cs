// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers.Interfaces;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers
{
    public class GroupNameParser : TypeParserBase, IGroupNameParser
    {
        private readonly IGroupNameDataStore _groupNameDataStore;

        public GroupNameParser(IGroupNameDataStore groupNameDataStore)
        {
            _groupNameDataStore = groupNameDataStore;
        }

        protected override string Keyword
        {
            get { return "g"; }
        }

        public override void Parse(string line)
        {
            _groupNameDataStore.PushGroup(line);
        }
    }
}
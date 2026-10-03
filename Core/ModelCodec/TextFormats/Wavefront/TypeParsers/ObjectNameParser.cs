// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers.Interfaces;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers;
using System;
using System.Collections.Generic;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers
{
    public class ObjectNameParser : TypeParserBase, IObjectNameParser
    {
        private readonly IObjectNameDataStore _objectNameDataStore;

        public ObjectNameParser(IObjectNameDataStore objectNameDataStore)
        {
            _objectNameDataStore = objectNameDataStore;
        }

        protected override string Keyword
        {
            get { return "o"; }
        }

        public override void Parse(string line)
        {
            _objectNameDataStore.PushObject(line);
        }
    }
}

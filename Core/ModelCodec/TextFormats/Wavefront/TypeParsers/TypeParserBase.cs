// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Common;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers.Interfaces;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers
{
    public abstract class TypeParserBase : ITypeParser
    {
        protected abstract string Keyword { get; }

        public bool CanParse(string keyword)
        {
            return keyword.EqualsOrdinalIgnoreCase(Keyword);
        }

        public abstract void Parse(string line);
    }
}
// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using System.Collections.Generic;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Common;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.Elements;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers.Interfaces;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers
{
    public class LineParser : TypeParserBase, ILineParser
    {
        private readonly ILineGroup _lineGroup;

        public LineParser(ILineGroup lineGroup)
        {
            _lineGroup = lineGroup;
        }

        protected override string Keyword
        {
            get { return "l"; }
        }

        public override void Parse(string line)
        {
            var s_Indexes = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> indexes = new List<int>();
            for (int i = 0; i < s_Indexes.Length; i++)
            {
                var vertexIndex = s_Indexes[i].ParseInvariantInt();
                indexes.Add(vertexIndex);
            }

            Line _line = new Line();
            _line.AddIndexes(indexes.ToArray());
            _lineGroup.AddLine(_line);
        }

     
    }
}
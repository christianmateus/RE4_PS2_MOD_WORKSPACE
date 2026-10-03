// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Common;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.VertexData;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers.Interfaces;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers
{
    public class VertexParser : TypeParserBase, IVertexParser
    {
        private readonly IVertexDataStore _vertexDataStore;

        public VertexParser(IVertexDataStore vertexDataStore)
        {
            _vertexDataStore = vertexDataStore;
        }

        protected override string Keyword
        {
            get { return "v"; }
        }

        public override void Parse(string line)
        {
            string[] parts = line.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);

            var x = parts[0].ParseInvariantFloat();
            var y = parts[1].ParseInvariantFloat();
            var z = parts[2].ParseInvariantFloat();

            var vertex = new Vertex(x, y, z);

            if (parts.Length == 4)
            {
                var w = parts[3].ParseInvariantFloat();
                vertex = new Vertex(x, y, z, w);
            }
            else if (parts.Length == 6)
            {
                var r = parts[3].ParseInvariantFloat();
                var g = parts[4].ParseInvariantFloat();
                var b = parts[5].ParseInvariantFloat();
                vertex = new Vertex(x, y, z, r, g, b);
            }
            else if (parts.Length == 7)
            {
                var r = parts[3].ParseInvariantFloat();
                var g = parts[4].ParseInvariantFloat();
                var b = parts[5].ParseInvariantFloat();
                var a = parts[6].ParseInvariantFloat();
                vertex = new Vertex(x, y, z, r, g, b, a);
            }          
            _vertexDataStore.AddVertex(vertex);
        }
    }
}
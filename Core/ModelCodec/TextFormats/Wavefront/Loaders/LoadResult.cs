// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System.Collections.Generic;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.Elements;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.VertexData;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Loaders
{
    public class LoadResult  
    {
        public IList<Vertex> Vertices { get; set; }
        public IList<Texture> Textures { get; set; }
        public IList<Normal> Normals { get; set; }
        public IList<Group> Groups { get; set; }
        public IList<string> Mtllibs { get; set; }
    }

    public class LoadResultMtl 
    {
        public IList<Material> Materials { get; set; }
    }
}
// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System.Collections.Generic;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.Elements;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.VertexData;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore
{
    public interface IDataStore 
    {
        IList<Vertex> Vertices { get; }
        IList<Texture> Textures { get; }
        IList<Normal> Normals { get; }
        IList<Group> Groups { get; }
        IList<string> MtlLibs { get; }
    }

    public interface IDataStoreMtl
    {
        IList<Material> Materials { get; }
    }

}
// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.VertexData
{
    public struct Texture
    {
        public Texture(float u, float v) : this()
        {
            U = u;
            V = v;
        }

        public float U { get; private set; }
        public float V { get; private set; }
    }
}
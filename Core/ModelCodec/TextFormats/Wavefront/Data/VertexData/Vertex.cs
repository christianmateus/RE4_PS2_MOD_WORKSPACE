// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.VertexData
{
    public struct Vertex
    {
        public Vertex(float x, float y, float z, float w = 1) : this()
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
            R = 1;
            G = 1;
            B = 1;
            A = 1;
        }

        public Vertex(float x, float y, float z, float r, float g, float b, float a = 1) : this()
        {
            X = x;
            Y = y;
            Z = z;
            W = 1;
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }

        public float W { get; private set; }
        public float R { get; private set; }
        public float G { get; private set; }
        public float B { get; private set; }
        public float A { get; private set; }
    }
}
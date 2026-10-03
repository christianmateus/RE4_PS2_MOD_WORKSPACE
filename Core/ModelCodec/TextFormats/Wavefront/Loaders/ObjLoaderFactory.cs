// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System.IO;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.TypeParsers;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Loaders
{
    public class ObjLoaderFactory : IObjLoaderFactory
    {
        public IObjLoader Create()
        {
            var dataStore = new DataStore();
            
            var faceParser = new FaceParser(dataStore);
            var lineParser = new LineParser(dataStore);
        
            var normalParser = new NormalParser(dataStore);
            var textureParser = new TextureParser(dataStore);
            var vertexParser = new VertexParser(dataStore);
            var mtlLibParser = new MtlLibParser(dataStore);
            var groupNameParser = new GroupNameParser(dataStore);
            var materialNameParser = new MaterialNameParser(dataStore);
            var objectNameParser = new ObjectNameParser(dataStore);

            return new ObjLoader(dataStore, faceParser, lineParser, normalParser, textureParser, vertexParser, mtlLibParser, groupNameParser, materialNameParser, objectNameParser);
        }
    }
}
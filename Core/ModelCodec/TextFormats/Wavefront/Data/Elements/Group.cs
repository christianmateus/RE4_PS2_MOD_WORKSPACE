// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System.Collections.Generic;
using RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.DataStore;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.Elements
{
    public class Group : IFaceGroup, ILineGroup
    {
        private readonly List<Face> _faces = new List<Face>();
        
        private readonly List<Line> _lines = new List<Line>();
        
        public Group(string groupName, string materialName, string objectName)
        {
            GroupName = groupName;
            MaterialName = materialName;
            ObjectName = objectName;
        }

        public string GroupName { get; private set; }
        public string MaterialName { get; private set; }
        public string ObjectName { get; private set; }

        public IList<Face> Faces { get { return _faces; } }

        public IList<Line> Lines { get { return _lines; } }

        public void AddFace(Face face)
        {
            _faces.Add(face);
        }

        public void AddLine(Line line)
        {
            _lines.Add(line);
        }

    }
}
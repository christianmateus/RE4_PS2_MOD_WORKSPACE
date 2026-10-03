// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System;
using System.Collections.Generic;
using System.Text;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Data.Elements
{
    public class Line
    {
        private readonly List<int> _indexes = new List<int>();

        public void AddIndexes(int[] Indexes)
        {
            _indexes.AddRange(Indexes);
        }

        public int this[int i]
        {
            get { return _indexes[i]; }
        }

        public int Count
        {
            get { return _indexes.Count; }
        }





    }
}

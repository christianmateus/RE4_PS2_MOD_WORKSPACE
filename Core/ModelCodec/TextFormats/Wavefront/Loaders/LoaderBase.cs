// Adapted from RE4 PS2 BIN Tool V.1.5.0 by JADERLINK.
// See THIRD_PARTY_NOTICES.txt for attribution and MIT licenses.
#nullable disable
using System.IO;

namespace RE4_PS2_MOD_WORKSPACE.Core.ModelCodec.TextFormats.Wavefront.Loaders
{
    public abstract class LoaderBase
    {
        private StreamReader _lineStreamReader;

        protected void StartLoad(StreamReader lineStream)
        {
            _lineStreamReader = lineStream;

            while (!_lineStreamReader.EndOfStream)
            {
                ParseLine();
            }
        }

        private void ParseLine()
        {
            var currentLine = _lineStreamReader.ReadLine();

            if (string.IsNullOrWhiteSpace(currentLine) || currentLine[0] == '#')
            {
                return;
            }

            var fields = currentLine.Trim().Split(null, 2);
            var keyword = fields[0].Trim();
            var data = (fields.Length >= 2) ? fields[1].Trim() : "";

            ParseLine(keyword, data);
        }

        protected abstract void ParseLine(string keyword, string data);
    }
}

using System;
using System.Drawing;
using System.IO;
using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    /// <summary>
    /// Application-level texture decoder. Keeps the WinForms layer independent from the
    /// low-level PS2 bitmap decoding helper and provides one place for future decoder changes.
    /// </summary>
    public sealed class TextureDecoder
    {
        public Bitmap Decode(TplModel texture, BinaryReader source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return TplHelper.DecodeTextureToBitmap(texture, source);
        }
    }
}

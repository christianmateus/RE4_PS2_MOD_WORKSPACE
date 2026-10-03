using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using TplModel = RE4_PS2_MOD_WORKSPACE.Core.Textures.TPLDefinition.TPL;

namespace RE4_PS2_MOD_WORKSPACE.Core.Textures
{
    public sealed partial class TextureEncoder
    {
        private static Bitmap ToArgbBitmap(Image image)
        {
            Bitmap bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            bitmap.SetResolution(image.HorizontalResolution > 0 ? image.HorizontalResolution : 96f,
                                 image.VerticalResolution > 0 ? image.VerticalResolution : 96f);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.DrawImageUnscaled(image, 0, 0);
            }
            return bitmap;
        }

        private static int[] ReadArgbPixels(Bitmap bitmap)
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                int[] result = new int[checked(width * height)];
                byte[] row = new byte[checked(width * 4)];

                for (int y = 0; y < height; y++)
                {
                    IntPtr rowPtr = IntPtr.Add(data.Scan0, y * data.Stride);
                    Marshal.Copy(rowPtr, row, 0, row.Length);
                    int dst = y * width;
                    int src = 0;
                    for (int x = 0; x < width; x++)
                    {
                        int b = row[src++];
                        int g = row[src++];
                        int r = row[src++];
                        int a = row[src++];

                        // RGB is irrelevant for a fully transparent texel. Collapsing these values
                        // prevents invisible colors from consuming scarce 4/8-bit palette entries.
                        if (a == 0) r = g = b = 0;
                        result[dst + x] = unchecked((int)((uint)(a << 24) | (uint)(r << 16) | (uint)(g << 8) | (uint)b));
                    }
                }
                return result;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

    }
}

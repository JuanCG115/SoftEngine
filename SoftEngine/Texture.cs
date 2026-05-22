using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SoftEngine
{
    public class Texture
    {
        private byte[] internalBuffer;
        private int width;
        private int height;

        public Texture(string filename)
        {
            using (Bitmap bitmap = new Bitmap(filename))
            {
                width = bitmap.Width;
                height = bitmap.Height;

                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                                     ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                int bytes = abs(bmpData.Stride) * bitmap.Height;
                internalBuffer = new byte[bytes];

                Marshal.Copy(bmpData.Scan0, internalBuffer, 0, bytes);
                bitmap.UnlockBits(bmpData);
            }
        }

        private int abs(int value) => value < 0 ? -value : value;

        public SharpDX.Color4 Map(float u, float v)
        {
            if (internalBuffer == null) return SharpDX.Color4.White;

            int x = (int)(Math.Abs(u) * width) % width;
            int y = (int)(Math.Abs(v) * height) % height;

            int index = (x + y * width) * 4;

            float b = internalBuffer[index] / 255.0f;
            float g = internalBuffer[index + 1] / 255.0f;
            float r = internalBuffer[index + 2] / 255.0f;
            float a = internalBuffer[index + 3] / 255.0f;

            return new SharpDX.Color4(r, g, b, a);
        }
    }
}
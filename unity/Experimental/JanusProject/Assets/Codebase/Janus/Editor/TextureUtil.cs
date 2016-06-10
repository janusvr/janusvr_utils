using System;
using System.Collections.Generic;

#if SYSTEM_DRAWING
using System.Drawing;
using System.Drawing.Imaging;
#endif

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class TextureUtil
    {
#if SYSTEM_DRAWING
        private static ImageFormat GetImageFormat(ImageFormatEnum format)
        {
            switch (format)
            {
                case ImageFormatEnum.JPG:
                    return ImageFormat.Jpeg;
                case ImageFormatEnum.PNG:
                default:
                    return ImageFormat.Png;
            }
        }
#endif

        public static void ExportTexture(Texture2D input, Stream output, ImageFormatEnum imageFormat, object data)
        {
#if SYSTEM_DRAWING
            ImageFormat format = GetImageFormat(imageFormat);
            Color32[] colors = input.GetPixels32();
            byte[] bdata = new byte[colors.Length * 4];

            // this is slower than linear, but easier to fix the texture mirrored
            for (int x = 0; x < input.width; x++)
            {
                for (int y = 0; y < input.height; y++)
                {
                    int index = x + (y * input.width);
                    Color32 col = colors[index];

                    index = (x + ((input.height - y - 1) * input.width)) * 4;
                    //index *= 4;

                    bdata[index] = col.b;
                    bdata[index + 1] = col.g;
                    bdata[index + 2] = col.r;
                    bdata[index + 3] = col.a;
                }
            }

            Bitmap bitmap = new Bitmap(input.width, input.height);
            BitmapData locked = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            Marshal.Copy(bdata, 0, locked.Scan0, bdata.Length);

            bitmap.UnlockBits(locked);
            bitmap.Save(output, format);

            bitmap.Dispose();
#else
            byte[] exported;
            switch (imageFormat)
            {
                case ImageFormatEnum.JPG:
                    exported = input.EncodeToJPG((int)data);
                    break;
                default:
                case ImageFormatEnum.PNG:
                    exported = input.EncodeToPNG();
                    break;
            }
            output.Write(exported, 0, exported.Length);
#endif
        }
    }
}

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
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    public struct TempTextureData
    {
#if UNITY_5_5_OR_NEWER
        public TextureImporterPlatformSettings settings;
        public TextureImporterCompression textureCompression;
#else
            public TextureImporterFormat format;
#endif
        public bool isReadable;
        public bool alphaIsTransparency;
        public string path;

        public bool changed;
        public bool empty;
    };

    public static class TextureUtil
    {
        public static TempTextureData LockTexture(Texture texture)
        {
            return LockTexture(texture, AssetDatabase.GetAssetPath(texture));
        }
        public static TempTextureData LockTexture(Texture texture, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                TempTextureData da = new TempTextureData();
                da.empty = true;
                return da;
            }

            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
            TempTextureData data = new TempTextureData();

#if UNITY_5_5_OR_NEWER
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Standalone");
            data.settings = settings;
            data.textureCompression = importer.textureCompression;
#else
            data.format = importer.textureFormat;
#endif
            data.isReadable = importer.isReadable;
            data.alphaIsTransparency = importer.alphaIsTransparency;
            data.path = path;

#if UNITY_5_5_OR_NEWER
            if (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
#else
            if (!importer.isReadable || importer.textureFormat != TextureImporterFormat.ARGB32)
#endif
            {
                importer.isReadable = true;
#if UNITY_5_5_OR_NEWER
                importer.textureCompression = TextureImporterCompression.Uncompressed;
#else
                importer.textureFormat = TextureImporterFormat.ARGB32;
#endif
                data.changed = true;

                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(path);
            }
            return data;
        }

        public static void UnlockTexture(TempTextureData data)
        {
            if (data.empty)
            {
                return;
            }

            if (data.changed)
            {
                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(data.path);

                importer.isReadable = data.isReadable;
#if UNITY_5_5_OR_NEWER
                importer.textureCompression = data.textureCompression;
#else
                importer.textureFormat = data.format;
#endif

                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(data.path);
            }
        }

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

        public static float DecodeLightmapRGBM(float alpha, float color, Vector2 decode)
        {
            return decode.x * (float)Math.Pow(alpha, decode.y) * color;
        }

        public static Color DecodeLightmapRGBM(Color data, Vector2 decode)
        {
            float r = DecodeLightmapRGBM(data.a, data.r, decode);
            float g = DecodeLightmapRGBM(data.a, data.g, decode);
            float b = DecodeLightmapRGBM(data.a, data.b, decode);

            return new Color(r, g, b, 1);
        }

        /// <summary>
        /// Sets alpha to 1 in the entire image
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Texture2D ZeroAlpha(Texture2D input)
        {
            Texture2D output = new Texture2D(input.width, input.height);
            Color[] source = input.GetPixels();
            Color[] target = new Color[input.width * input.height];

            for (int i = 0; i < source.Length; i++)
            {
                Color s = source[i];
                s.a = 1;
                target[i] = s;
            }

            output.SetPixels(target);
            output.Apply();
            return output;
        }

        public static bool SupportsAlpha(ExportTextureFormat format)
        {
            switch (format)
            {
                case ExportTextureFormat.PNG:
                    return true;
                case ExportTextureFormat.JPG:
                default:
                    return false;
            }
        }

        public static void ExportTexture(Cubemap input, Stream output, ExportTextureFormat imageFormat, object data)
        {
            Texture2D cache = new Texture2D(input.width * 6, input.height);

            for (int i = 0; i < 6; i++)
            {
                Color[] pixels = input.GetPixels((CubemapFace)i);
                cache.SetPixels(i * input.width, 0, input.width, input.height, pixels);
            }

            cache.Apply();

            byte[] exported;
            switch (imageFormat)
            {
                case ExportTextureFormat.JPG:
                    {
                        exported = cache.EncodeToJPG((int)data);
                    }
                    break;
                default:
                case ExportTextureFormat.PNG:
                    exported = cache.EncodeToPNG();
                    break;
            }
            if (exported == null)
            {
                // log texture name
                Debug.LogError("Texture failed exporting: " + input.name);
            }

            output.Write(exported, 0, exported.Length);
        }

        public static void ExportTexture(Texture2D input, Stream output, ExportTextureFormat imageFormat, object data, bool zeroAlpha)
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


            Texture2D inp;
            if (zeroAlpha)
            {
                inp = ZeroAlpha(input);
            }
            else
            {
                inp = input;
            }

            byte[] exported;
            switch (imageFormat)
            {
                case ExportTextureFormat.JPG:
                    {
                        exported = inp.EncodeToJPG((int)data);
                    }
                    break;
                default:
                case ExportTextureFormat.PNG:
                    exported = inp.EncodeToPNG();
                    break;
            }
            if (exported == null)
            {
                // log texture name
                Debug.LogError("Texture failed exporting: " + input.name);
            }

            output.Write(exported, 0, exported.Length);

            if (zeroAlpha)
            {
                // destroy the texture
                UnityEngine.Object.DestroyImmediate(inp);
            }
#endif
        }
    }
}
#if UNITY_EDITOR
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
    public static class TextureUtil
    {
        public struct TempTextureData
        {
            public TextureImporterPlatformSettings settings;
            public bool isReadable;
            public bool alphaIsTransparency;
            public TextureImporterCompression textureCompression;
            public string path;

            public bool changed;
        };

        public static TempTextureData LockTexture(Texture2D texture, string path)
        {
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);

            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Standalone");
            TempTextureData data = new TempTextureData();
            data.settings = settings;
            data.isReadable = importer.isReadable;
            data.alphaIsTransparency = importer.alphaIsTransparency;
            data.textureCompression = importer.textureCompression;
            data.path = path;

            if (!JanusVRExporter.UpdateOnlyHTML)
            {
                if (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    data.changed = true;

                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(path);
                }
            }
            return data;
        }

        public static void UnlockTexture(TempTextureData data)
        {
            if (data.changed && !JanusVRExporter.UpdateOnlyHTML)
            {
                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(data.path);

                importer.isReadable = data.isReadable;
                importer.textureCompression = data.textureCompression;

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

        public static Texture2D ScaleTexture(TextureExportData tex, int res, bool zeroAlpha = true, TextureFilterMode filterMode = TextureFilterMode.Nearest)
        {
            Texture2D texture = tex.Texture;

            int width = res;
            int height = res;
            if (texture.height != texture.width)
            {
                // cant directly scale
                if (texture.width > texture.height)
                {
                    height = (int)((texture.height / (float)texture.width) * width);
                }
                else
                {
                    width = (int)((texture.width / (float)texture.height) * height);
                }
            }


            // scale the texture
            Texture2D scaled = new Texture2D(width, height);

            Color[] source = texture.GetPixels();
            Color[] target = new Color[width * height];

            int xscale = texture.width / width;
            int yscale = texture.height / height;
            float xsca = xscale * 2;
            float ysca = yscale * 2;

            switch (filterMode)
            {
                //case TextureFilterMode.Average:
                //    {
                //        for (int x = 0; x < width; x++)
                //        {
                //            for (int y = 0; y < height; y++)
                //            {
                //                // sample neighbors
                //                int xx = x * xscale;
                //                int yy = y * yscale;

                //                float r = 0, g = 0, b = 0, a = 0;

                //                int ind = xx + (yy * texture.width);
                //                for (int j = 0; j < xscale; j++)
                //                {
                //                    Color col = source[ind + j];
                //                    r += col.r;
                //                    g += col.g;
                //                    b += col.b;
                //                    a += col.a;
                //                }
                //                ind = xx + ((yy + 1) * texture.width);
                //                for (int j = 0; j < xscale; j++)
                //                {
                //                    Color col = source[ind + j];
                //                    r += col.r;
                //                    g += col.g;
                //                    b += col.b;
                //                    a += col.a;
                //                }

                //                r = r / sca;
                //                g = g / sca;
                //                b = b / sca;
                //                a = a / sca;

                //                Color sampled = new Color(r, g, b, a);
                //                if (zeroAlpha)
                //                {
                //                    sampled.a = 1;
                //                }
                //                target[x + (y * width)] = sampled;
                //            }
                //        }
                //    }
                //    break;
                case TextureFilterMode.Nearest:
                    {
                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                // sample neighbors
                                int xx = x * xscale;
                                int yy = y * yscale;
                                int ind = xx + (yy * texture.width);

                                Color col = source[ind];
                                if (zeroAlpha)
                                {
                                    col.a = 1;
                                }
                                target[x + (y * width)] = col;
                            }
                        }
                    }
                    break;
            }

            scaled.SetPixels(target);
            scaled.Apply();
            return scaled;
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
#endif
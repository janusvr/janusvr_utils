#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public static class JanusUtil
    {
        private static string format2Cases = "F2";
        private static CultureInfo c = CultureInfo.InvariantCulture;

        public static bool SupportsQuality(ExportTextureFormat format)
        {
            switch (format)
            {
                case ExportTextureFormat.JPG:
                    return true;
                case ExportTextureFormat.PNG:// PNG is lossless
                default:
                    return false;
            }
        }

        public static string GetImageExtension(ExportTextureFormat format)
        {
            switch (format)
            {
                case ExportTextureFormat.JPG:
                    return ".jpg";
                case ExportTextureFormat.PNG:
                default:
                    return ".png";
            }
        }

        public static string GetWorkspacesFolder()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, @"JanusVR\workspaces");
        }

        public static string GetDefaultExportPath()
        {
            string workspace = GetWorkspacesFolder();
            return Path.Combine(workspace, Application.productName);
        }

        public static bool SupportsImageFormat(string format)
        {
            switch (format)
            {
                case ".png":
                case ".jpg":
                case ".gif":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IgnoreReExport(string format)
        {
            switch (format)
            {
                case ".gif":
                    return true;
                default:
                    return false;
            }
        }

        public static bool AssertShader(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("Shaders not found! Please reimport the Janus Exporter package");
                return false;
            }
            return true;
        }

        public static Vector3 ConvertPosition(Vector3 v, float scale)
        {
            v.x *= -(Math.Abs(scale));
            return v;
        }
        public static Vector3 ConvertDirection(Vector3 v)
        {
            v.x *= -1;
            return v;
        }

        public static string FormatFloat(float value, string format)
        {
            int ival = (int)value;
            return value == ival ? ival.ToString(c) : value.ToString(format, c);
        }

        public static string FormatFloat(float value, int decimalPlaces)
        {
            string format = "F" + decimalPlaces.ToString(c);
            int ival = (int)value;
            return value == ival ? ival.ToString(c) : value.ToString(format, c);
        }

        public static string FormatColor(Color v)
        {
            return v.r.ToString(format2Cases, c) + " " + v.g.ToString(format2Cases, c) + " " + v.b.ToString(format2Cases, c);
        }

        public static string FormatVector3(Vector3 v)
        {
            return FormatVector3(v, JanusGlobals.DecimalCasesPosition);
        }

        public static string FormatVector3(Vector3 v, int decimalPlaces)
        {
            string format = "F" + decimalPlaces.ToString(c);
            return FormatFloat(v.x, format) + " " + FormatFloat(v.y, format) + " " + FormatFloat(v.z, format);
        }

        public static string FormatVector4(Vector4 v)
        {
            return FormatVector4(v, JanusGlobals.DecimalCasesPosition);
        }

        public static string FormatVector4(Vector4 v, int decimalPlaces)
        {
            string format = "F" + decimalPlaces.ToString(c);
            return FormatFloat(v.x, format) + " " + FormatFloat(v.y, format) + " " + FormatFloat(v.z, format) + " " + FormatFloat(v.w, format);
        }

        public static void GetJanusVectors(Transform trans,
            out Vector3 xDir, out Vector3 yDir, out Vector3 zDir)
        {
            Quaternion rot = trans.rotation;
            xDir = JanusUtil.ConvertDirection(rot * Vector3.right);
            yDir = JanusUtil.ConvertDirection(rot * Vector3.up);
            zDir = JanusUtil.ConvertDirection(rot * Vector3.forward);
        }
    }
}
#endif
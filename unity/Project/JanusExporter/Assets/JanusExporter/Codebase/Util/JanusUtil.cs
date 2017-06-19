#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public static class JanusUtil
    {
        private static string format2Cases = "F2";
        private static CultureInfo c = CultureInfo.InvariantCulture;

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

        public static string FormatColor(Color v)
        {
            return v.r.ToString(format2Cases, c) + " " + v.g.ToString(format2Cases, c) + " " + v.b.ToString(format2Cases, c);
        }

        public static string FormatVector3(Vector3 v)
        {
            return v.x.ToString(format2Cases, c) + " " + v.y.ToString(format2Cases, c) + " " + v.z.ToString(format2Cases, c);
        }

        public static string FormatVector4(Vector4 v)
        {
            return v.x.ToString(format2Cases, c) + " " + v.y.ToString(format2Cases, c) + " " + v.z.ToString(format2Cases, c) + " " + v.w.ToString(format2Cases, c);
        }

        public static string FormatVector4(Vector4 v, int decimalPlaces)
        {
            string format = "F" + decimalPlaces.ToString(c);
            return v.x.ToString(format, c) + " " + v.y.ToString(format, c) + " " + v.z.ToString(format, c) + " " + v.w.ToString(format, c);
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
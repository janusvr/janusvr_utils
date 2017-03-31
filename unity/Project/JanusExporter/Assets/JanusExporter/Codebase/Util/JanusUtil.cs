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
        private static CultureInfo c = CultureInfo.InvariantCulture;

        

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
            return v.r.ToString("F2", c) + " " + v.g.ToString("F2", c) + " " + v.b.ToString("F2", c);
        }

        public static string FormatVector3(Vector3 v)
        {
            return v.x.ToString("F2", c) + " " + v.y.ToString("F2", c) + " " + v.z.ToString("F2", c);
        }

        public static string FormatVector4(Vector4 v)
        {
            return v.x.ToString("F2", c) + " " + v.y.ToString("F2", c) + " " + v.z.ToString("F2", c) + " " + v.w.ToString("F2", c);
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
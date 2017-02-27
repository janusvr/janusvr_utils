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

        public static Vector3 ConvertSpace(Vector3 v)
        {
            v.x *= -1;
            return v;
        }

        public static string FormatVector3(Vector3 v)
        {
            return v.x.ToString(c) + " " + v.y.ToString(c) + " " + v.z.ToString(c);
        }

        public static string FormatVector4(Vector4 v)
        {
            return v.x.ToString(c) + " " + v.y.ToString(c) + " " + v.z.ToString(c) + " " + v.w.ToString(c);
        }

        public static void GetJanusVectors(Transform trans,
            out Vector3 xDir, out Vector3 yDir, out Vector3 zDir)
        {
            Quaternion rot = trans.rotation;
            xDir = JanusUtil.ConvertSpace(rot * Vector3.right);
            yDir = JanusUtil.ConvertSpace(rot * Vector3.up);
            zDir = JanusUtil.ConvertSpace(rot * Vector3.forward);
        }
    }
}

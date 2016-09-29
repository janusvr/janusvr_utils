using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.FBX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FbxVector2
    {
        public double X;
        public double Y;

        public FbxVector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("{X:");
            sb.Append(this.X.ToString("F2"));
            sb.Append(" Y:");
            sb.Append(this.Y.ToString("F2"));
            sb.Append("}");
            return sb.ToString();
        }
    }
}

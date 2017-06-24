using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JanusVR.FBX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FbxVector2
    {
        public float X;
        public float Y;

        public FbxVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("{X:");
            sb.Append(this.X.ToString("F6"));
            sb.Append(" Y:");
            sb.Append(this.Y.ToString("F6"));
            sb.Append("}");
            return sb.ToString();
        }
    }
}
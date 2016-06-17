using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.FBX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FbxVector3
    {
        public double X;
        public double Y;
        public double Z;

        public FbxVector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}

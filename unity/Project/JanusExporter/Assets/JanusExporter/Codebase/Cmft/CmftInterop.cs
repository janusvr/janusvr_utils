using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JanusVR
{
    public static class CmftInterop
    {
        [DllImport("cmftRelease", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void Execute([MarshalAs(UnmanagedType.LPStr)] string cmd);
    }
}

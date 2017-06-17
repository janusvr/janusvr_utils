
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JanusVR.CMFT
{
    public static class CmftInterop
    {
        [DllImport("cmftRelease", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private extern static void Execute([MarshalAs(UnmanagedType.LPStr)] string cmd);

        public static void DoExecute(string cmd)
        {
            Execute(cmd);
        }
    }
}

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanusVR
{
    public static class JanusGlobals
    {
        public const int Version = 208;
        public const string UpdateUrl = @"https://raw.githubusercontent.com/JamesMcCrae/janusvr_utils/master/unity/Release/version.txt";
        public const string UnityPkgUrl = @"https://github.com/JamesMcCrae/janusvr_utils/raw/master/unity/Release/JanusVRExporter.unitypackage";
    }
}
#endif
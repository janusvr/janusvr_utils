using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JanusVR.FBX
{
    public static class FbxExporterInterop
    {
        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void Initialize([MarshalAs(UnmanagedType.LPStr)] string SceneName);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void SetFBXCompatibility(int CompatibilityVersion);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void BeginMesh([MarshalAs(UnmanagedType.LPStr)] string MeshName);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void EndMesh();

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddVertices(FbxVector3[] Vertices, int Count);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddIndices(int[] Triangles, int Count, int Material);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddNormals(FbxVector3[] Normals, int Count);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddTexCoords(FbxVector2[] TexCoords, int Count, int UVLayer, string ChannelName);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void EnableDefaultMaterial([MarshalAs(UnmanagedType.LPStr)] string materialName);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl)]
        public extern static void SetMaterial([MarshalAs(UnmanagedType.LPStr)] string materialName,
            FbxVector3 emissive, FbxVector3 ambient, FbxVector3 diffuse, FbxVector3 specular,
            FbxVector3 reflection, double shininess);

        [DllImport("UnityFBXExporter", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void Export([MarshalAs(UnmanagedType.LPStr)] string SceneName);
    }
}
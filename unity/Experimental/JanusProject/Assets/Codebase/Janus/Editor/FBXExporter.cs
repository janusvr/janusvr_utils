using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.FBX
{
    public static class FBXExporter
    {
        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void Initialize([MarshalAs(UnmanagedType.LPStr)] string SceneName);

        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void SetFBXCompatibility(int CompatibilityVersion);

        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void AddMesh([MarshalAs(UnmanagedType.LPStr)] string MeshName);

        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddVertices(FbxVector3[] Vertices, int Count);

        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddIndices(int[] Triangles, int Count, int Material);

        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddNormals(FbxVector3[] Normals, int Count);

        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddTexCoords(FbxVector2[] TexCoords, int Count, int UVLayer, string ChannelName);

        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl)]
        public extern static void AddMaterial(FbxVector3 DiffuseColor);

        [DllImport("UnityFBXExporter86", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static void Export([MarshalAs(UnmanagedType.LPStr)] string SceneName);

        public static void ExportMesh(Mesh mesh, string path, int fbxVersion = 1)
        {
            FBXExporter64.Initialize(mesh.name);
            FBXExporter64.SetFBXCompatibility(fbxVersion);
            FBXExporter64.AddMesh(mesh.name);

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            FbxVector3[] nvertices = new FbxVector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                nvertices[i] = new FbxVector3(v.x, v.y, v.z);
            }

            FbxVector3[] nnormals = new FbxVector3[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                Vector3 v = normals[triangles[i]];
                nnormals[i] = new FbxVector3(v.x, v.y, v.z);
            }

            FBXExporter64.AddMaterial(new FbxVector3(0.7, 0.7, 0.7));
            FBXExporter64.AddIndices(triangles, triangles.Length, 0);
            FBXExporter64.AddVertices(nvertices, nvertices.Length);
            FBXExporter64.AddNormals(nnormals, nnormals.Length);

            for (int i = 0; i < 4; i++)
            {
                List<Vector2> tverts = new List<Vector2>();
                mesh.GetUVs(i, tverts);

                if (tverts.Count == 0)
                {
                    continue;
                }

                FbxVector2[] uv = new FbxVector2[triangles.Length];
                for (int j = 0; j < triangles.Length; j++)
                {
                    Vector2 v = tverts[triangles[j]];
                    uv[j] = new FbxVector2(v.x, v.y);
                }
                FBXExporter64.AddTexCoords(uv, uv.Length, i, "UV");
            }

            FBXExporter64.Export(path);
        }
    }
}

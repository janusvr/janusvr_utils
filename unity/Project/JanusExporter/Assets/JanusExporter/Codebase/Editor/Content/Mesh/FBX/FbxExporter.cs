using JanusVR;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace JanusVR.FBX
{
    /// <summary>
    /// Class that handles exporting Unity meshes to FBX
    /// </summary>
    public class FbxExporter : MeshExporter
    {
        private JanusRoom room;

        public override void Initialize(JanusRoom room)
        {
            this.room = room;
        }

        public override string GetFormat()
        {
            return ".fbx";
        }

        public override void ExportMesh(MeshData mesh, string exportPath, MeshExportParameters parameters)
        {
            Vector3[] vertices = mesh.Vertices;
            Vector3[] normals = mesh.Normals;
            int[] triangles = mesh.Triangles;
            Vector2[][] uvs = mesh.UV;

            FbxExporterInterop.Initialize(mesh.Name + "Scene");
            FbxExporterInterop.SetFBXCompatibility(4);
            FbxExporterInterop.BeginMesh(mesh.Name);

            FbxVector3[] nvertices = new FbxVector3[vertices.Length];
            FbxVector3[] nnormals = new FbxVector3[vertices.Length];

            if (parameters.Mirror)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 v = vertices[i];
                    vertices[i] = new Vector3(-v.x, v.y, v.z);
                }
                for (int i = 0; i < normals.Length; i++)
                {
                    Vector3 v = normals[i];
                    normals[i] = new Vector3(-v.x, v.y, v.z);
                }

                // change triangles order
                for (int i = 0; i < triangles.Length - 2; i += 3)
                {
                    int i0 = triangles[i];
                    int i1 = triangles[i + 1];
                    int i2 = triangles[i + 2];

                    triangles[i] = i2;
                    triangles[i + 1] = i1;
                    triangles[i + 2] = i0;
                }
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                nvertices[i] = new FbxVector3(v.x, v.y, v.z);
            }
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 v = normals[i];
                nnormals[i] = new FbxVector3(v.x, v.y, v.z);
            }

            FbxExporterInterop.EnableDefaultMaterial(mesh.Name + "Material");
            FbxExporterInterop.AddVertices(nvertices, nvertices.Length);
            FbxExporterInterop.AddNormals(nnormals, nnormals.Length);

            for (int i = 0; i < uvs.Length; i++)
            {
                Vector2[] uv = uvs[i];
                if (uv == null || uv.Length == 0)
                {
                    continue;
                }

                FbxVector2[] fbxUv = new FbxVector2[uv.Length];
                for (int j = 0; j < uv.Length; j++)
                {
                    Vector2 v = uv[j];
                    fbxUv[j] = new FbxVector2(v.x, v.y);
                }
                FbxExporterInterop.AddTexCoords(fbxUv, fbxUv.Length, i, "UV" + i.ToString(CultureInfo.InvariantCulture));
            }

            FbxExporterInterop.AddIndices(triangles, triangles.Length, 0);
            FbxExporterInterop.EndMesh();

            string fullPath = Path.Combine(room.RootFolder, exportPath);
            FbxExporterInterop.Export(fullPath);
        }
    }
}
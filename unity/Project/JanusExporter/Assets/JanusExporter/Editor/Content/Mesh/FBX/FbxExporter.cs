using JanusVR;
using System;
using System.Collections.Generic;
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

        public override void ExportMesh(Mesh mesh, string exportPath, MeshExportParameters parameters)
        {
            if (mesh.GetTopology(0) != MeshTopology.Triangles)
            {
                return;
            }

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            if (triangles == null || triangles.Length == 0)
            {
                Debug.LogWarning("Mesh is empty " + mesh.name, mesh);
                return;
            }

            // check if we have all the data
            int maximum = triangles.Max();
            if (normals.Length < maximum)
            {
                Debug.LogWarning("Mesh has not enough normals - " + mesh.name, mesh);
                return;
            }

            FbxExporterInterop.Initialize(mesh.name);
            FbxExporterInterop.SetFBXCompatibility(1);
            FbxExporterInterop.AddMesh(mesh.name);

            FbxVector3[] nvertices = new FbxVector3[vertices.Length];
            FbxVector3[] nnormals = new FbxVector3[triangles.Length];

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
            for (int i = 0; i < triangles.Length; i++)
            {
                Vector3 v = normals[triangles[i]];
                nnormals[i] = new FbxVector3(v.x, v.y, v.z);
            }

            FbxExporterInterop.AddMaterial(new FbxVector3(0.7, 0.7, 0.7));
            FbxExporterInterop.AddIndices(triangles, triangles.Length, 0);
            FbxExporterInterop.AddVertices(nvertices, nvertices.Length);
            FbxExporterInterop.AddNormals(nnormals, nnormals.Length);

            if (parameters.SwitchUV)
            {
                List<Vector2> tverts = new List<Vector2>();
#if UNITY_5_3_OR_NEWER
                mesh.GetUVs(1, tverts);
#else
                tverts.AddRange(mesh.uv2);
#endif

                if (tverts.Count != 0)
                {
                    FbxVector2[] uv = new FbxVector2[triangles.Length];
                    for (int j = 0; j < triangles.Length; j++)
                    {
                        Vector2 v = tverts[triangles[j]];
                        uv[j] = new FbxVector2(v.x, v.y);
                    }

                    FbxExporterInterop.AddTexCoords(uv, uv.Length, 0, "UV0");
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    List<Vector2> tverts = new List<Vector2>();
#if UNITY_5_3_OR_NEWER
                    mesh.GetUVs(i, tverts);
#else
                    switch (i)
                    {
                        case 0:
                            tverts.AddRange(mesh.uv);
                            break;
                        case 1:
                            tverts.AddRange(mesh.uv2);
                            break;
                        case 2:
                            tverts.AddRange(mesh.uv3);
                            break;
                        case 3:
                            tverts.AddRange(mesh.uv4);
                            break;
                    }
#endif

                    if (tverts.Count == 0)
                    {
                        if (room.LightmapType != LightmapExportType.None && i == 1)
                        {
                            Debug.LogWarning("Lightmapping is enabled but mesh has no UV1 channel - " + mesh.name + " - Tick the Generate Lightmap UVs", mesh);
                        }
                        continue;
                    }

                    FbxVector2[] uv = new FbxVector2[triangles.Length];

                    for (int j = 0; j < triangles.Length; j++)
                    {
                        Vector2 v = tverts[triangles[j]];
                        uv[j] = new FbxVector2(v.x, v.y);
                    }

                    FbxExporterInterop.AddTexCoords(uv, uv.Length, i, "UV" + i);
                }
            }

            string fullPath = Path.Combine(room.RootFolder, exportPath);
            FbxExporterInterop.Export(fullPath);
        }
    }
}
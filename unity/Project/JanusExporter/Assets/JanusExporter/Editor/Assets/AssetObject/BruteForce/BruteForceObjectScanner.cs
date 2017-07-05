using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    /// <summary>
    /// Each mesh becomes a new model file
    /// </summary>
    public class BruteForceObjectScanner : ObjectScanner
    {
        private Dictionary<Mesh, BruteForceMeshExportData> meshesToExport;
        private List<string> meshNames;

        private Bounds sceneBounds;
        private JanusRoom room;

        private MaterialScanner materialScanner;
        private JanusComponentExtractor compExtractor;

        public BruteForceObjectScanner()
        {
            meshesToExport = new Dictionary<Mesh, BruteForceMeshExportData>();
            // for making sure we have no conflicting names
            meshNames = new List<string>();
            sceneBounds = new Bounds();
        }

        public override void Initialize(JanusRoom room, GameObject[] rootObjects)
        {
            this.room = room;

            materialScanner = new MaterialScanner(room);
            compExtractor = new JanusComponentExtractor(room);

            EditorUtility.DisplayProgressBar("Janus VR Exporter", "Brute force scanning for AssetObjects...", 0.0f);

            materialScanner.Initialize();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject root = rootObjects[i];
                RecursiveSearch(root);
            }

            room.FarPlaneDistance = (int)Math.Max(500, sceneBounds.size.magnitude * 1.3f);
        }

        public void RecursiveSearch(GameObject root)
        {
            if (room.IgnoreInactiveObjects &&
                !root.activeInHierarchy)
            {
                return;
            }

            // loop thorugh all the gameObjects components
            // to see what we can extract
            Component[] comps = root.GetComponents<Component>();
            if (!compExtractor.CanExport(comps))
            {
                compExtractor.Process(comps);
                return;
            }

            for (int i = 0; i < comps.Length; i++)
            {
                Component comp = comps[i];
                if (!comp)
                {
                    continue;
                }

                if (comp is MeshRenderer)
                {
                    MeshRenderer meshRen = (MeshRenderer)comp;
                    MeshFilter filter = comps.FirstOrDefault(c => c is MeshFilter) as MeshFilter;
                    if (filter == null)
                    {
                        continue;
                    }

                    Mesh mesh = filter.sharedMesh;
                    if (mesh == null ||
                        !room.CanExportObj(comps) || 
                        mesh.GetTopology(0) != MeshTopology.Triangles)
                    {
                        continue;
                    }

                    sceneBounds.Encapsulate(meshRen.bounds);

                    // Only export the mesh if we never exported this one mesh
                    BruteForceMeshExportData exp;
                    if (!meshesToExport.TryGetValue(mesh, out exp))
                    {
                        exp = new BruteForceMeshExportData();
                        exp.Mesh = mesh;
                        // generate name
                        string meshId = mesh.name;
                        if (string.IsNullOrEmpty(meshId) ||
                            meshNames.Contains(meshId))
                        {
                            meshId = "ExportedMesh" + meshNames.Count;
                        }
                        meshNames.Add(meshId);

                        // keep our version of the data
                        exp.MeshId = meshId;
                        meshesToExport.Add(mesh, exp);

                        // but also supply the data to the Janus Room so the Html can be built
                        AssetObject asset = new AssetObject();
                        exp.Asset = asset;

                        asset.id = meshId;
                        asset.src = meshId + ".fbx";

                        room.AddAssetObject(asset);
                    }

                    // We are brute force, so all objects become Room Objects
                    RoomObject obj = new RoomObject();
                    obj.id = exp.Asset.id;
                    obj.SetUnityObj(root, room);
                    room.AddRoomObject(obj);
                    compExtractor.ProcessNewRoomObject(obj, comps);

                    // let the material scanner process this object
                    materialScanner.PreProcessObject(meshRen, mesh, exp.Asset, obj, true);
                }
            }

            // loop through all this GameObject children
            foreach (Transform child in root.transform)
            {
                RecursiveSearch(child.gameObject);
            }
        }

        public override void ExportAssetImages()
        {
            // process textures
            materialScanner.ProcessTextures();
        }

        public static bool LightmapNeedsUV1(JanusRoom room)
        {
            return room.LightmapType == LightmapExportType.Packed ||
                room.LightmapType == LightmapExportType.PackedSourceEXR ||
                room.LightmapType == LightmapExportType.Unpacked;
        }

        public static bool LightmapNeedsUVOverride(JanusRoom room)
        {
            return room.LightmapType == LightmapExportType.BakedMaterial;
        }

        public MeshData GetMeshData(Mesh mesh)
        {
            return GetMeshData(this.room, mesh);
        }
        public static MeshData GetMeshData(JanusRoom room, Mesh mesh)
        {
            MeshData data = new MeshData();

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            data.Vertices = vertices;
            data.Normals = normals;
            data.Triangles = triangles;
            data.Name = mesh.name;

            if (vertices == null || vertices.Length == 0)
            {
                Debug.LogWarning("Mesh is empty " + mesh.name, mesh);
                return null;
            }

            // check if we have all the data
            int maximum = triangles.Max();
            if (normals.Length < maximum)
            {
                Debug.LogWarning("Mesh has not enough normals - " + mesh.name, mesh);
                return null;
            }

            data.UV = GetMeshUVs(room, mesh);
            return data;
        }

        public static Vector2[][] GetMeshUVs(JanusRoom room, Mesh mesh, int fakeUvSize = 0)
        {
            int uvLayers = LightmapNeedsUV1(room) ? 2 : 1;
            bool needUvOverride = LightmapNeedsUVOverride(room);
            Vector2[][] uvs = new Vector2[uvLayers][];

            for (int i = 0; i < uvLayers; i++)
            {
                Vector2[] array = null;
#if !UNITY_5_3_OR_NEWER
                List<Vector2> tverts = new List<Vector2>();
                if (needUvOverride)
                {
                    mesh.GetUVs(1, tverts);
                }
                else
                {
                    mesh.GetUVs(i, tverts);
                }
                array = tverts.ToArray();
#else
                switch (i)
                {
                    case 0:
                        if (needUvOverride)
                        {
                            array = mesh.uv2;
                        }
                        else
                        {
                            array = mesh.uv;
                        }
                        break;
                    case 1:
                        array = mesh.uv2;
                        break;
                }
#endif
                if (fakeUvSize > 0 && (array == null || array.Length == 0))
                {
                    array = new Vector2[fakeUvSize];
                }
                if (i == 1 && array == null ||
                    array.Length == 0 &&
                    room.LightmapType != LightmapExportType.None)
                {
                    Debug.LogWarning("Lightmapping is enabled but mesh has no UV1 channel - " + mesh.name + " - Tick the Generate Lightmap UVs", mesh);
                }
                uvs[i] = array;
            }
            return uvs;
        }

        public override void ExportAssetObjects()
        {
            if (room.ExportOnlyHtml)
            {
                return;
            }

            int exported = 0;

            MeshExporter exporter = room.GetMeshExporter(ExportMeshFormat.FBX);
            bool switchUv = room.LightmapType == LightmapExportType.BakedMaterial;
            MeshExportParameters parameters = new MeshExportParameters(switchUv, true);

            foreach (BruteForceMeshExportData data in meshesToExport.Values)
            {
                Mesh mesh = data.Mesh;

                MeshData meshData = GetMeshData(mesh);
                if (meshData == null)
                {
                    continue;
                }

                string meshId = data.MeshId;
                exporter.ExportMesh(meshData, meshId, parameters);
                exported++;
                EditorUtility.DisplayProgressBar("Exporting meshes...", meshId + ".fbx", exported / (float)meshesToExport.Count);
            }
        }
    }
}
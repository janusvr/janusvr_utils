using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    public class PerMaterialObjectScanner : ObjectScanner
    {
        private Dictionary<Material, PerMaterialMeshExportData> meshesToExport;
        private JanusRoom room;
        private MaterialScanner materialScanner;
        private JanusComponentExtractor compExtractor;
        private Bounds sceneBounds;

        public override void Initialize(JanusRoom room, GameObject[] rootObjects)
        {
            this.room = room;

            materialScanner = new MaterialScanner(room);
            compExtractor = new JanusComponentExtractor(room);

            meshesToExport = new Dictionary<Material, PerMaterialMeshExportData>();

            EditorUtility.DisplayProgressBar("Janus VR Exporter", "Per material scanning for AssetObjects...", 0.0f);
            materialScanner.Initialize();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject root = rootObjects[i];
                RecursiveSearch(root);
            }

            room.FarPlaneDistance = (int)Math.Max(500, sceneBounds.size.magnitude * 1.3f * room.UniformScale);
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
                    if (filter == null || !meshRen.isVisible)
                    {
                        continue;
                    }

                    Mesh mesh = filter.sharedMesh;
                    if (mesh == null ||
                        !room.CanExportObj(comps))
                    {
                        continue;
                    }

                    if (!room.ExportDynamicGameObjects &&
                         !root.isStatic)
                    {
                        continue;
                    }

                    sceneBounds.Encapsulate(meshRen.bounds);

                    Material[] materials = meshRen.sharedMaterials;

                    for (int j = 0; j < materials.Length; j++)
                    {
                        Material mat = materials[j];
                        if (!mat)
                        {
                            continue;
                        }

                        PerMaterialMeshExportData data;
                        if (!meshesToExport.TryGetValue(mat, out data))
                        {
                            data = new PerMaterialMeshExportData();
                            meshesToExport.Add(mat, data);

                            AssetObject asset = new AssetObject();
                            data.Asset = asset;

                            asset.id = mat.name;
                            asset.src = mat.name + ".fbx";
                            room.AddAssetObject(asset);

                            RoomObject obj = new RoomObject();
                            data.Object = obj;
                            obj.id = mat.name;
                            obj.SetNoUnityObj(room);

                            room.AddRoomObject(obj);
                            //compExtractor.ProcessNewRoomObject(obj, comps);
                        }
                        compExtractor.ProcessNewRoomObject(data.Object, comps);

                        materialScanner.PreProcessObject(meshRen, mesh, data.Asset, data.Object, false, j);

                        PerMaterialMeshExportDataObj dObj = new PerMaterialMeshExportDataObj();
                        dObj.MaterialId = j;
                        dObj.Mesh = mesh;
                        dObj.Transform = root.transform;
                        dObj.Renderer = meshRen;
                        data.Meshes.Add(dObj);
                    }
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

        public MeshData GetMeshData(Material mat, PerMaterialMeshExportData data)
        {
            MeshData meshData = new MeshData();
            List<PerMaterialMeshExportDataObj> objs = data.Meshes;

            meshData.Name = mat.name;

            // pre-calc
            List<Vector3> allVertices = new List<Vector3>();
            List<Vector3> allNormals = new List<Vector3>();
            List<int> allTriangles = new List<int>();
            List<List<Vector2>> allUVs = new List<List<Vector2>>();

            int uvLayers = BruteForceObjectScanner.LightmapNeedsUV1(room) ? 2 : 1;
            for (int i = 0; i < uvLayers; i++)
            {
                allUVs.Add(new List<Vector2>());
            }

            for (int i = 0; i < objs.Count; i++)
            {
                PerMaterialMeshExportDataObj obj = objs[i];
                Transform trans = obj.Transform;
                Mesh mesh = obj.Mesh;

                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                int[] triangles = mesh.triangles;

                if (vertices == null || vertices.Length == 0)
                {
                    Debug.LogWarning("Mesh is empty " + mesh.name, mesh);
                    return null;
                }

                string meshPath = AssetDatabase.GetAssetPath(mesh);
                if (!string.IsNullOrEmpty(meshPath))
                {
                    ModelImporter importer = (ModelImporter)ModelImporter.GetAtPath(meshPath);
                    if (importer != null)
                    {
                        meshData.Scale = importer.globalScale;
                    }
                }

                Vector3 localScale = trans.localScale;

                uint vStart = mesh.GetIndexStart(obj.MaterialId);
                uint vEnd = vStart + mesh.GetIndexCount(obj.MaterialId);
                Dictionary<int, int> added = new Dictionary<int, int>();
                Vector2[][] uvs = BruteForceObjectScanner.GetMeshUVs(room, mesh, obj.LightmapEnabled, vertices.Length);

                for (uint j = vStart; j < vEnd; j++)
                {
                    int tri = triangles[j];

                    int vertexIndex;
                    if (added.TryGetValue(tri, out vertexIndex))
                    {
                        allTriangles.Add(vertexIndex);
                        continue;
                    }

                    Vector3 vertex = vertices[tri];
                    Vector3 normal = normals[tri];

                    allTriangles.Add(allVertices.Count);
                    added.Add(tri, allVertices.Count);

                    Vector3 vWorldSpace = trans.TransformPoint(vertex);
                    Vector3 nWorldSpace = trans.TransformDirection(normal);
                    allVertices.Add(vWorldSpace);
                    allNormals.Add(nWorldSpace);

                    for (int k = 0; k < allUVs.Count; k++)
                    {
                        List<Vector2> uvLayer = allUVs[k];
                        Vector2[] uv = uvs[k];
                        Vector2 tvert = uv[tri];
                        if (k == 1)
                        {
                            Vector4 lmap = obj.Renderer.lightmapScaleOffset;
                            tvert.x *= lmap.x;
                            tvert.y *= lmap.y;
                            tvert.x += lmap.z;
                            tvert.y += lmap.w;
                        }
                        uvLayer.Add(tvert);
                    }
                }
            }

            meshData.Vertices = allVertices.ToArray();
            meshData.Normals = allNormals.ToArray();
            meshData.Triangles = allTriangles.ToArray();
            Vector2[][] meshUVs = new Vector2[2][];
            for (int j = 0; j < allUVs.Count; j++)
            {
                meshUVs[j] = allUVs[j].ToArray();
            }
            meshData.UV = meshUVs;

            return meshData;
        }

        public override void ExportAssetObjects()
        {
            if (room.ExportOnlyHtml)
            {
                return;
            }

            int exported = 0;
            MeshExporter exporter = room.GetMeshExporter(ExportMeshFormat.FBX);
            MeshExportParameters parameters = new MeshExportParameters(false, true);

            foreach (var pair in meshesToExport)
            {
                Material mat = pair.Key;
                PerMaterialMeshExportData data = pair.Value;

                MeshData meshData = GetMeshData(mat, data);

                //Mesh mesh = data.Mesh;
                string meshId = meshData.Name;
                exporter.ExportMesh(meshData, meshId, parameters);
                exported++;
                EditorUtility.DisplayProgressBar("Exporting meshes...", meshId + ".fbx", exported / (float)meshesToExport.Count);
            }
        }

    }
}

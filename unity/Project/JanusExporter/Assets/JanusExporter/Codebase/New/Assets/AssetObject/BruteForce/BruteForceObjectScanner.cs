#if UNITY_EDITOR

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
        private bool ignoreInactiveComponents = true;
        private JanusRoom room = null;

        private MaterialScanner materialScanner;

        public bool IgnoreInactiveComponents
        {
            get { return ignoreInactiveComponents; }
        }

        public BruteForceObjectScanner(JanusRoom room)
        {
            this.room = room;
            meshesToExport = new Dictionary<Mesh, BruteForceMeshExportData>();
            // for making sure we have no conflicting names
            meshNames = new List<string>();
            sceneBounds = new Bounds();

            materialScanner = new MaterialScanner(room);
        }

        public override void Initialize(GameObject[] rootObjects)
        {
            EditorUtility.DisplayProgressBar("Janus VR Exporter", "Brute force scanning for AssetObjects...", 0.0f);

            materialScanner.Initialize();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject root = rootObjects[i];
                RecursiveSearch(root);
            }
        }

        public void RecursiveSearch(GameObject root)
        {
            if (IgnoreInactiveComponents &&
                !root.activeInHierarchy)
            {
                return;
            }

            // loop thorugh all the gameObjects components
            // to see what we can extract
            Component[] comps = root.GetComponents<Component>();
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
                        !room.CanExportObj(comps))
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
                    obj.id = exp.Asset;
                    obj.SetUnityObj(root);
                    room.AddRoomObject(obj);

                    // let the material scanner process this object
                    materialScanner.PreProcessObject(meshRen, mesh, exp.Asset, obj);
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

        public override void ExportAssetObjects()
        {
            int exported = 0;

            MeshExporter exporter = room.GetMeshExporter(ExportMeshFormat.FBX);
            bool switchUv = room.LightmapType == LightmapExportType.BakedMaterial;
            MeshExportParameters parameters = new MeshExportParameters(switchUv, true);

            foreach (BruteForceMeshExportData data in meshesToExport.Values)
            {
                Mesh mesh = data.Mesh;
                string meshId = data.MeshId;
                exporter.ExportMesh(mesh, meshId, parameters);
                exported++;
                EditorUtility.DisplayProgressBar("Exporting meshes...", meshId + ".fbx", exported / (float)meshesToExport.Count);
            }
        }
    }
}
#endif
using JanusVR.FBX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    public class JanusRoom
    {
        // assets data
        public List<JanusAsset> AllAssets { get; private set; }
        public List<AssetObject> AssetObjects { get; private set; }
        public List<AssetImage> AssetImages { get; private set; }

        // runtime data
        public List<RoomObject> RoomObjects { get; private set; }
        public List<LinkObject> LinkObjects { get; private set; }

        public List<JanusRoomElement> RoomElements { get; private set; }

        // export
        private SkyboxExporter skyboxExporter;
        private ProbeExporter probeExporter;
        private ObjectScanner objectScanner;
        private JanusRoomWriterXml writer;

        private Dictionary<ExportMeshFormat, MeshExporter> meshExporters;

        public string RootFolder { get; private set; }
        public ExportTextureFormat TextureFormat { get; set; }
        public bool TextureForceReExport { get; set; }
        public bool ExportOnlyHtml { get; set; }

        public float LightmapExposure { get; set; }
        public int LightmapMaxResolution { get; set; }
        public LightmapExportType LightmapType { get; set; }
        public bool SkyboxEnabled { get; set; }
        public int SkyboxResolution { get; set; }
        public object TextureData { get; set; }
        public float UniformScale { get; set; }
        public bool ExportMaterials { get; set; }
        public bool EnvironmentProbeExport { get; set; }
        public int EnvironmentProbeRadResolution { get; set; }
        public int EnvironmentProbeIrradResolution { get; set; }
        public ReflectionProbe EnvironmentProbeOverride { get; set; }

        public AssetImage SkyboxFront { get; set; }
        public AssetImage SkyboxBack { get; set; }
        public AssetImage SkyboxLeft { get; set; }
        public AssetImage SkyboxRight { get; set; }
        public AssetImage SkyboxUp { get; set; }
        public AssetImage SkyboxDown { get; set; }

        public AssetImage CubemapRadiance { get; set; }
        public AssetImage CubemapIrradiance { get; set; }

        public float? FarPlaneDistance { get; set; }

        public Vector3? PortalPos { get; set; }
        public Vector3? PortalXDir { get; set; }
        public Vector3? PortalYDir { get; set; }
        public Vector3? PortalZDir { get; set; }

        public JanusRoom()
        {
            UniformScale = 1;

            AllAssets = new List<JanusAsset>();
            AssetObjects = new List<AssetObject>();
            AssetImages = new List<AssetImage>();

            RoomElements = new List<JanusRoomElement>();
            RoomObjects = new List<RoomObject>();
            LinkObjects = new List<LinkObject>();

            meshExporters = new Dictionary<ExportMeshFormat, MeshExporter>();
            meshExporters.Add(ExportMeshFormat.FBX, new FbxExporter());

            writer = new JanusRoomWriterXml(this);
            skyboxExporter = new SkyboxExporter();
            probeExporter = new ProbeExporter();
        }

        public AssetImage GetTexture(string id)
        {
            return AssetImages.First(c => c.id == id);
        }
        public AssetImage TryGetTexture(string id)
        {
            return AssetImages.FirstOrDefault(c => c.id == id);
        }

        public bool CanExportObj(Component[] comps)
        {
            return !comps.Any(c => (c is JanusVREntryPortal) || (c is JanusVRLink));
        }

        public void AddAssetObject(AssetObject assetObj)
        {
            AllAssets.Add(assetObj);
            AssetObjects.Add(assetObj);
        }

        public void AddAssetImage(AssetImage assetImg)
        {
            AllAssets.Add(assetImg);
            AssetImages.Add(assetImg);
        }

        public void AddRoomObject(RoomObject roomObj)
        {
            RoomObjects.Add(roomObj);
            RoomElements.Add(roomObj);
        }

        public void AddLinkObject(LinkObject linkObj)
        {
            LinkObjects.Add(linkObj);
            RoomElements.Add(linkObj);
        }

        public void Initialize(AssetObjectSearchType type)
        {
            try
            {
                skyboxExporter.Initialize(this);
                probeExporter.Initialize(this);

                GameObject[] root = UnityUtil.GetSceneRoots();
                objectScanner = ObjectScannerFactory.GetObjectScanner(type, this);
                objectScanner.Initialize(root);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public MeshExporter GetMeshExporter(ExportMeshFormat format)
        {
            return meshExporters[format];
        }

        public void PreExport(string rootFolder)
        {
            try
            {
                RootFolder = rootFolder;
                Directory.CreateDirectory(rootFolder);

                foreach (MeshExporter exporter in meshExporters.Values)
                {
                    exporter.Initialize(this);
                }

                skyboxExporter.PreExport();
                probeExporter.PreExport();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void ExportAssetImages()
        {
            try
            {
                skyboxExporter.Export();
                probeExporter.Export();
                objectScanner.ExportAssetImages();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void ExportAssetObjects()
        {
            try
            {
                objectScanner.ExportAssetObjects();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void WriteHtml(string htmlName)
        {
            try
            {
                string path = Path.Combine(RootFolder, htmlName);
                writer.WriteHtml(path);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void Cleanup()
        {
            try
            {
                skyboxExporter.Cleanup();
            }
            finally
            {

            }
        }

    }
}
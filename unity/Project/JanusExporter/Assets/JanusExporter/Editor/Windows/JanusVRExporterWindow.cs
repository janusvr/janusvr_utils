using JanusVR.FBX;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UObject = UnityEngine.Object;

namespace JanusVR
{
    /// <summary>
    /// Main class for the Janus VR Exporter
    /// </summary>
    public class JanusVRExporterWindow : EditorWindow
    {
        private static JanusVRExporterWindow instance;

        /// <summary>
        /// Singleton
        /// </summary>
        public static JanusVRExporterWindow Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// The folder were exporting the scene to
        /// </summary>
        [SerializeField]
        private string exportPath;

        /// <summary>
        /// If we should export inactive gameobjects
        /// </summary>
        [SerializeField]
        private bool exportInactiveObjects;

        /// <summary>
        /// The format to export all textures to
        /// </summary>
        [SerializeField]
        private ExportTextureFormat defaultTexFormat;

        [SerializeField]
        private bool textureForceReExport;

        /// <summary>
        /// The quality to export all textures
        /// </summary>
        [SerializeField]
        private int defaultQuality;

        /// <summary>
        /// An uniform scale to apply to the whole scene (useful for matching VR scale inside janus)
        /// </summary>
        [SerializeField]
        public float uniformScale;

        /// <summary>
        /// The type of lightmaps you want to export
        /// </summary>
        [SerializeField]
        private LightmapExportType lightmapExportType;

        /// <summary>
        /// The maximum resolution a lightmap atlas can have
        /// </summary>
        [SerializeField]
        private int maxLightMapResolution;

        /// <summary>
        /// How much we should expose the lightmap when converting to low dynamic range
        /// </summary>
        [SerializeField]
        private float lightmapExposure;

        /// <summary>
        /// If the exporter should output the materials (if disabled, lightmaps are still exported, so you can take a look at only lightmap
        /// data with a gray tone)
        /// </summary>
        [SerializeField]
        private bool exportMaterials;

        [SerializeField]
        private bool exportTextures;

        /// <summary>
        /// The resolution to render the skybox to, if it's a procedural one
        /// </summary>
        [SerializeField]
        private int exportSkyboxResolution;

        [SerializeField]
        private bool exportSkybox;

        [SerializeField]
        private bool environmentProbeExport = true;
        [SerializeField]
        private int environmentProbeRadResolution = 128;
        [SerializeField]
        private int environmentProbeIrradResolution = 32;
        [SerializeField]
        private ReflectionProbe environmentProbeOverride;

        private GUIStyle errorStyle;

        [NonSerialized]
        private Rect border = new Rect(5, 5, 10, 10);

        public const int PreviewSize = 64;

        public JanusVRExporterWindow()
        {
            instance = this;
        }

        private void OnEnable()
        {
            // search for the icon file
            Texture2D icon = Resources.Load<Texture2D>("janusvricon");

            this.SetWindowTitle("JanusVR", icon);

            errorStyle = new GUIStyle();
            errorStyle.normal.textColor = Color.red;
        }

        [MenuItem("Window/JanusVR Exporter")]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            JanusVRExporterWindow window = EditorWindow.GetWindow<JanusVRExporterWindow>();
            window.Show();

            window.ResetParameters();
        }

        /// <summary>
        /// Resets all possible parameters to their default values
        /// </summary>
        private void ResetParameters()
        {
            string proj = JanusUtil.GetDefaultExportPath();
            exportPath = proj;

            uniformScale = 1;
            defaultTexFormat = ExportTextureFormat.JPG;
            defaultQuality = 70;

            exportTextures = true;
            exportMaterials = true;
            exportSkyboxResolution = 1024;
            exportInactiveObjects = false;

            lightmapExportType = LightmapExportType.PackedSourceEXR;
            lightmapExposureVisible = true;
            lightmapExposure = 0;

            scrollPos = Vector2.zero;

            maxLightMapResolution = 2048;

            environmentProbeExport = false;
            environmentProbeRadResolution = 128;
            environmentProbeIrradResolution = 32;
            environmentProbeOverride = null;
        }

        public static bool NeedsLDRConversion(LightmapExportType type)
        {
            switch (type)
            {
                case LightmapExportType.BakedMaterial:
                case LightmapExportType.Packed:
                case LightmapExportType.Unpacked:
                    return true;
                default:
                    return false;
            }
        }

        [NonSerialized]
        private Vector2 scrollPos;

        [NonSerialized]
        private Vector2 meshScrollPos;

        [SerializeField]
        private bool lightmapExposureVisible = true;

        [SerializeField]
        private AssetObjectSearchType searchType;

        private JanusRoom room;
        private bool meshPreviewShow;
        private int meshPreviewWidth;

        private void OnGUI()
        {
            Rect rect = this.position;
            Rect showArea = new Rect(border.x, border.y, rect.width - border.width, rect.height - border.height);

            GUILayout.BeginArea(showArea);
            scrollPos = GUILayout.BeginScrollView(scrollPos);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Janus Exporter " + (JanusGlobals.Version).ToString("F2"), EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            // Asset Objects (Models)
            GUILayout.Label("Asset Objects", EditorStyles.boldLabel);
            searchType = (AssetObjectSearchType)EditorGUILayout.EnumPopup("Search Type", searchType);
            //meshFormat = (ExportMeshFormat)EditorGUILayout.EnumPopup("Mesh Format", meshFormat);

            //meshPreviewShow = EditorGUILayout.Foldout(meshPreviewShow, "Asset Objects Extracted");
            meshPreviewShow = false;
            if (meshPreviewShow)
            {
                Color defaultColor = GUI.color;

                int previewHeight = 200;
                Rect area = GUILayoutUtility.GetRect(rect.width - border.width, previewHeight);
                Rect meshPreviewFullArea = area;
                meshPreviewFullArea.width = meshPreviewWidth;
                meshPreviewFullArea.height -= 20;
                meshScrollPos = GUI.BeginScrollView(area, meshScrollPos, meshPreviewFullArea);

                GUI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                GUI.DrawTexture(new Rect(area.x + meshScrollPos.x, area.y, area.width, area.height), EditorGUIUtility.whiteTexture);
                GUI.color = new Color(0, 0, 0, 1f);
                GUI.DrawTexture(new Rect(area.x + meshScrollPos.x, area.y, area.width, 1), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(area.x + meshScrollPos.x, area.y + area.height - 1, area.width, 1), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(area.x + meshScrollPos.x, area.y, 1, area.height), EditorGUIUtility.whiteTexture);
                GUI.DrawTexture(new Rect(area.x + area.width - 1 + meshScrollPos.x, area.y, 1, area.height), EditorGUIUtility.whiteTexture);
                GUI.color = defaultColor;

                if (room != null)
                {
                    List<RoomObject> objects = room.RoomObjects;
                    int objUiSize = 80;
                    int objUiBorder = 5;
                    int columns = previewHeight / objUiSize;

                    meshPreviewWidth = objects.Count * (objUiSize + objUiBorder);
                    int rowSize = meshPreviewWidth / columns;
                    meshPreviewWidth = rowSize + objUiSize;

                    for (int i = 0; i < objects.Count; i++)
                    {
                        RoomObject obj = objects[i];
                        if (obj.Preview == null)
                        {
                            obj.Preview = AssetPreview.GetMiniThumbnail(obj.UnityObj);
                            objects[i] = obj;
                        }

                        float x = area.x + (i * (objUiSize + objUiBorder));
                        int lineIndex = (int)(x / rowSize);
                        float y = area.y + objUiBorder + (lineIndex * objUiSize);
                        x -= lineIndex * rowSize;

                        Rect display = new Rect(x, y, objUiSize, objUiSize);
                        GUI.DrawTexture(display, obj.Preview);
                    }
                }

                GUI.EndScrollView();
            }

            float scale = EditorGUILayout.FloatField("Uniform Scale", uniformScale);
            if (uniformScale != scale)
            {
                uniformScale = scale;
                // update the scale on all possible Janus objects on screen
                JanusGlobals.UpdateScale(uniformScale);
            }

            // Main Parameters
            GUILayout.Label("Main", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Export Path");
            //exportPath = EditorGUILayout.TextField(exportPath);
            EditorGUILayout.LabelField(exportPath);
            if (GUILayout.Button("..."))
            {
                // search for a folder
                string newExportPath = EditorUtility.SaveFolderPanel("JanusVR Export Folder", "", "");
                if (!string.IsNullOrEmpty(newExportPath))
                {
                    exportPath = newExportPath;
                }
            }
            EditorGUILayout.EndHorizontal();

            // Texture
            GUILayout.Label("Texture", EditorStyles.boldLabel);
            textureForceReExport = EditorGUILayout.Toggle("Force ReExport", textureForceReExport);
            //exportGifs = EditorGUILayout.Toggle("Export GIFs", exportGifs);
            defaultTexFormat = (ExportTextureFormat)EditorGUILayout.EnumPopup("Unsupported Textures Format", defaultTexFormat);
            if (JanusUtil.SupportsQuality(defaultTexFormat))
            {
                defaultQuality = EditorGUILayout.IntSlider("Textures Quality", defaultQuality, 0, 100);
            }

            // Scene
            GUILayout.Label("Scene", EditorStyles.boldLabel);

            exportInactiveObjects = EditorGUILayout.Toggle("Export Inactive Objects", exportInactiveObjects);
            exportTextures = EditorGUILayout.Toggle("Export Textures", exportTextures);
            exportMaterials = EditorGUILayout.Toggle("Export Materials", exportMaterials);
            
            EditorGUILayout.LabelField("    Useful for testing lighting results in Janus");

            exportSkybox = EditorGUILayout.Toggle("Export Skybox", exportSkybox);
            if (exportSkybox && UnityUtil.IsProceduralSkybox())
            {
                exportSkyboxResolution = Math.Max(4, EditorGUILayout.IntField("Skybox Render Resolution", exportSkyboxResolution));
            }
            EditorGUILayout.LabelField("    Will render the Skybox into 6 textures with the specified resolution");

            // Probes
            GUILayout.Label("Probes", EditorStyles.boldLabel);
            environmentProbeExport = EditorGUILayout.Toggle("Export Environment Probes", environmentProbeExport);
            if (environmentProbeExport)
            {
                environmentProbeOverride = EditorGUILayout.ObjectField(environmentProbeOverride, typeof(ReflectionProbe), true) as ReflectionProbe;
                environmentProbeRadResolution = Math.Max(4, EditorGUILayout.IntField("Probe Radiance Resolution", environmentProbeRadResolution));
                environmentProbeIrradResolution = Math.Max(4, EditorGUILayout.IntField("Probe Irradiance Resolution", environmentProbeIrradResolution));
            }

            // Lightmap
            GUILayout.Label("Lightmaps", EditorStyles.boldLabel);
            lightmapExportType = (LightmapExportType)EditorGUILayout.EnumPopup("Lightmap Type", lightmapExportType);

            switch (lightmapExportType)
            {
                case LightmapExportType.None:
                    EditorGUILayout.LabelField("    No lightmaps are going to be exported");
                    break;
                case LightmapExportType.Packed:
                    EditorGUILayout.LabelField("    Converts the source EXR files to Low-Dynamic Range");
                    break;
                case LightmapExportType.PackedSourceEXR:
                    EditorGUILayout.LabelField("    Copies the source EXR High-Dynamic Range lightmaps");
                    EditorGUILayout.LabelField("    directly into the exported project (experimental)");
                    break;
                case LightmapExportType.BakedMaterial:
                    EditorGUILayout.LabelField("    Bakes the lightmap into the material (for testing purposes)");
                    break;
                case LightmapExportType.Unpacked:
                    EditorGUILayout.LabelField("    Converts the source EXR files to Low-Dynamic Range and unpacks");
                    EditorGUILayout.LabelField("    into individual textures (for testing purposes)");
                    break;
            }

            if (lightmapExportType != LightmapExportType.None && lightmapExportType != LightmapExportType.PackedSourceEXR)
            {
                maxLightMapResolution = Math.Max(4, EditorGUILayout.IntField("Max Lightmap Resolution", maxLightMapResolution));
            }
            if (NeedsLDRConversion(lightmapExportType))
            {
                lightmapExposure = EditorGUILayout.Slider("Lightmap Exposure", lightmapExposure, -5, 5);

                lightmapExposureVisible = EditorGUILayout.Foldout(lightmapExposureVisible, "Preview Exposure");
                //if (lightmapExposureVisible)
                //{
                //    MakePreviewExportData();
                //    exported.Lightmaps.BuildPreview(lightmapExportType, lightmapExposure);

                //    Texture2D preview = exported.Lightmaps.Preview;
                //    if (preview)
                //    {
                //        int texSize = (int)(showArea.width * 0.4);
                //        Rect tex = GUILayoutUtility.GetRect(texSize, texSize + 30);

                //        GUI.Label(new Rect(tex.x + 20, tex.y + 5, texSize, texSize), "Lightmap Preview");
                //        GUI.DrawTexture(new Rect(tex.x + 20, tex.y + 30, texSize, texSize), preview);

                //        if (previewWindow)
                //        {
                //            previewWindow.Tex = preview;
                //            previewWindow.Repaint();
                //        }

                //        if (GUI.Button(new Rect(tex.x + texSize - 60, tex.y, 80, 20), "Preview"))
                //        {
                //            previewWindow = EditorWindow.GetWindow<LightmapPreviewWindow>();
                //            previewWindow.Show();
                //        }
                //    }
                //    else
                //    {
                //        GUILayout.Label("No Lightmap Preview");
                //    }
                //}
            }

            GUILayout.FlexibleSpace();

//            if (exported != null)
//            {
//                // Exported
//                GUILayout.Label("Exported", EditorStyles.boldLabel);

//                GUILayout.Label("Scene size " + sceneSize.size);
//                if (farPlaneDistance < 500)
//                {
//                    GUILayout.Label("Far Plane " + farPlaneDistance.ToString("F2") + " (Exported as 500)");
//                }
//                else
//                {
//                    GUILayout.Label("Far Plane " + farPlaneDistance.ToString("F2"));
//                }

//                Cubemap cubemap = exported.environmentCubemap;
//                if (cubemap == null)
//                {
//#if UNITY_5_0
//                    GUILayout.Label("Environment Probe: Not supported on Unity 5.0", errorStyle);
//#elif UNITY_5_3
//                    GUILayout.Label("Environment Probe: On Unity 5.3 can only be exported if set as cubemap on the Lighting window", errorStyle);
//#else
//                    GUILayout.Label("Environment Probe: None (need baked lightmaps)", errorStyle);
//#endif
//                }
//                else
//                {
//                    GUILayout.Label("Environment Probe: " + cubemap.width);
//                }
//            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export HTML only"))
            {
                try
                {
                    Export(true);
                }
                catch (Exception ex)
                {
                    Debug.Log("Error exporting: " + ex.Message);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            if (GUILayout.Button("Reset Parameters"))
            {
                ResetParameters();
            }

            if (!string.IsNullOrEmpty(exportPath) &&
                Directory.Exists(exportPath) &&
                GUILayout.Button("Show In Explorer"))
            {
                UnityUtil.StartProcess(exportPath);
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(exportPath))
            {
                if (GUILayout.Button("Full Export", GUILayout.Height(30)))
                {
                    if (lightmapExportType == LightmapExportType.Unpacked)
                    {
                        Debug.LogError("Unpacked is unsupported right now.");
                        return;
                    }

                    //try
                    {
                        Export(false);
                    }
                    //catch (Exception ex)
                    {
                        //Debug.Log("Error exporting: " + ex.Message);
                    }
                    //finally
                    {
                        // delete room
                        room = null;
                    }
                    EditorUtility.ClearProgressBar();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Export(bool onlyHtml)
        {
            room = new JanusRoom();
            room.ExportOnlyHtml = onlyHtml;
            CopyParametersToRoom();
            room.Initialize(searchType);
            room.PreExport(exportPath);
            room.ExportAssetImages();
            room.ExportAssetObjects();
            room.WriteHtml("index.html");
            room.Cleanup();
        }

        private void CopyParametersToRoom()
        {
            room.IgnoreInactiveObjects = !exportInactiveObjects;
            room.EnvironmentProbeExport = environmentProbeExport;
            room.EnvironmentProbeIrradResolution = environmentProbeIrradResolution;
            room.EnvironmentProbeRadResolution = environmentProbeRadResolution;
            room.EnvironmentProbeOverride = environmentProbeOverride;
            room.ExportMaterials = exportMaterials;
            room.ExportTextures = exportTextures;
            room.LightmapExposure = lightmapExposure;
            room.LightmapMaxResolution = maxLightMapResolution;
            room.LightmapType = lightmapExportType;
            room.SkyboxEnabled = exportSkybox;
            room.SkyboxResolution = exportSkyboxResolution;
            room.TextureData = defaultQuality;
            room.TextureFormat = defaultTexFormat;
            room.TextureForceReExport = textureForceReExport;
            room.UniformScale = uniformScale;
        }
    }
}
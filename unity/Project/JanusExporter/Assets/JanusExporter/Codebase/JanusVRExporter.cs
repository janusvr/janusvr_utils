#if UNITY_EDITOR

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
    public class JanusVRExporter : EditorWindow
    {
        private static JanusVRExporter instance;

        /// <summary>
        /// Singleton
        /// </summary>
        public static JanusVRExporter Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// The folder were exporting the scene to
        /// </summary>
        [SerializeField]
        private string exportPath;

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
        /// The format to export all meshes in
        /// </summary>
        [SerializeField]
        private ExportMeshFormat meshFormat;

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

        /// <summary>
        /// Export all textures that are GIFs as actual GIFs
        /// </summary>
        [SerializeField]
        private bool exportGifs;

        /// <summary>
        /// Exports the scene's skybox
        /// </summary>
        [SerializeField]
        private bool exportSkybox;

        /// <summary>
        /// Exports the scene's reflection probes
        /// </summary>
        //private bool exportProbes;

        /// <summary>
        /// The resolution to render the skybox to, if it's a procedural one
        /// </summary>
        [SerializeField]
        private int exportSkyboxResolution;

        [SerializeField]
        private bool environmentProbeExport = true;
        [SerializeField]
        private int environmentProbeRadResolution = 128;
        [SerializeField]
        private int environmentProbeIrradResolution = 32;
        [SerializeField]
        private ReflectionProbe environmentProbeOverride;

        [NonSerialized]
        private SceneExportData exported;

        private bool updateOnlyHtml = false;
        private GUIStyle errorStyle;

        /// <summary>
        /// The farplane distance of the camera
        /// </summary>
        private float farPlaneDistance = 1000;

        private Bounds sceneSize;

        [NonSerialized]
        private Rect border = new Rect(5, 5, 10, 10);

        public const int PreviewSize = 64;

        public JanusVRExporter()
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
            JanusVRExporter window = EditorWindow.GetWindow<JanusVRExporter>();
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

            meshFormat = ExportMeshFormat.FBX;
            uniformScale = 1;
            defaultTexFormat = ExportTextureFormat.JPG;
            defaultQuality = 70;

            exportGifs = true;
            exportMaterials = true;
            exportSkybox = true;
            exportSkyboxResolution = 1024;

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

        private void MakePreviewExportData()
        {
            if (exported == null)
            {
                exported = new SceneExportData();
                exported.IsPreview = true;
            }
        }

        private void PreviewRefresh()
        {
            if (exported != null && exported.IsPreview)
            {
                exported = null;
            }
        }

        private LightmapPreviewWindow previewWindow;
        private JanusRoom room;
        private bool meshPreviewShow;
        private int meshPreviewWidth;

        [SerializeField]
        private AssetObjectSearchType searchType;

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
            environmentProbeExport = EditorGUILayout.Toggle("Export Environment Probes", exportMaterials);
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
                if (lightmapExposureVisible)
                {
                    MakePreviewExportData();
                    exported.Lightmaps.BuildPreview(lightmapExportType, lightmapExposure);

                    Texture2D preview = exported.Lightmaps.Preview;
                    if (preview)
                    {
                        int texSize = (int)(showArea.width * 0.4);
                        Rect tex = GUILayoutUtility.GetRect(texSize, texSize + 30);

                        GUI.Label(new Rect(tex.x + 20, tex.y + 5, texSize, texSize), "Lightmap Preview");
                        GUI.DrawTexture(new Rect(tex.x + 20, tex.y + 30, texSize, texSize), preview);

                        if (previewWindow)
                        {
                            previewWindow.Tex = preview;
                            previewWindow.Repaint();
                        }

                        if (GUI.Button(new Rect(tex.x + texSize - 60, tex.y, 80, 20), "Preview"))
                        {
                            previewWindow = EditorWindow.GetWindow<LightmapPreviewWindow>();
                            previewWindow.Show();
                        }
                    }
                    else
                    {
                        GUILayout.Label("No Lightmap Preview");
                    }
                }
            }

            GUILayout.FlexibleSpace();

            if (exported != null)
            {
                // Exported
                GUILayout.Label("Exported", EditorStyles.boldLabel);

                GUILayout.Label("Scene size " + sceneSize.size);
                if (farPlaneDistance < 500)
                {
                    GUILayout.Label("Far Plane " + farPlaneDistance.ToString("F2") + " (Exported as 500)");
                }
                else
                {
                    GUILayout.Label("Far Plane " + farPlaneDistance.ToString("F2"));
                }

                Cubemap cubemap = exported.environmentCubemap;
                if (cubemap == null)
                {
#if UNITY_5_0
                    GUILayout.Label("Environment Probe: Not supported on Unity 5.0", errorStyle);
#elif UNITY_5_3
                    GUILayout.Label("Environment Probe: On Unity 5.3 can only be exported if set as cubemap on the Lighting window", errorStyle);
#else
                    GUILayout.Label("Environment Probe: None (need baked lightmaps)", errorStyle);
#endif
                }
                else
                {
                    GUILayout.Label("Environment Probe: " + cubemap.width);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export HTML only"))
            {
                //try
                {
                    Export(true);
                }
                //catch
                {
                    //Debug.Log("Error exporting");
                }
                //finally
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

                    try
                    {
                        Export(false);
                    }
                    catch
                    {
                        Debug.Log("Error exporting");
                    }
                    finally
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
            room.LightmapMaxResolution = maxLightMapResolution;
            room.LightmapExposure = lightmapExposure;
            room.LightmapType = lightmapExportType;
            room.SkyboxEnabled = exportSkybox;
            room.SkyboxResolution = exportSkyboxResolution;
            room.TextureData = defaultQuality;
            room.TextureFormat = defaultTexFormat;
            room.TextureForceReExport = textureForceReExport;
            room.UniformScale = uniformScale;
            room.EnvironmentProbeExport = environmentProbeExport;
            room.EnvironmentProbeIrradResolution = environmentProbeIrradResolution;
            room.EnvironmentProbeRadResolution = environmentProbeRadResolution;
        }

        private void RecursiveSearch(GameObject root, SceneExportData data)
        {
            //if (!root.activeInHierarchy)
            //{
            //    return;
            //}

            //Component[] comps = root.GetComponents<Component>();

            //for (int i = 0; i < comps.Length; i++)
            //{
            //    Component comp = comps[i];
            //    if (comp == null)
            //    {
            //        continue;
            //    }

            //    if (comp is MeshCollider)
            //    {
            //        MeshCollider col = (MeshCollider)comp;

            //        ExportedObject exp = data.exportedObjs.FirstOrDefault(c => c.GameObject == root);
            //        if (exp == null)
            //        {
            //            exp = new ExportedObject();
            //            exp.GameObject = root;
            //            data.exportedObjs.Add(exp);
            //        }
            //        exp.MeshCol = col;
            //    }
            //    else if (comp is ReflectionProbe)
            //    {
            //        ReflectionProbe probe = (ReflectionProbe)comp;

            //        ExportedObject exp = new ExportedObject();
            //        exp.GameObject = root;
            //        exp.Texture = probe.texture;
            //        exp.ReflectionProbe = probe;

            //        data.exportedReflectionProbes.Add(exp);
            //    }
            //    else if (comp is JanusVREntryPortal)
            //    {
            //        JanusVREntryPortal portal = (JanusVREntryPortal)comp;
            //        data.entryPortal = portal;
            //    }
            //    else if (comp is JanusVRLink)
            //    {
            //        JanusVRLink link = (JanusVRLink)comp;
            //        data.exportedLinks.Add(link);

            //        Material mat = link.meshRenderer.sharedMaterial;
            //        Texture tex = mat.mainTexture;
            //        if (tex != null)
            //        {
            //            Texture2D texture = (Texture2D)tex;
            //            if (!texturesExported.Contains(texture))
            //            {
            //                texturesExported.Add(texture);
            //            }

            //            link.texture = texture;
            //        }
            //    }
            //}

            //// loop through all this GameObject children
            //foreach (Transform child in root.transform)
            //{
            //    RecursiveSearch(child.gameObject, data);
            //}
        }

        private void Clean()
        {
            //skyBoxLeft = null;
            //skyBoxRight = null;
            //skyBoxForward = null;
            //skyBoxBack = null;
            //skyBoxUp = null;
            //skyBoxDown = null;

            //sceneSize = new Bounds();

            //if (texturesExportedData != null)
            //{
            //    for (int i = 0; i < texturesExportedData.Count; i++)
            //    {
            //        TextureExportData tex = texturesExportedData[i];

            //        if (tex.Created)
            //        {
            //            // we made this, we delete this
            //            UObject.DestroyImmediate(tex.Texture);
            //        }

            //        if (tex.Preview)
            //        {
            //            string path = AssetDatabase.GetAssetPath(tex.Preview);
            //            if (string.IsNullOrEmpty(path))
            //            {
            //                // make sure we didnt just copy the Preview because it was the same resolution
            //                // as the requested preview
            //                UObject.DestroyImmediate(tex.Preview);
            //            }
            //        }
            //    }
            //}
            //exported = null;
        }

        private TextureExportData GenerateCmftRad(Cubemap cubemap, string forceName = "")
        {
            string path = AssetDatabase.GetAssetPath(cubemap);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // remove assets
            path = path.Remove(0, path.IndexOf('/'));

            string appPath = Application.dataPath;
            string fullPath = appPath + path;
            string name = forceName;
            if (string.IsNullOrEmpty(name))
            {
                name = cubemap.name + "_radiance";
            }
            string radPath = Path.Combine(exportPath, name);

            string dstFaceSize = "256";
            string excludeBase = "false";
            string mipCount = "9";
            string glossScale = "10";
            string glossBias = "1";
            string lightingModel = "phongbrdf";
            string inputGammaNumerator = "1.0";
            string inputGammaDenominator = "1.0";
            string outputGammaNumerator = "1.0";
            string outputGammaDenominator = "1.0";
            string generateMipChain = "false";
            string numCpuProcessingThreads = "1";

            StringBuilder builder = new StringBuilder();
            builder.Append("--input \"" + fullPath + "\"");
            builder.Append(" --srcFaceSize " + cubemap.width);
            builder.Append(" --filter radiance");
            builder.Append(" --dstFaceSize " + dstFaceSize);
            builder.Append(" --excludeBase " + excludeBase);
            builder.Append(" --mipCount " + mipCount);
            builder.Append(" --glossBias " + glossBias);
            builder.Append(" --glossScale " + glossScale);
            builder.Append(" --lightingModel " + lightingModel);
            builder.Append(" --numCpuProcessingThreads " + numCpuProcessingThreads);
            builder.Append(" --inputGammaNumerator " + inputGammaNumerator);
            builder.Append(" --inputGammaDenominator " + inputGammaDenominator);
            builder.Append(" --outputGammaNumerator " + outputGammaNumerator);
            builder.Append(" --outputGammaDenominator " + outputGammaDenominator);
            builder.Append(" --generateMipChain " + generateMipChain);
            builder.Append(" --outputNum 1");
            builder.Append(" --output0 \"" + radPath + "\"");
            builder.Append(" --output0params dds,bgra8,cubemap");
            string cmd = builder.ToString();

            // we refer by namespace so Unity never really imports CMFT on Unity 5.0
            CMFT.CmftInterop.DoExecute(cmd);

            TextureExportData data = new TextureExportData();
            data.ExportedPath = name + ".dds";
            return data;
        }
        private TextureExportData GenerateCmftIrrad(Cubemap cubemap, string forceName = "")
        {
            string path = AssetDatabase.GetAssetPath(cubemap);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // remove assets
            path = path.Remove(0, path.IndexOf('/'));

            string appPath = Application.dataPath;
            string fullPath = appPath + path;
            string name = forceName;
            if (string.IsNullOrEmpty(name))
            {
                name = cubemap.name + "_irradiance";
            }
            string irradPath = Path.Combine(exportPath, name);

            string cmd = "--input \"" + fullPath + "\" --srcFaceSize " + cubemap.width + " --filter irradiance --outputNum 1 --output0 \"" + irradPath + "\" --output0params dds,bgra8,cubemap";
            CMFT.CmftInterop.DoExecute(cmd);

            TextureExportData data = new TextureExportData();
            data.ExportedPath = name + ".dds";
            return data;
        }

        private void DoExport()
        {
            //float farPlane = Math.Max(farPlaneDistance, 500);
            //writer.WriteAttributeString("far_dist", ((int)farPlane).ToString(culture));

            //if (exported.entryPortal != null)
            //{
            //    JanusVREntryPortal portal = exported.entryPortal;
            //    Transform portalTransform = portal.transform;

            //    Vector3 portalPos = JanusUtil.ConvertPosition(portal.GetJanusPosition(), uniformScale);
            //    Vector3 xDir, yDir, zDir;
            //    JanusUtil.GetJanusVectors(portalTransform, out xDir, out yDir, out zDir);

            //    writer.WriteAttributeString("pos", JanusUtil.FormatVector3(portalPos));
            //    writer.WriteAttributeString("xdir", JanusUtil.FormatVector3(xDir));
            //    writer.WriteAttributeString("ydir", JanusUtil.FormatVector3(yDir));
            //    writer.WriteAttributeString("zdir", JanusUtil.FormatVector3(zDir));
            //}

            //for (int i = 0; i < exported.exportedLinks.Count; i++)
            //{
            //    JanusVRLink link = exported.exportedLinks[i];
            //    Transform trans = link.transform;

            //    Vector3 pos = JanusUtil.ConvertPosition(link.GetJanusPosition(), uniformScale);
            //    Vector3 sca = trans.localScale;
            //    Vector3 xDir, yDir, zDir;
            //    JanusUtil.GetJanusVectors(trans, out xDir, out yDir, out zDir);

            //    writer.WriteStartElement("Link");
            //    writer.WriteAttributeString("pos", JanusUtil.FormatVector3(pos));
            //    writer.WriteAttributeString("col", JanusUtil.FormatColor(link.Color));
            //    writer.WriteAttributeString("scale", JanusUtil.FormatVector3(sca));
            //    writer.WriteAttributeString("url", link.url);
            //    writer.WriteAttributeString("title", link.title);

            //    if (link.texture != null)
            //    {
            //        string linkTex = Path.GetFileNameWithoutExtension(texturesExportedData.First(c => c.Texture == link.texture).ExportedPath);
            //        writer.WriteAttributeString("thumb_id", linkTex);
            //    }

            //    writer.WriteAttributeString("xdir", JanusUtil.FormatVector3(xDir));
            //    writer.WriteAttributeString("ydir", JanusUtil.FormatVector3(xDir));
            //    writer.WriteAttributeString("zdir", JanusUtil.FormatVector3(zDir));
            //    writer.WriteEndElement();
            //}

            //for (int i = 0; i < exported.exportedObjs.Count; i++)
            //{
            //    ExportedObject obj = exported.exportedObjs[i];
            //    GameObject go = obj.GameObject;

            //    string diffuseID = "";
            //    string lmapID = "";
            //    TextureExportData lightmap = null;

            //    if (obj.DiffuseMapTex != null)
            //    {
            //        diffuseID = Path.GetFileNameWithoutExtension(texturesExportedData.First(k => k.Texture == obj.DiffuseMapTex).ExportedPath);
            //    }
            //    if (obj.LightMapTex != null)
            //    {
            //        lightmap = texturesExportedData.First(k => k.Texture == obj.LightMapTex);
            //        lmapID = Path.GetFileNameWithoutExtension(lightmap.ExportedPath);
            //    }

            //    Mesh mesh = obj.Mesh;
            //    if (mesh == null)
            //    {
            //        continue;
            //    }

            //    string meshName = meshesNames[mesh];

            //    writer.WriteStartElement("Object");
            //    writer.WriteAttributeString("id", meshName);
            //    writer.WriteAttributeString("lighting", "true");

            //    if (!string.IsNullOrEmpty(diffuseID))
            //    {
            //        writer.WriteAttributeString("image_id", diffuseID);
            //    }

            //    if (lightmap != null)
            //    {
            //        writer.WriteAttributeString("lmap_id", lmapID);
            //        if (lightmapExportType == LightmapExportType.Packed ||
            //            lightmapExportType == LightmapExportType.PackedSourceEXR)
            //        {
            //            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            //            Vector4 lmap = renderer.lightmapScaleOffset;
            //            lmap.x = Mathf.Clamp(lmap.x, -2, 2);
            //            lmap.y = Mathf.Clamp(lmap.y, -2, 2);
            //            lmap.z = Mathf.Clamp(lmap.z, -2, 2);
            //            lmap.w = Mathf.Clamp(lmap.w, -2, 2);

            //            // use a higher precision output (6 cases)
            //            writer.WriteAttributeString("lmap_sca", JanusUtil.FormatVector4(lmap, 6));
            //        }
            //    }

            //    if (obj.Tiling != null)
            //    {
            //        Vector4 tiling = obj.Tiling.Value;
            //        writer.WriteAttributeString("tile", JanusUtil.FormatVector4(tiling));
            //    }

            //    if (obj.MeshCol != null)
            //    {
            //        writer.WriteAttributeString("collision_id", meshName);
            //    }

            //    if (obj.Color != null)
            //    {
            //        Color objColor = obj.Color.Value;
            //        writer.WriteAttributeString("col", JanusUtil.FormatColor(objColor));
            //    }

            //    Transform trans = go.transform;
            //    Vector3 pos = trans.position;
            //    pos *= uniformScale;
            //    pos.x *= -1;

            //    Quaternion rot = trans.rotation;
            //    Vector3 xDir = rot * Vector3.right;
            //    Vector3 yDir = rot * Vector3.up;
            //    Vector3 zDir = rot * Vector3.forward;
            //    xDir.x *= -1;
            //    yDir.x *= -1;
            //    zDir.x *= -1;

            //    Vector3 sca = trans.lossyScale;
            //    sca *= uniformScale;

            //    writer.WriteAttributeString("pos", JanusUtil.FormatVector3(pos));
            //    if (sca.x < 0 || sca.y < 0 || sca.z < 0)
            //    {
            //        writer.WriteAttributeString("cull_face", "front");
            //    }

            //    writer.WriteAttributeString("scale", JanusUtil.FormatVector3(sca));
            //    writer.WriteAttributeString("xdir", JanusUtil.FormatVector3(xDir));
            //    writer.WriteAttributeString("ydir", JanusUtil.FormatVector3(yDir));
            //    writer.WriteAttributeString("zdir", JanusUtil.FormatVector3(zDir));

            //    writer.WriteEndElement();
            //}

            //writer.WriteEndDocument();
            //writer.Close();
            //writer.Flush();
            //string indexPath = Path.Combine(exportPath, "index.html");

            //UnityUtil.WriteAllText(indexPath, builder.ToString());


            //EditorUtility.ClearProgressBar();
        }
    }
}
#endif
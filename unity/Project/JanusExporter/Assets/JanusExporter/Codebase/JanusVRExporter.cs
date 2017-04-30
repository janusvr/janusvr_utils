#if UNITY_EDITOR
using JanusVR.FBX;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UObject = UnityEngine.Object;
using System.Xml;

#if UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace JanusVR
{
    /// <summary>
    /// Main class for the Janus VR Exporter
    /// </summary>
    public class JanusVRExporter : EditorWindow
    {
        private static JanusVRExporter instance;
        private static List<IJanusObject> objects;

        static JanusVRExporter()
        {
            objects = new List<IJanusObject>();
        }

        public static bool UpdateOnlyHTML { get; private set; }

        /// <summary>
        /// Singleton
        /// </summary>
        public static JanusVRExporter Instance
        {
            get { return instance; }
        }

        public static void AddObject(IJanusObject obj)
        {
            objects.Add(obj);
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

        /// <summary>
        /// The quality to export all textures
        /// </summary>
        [SerializeField]
        private int defaultQuality;

        /// <summary>
        /// The filtering mode to use when downsampling textures
        /// </summary>
        [SerializeField]
        private TextureFilterMode filterMode = TextureFilterMode.Nearest;

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
        private bool exportProbes;

        /// <summary>
        /// The resolution to render the skybox to, if it's a procedural one
        /// </summary>
        [SerializeField]
        private int exportSkyboxResolution;

        /// <summary>
        /// Compress the scene models using GZip (WIP and extremely slow)
        /// </summary>
        [SerializeField]
        private bool compressFiles = false;

        /// <summary>
        /// Lower case values that the exporter will consider for being the Main Texture on a shader
        /// </summary>
        private string[] mainTexSemantics = new string[]
        {
            "_maintex"
        };

        /// <summary>
        /// Lower case values that the exporter will consider for being the Tiling
        /// </summary>
        private string[] tilingSemantics = new string[]
        {
            "_maintex_st"
        };

        /// <summary>
        /// Lower case values that the exporter will consider for being the Color off a shader
        /// </summary>
        private string[] colorSemantics = new string[]
        {
            "_color"
        };

        /// <summary>
        /// Lower case values that the exporter will consider for the shader using transparent textures
        /// </summary>
        private string[] transparentSemantics = new string[]
        {
            "transparent"
        };

        /// <summary>
        /// The semantic names for all the skybox 6-sided faces
        /// </summary>
        private string[] skyboxTexNames = new string[]
        {
            "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex"
        };

        private Dictionary<ExportMeshFormat, MeshExporter> meshExporters;

        private Dictionary<int, List<GameObject>> lightmapped;

        private List<Texture2D> texturesExported;
        private List<TextureExportData> texturesExportedData;

        private List<Mesh> meshesExported;
        private List<MeshExportData> meshesExportedData;
        private Dictionary<Mesh, string> meshesNames;
        private int meshesCount;

        private ExportedData exported;

        private bool updateOnlyHtml = false;
        private GUIStyle errorStyle;

        /// <summary>
        /// The farplane distance of the camera
        /// </summary>
        private float farPlaneDistance = 1000;

        private Bounds sceneSize;

        [NonSerialized]
        private Rect border = new Rect(10, 5, 20, 15);

        public const int PreviewSize = 64;

        internal class ExportedData
        {
            internal List<ExportedObject> exportedObjs;

            internal List<JanusVRLink> exportedLinks;

            internal JanusVREntryPortal entryPortal;

            internal Cubemap environmentCubemap;
            internal List<ExportedObject> exportedReflectionProbes;

            internal ExportedData()
            {
                exportedObjs = new List<ExportedObject>();
                exportedReflectionProbes = new List<ExportedObject>();

                exportedLinks = new List<JanusVRLink>();
            }
        }

        public JanusVRExporter()
        {
            instance = this;

            meshExporters = new Dictionary<ExportMeshFormat, MeshExporter>();
            meshExporters.Add(ExportMeshFormat.FBX, new FbxExporter());

            //EditorApplication.update += UnityConnection;
        }

        private void UnityConnection()
        {
            EditorApplication.update -= UnityConnection;

            bool shownWelcome = EditorPrefs.GetBool("__JanusVR.Welcome");
            if (!shownWelcome)
            {
                EditorPrefs.SetBool("__JanusVR.Welcome", true);
                JanusVRWelcome.ShowWindow();
            }
        }

        private void OnEnable()
        {
            // search for the icon file
            Texture2D icon = Resources.Load<Texture2D>("janusvricon");
            this.titleContent = new GUIContent("Janus", icon);

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
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string workspace = Path.Combine(documents, @"JanusVR\workspaces");
            string proj = Path.Combine(workspace, Application.productName);
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

            maxLightMapResolution = 2048;
        }

        private void UpdateScale()
        {
            for (int i = 0; i < objects.Count; i++)
            {
                IJanusObject obj = objects[i];
                UObject uobj = (UObject)obj;
                if (!uobj)
                {
                    objects.RemoveAt(i);
                    i--;
                    continue;
                }

                obj.UpdateScale(uniformScale);
            }
        }

        private void OnGUI()
        {
            Rect rect = this.position;
            GUILayout.BeginArea(new Rect(border.x, border.y, rect.width - border.width, rect.height - border.height));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Janus Exporter " + (JanusGlobals.Version / 100.0).ToString("F2"), EditorStyles.boldLabel);
            //if (GUILayout.Button("Update"))
            //{
            //    //JanusVRUpdater.ShowWindow();
            //}
            GUILayout.EndHorizontal();

            // Main Parameters
            GUILayout.Label("Main", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Export Path");
            //exportPath = EditorGUILayout.TextField(exportPath);
            EditorGUILayout.LabelField(exportPath);
            if (GUILayout.Button("..."))
            {
                // search for a folder
                exportPath = EditorUtility.OpenFolderPanel("JanusVR Export Folder", exportPath, @"C:\");
            }
            EditorGUILayout.EndHorizontal();

            // Models
            GUILayout.Label("Model", EditorStyles.boldLabel);

            //meshFormat = (ExportMeshFormat)EditorGUILayout.EnumPopup("Mesh Format", meshFormat);

            float scale = EditorGUILayout.FloatField("Uniform Scale", uniformScale);
            if (uniformScale != scale)
            {
                uniformScale = scale;
                UpdateScale();
            }

            // Texture
            GUILayout.Label("Texture", EditorStyles.boldLabel);
            exportGifs = EditorGUILayout.Toggle("Export GIFs", exportGifs);
            defaultTexFormat = (ExportTextureFormat)EditorGUILayout.EnumPopup("Textures Format", defaultTexFormat);
            if (SupportsQuality(defaultTexFormat))
            {
                defaultQuality = EditorGUILayout.IntSlider("Textures Quality", defaultQuality, 0, 100);
            }

            // Scene
            GUILayout.Label("Scene", EditorStyles.boldLabel);

            exportMaterials = EditorGUILayout.Toggle("Export Materials", exportMaterials);
            EditorGUILayout.LabelField("    Useful for testing lighting results in Janus");

            exportSkybox = EditorGUILayout.Toggle("Export Skybox", exportSkybox);
            if (exportSkybox && IsProceduralSkybox())
            {
                exportSkyboxResolution = Math.Max(4, EditorGUILayout.IntField("Skybox Render Resolution", exportSkyboxResolution));
            }
            EditorGUILayout.LabelField("    Will render the Skybox into 6 textures with the specified resolution");

            lightmapExportType = (LightmapExportType)EditorGUILayout.EnumPopup("Lightmap Type", lightmapExportType);
            if (lightmapExportType != LightmapExportType.None && lightmapExportType != LightmapExportType.PackedSourceEXR)
            {
                maxLightMapResolution = Math.Max(4, EditorGUILayout.IntField("Max Lightmap Resolution", maxLightMapResolution));
            }

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
                    EditorGUILayout.LabelField("    directly into the exported project (only on Janus 56.0)");
                    break;
                case LightmapExportType.BakedMaterial:
                    EditorGUILayout.LabelField("    Bakes the lightmap into the material (for testing purposes)");
                    break;
                case LightmapExportType.Unpacked:
                    EditorGUILayout.LabelField("    Converts the source EXR files to Low-Dynamic Range and unpacks");
                    EditorGUILayout.LabelField("    into individual textures (for testing purposes)");
                    break;
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
                    GUILayout.Label("Environment Probe: None (need baked lightmaps)", errorStyle);
                }
                else
                {
                    GUILayout.Label("Environment Probe: " + cubemap.width);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export HTML only"))
            {
                try
                {
                    updateOnlyHtml = true;
                    PreExport();
                    DoExport();
                    updateOnlyHtml = false;
                }
                catch
                {
                    Clean();
                    EditorUtility.ClearProgressBar();

                    Debug.Log("Error exporting");
                }
            }

            if (GUILayout.Button("Reset Parameters"))
            {
                ResetParameters();
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(exportPath))
            {
                if (GUILayout.Button("Full Export", GUILayout.Height(30)))
                {
                    //try
                    {
                        PreExport();
                        DoExport();
                    }
                    //catch (Exception ex)
                    {
                        //Clean();
                        EditorUtility.ClearProgressBar();

                        //Debug.LogError("Error exporting: " + ex);
                    }
                }
            }

            GUILayout.EndArea();
        }

        private string GetMeshFormat(ExportMeshFormat format)
        {
            return meshExporters[format].GetFormat();
        }

        private static string GetImageFormatName(ExportTextureFormat format)
        {
            switch (format)
            {
                case ExportTextureFormat.JPG:
                    return ".jpg";
                case ExportTextureFormat.PNG:
                default:
                    return ".png";
            }
        }
        private static bool SupportsQuality(ExportTextureFormat format)
        {
            switch (format)
            {
                case ExportTextureFormat.JPG:
                    return true;
                case ExportTextureFormat.PNG:// PNG is lossless
                default:
                    return false;
            }
        }

        private void AssertShader(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("Shaders not found! Please reimport the Janus Exporter package");
            }
        }

        private Texture2D RenderSkyBoxSide(Vector3 direction, string name, RenderTexture tmpTex, Camera cam)
        {
            cam.gameObject.transform.LookAt(direction);
            cam.Render();

            Graphics.SetRenderTarget(tmpTex);
            Texture2D left = new Texture2D(tmpTex.width, tmpTex.height, TextureFormat.ARGB32, false, false);
            left.ReadPixels(new Rect(0, 0, tmpTex.width, tmpTex.height), 0, 0);
            left.name = "SkyBox" + name;

            texturesExported.Add(left);

            return left;
        }

        private Cubemap RenderSkyboxCubemap()
        {
            GameObject temp = new GameObject("__TempSkyProbeRender");
            Camera cam = temp.AddComponent<Camera>();

            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.cullingMask = 0;

            int res = RenderSettings.defaultReflectionResolution;
            Cubemap cubemap = new Cubemap(res, TextureFormat.RGFloat, false);
            cam.RenderToCubemap(cubemap);

            GameObject.DestroyImmediate(temp);
            return cubemap;
        }

        private bool IsProceduralSkybox()
        {
            Material skybox = RenderSettings.skybox;
            if (skybox != null)
            {
                for (int i = 0; i < skyboxTexNames.Length; i++)
                {
                    if (!skybox.HasProperty(skyboxTexNames[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Texture2D skyBoxLeft;
        private Texture2D skyBoxRight;
        private Texture2D skyBoxUp;
        private Texture2D skyBoxDown;
        private Texture2D skyBoxForward;
        private Texture2D skyBoxBack;

        public static IEnumerable<GameObject> SceneRoots()
        {
            var prop = new HierarchyProperty(HierarchyType.GameObjects);
            var expanded = new int[0];
            while (prop.Next(expanded))
            {
                yield return prop.pptrValue as GameObject;
            }
        }

        private bool cancelExport;

        private void PreExport()
        {
            cancelExport = false;
            Clean();

            texturesExported = new List<Texture2D>();
            texturesExportedData = new List<TextureExportData>();

            meshesExported = new List<Mesh>();
            meshesExportedData = new List<MeshExportData>();
            meshesNames = new Dictionary<Mesh, string>();
            meshesCount = 0;

#if UNITY_5_3_OR_NEWER
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            string scenePath = scene.path;
            string sceneName = scene.name;
#else
            GameObject[] roots = SceneRoots().ToArray();
            string scenePath = EditorApplication.currentScene;
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
#endif

            if (string.IsNullOrEmpty(scenePath))
            {
                cancelExport = true;
                Debug.LogError("Scene is not saved. Can't export.");
                return;
            }

            lightmapped = new Dictionary<int, List<GameObject>>();
            exported = new ExportedData();

            for (int i = 0; i < roots.Length; i++)
            {
                RecursiveSearch(roots[i], exported);
            }

            if (exportSkybox)
            {
                // look for skybox and grab all 6 textures if it's 6-sided
                Material skybox = RenderSettings.skybox;
                if (skybox != null)
                {
                    bool proceed = true;
                    for (int i = 0; i < skyboxTexNames.Length; i++)
                    {
                        if (!skybox.HasProperty(skyboxTexNames[i]))
                        {
                            proceed = false;
                        }
                    }

                    if (proceed)
                    {
                        for (int i = 0; i < skyboxTexNames.Length; i++)
                        {
                            Texture2D tex = (Texture2D)skybox.GetTexture(skyboxTexNames[i]);
                            if (!texturesExported.Contains(tex))
                            {
                                texturesExported.Add(tex);
                            }
                        }

                        skyBoxForward = (Texture2D)skybox.GetTexture("_FrontTex");
                        skyBoxBack = (Texture2D)skybox.GetTexture("_BackTex");
                        skyBoxLeft = (Texture2D)skybox.GetTexture("_LeftTex");
                        skyBoxRight = (Texture2D)skybox.GetTexture("_RightTex");
                        skyBoxUp = (Texture2D)skybox.GetTexture("_UpTex");
                        skyBoxDown = (Texture2D)skybox.GetTexture("_DownTex");
                    }
                    else
                    {
                        if (!updateOnlyHtml)
                        {
                            // the skybox is not a 6-texture skybox
                            // lets render it to one then
                            GameObject temp = new GameObject("__TempSkyRender");
                            Camera cam = temp.AddComponent<Camera>();

                            cam.enabled = false;

                            RenderTexture tex = new RenderTexture(exportSkyboxResolution, exportSkyboxResolution, 0);
                            cam.targetTexture = tex;
                            cam.clearFlags = CameraClearFlags.Skybox;
                            cam.cullingMask = 0;
                            cam.orthographic = true;

                            skyBoxLeft = RenderSkyBoxSide(Vector3.left, "Left", tex, cam);
                            skyBoxRight = RenderSkyBoxSide(Vector3.right, "Right", tex, cam);
                            skyBoxForward = RenderSkyBoxSide(Vector3.forward, "Forward", tex, cam);
                            skyBoxBack = RenderSkyBoxSide(Vector3.back, "Back", tex, cam);
                            skyBoxUp = RenderSkyBoxSide(Vector3.up, "Up", tex, cam);
                            skyBoxDown = RenderSkyBoxSide(Vector3.down, "Down", tex, cam);

                            cam.targetTexture = null;
                            Graphics.SetRenderTarget(null);

                            GameObject.DestroyImmediate(tex);
                            GameObject.DestroyImmediate(temp);
                        }
                    }
                }
            }

            scenePath = Path.GetDirectoryName(scenePath);
            string lightMapsFolder = Path.Combine(scenePath, sceneName);
            DirectoryInfo lightMapsDir = new DirectoryInfo(lightMapsFolder);

            if (lightmapExportType != LightmapExportType.None &&
                lightmapped.Count > 0)
            {
                switch (lightmapExportType)
                {
                    case LightmapExportType.BakedMaterial:
                        #region Baked
                        {
                            // only load shader now, so if the user is not exporting lightmaps
                            // he doesn't need to have it on his project folder
                            Shader lightMapShader = Shader.Find("Hidden/LMapBaked");
                            AssertShader(lightMapShader);

                            Material lightMap = new Material(lightMapShader);
                            lightMap.SetPass(0);

                            // export lightmaps
                            int lmap = 0;
                            foreach (var lightPair in lightmapped)
                            {
                                int id = lightPair.Key;
                                List<GameObject> toRender = lightPair.Value;

                                // get the path to the lightmap file
                                string lightMapFile = Path.Combine(lightMapsFolder, "Lightmap-" + id + "_comp_light.exr");
                                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(lightMapFile);
                                if (texture == null)
                                {
                                    continue;
                                }

                                lightMap.SetTexture("_LightMapTex", texture);
                                lightMap.SetFloat("_IsLinear", PlayerSettings.colorSpace == ColorSpace.Linear ? 1 : 0);

                                for (int i = 0; i < toRender.Count; i++)
                                {
                                    GameObject obj = toRender[i];
                                    MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                                    MeshFilter filter = obj.GetComponent<MeshFilter>();

                                    Mesh mesh = filter.sharedMesh;
                                    Transform trans = obj.transform;
                                    Matrix4x4 world = Matrix4x4.TRS(trans.position, trans.rotation, trans.lossyScale);

                                    Vector4 scaleOffset = renderer.lightmapScaleOffset;
                                    float width = (1 - scaleOffset.z) * scaleOffset.x;
                                    float height = (1 - scaleOffset.w) * scaleOffset.y;
                                    float size = Math.Max(width, height);

                                    int lightMapSize = (int)(maxLightMapResolution * size);
                                    lightMapSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(lightMapSize) / Math.Log(2)));
                                    lightMapSize = Math.Min(maxLightMapResolution, Math.Max(lightMapSize, 16));

                                    RenderTexture renderTexture = RenderTexture.GetTemporary(lightMapSize, lightMapSize, 0, RenderTextureFormat.ARGB32);
                                    Graphics.SetRenderTarget(renderTexture);
                                    GL.Clear(true, true, new Color(0, 0, 0, 0)); // clear to transparent

                                    Material[] mats = renderer.sharedMaterials;
                                    for (int j = 0; j < mats.Length; j++)
                                    {
                                        Material mat = mats[j];

                                        lightMap.SetTexture("_MainTex", null);

                                        Shader shader = mat.shader;
                                        int props = ShaderUtil.GetPropertyCount(shader);
                                        for (int k = 0; k < props; k++)
                                        {
                                            string name = ShaderUtil.GetPropertyName(shader, k);

                                            ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(shader, k);
                                            if (propType == ShaderUtil.ShaderPropertyType.TexEnv)
                                            {
                                                if (mainTexSemantics.Contains(name.ToLower()))
                                                {
                                                    // main texture texture
                                                    lightMap.SetTexture("_MainTex", mat.GetTexture(name));
                                                }
                                            }
                                            else if (propType == ShaderUtil.ShaderPropertyType.Color)
                                            {
                                                if (colorSemantics.Contains(name.ToLower()))
                                                {
                                                    lightMap.SetColor("_Color", mat.GetColor(name));
                                                }
                                            }
                                        }

                                        lightMap.SetVector("_LightMapUV", renderer.lightmapScaleOffset);
                                        lightMap.SetPass(0);
                                        Graphics.DrawMeshNow(mesh, world, j);
                                    }

                                    // This is the only way to access data from a RenderTexture
                                    Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false);
                                    tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                                    tex.name = "Lightmap" + lmap;
                                    tex.Apply(); // send the data back to the GPU so we can draw it on the preview area

                                    if (!texturesExported.Contains(tex))
                                    {
                                        texturesExported.Add(tex);
                                    }

                                    ExportedObject eobj = exported.exportedObjs.First(c => c.GameObject == obj);
                                    eobj.DiffuseMapTex = tex;

                                    Graphics.SetRenderTarget(null);
                                    RenderTexture.ReleaseTemporary(renderTexture);

                                    lmap++;
                                }
                            }
                            UnityEngine.Object.DestroyImmediate(lightMap);
                        }
                        #endregion
                        break;
                    case LightmapExportType.Packed:
                        #region Packed
                        {
                            Shader lightMapShader = Shader.Find("Hidden/LMapPacked");
                            AssertShader(lightMapShader);

                            Material lightMap = new Material(lightMapShader);
                            lightMap.SetPass(0);

                            // just pass the textures forward
                            // export lightmaps
                            foreach (var lightPair in lightmapped)
                            {
                                int id = lightPair.Key;
                                List<GameObject> toRender = lightPair.Value;

                                // get the path to the lightmap file
                                string lightMapFile = Path.Combine(lightMapsFolder, "Lightmap-" + id + "_comp_light.exr");
                                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(lightMapFile);
                                if (texture == null)
                                {
                                    continue;
                                }

                                lightMap.SetTexture("_LightMapTex", texture);

                                // We need to access unity_Lightmap_HDR to decode the lightmap,
                                // but we can't, so we have to render everything to a custom RenderTexture!
                                Texture2D decTex = new Texture2D(texture.width, texture.height);
                                decTex.name = "Lightmap" + id;
                                texturesExported.Add(decTex);

                                RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height);
                                Graphics.SetRenderTarget(renderTexture);
                                GL.Clear(true, true, new Color(0, 0, 0, 0)); // clear to transparent

                                for (int i = 0; i < toRender.Count; i++)
                                {
                                    GameObject obj = toRender[i];
                                    MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                                    MeshFilter filter = obj.GetComponent<MeshFilter>();

                                    Mesh mesh = filter.sharedMesh;
                                    Transform trans = obj.transform;
                                    Matrix4x4 world = Matrix4x4.TRS(trans.position, trans.rotation, trans.lossyScale);

                                    lightMap.SetVector("_LightMapUV", renderer.lightmapScaleOffset);
                                    lightMap.SetPass(0);
                                    Graphics.DrawMeshNow(mesh, Matrix4x4.identity);

                                    ExportedObject eobj = exported.exportedObjs.First(c => c.GameObject == obj);
                                    eobj.LightMapTex = decTex;
                                }

                                decTex.ReadPixels(new Rect(0, 0, decTex.width, decTex.height), 0, 0);
                                decTex.Apply(); // send the data back to the GPU so we can draw it on the preview area

                                Graphics.SetRenderTarget(null);
                                RenderTexture.ReleaseTemporary(renderTexture);
                            }
                            UObject.DestroyImmediate(lightMap);
                        }
                        #endregion
                        break;
                    case LightmapExportType.PackedSourceEXR:
                        #region Packed Source EXR
                        {
                            foreach (var lightPair in lightmapped)
                            {
                                int id = lightPair.Key;
                                List<GameObject> toRender = lightPair.Value;

                                // get the path to the lightmap file
                                string lightMapFile = Path.Combine(lightMapsFolder, "Lightmap-" + id + "_comp_light.exr");
                                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(lightMapFile);
                                if (texture == null)
                                {
                                    continue;
                                }

                                for (int i = 0; i < toRender.Count; i++)
                                {
                                    GameObject obj = toRender[i];
                                    ExportedObject eobj = exported.exportedObjs.First(c => c.GameObject == obj);
                                    eobj.LightMapTex = texture;
                                }

                                texturesExported.Add(texture);
                            }
                        }
                        #endregion
                        break;
                    case LightmapExportType.Unpacked:
                        #region Unpacked
                        {
                            Shader lightMapShader = Shader.Find("Hidden/LMapUnpacked");
                            AssertShader(lightMapShader);

                            Material lightMap = new Material(lightMapShader);
                            lightMap.SetPass(0);

                            // export lightmaps
                            int lmap = 0;
                            foreach (var lightPair in lightmapped)
                            {
                                int id = lightPair.Key;
                                List<GameObject> toRender = lightPair.Value;

                                // get the path to the lightmap file
                                string lightMapFile = Path.Combine(lightMapsFolder, "Lightmap-" + id + "_comp_light.exr");
                                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(lightMapFile);
                                if (texture == null)
                                {
                                    continue;
                                }

                                lightMap.SetTexture("_LightMapTex", texture);

                                for (int i = 0; i < toRender.Count; i++)
                                {
                                    GameObject obj = toRender[i];
                                    MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                                    MeshFilter filter = obj.GetComponent<MeshFilter>();

                                    Mesh mesh = filter.sharedMesh;
                                    Transform trans = obj.transform;
                                    Matrix4x4 world = Matrix4x4.TRS(trans.position, trans.rotation, trans.lossyScale);

                                    Vector4 scaleOffset = renderer.lightmapScaleOffset;
                                    float width = (1 - scaleOffset.z) * scaleOffset.x;
                                    float height = (1 - scaleOffset.w) * scaleOffset.y;
                                    float size = Math.Max(width, height);

                                    int lightMapSize = (int)(maxLightMapResolution * size);
                                    lightMapSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(lightMapSize) / Math.Log(2)));
                                    lightMapSize = Math.Min(maxLightMapResolution, Math.Max(lightMapSize, 16));

                                    RenderTexture renderTexture = RenderTexture.GetTemporary(lightMapSize, lightMapSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                                    Graphics.SetRenderTarget(renderTexture);
                                    GL.Clear(true, true, new Color(0, 0, 0, 0)); // clear to transparent

                                    Material[] mats = renderer.sharedMaterials;
                                    for (int j = 0; j < mats.Length; j++)
                                    {
                                        Material mat = mats[j];

                                        Shader shader = mat.shader;

                                        lightMap.SetVector("_LightMapUV", renderer.lightmapScaleOffset);
                                        lightMap.SetPass(0);
                                        Graphics.DrawMeshNow(mesh, world, j);
                                    }

                                    // This is the only way to access data from a RenderTexture
                                    Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false);
                                    tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                                    tex.name = "Lightmap" + lmap;
                                    tex.Apply(); // send the data back to the GPU so we can draw it on the preview area

                                    if (!texturesExported.Contains(tex))
                                    {
                                        texturesExported.Add(tex);
                                    }

                                    ExportedObject eobj = exported.exportedObjs.First(c => c.GameObject == obj);
                                    eobj.LightMapTex = tex;

                                    Graphics.SetRenderTarget(null);
                                    RenderTexture.ReleaseTemporary(renderTexture);

                                    lmap++;
                                }
                            }
                            UObject.DestroyImmediate(lightMap);
                        }
                        #endregion
                        break;
                }
            }

            for (int i = 0; i < texturesExported.Count; i++)
            {
                Texture2D tex = texturesExported[i];
                TextureExportData data = new TextureExportData();
                string path = AssetDatabase.GetAssetPath(tex);

                // look for at least 1 object exported that uses us as a transparent texture
                for (int j = 0; j < exported.exportedObjs.Count; j++)
                {
                    ExportedObject obj = exported.exportedObjs[j];
                    if (obj.IsTransparent)
                    {
                        if (obj.DiffuseMapTex == tex ||
                            obj.LightMapTex == tex ||
                            obj.Texture == tex)
                        {
                            data.ExportAlpha = true;
                        }
                    }
                }

                data.Created = string.IsNullOrEmpty(path);
                data.Format = this.defaultTexFormat; // start up with a default format
                data.Texture = tex;
                data.Resolution = tex.width;
                data.Quality = defaultQuality;

                texturesExportedData.Add(data);
            }

            for (int i = 0; i < meshesExported.Count; i++)
            {
                Mesh mesh = meshesExported[i];
                MeshExportData data = new MeshExportData();
                data.Format = this.meshFormat;
                data.Mesh = mesh;

                string name = mesh.name;
                if (string.IsNullOrEmpty(name))
                {
                    meshesCount++;
                    name = "ExportedMesh" + meshesCount;
                }
                meshesNames.Add(mesh, name);

                meshesExportedData.Add(data);
            }

            farPlaneDistance = sceneSize.size.magnitude * 1.3f;

            // find the Reflection probe for the sky, if we have one

            Cubemap cubemap = RenderSettings.customReflection;
            if (cubemap == null)
            {
                // search thorugh files
                if (lightMapsDir.Exists)
                {
                    FileInfo[] probes = lightMapsDir.GetFiles("ReflectionProbe-*");
                    FileInfo first = probes.FirstOrDefault();
                    if (first == null)
                    {
                        return;
                    }

                    string probePath = Path.Combine(lightMapsFolder, first.Name);
                    cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(probePath);
                }
            }
            exported.environmentCubemap = cubemap;
        }

        private void RecursiveSearch(GameObject root, ExportedData data)
        {
            if (!root.activeInHierarchy)
            {
                return;
            }

            Component[] comps = root.GetComponents<Component>();

            for (int i = 0; i < comps.Length; i++)
            {
                Component comp = comps[i];
                if (comp == null)
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
                        comps.Any(c => c is JanusVREntryPortal) ||
                        comps.Any(c => c is JanusVRLink))
                    {
                        continue;
                    }

                    sceneSize.Encapsulate(meshRen.bounds);

                    // Only export the mesh if we never exported this one mesh
                    if (!meshesExported.Contains(mesh))
                    {
                        meshesExported.Add(mesh);
                    }

                    ExportedObject exp = data.exportedObjs.FirstOrDefault(c => c.GameObject == root);
                    if (exp == null)
                    {
                        exp = new ExportedObject();
                        exp.GameObject = root;
                        data.exportedObjs.Add(exp);
                    }
                    exp.Mesh = mesh;

                    // export textures
                    if (lightmapExportType != LightmapExportType.BakedMaterial && exportMaterials) // if were baking we dont need the original textures
                    {
                        Material[] mats = meshRen.sharedMaterials;
                        for (int j = 0; j < mats.Length; j++)
                        {
                            Material mat = mats[j];
                            if (mat == null)
                            {
                                continue;
                            }

                            Vector2 sca = mat.mainTextureScale;
                            Vector2 off = mat.mainTextureOffset;
                            if (sca != Vector2.one || off != Vector2.zero)
                            {
                                exp.Tiling = new Vector4(sca.x, sca.y, off.x, off.y);
                            }

                            Shader shader = mat.shader;
                            if (!string.IsNullOrEmpty(shader.name))
                            {
                                string shaderLowercase = shader.name.ToLower();
                                for (int k = 0; k < transparentSemantics.Length; k++)
                                {
                                    if (shaderLowercase.Contains(transparentSemantics[k]))
                                    {
                                        exp.IsTransparent = true;
                                        break;
                                    }
                                }
                            }

                            int props = ShaderUtil.GetPropertyCount(shader);
                            for (int k = 0; k < props; k++)
                            {
                                string name = ShaderUtil.GetPropertyName(shader, k);

                                ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(shader, k);
                                if (propType == ShaderUtil.ShaderPropertyType.TexEnv)
                                {
                                    if (mainTexSemantics.Contains(name.ToLower()))
                                    {
                                        Texture2D tex = mat.GetTexture(name) as Texture2D;
                                        if (tex == null)
                                        {
                                            continue;
                                        }

                                        exp.DiffuseMapTex = tex;
                                        if (!texturesExported.Contains(tex))
                                        {
                                            texturesExported.Add(tex);
                                        }
                                    }
                                }
                                else if (propType == ShaderUtil.ShaderPropertyType.Color)
                                {
                                    string nameLower = name.ToLower();
                                    if (colorSemantics.Contains(nameLower))
                                    {
                                        Color c = mat.GetColor(name);
                                        exp.Color = c;
                                    }
                                }
                            }
                        }
                    }

                    int lightMap = meshRen.lightmapIndex;
                    if (lightMap != -1)
                    {
                        // Register mesh for lightmap render
                        List<GameObject> toRender;
                        if (!lightmapped.TryGetValue(lightMap, out toRender))
                        {
                            toRender = new List<GameObject>();
                            lightmapped.Add(lightMap, toRender);
                        }

                        toRender.Add(root);
                    }
                }
                else if (comp is Collider)
                {
                    Collider col = (Collider)comp;

                    ExportedObject exp = data.exportedObjs.FirstOrDefault(c => c.GameObject == root);
                    if (exp == null)
                    {
                        exp = new ExportedObject();
                        exp.GameObject = root;
                        data.exportedObjs.Add(exp);
                    }
                    exp.Col = col;
                }
                else if (comp is ReflectionProbe)
                {
                    ReflectionProbe probe = (ReflectionProbe)comp;

                    ExportedObject exp = new ExportedObject();
                    exp.GameObject = root;
                    exp.Texture = probe.texture;
                    exp.ReflectionProbe = probe;

                    data.exportedReflectionProbes.Add(exp);
                }
                else if (comp is JanusVREntryPortal)
                {
                    JanusVREntryPortal portal = (JanusVREntryPortal)comp;
                    data.entryPortal = portal;
                }
                else if (comp is JanusVRLink)
                {
                    JanusVRLink link = (JanusVRLink)comp;
                    data.exportedLinks.Add(link);

                    Material mat = link.meshRenderer.sharedMaterial;
                    Texture tex = mat.mainTexture;
                    if (tex != null)
                    {
                        Texture2D texture = (Texture2D)tex;
                        if (!texturesExported.Contains(texture))
                        {
                            texturesExported.Add(texture);
                        }

                        link.texture = texture;
                    }
                }
            }

            // loop through all this GameObject children
            foreach (Transform child in root.transform)
            {
                RecursiveSearch(child.gameObject, data);
            }
        }

        private void Clean()
        {
            skyBoxLeft = null;
            skyBoxRight = null;
            skyBoxForward = null;
            skyBoxBack = null;
            skyBoxUp = null;
            skyBoxDown = null;

            sceneSize = new Bounds();

            if (texturesExportedData != null)
            {
                for (int i = 0; i < texturesExportedData.Count; i++)
                {
                    TextureExportData tex = texturesExportedData[i];

                    if (tex.Created)
                    {
                        // we made this, we delete this
                        UObject.DestroyImmediate(tex.Texture);
                    }

                    if (tex.Preview)
                    {
                        string path = AssetDatabase.GetAssetPath(tex.Preview);
                        if (string.IsNullOrEmpty(path))
                        {
                            // make sure we didnt just copy the Preview because it was the same resolution
                            // as the requested preview
                            UObject.DestroyImmediate(tex.Preview);
                        }
                    }
                }
            }
            exported = null;
            UpdateOnlyHTML = updateOnlyHtml;
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

            CmftInterop.Execute(cmd);

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
            CmftInterop.Execute(cmd);

            TextureExportData data = new TextureExportData();
            data.ExportedPath = name + ".dds";
            return data;
        }

        private void DoExport()
        {
            if (cancelExport)
            {
                return;
            }

            try
            {
                if (!Directory.Exists(exportPath))
                {
                    Directory.CreateDirectory(exportPath);
                }
            }
            catch
            {
                Debug.LogError("Error while creating the export folder!");
                return;
            }

            CultureInfo culture = CultureInfo.InvariantCulture;

            // export all the textures
            int lmapCounter = 0;
            for (int i = 0; i < texturesExportedData.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Janus VR Exporter", "Exporting textures...", i / (float)texturesExportedData.Count);

                TextureExportData tex = texturesExportedData[i];
                string path = AssetDatabase.GetAssetPath(tex.Texture);
                string expPath = Path.Combine(exportPath, tex.Texture.name);

                Texture2D texture = tex.Texture;

                // force power of 2 resolution
                if (!MathUtil.IsPowerOf2(tex.Resolution))
                {
                    // not a power of 2
                    tex.Resolution = Math.Min(MathUtil.NextPowerOf2(tex.Resolution), maxLightMapResolution);
                }

                if (string.IsNullOrEmpty(path))
                {
                    // we created this texture, just export it (were free to read it)
                    tex.ExportedPath = ExportTexture(texture, expPath, defaultTexFormat, tex.Quality, true);
                }
                else
                {
                    string lowerPath = path.ToLower();
                    if (lightmapExportType == LightmapExportType.PackedSourceEXR &&
                        lowerPath.EndsWith("exr"))
                    {
                        lmapCounter++;

                        string expName = "Lightmap" + lmapCounter + ".exr";
                        string destination = Path.Combine(exportPath, expName);

                        tex.ExportedPath = expName;
                        if (!updateOnlyHtml)
                        {
                            if (File.Exists(destination))
                            {
                                File.Delete(destination);
                            }
                            File.Copy(path, destination);
                        }
                    }
                    else
                    {
                        // look at the source path first
                        if (exportGifs && lowerPath.EndsWith("gif"))
                        {
                            // copy
                            string fileName = texture.name + ".gif";
                            string fPath = expPath + ".gif";
                            File.Copy(path, fPath, true);

                            tex.ExportedPath = fileName;
                        }
                        else
                        {
                            TextureUtil.TempTextureData data = TextureUtil.LockTexture(texture, path);
                            tex.ExportedPath = ExportTexture(texture, expPath, tex.Format, tex.Quality, !data.alphaIsTransparency && !tex.ExportAlpha);
                            TextureUtil.UnlockTexture(data);
                        }
                    }
                }
            }

            List<ExportedObject> refProbs = exported.exportedReflectionProbes;
            for (int i = 0; i < refProbs.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Janus VR Exporter", "Generating radiance and irradiance maps using cmft...", i / (float)(refProbs.Count + 1));
                ExportedObject obj = refProbs[i];
                ReflectionProbe probe = obj.ReflectionProbe;
                Cubemap cubemap;
                if (probe.bakedTexture is Cubemap)
                {
                    cubemap = (Cubemap)probe.bakedTexture;
                }
                else
                {
                    cubemap = probe.customBakedTexture as Cubemap;
                }

                if (cubemap != null)
                {
                    //GenerateCmftIrrad(cubemap);
                    //GenerateCmftRad(cubemap);
                }
            }
            TextureExportData envIrrad = GenerateCmftIrrad(exported.environmentCubemap, "irradiance");
            TextureExportData envRad = GenerateCmftRad(exported.environmentCubemap, "radiance");

            bool switchUv = lightmapExportType == LightmapExportType.BakedMaterial;

            for (int i = 0; i < meshesExportedData.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Janus VR Exporter", "Exporting meshes...", i / (float)meshesExportedData.Count);

                MeshExportData model = meshesExportedData[i];
                string name = meshesNames[model.Mesh];
                string expPath = Path.Combine(exportPath, name);
                ExportMesh(model.Mesh, expPath, model.Format, null, switchUv);
                model.ExportedPath = name + GetMeshFormat(model.Format);
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;

            StringBuilder builder = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(builder, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("html");

            writer.WriteStartElement("head");
            writer.WriteStartElement("title");
            writer.WriteString("Janus Unity Exporter v" + JanusGlobals.Version);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("body");
            writer.WriteStartElement("FireBoxRoom");
            writer.WriteStartElement("Assets");

            // Make the index.html file
            List<Mesh> meshWritten = new List<Mesh>();

            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                if (!meshWritten.Contains(obj.Mesh))
                {
                    MeshExportData data = meshesExportedData.FirstOrDefault(c => c.Mesh == obj.Mesh);
                    if (data == null || string.IsNullOrEmpty(data.ExportedPath))
                    {
                        continue;
                    }

                    string path = data.ExportedPath;
                    string name = meshesNames[obj.Mesh];
                    meshWritten.Add(obj.Mesh);

                    writer.WriteStartElement("AssetObject");
                    writer.WriteAttributeString("id", name);
                    writer.WriteAttributeString("src", path);
                    writer.WriteEndElement();
                }
            }

            string refprobeData = "";
            if (envIrrad != null && envRad != null)
            {
                refprobeData = " cubemap_radiance_id=\"" + Path.GetFileNameWithoutExtension(envRad.ExportedPath) + "\" ";
                refprobeData += "cubemap_irradiance_id=\"" + Path.GetFileNameWithoutExtension(envIrrad.ExportedPath) + "\"";

                texturesExportedData.Add(envIrrad);
                texturesExportedData.Add(envRad);
            }

            // textures appear only once, while exported objects can appear multiple times
            // that's why we have a meshwritten list, and not a texturewritten (not anymore at least)
            for (int i = 0; i < texturesExportedData.Count; i++)
            {
                TextureExportData data = texturesExportedData[i];
                string path = data.ExportedPath;
                writer.WriteStartElement("AssetImage");
                writer.WriteAttributeString("id", Path.GetFileNameWithoutExtension(path));
                writer.WriteAttributeString("src", path);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteStartElement("Room");

            float farPlane = Math.Max(farPlaneDistance, 500);
            writer.WriteAttributeString("far_dist", ((int)farPlane).ToString(culture));

            TextureExportData fronttex, backtex, lefttex, righttex, uptex, downtex = null;

            EditorUtility.DisplayProgressBar("Janus VR Exporter", "Rendering skybox...", 0);

            if (exportSkybox)
            {
                fronttex = texturesExportedData.FirstOrDefault(c => c.Texture == skyBoxForward);
                backtex = texturesExportedData.FirstOrDefault(c => c.Texture == skyBoxBack);
                lefttex = texturesExportedData.FirstOrDefault(c => c.Texture == skyBoxLeft);
                righttex = texturesExportedData.FirstOrDefault(c => c.Texture == skyBoxRight);
                uptex = texturesExportedData.FirstOrDefault(c => c.Texture == skyBoxUp);
                downtex = texturesExportedData.FirstOrDefault(c => c.Texture == skyBoxDown);

                if (fronttex != null)
                {
                    writer.WriteAttributeString("skybox_front_id", Path.GetFileNameWithoutExtension(fronttex.ExportedPath));
                }
                if (backtex != null)
                {
                    writer.WriteAttributeString("skybox_back_id", Path.GetFileNameWithoutExtension(backtex.ExportedPath));
                }
                if (lefttex != null)
                {
                    writer.WriteAttributeString("skybox_left_id", Path.GetFileNameWithoutExtension(lefttex.ExportedPath));
                }
                if (righttex != null)
                {
                    writer.WriteAttributeString("skybox_right_id", Path.GetFileNameWithoutExtension(righttex.ExportedPath));
                }
                if (uptex != null)
                {
                    writer.WriteAttributeString("skybox_up_id", Path.GetFileNameWithoutExtension(uptex.ExportedPath));
                }
                if (downtex != null)
                {
                    writer.WriteAttributeString("skybox_down_id", Path.GetFileNameWithoutExtension(downtex.ExportedPath));
                }
            }

            if (envIrrad != null && envRad != null)
            {
                writer.WriteAttributeString("cubemap_radiance_id", Path.GetFileNameWithoutExtension(envRad.ExportedPath));
                writer.WriteAttributeString("cubemap_irradiance_id", Path.GetFileNameWithoutExtension(envIrrad.ExportedPath));
            }

            if (exported.entryPortal != null)
            {
                JanusVREntryPortal portal = exported.entryPortal;
                Transform portalTransform = portal.transform;

                Vector3 portalPos = JanusUtil.ConvertPosition(portal.GetJanusPosition(), uniformScale);
                Vector3 xDir, yDir, zDir;
                JanusUtil.GetJanusVectors(portalTransform, out xDir, out yDir, out zDir);

                writer.WriteAttributeString("pos", JanusUtil.FormatVector3(portalPos));
                writer.WriteAttributeString("xdir", JanusUtil.FormatVector3(xDir));
                writer.WriteAttributeString("ydir", JanusUtil.FormatVector3(yDir));
                writer.WriteAttributeString("zdir", JanusUtil.FormatVector3(zDir));
            }

            for (int i = 0; i < exported.exportedLinks.Count; i++)
            {
                JanusVRLink link = exported.exportedLinks[i];
                Transform trans = link.transform;

                Vector3 pos = JanusUtil.ConvertPosition(link.GetJanusPosition(), uniformScale);
                Vector3 sca = trans.localScale;
                Vector3 xDir, yDir, zDir;
                JanusUtil.GetJanusVectors(trans, out xDir, out yDir, out zDir);

                writer.WriteStartElement("Link");
                writer.WriteAttributeString("pos", JanusUtil.FormatVector3(pos));
                writer.WriteAttributeString("col", JanusUtil.FormatColor(link.Color));
                writer.WriteAttributeString("scale", JanusUtil.FormatVector3(sca));
                writer.WriteAttributeString("url", link.url);
                writer.WriteAttributeString("title", link.title);

                if (link.texture != null)
                {
                    string linkTex = Path.GetFileNameWithoutExtension(texturesExportedData.First(c => c.Texture == link.texture).ExportedPath);
                    writer.WriteAttributeString("thumb_id", linkTex);
                }

                writer.WriteAttributeString("xdir", JanusUtil.FormatVector3(xDir));
                writer.WriteAttributeString("ydir", JanusUtil.FormatVector3(xDir));
                writer.WriteAttributeString("zdir", JanusUtil.FormatVector3(zDir));
                writer.WriteEndElement();
            }

            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                GameObject go = obj.GameObject;

                string diffuseID = "";
                string lmapID = "";
                if (obj.DiffuseMapTex != null)
                {
                    diffuseID = Path.GetFileNameWithoutExtension(texturesExportedData.First(k => k.Texture == obj.DiffuseMapTex).ExportedPath);
                }
                if (obj.LightMapTex != null)
                {
                    lmapID = Path.GetFileNameWithoutExtension(texturesExportedData.First(k => k.Texture == obj.LightMapTex).ExportedPath);
                }

                Mesh mesh = obj.Mesh;
                if (mesh == null)
                {
                    continue;
                }

                string meshName = meshesNames[mesh];

                writer.WriteStartElement("Object");
                writer.WriteAttributeString("id", meshName);
                writer.WriteAttributeString("lighting", "true");

                if (!string.IsNullOrEmpty(diffuseID))
                {
                    writer.WriteAttributeString("image_id", diffuseID);
                }

                if (!string.IsNullOrEmpty(lmapID))
                {
                    writer.WriteAttributeString("lmap_id", lmapID);
                    if (lightmapExportType == LightmapExportType.Packed ||
                        lightmapExportType == LightmapExportType.PackedSourceEXR)
                    {
                        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                        Vector4 lmap = renderer.lightmapScaleOffset;
                        lmap.x = Mathf.Clamp(lmap.x, 0, 1);
                        lmap.y = Mathf.Clamp(lmap.y, 0, 1);
                        lmap.z = Mathf.Clamp(lmap.z, 0, 1);
                        lmap.w = Mathf.Clamp(lmap.w, 0, 1);
                        writer.WriteAttributeString("lmap_sca", JanusUtil.FormatVector4(lmap));
                    }
                }

                if (obj.Tiling != null)
                {
                    Vector4 tiling = obj.Tiling.Value;
                    writer.WriteAttributeString("tile", JanusUtil.FormatVector4(tiling));
                }

                if (obj.Col != null)
                {
                    writer.WriteAttributeString("collision_id", meshName);
                }

                if (obj.Color != null)
                {
                    Color objColor = obj.Color.Value;
                    writer.WriteAttributeString("col", JanusUtil.FormatColor(objColor));
                }

                Transform trans = go.transform;
                Vector3 pos = trans.position;
                pos *= uniformScale;
                pos.x *= -1;

                Quaternion rot = trans.rotation;
                Vector3 xDir = rot * Vector3.right;
                Vector3 yDir = rot * Vector3.up;
                Vector3 zDir = rot * Vector3.forward;
                xDir.x *= -1;
                yDir.x *= -1;
                zDir.x *= -1;

                Vector3 sca = trans.lossyScale;
                sca *= uniformScale;

                writer.WriteAttributeString("pos", JanusUtil.FormatVector3(pos));
                if (sca.x < 0 || sca.y < 0 || sca.z < 0)
                {
                    writer.WriteAttributeString("cull_face", "front");
                }

                writer.WriteAttributeString("scale", JanusUtil.FormatVector3(sca));
                writer.WriteAttributeString("xdir", JanusUtil.FormatVector3(xDir));
                writer.WriteAttributeString("ydir", JanusUtil.FormatVector3(yDir));
                writer.WriteAttributeString("zdir", JanusUtil.FormatVector3(zDir));

                writer.WriteEndElement();
            }

            writer.WriteEndDocument();
            writer.Close();
            writer.Flush();
            string indexPath = Path.Combine(exportPath, "index.html");
            File.WriteAllText(indexPath, builder.ToString());

            EditorUtility.ClearProgressBar();
        }

        private void ExportMesh(Mesh mesh, string path, ExportMeshFormat format, object data, bool switchUv)
        {
            if (updateOnlyHtml)
            {
                return;
            }

            string formatName = GetMeshFormat(format);
            string finalPath = path + formatName;
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            MeshExporter exporter = meshExporters[format];
            exporter.Initialize(lightmapExportType != LightmapExportType.None);
            exporter.ExportMesh(mesh, finalPath, new MeshExportParameters(switchUv, true));
        }

        private string ExportTexture(Texture2D tex, string path, ExportTextureFormat format, object data, bool zeroAlpha)
        {
            // see if the exporting format supports alpha
            if (!zeroAlpha && !TextureUtil.SupportsAlpha(format))
            {
                format = ExportTextureFormat.PNG;
            }

            string formatName = GetImageFormatName(format);
            string fileName = tex.name + formatName;

            if (updateOnlyHtml)
            {
                return fileName;
            }
            string fpath = path + formatName;

            try
            {
                using (Stream output = File.OpenWrite(fpath))
                {
                    TextureUtil.ExportTexture(tex, output, format, data, zeroAlpha);
                }
            }
            catch { }

            return fileName;
        }
    }
}
#endif
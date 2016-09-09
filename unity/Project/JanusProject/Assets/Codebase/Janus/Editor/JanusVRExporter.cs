using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.FBX;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

namespace JanusVR
{
    public class JanusVRExporter : EditorWindow
    {
        private const int PreviewSize = 64;
        public const int Version = 201;

        internal class ExportedData
        {
            internal List<ExportedObject> exportedObjs;

            internal ExportedData()
            {
                exportedObjs = new List<ExportedObject>();
            }
        }

        public JanusVRExporter()
        {
            this.titleContent = new GUIContent("Janus VR Exporter");
        }

        [MenuItem("Edit/JanusVR Exporter 2.0")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:
            JanusVRExporter window = EditorWindow.GetWindow<JanusVRExporter>();
            window.Show();

            if (string.IsNullOrEmpty(window.exportPath))
            {
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string workspace = Path.Combine(documents, @"JanusVR\workspaces");
                string proj = Path.Combine(workspace, Application.productName);
                window.exportPath = proj;
            }
        }

        [SerializeField]
        private string exportPath = @"";
        [SerializeField]
        private ImageFormatEnum defaultTexFormat = ImageFormatEnum.PNG;
        [SerializeField]
        private int defaultQuality = 100;
        [SerializeField]
        private TextureFilterMode filterMode = TextureFilterMode.Average;

        [SerializeField]
        private ExportMeshFormat defaultMeshFormat = ExportMeshFormat.FBX;
        [SerializeField]
        private float uniformScale = 1;

        [SerializeField]
        private LightmapExportType lightmapExportType = LightmapExportType.Packed;
        [SerializeField]
        private int maxLightMapResolution = 1024;

        [SerializeField]
        private bool exportMaterials = true;
        [SerializeField]
        private bool exportSkybox = true;

        /// <summary>
        /// Lower case values that the exporter will consider for being the Main Texture on a shader
        /// </summary>
        private string[] mainTexSemantics = new string[]
        {
            "_maintex"
        };

        private string[] skyboxTexNames = new string[]
        {
            "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex"
        };

        private Dictionary<int, List<GameObject>> lightmapped;

        private List<Texture2D> texturesExported;
        private List<TextureExportData> texturesExportedData;

        private List<Mesh> meshesExported;
        private List<MeshExportData> meshesExportedData;
        private Dictionary<Mesh, string> meshesNames;
        private int meshesCount;

        private ExportedData exported;

        private bool perTextureOptions = false;
        private bool perModelOptions = false;

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Export Path");
            exportPath = EditorGUILayout.TextField(exportPath);
            if (GUILayout.Button("..."))
            {
                // search for a folder
                exportPath = EditorUtility.OpenFolderPanel("JanusVR Export Folder", exportPath, @"C:\");
            }
            EditorGUILayout.EndHorizontal();

            defaultMeshFormat = (ExportMeshFormat)EditorGUILayout.EnumPopup("Default Mesh Format", defaultMeshFormat);
            defaultTexFormat = (ImageFormatEnum)EditorGUILayout.EnumPopup("Default Textures Format", defaultTexFormat);
            filterMode = (TextureFilterMode)EditorGUILayout.EnumPopup("Texture Filter", filterMode);
            defaultQuality = EditorGUILayout.IntSlider("Default Textures Quality", defaultQuality, 0, 100);

            uniformScale = EditorGUILayout.FloatField("Uniform Scale", uniformScale);
            exportMaterials = EditorGUILayout.Toggle("Export Materials", exportMaterials);
            exportSkybox = EditorGUILayout.Toggle("Export Skybox (6-sided)", exportSkybox);

            lightmapExportType = (LightmapExportType)EditorGUILayout.EnumPopup("Lightmap Type", lightmapExportType);
            if (lightmapExportType != LightmapExportType.None)
            {
                maxLightMapResolution = Math.Max(32, EditorGUILayout.IntField("Max Lightmap Resolution", maxLightMapResolution));
            }

            if (!string.IsNullOrEmpty(exportPath))
            {
                if (GUILayout.Button("Start Export"))
                {
                    PreExport();
                }
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            if (exported != null)
            {
                perTextureOptions = EditorGUILayout.Foldout(perTextureOptions, "Per Texture Options");

                Rect last = GUILayoutUtility.GetLastRect();
                last.y = last.y + last.height;

                float size = Math.Min(Screen.width * 0.3f, Screen.height * 0.1f);
                float half = Screen.width * 0.5f;

                if (perTextureOptions)
                {
                    for (int i = 0; i < texturesExportedData.Count; i++)
                    {
                        TextureExportData tex = texturesExportedData[i];
                        Rect r = GUILayoutUtility.GetRect(Screen.width, size * 1.05f);

                        GUI.DrawTexture(new Rect(size * 0.1f, r.y, size, size), tex.Preview);
                        GUI.Label(new Rect(size * 1.1f, r.y, half - size, size), tex.Texture.name);

                        float x1 = half * 1.3f;
                        float y = r.y;
                        float wid = half * 0.6f;

                        GUI.Label(new Rect(half, y, half * 0.3f, last.height), "Format");
                        tex.Format = (ImageFormatEnum)EditorGUI.EnumPopup(new Rect(x1, y, wid, last.height), tex.Format);
                        y += last.height;

                        GUI.Label(new Rect(half, y, half * 0.3f, last.height), "Resolution");
                        tex.Resolution = EditorGUI.IntSlider(new Rect(x1, y, wid, last.height), tex.Resolution, 32, tex.Texture.width);
                        y += last.height;

                        GUI.Label(new Rect(half, y, half * 0.3f, last.height), "Export Alpha");
                        tex.ExportAlpha = EditorGUI.Toggle(new Rect(x1, y, wid, last.height), tex.ExportAlpha);
                        y += last.height;

                        if (SupportsQuality(tex.Format))
                        {
                            GUI.Label(new Rect(half, y, half * 0.3f, last.height), "Quality");
                            tex.Quality = EditorGUI.IntSlider(new Rect(x1, y, wid, last.height), tex.Quality, 0, 100);
                        }
                    }
                }

                //perModelOptions = EditorGUILayout.Foldout(perModelOptions, "Per Model Options");
                //if (perModelOptions)
                //{
                //    for (int i = 0; i < meshesExportedData.Count; i++)
                //    {
                //        MeshExportData model = meshesExportedData[i];
                //        string name = meshesNames[model.Mesh];
                //        Rect r = GUILayoutUtility.GetRect(Screen.width, size * 1.05f);

                //        //GUI.DrawTexture(new Rect(size * 0.1f, r.y, size, size), model.Preview);
                //        GUI.Label(new Rect(size * 1.1f, r.y, half - size, size), name);

                //        GUI.Label(new Rect(half, r.y, half * 0.3f, last.height), "Format");
                //        model.Format = (ExportMeshFormat)EditorGUI.EnumPopup(new Rect(half * 1.3f, r.y, half * 0.7f, last.height), model.Format);
                //    }
                //}

                if (GUILayout.Button("Do Export"))
                {
                    DoExport();
                }
            }
            GUILayout.EndScrollView();
        }

        private static string GetMeshFormat(ExportMeshFormat format)
        {
            switch (format)
            {
                case ExportMeshFormat.FBX:
                    return ".fbx";
                case ExportMeshFormat.OBJ_NotWorking:
                    return ".obj";
                default:
                    return "";
            }
        }
        private static string GetImageFormatName(ImageFormatEnum format)
        {
            switch (format)
            {
                case ImageFormatEnum.JPG:
                    return ".jpg";
                case ImageFormatEnum.PNG:
                default:
                    return ".png";
            }
        }
        private static bool SupportsQuality(ImageFormatEnum format)
        {
            switch (format)
            {
                case ImageFormatEnum.JPG:
                    return true;
                case ImageFormatEnum.PNG:
                default:
                    return false;// lossless PNG
            }
        }

        private bool IsPowerOf2(int value)
        {
            return (value & (value - 1)) == 0;
        }

        private int NextPowerOf2(int value)
        {
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(value) / Math.Log(2)));
        }

        private void AssertShader(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogError("Shaders not found! Please reimport the Janus Exporter package");
            }
        }

        private void PreExport()
        {
            Clean();

            texturesExported = new List<Texture2D>();
            texturesExportedData = new List<TextureExportData>();

            meshesExported = new List<Mesh>();
            meshesExportedData = new List<MeshExportData>();
            meshesNames = new Dictionary<Mesh, string>();
            meshesCount = 0;

            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

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
                    }
                }
            }

            if (lightmapExportType != LightmapExportType.None &&
                lightmapped.Count > 0)
            {
                string scenePath = scene.path;
                scenePath = Path.GetDirectoryName(scenePath);
                string lightMapsFolder = Path.Combine(scenePath, scene.name);

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

                                            if (ShaderUtil.GetPropertyType(shader, k) == ShaderUtil.ShaderPropertyType.TexEnv)
                                            {
                                                if (mainTexSemantics.Contains(name.ToLower()))
                                                {
                                                    // main texture texture
                                                    lightMap.SetTexture("_MainTex", mat.GetTexture(name));
                                                }
                                            }
                                        }

                                        lightMap.SetVector("_LightMapUV", renderer.lightmapScaleOffset);
                                        lightMap.SetPass(0);
                                        Graphics.DrawMeshNow(mesh, world, j);
                                    }

                                    // This is the only way to access data from a RenderTexture
                                    Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height);
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

                                    RenderTexture renderTexture = RenderTexture.GetTemporary(lightMapSize, lightMapSize, 0, RenderTextureFormat.ARGB32);
                                    Graphics.SetRenderTarget(renderTexture);
                                    GL.Clear(true, true, new Color(0, 0, 0, 0)); // clear to transparent

                                    Material[] mats = renderer.sharedMaterials;
                                    for (int j = 0; j < mats.Length; j++)
                                    {
                                        Material mat = mats[j];

                                        lightMap.SetVector("_LightMapUV", renderer.lightmapScaleOffset);
                                        lightMap.SetPass(0);
                                        Graphics.DrawMeshNow(mesh, world, j);
                                    }

                                    // This is the only way to access data from a RenderTexture
                                    Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height);
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

                data.Created = string.IsNullOrEmpty(path);
                data.Format = this.defaultTexFormat; // start up with a default format
                data.Texture = tex;
                data.Resolution = tex.width;
                data.Quality = defaultQuality;

                if (tex.width <= PreviewSize && tex.height <= PreviewSize)
                {
                    data.Preview = data.Texture;
                }
                else
                {
                    if (data.Created)
                    {
                        data.Preview = TextureUtil.ScaleTexture(data, PreviewSize, true);
                        data.ExportAlpha = false;
                    }
                    else
                    {
                        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
                        bool wasReadable = importer.isReadable;
                        TextureImporterFormat lastFormat = importer.textureFormat;
                        if (!importer.isReadable || importer.textureFormat != TextureImporterFormat.ARGB32) // only reimport if truly needed
                        {
                            importer.isReadable = true;
                            if (!tex.name.Contains("comp_light"))
                            {
                                importer.textureFormat = TextureImporterFormat.ARGB32;
                            }
                        }

                        AssetDatabase.Refresh();
                        AssetDatabase.ImportAsset(path);

                        data.Preview = TextureUtil.ScaleTexture(data, PreviewSize, true);
                        data.ExportAlpha = importer.alphaIsTransparency;

                        if (!wasReadable)
                        {
                            importer.isReadable = false;
                            importer.textureFormat = lastFormat;
                        }
                    }
                }

                texturesExportedData.Add(data);
            }

            for (int i = 0; i < meshesExported.Count; i++)
            {
                Mesh mesh = meshesExported[i];
                MeshExportData data = new MeshExportData();
                data.Format = this.defaultMeshFormat;
                data.Mesh = mesh;
                data.Preview = AssetPreview.GetAssetPreview(mesh);

                string name = mesh.name;
                if (string.IsNullOrEmpty(name))
                {
                    meshesCount++;
                    name = "ExportedMesh" + meshesCount;
                }
                meshesNames.Add(mesh, name);

                meshesExportedData.Add(data);
            }
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
                    if (mesh == null)
                    {
                        continue;
                    }

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

                            Shader shader = mat.shader;
                            int props = ShaderUtil.GetPropertyCount(shader);
                            for (int k = 0; k < props; k++)
                            {
                                string name = ShaderUtil.GetPropertyName(shader, k);

                                if (ShaderUtil.GetPropertyType(shader, k) == ShaderUtil.ShaderPropertyType.TexEnv)
                                {
                                    if (mainTexSemantics.Contains(name.ToLower()))
                                    {
                                        Texture2D tex = (Texture2D)mat.GetTexture(name);
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
            }

            foreach (Transform child in root.transform)
            {
                RecursiveSearch(child.gameObject, data);
            }
        }

        private void Clean()
        {
            if (texturesExportedData == null)
            {
                return;
            }

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

        private int GetExponentOf2(int value)
        {
            int times = 1;
            for (int result = value; result > 2; result /= 2)
            {
                times++;
            }
            return times;
        }



        private void DoExport()
        {
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

            FBXExporter.LightmappingEnabled = lightmapExportType != LightmapExportType.None;

            for (int i = 0; i < texturesExportedData.Count; i++)
            {
                TextureExportData tex = texturesExportedData[i];
                string path = AssetDatabase.GetAssetPath(tex.Texture);
                string expPath = Path.Combine(exportPath, tex.Texture.name);

                Texture2D texture = tex.Texture;

                // force power of 2 resolution
                if (!IsPowerOf2(tex.Resolution))
                {
                    // not a power of 2
                    tex.Resolution = Math.Min(NextPowerOf2(tex.Resolution), maxLightMapResolution);
                }

                if (string.IsNullOrEmpty(path))
                {
                    // we created this texture, just export it (were free to read it)
                    tex.ExportedPath = tex.Texture.name + GetImageFormatName(tex.Format);

                    if (tex.Resolution != texture.width)
                    {
                        Texture2D scaled = TextureUtil.ScaleTexture(tex, tex.Resolution, true, filterMode);
                        ExportTexture(scaled, expPath, defaultTexFormat, null, true);
                        UObject.DestroyImmediate(scaled);
                    }
                    else
                    {
                        ExportTexture(tex.Texture, expPath, defaultTexFormat, null, true);
                    }
                }
                else
                {
                    TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
                    bool wasReadable = importer.isReadable;
                    TextureImporterFormat lastFormat = importer.textureFormat;
                    bool changed = false;
                    bool alpha = importer.alphaIsTransparency;
                    if (!importer.isReadable || importer.textureFormat != TextureImporterFormat.ARGB32) // only reimport if truly needed
                    {
                        changed = true;
                        importer.isReadable = true;
                        if (!tex.Texture.name.Contains("comp_light"))
                        {
                            importer.textureFormat = TextureImporterFormat.ARGB32;
                        }
                    }

                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(path);

                    if (tex.Resolution != texture.width)
                    {
                        Texture2D scaled = TextureUtil.ScaleTexture(tex, tex.Resolution, !alpha, filterMode);
                        ExportTexture(scaled, expPath, defaultTexFormat, null, !alpha);
                        UObject.DestroyImmediate(scaled);
                    }
                    else
                    {
                        ExportTexture(tex.Texture, expPath, tex.Format, tex.Quality, !alpha);
                    }
                    tex.ExportedPath = tex.Texture.name + GetImageFormatName(tex.Format);

                    if (changed)
                    {
                        importer.isReadable = wasReadable;
                        importer.textureFormat = lastFormat;
                        AssetDatabase.Refresh();
                        AssetDatabase.ImportAsset(path);
                    }
                }
            }

            bool switchUv = lightmapExportType == LightmapExportType.BakedMaterial;

            for (int i = 0; i < meshesExportedData.Count; i++)
            {
                MeshExportData model = meshesExportedData[i];
                string name = meshesNames[model.Mesh];
                string expPath = Path.Combine(exportPath, name);
                ExportMesh(model.Mesh, expPath, model.Format, null, switchUv);
                model.ExportedPath = name + GetMeshFormat(model.Format);
            }

            // Make the index.html file
            StringBuilder index = new StringBuilder("<html>\n\t<head>\n\t\t<title>Janus Unity Exporter v" + Version + "</title>\n\t</head>\n\t<body>\n\t\t<FireBoxRoom>\n\t\t\t<Assets>");

            List<Mesh> meshWritten = new List<Mesh>();

            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                if (!meshWritten.Contains(obj.Mesh))
                {
                    string path = meshesExportedData.First(c => c.Mesh == obj.Mesh).ExportedPath;
                    string name = meshesNames[obj.Mesh];
                    index.Append("\n\t\t\t\t<AssetObject id=\"" + name + "\" src=\"" + path + "\" />");
                    meshWritten.Add(obj.Mesh);
                }
            }

            // textures appear only once, while exported objects can appear multiple times
            // that's why we have a meshwritten list, and not a texturewritten (not anymore at least)
            for (int i = 0; i < texturesExportedData.Count; i++)
            {
                TextureExportData data = texturesExportedData[i];
                string path = data.ExportedPath;
                index.Append("\n\t\t\t\t<AssetImage id=\"" + Path.GetFileNameWithoutExtension(path) + "\" src=\"" + path + "\" />");
            }

            TextureExportData fronttex, backtex, lefttex, righttex, uptex, downtex = null;
            string skyboxdata = "";

            if (exportSkybox)
            {
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
                        fronttex = texturesExportedData.FirstOrDefault(c => c.Texture == (Texture2D)skybox.GetTexture("_FrontTex"));
                        backtex = texturesExportedData.FirstOrDefault(c => c.Texture == (Texture2D)skybox.GetTexture("_BackTex"));
                        lefttex = texturesExportedData.FirstOrDefault(c => c.Texture == (Texture2D)skybox.GetTexture("_LeftTex"));
                        righttex = texturesExportedData.FirstOrDefault(c => c.Texture == (Texture2D)skybox.GetTexture("_RightTex"));
                        uptex = texturesExportedData.FirstOrDefault(c => c.Texture == (Texture2D)skybox.GetTexture("_UpTex"));
                        downtex = texturesExportedData.FirstOrDefault(c => c.Texture == (Texture2D)skybox.GetTexture("_DownTex"));

                        if (fronttex != null)
                        {
                            skyboxdata += "skybox_front_id=\"" + Path.GetFileNameWithoutExtension(fronttex.ExportedPath) + "\" ";
                        }
                        if (backtex != null)
                        {
                            skyboxdata += "skybox_back_id=\"" + Path.GetFileNameWithoutExtension(backtex.ExportedPath) + "\" ";
                        }
                        if (lefttex != null)
                        {
                            skyboxdata += "skybox_left_id=\"" + Path.GetFileNameWithoutExtension(lefttex.ExportedPath) + "\" ";
                        }
                        if (righttex != null)
                        {
                            skyboxdata += "skybox_right_id=\"" + Path.GetFileNameWithoutExtension(righttex.ExportedPath) + "\" ";
                        }
                        if (uptex != null)
                        {
                            skyboxdata += "skybox_up_id=\"" + Path.GetFileNameWithoutExtension(uptex.ExportedPath) + "\" ";
                        }
                        if (downtex != null)
                        {
                            skyboxdata += "skybox_down_id=\"" + Path.GetFileNameWithoutExtension(downtex.ExportedPath) + "\"";
                        }

                        if (skyboxdata.EndsWith(" "))
                        {
                            skyboxdata = skyboxdata.Remove(skyboxdata.Length - 1, 1);
                        }
                        skyboxdata = " " + skyboxdata;
                    }
                }
            }

            index.Append("\n\t\t\t</Assets>\n\t\t\t<Room" + skyboxdata + ">");

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

                CultureInfo c = CultureInfo.InvariantCulture;
                Mesh mesh = obj.Mesh;
                string meshName = meshesNames[mesh];

                index.Append("\n\t\t\t\t<Object id=\"" + meshName + "\" lighting=\"true\" ");

                if (!string.IsNullOrEmpty(diffuseID))
                {
                    index.Append("image_id=\"" + diffuseID + "\" ");
                }

                if (!string.IsNullOrEmpty(lmapID))
                {
                    index.Append("lmap_id=\"" + lmapID + "\" ");
                    if (lightmapExportType == LightmapExportType.Packed)
                    {
                        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                        Vector4 lmap = renderer.lightmapScaleOffset;
                        lmap.x = Mathf.Clamp(lmap.x, 0, 1);
                        lmap.y = Mathf.Clamp(lmap.y, 0, 1);
                        lmap.z = Mathf.Clamp(lmap.z, 0, 1);
                        lmap.w = Mathf.Clamp(lmap.w, 0, 1);
                        index.Append("lmap_sca=\"" + lmap.x.ToString(c) + " " + lmap.y.ToString(c) + " " + lmap.z.ToString(c) + " " + lmap.w.ToString(c) + "\" ");
                    }
                }

                if (obj.Col != null)
                {
                    index.Append("collision_id=\"" + meshName + "\" ");
                }

                Transform trans = go.transform;
                Vector3 pos = trans.position;
                pos *= uniformScale;
                pos.x *= -1;

                Quaternion rot = trans.rotation;
                Vector3 xDir = rot * Vector3.right;
                Vector3 yDir = rot * Vector3.up;
                Vector3 zDir = rot * Vector3.forward;

                Vector3 sca = trans.lossyScale;
                sca *= uniformScale;

                index.Append("pos=\"" + pos.x.ToString(c) + " " + pos.y.ToString(c) + " " + pos.z.ToString(c) + "\" ");
                if (sca.x < 0 || sca.y < 0 || sca.z < 0)
                {
                    index.Append("cull_face=\"front\"" + "\" ");
                }

                index.Append("scale=\"");
                index.Append(sca.x.ToString(c) + " " + sca.y.ToString(c) + " " + sca.z.ToString(c) + "\" ");

                index.Append("xdir=\"");
                index.Append(xDir.x.ToString(c) + " " + xDir.y.ToString(c) + " " + xDir.z.ToString(c) + "\" ");

                index.Append("ydir=\"");
                index.Append(yDir.x.ToString(c) + " " + yDir.y.ToString(c) + " " + yDir.z.ToString(c) + "\" ");

                index.Append("zdir=\"");
                index.Append(zDir.x.ToString(c) + " " + zDir.y.ToString(c) + " " + zDir.z.ToString(c) + "\" ");

                index.Append("/>");

            }

            index.Append("\n\t\t\t</Room>\n\t\t</FireBoxRoom>\n\t</body>\n</html>");

            string indexPath = Path.Combine(exportPath, "index.html");
            File.WriteAllText(indexPath, index.ToString());
        }

        private static void ExportMesh(Mesh mesh, string path, ExportMeshFormat format, object data, bool switchUv)
        {
            string formatName = GetMeshFormat(format);
            string finalPath = path + formatName;
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            switch (format)
            {
                case ExportMeshFormat.FBX:
                    FBXExporter.ExportMesh(mesh, finalPath, switchUv);
                    break;
                case ExportMeshFormat.OBJ_NotWorking:
                    break;
            }
        }

        private static void ExportTexture(Texture2D tex, string path, ImageFormatEnum format, object data, bool zeroAlpha)
        {
            string formatName = GetImageFormatName(format);
            using (Stream output = File.OpenWrite(path + formatName))
            {
                TextureUtil.ExportTexture(tex, output, format, data, zeroAlpha);
            }
        }
    }
}

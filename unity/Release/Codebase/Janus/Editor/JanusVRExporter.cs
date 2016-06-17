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

namespace JanusVR
{
    public class JanusVRExporter : EditorWindow
    {
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
        }

        [SerializeField]
        private string exportPath = @"C:\janus";
        [SerializeField]
        private ImageFormatEnum defaultTexFormat = ImageFormatEnum.PNG;
        [SerializeField]
        private int defaultQuality = 100;
        [SerializeField]
        private TextureFilterMode filterMode;

        [SerializeField]
        private ExportMeshFormat defaultMeshFormat;
        [SerializeField]
        private float uniformScale = 1;

        [SerializeField]
        private bool exportLightmaps = true;
        [SerializeField]
        private int maxLightMapResolution = 1024;
        [SerializeField]
        private bool bakeMatLightMaps = true;



        /// <summary>
        /// Lower case values that the exporter will consider for being the Main Texture on a shader
        /// </summary>
        private string[] mainTexSemantics = new string[]
            {
                "_maintex"
            };

        private Dictionary<int, List<GameObject>> lightmapped;

        private List<Texture2D> texturesExported;
        private List<TextureExportData> texturesExportedData;

        private List<Mesh> meshesExported;
        private List<MeshExportData> meshesExportedData;
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
            filterMode = (TextureFilterMode)EditorGUILayout.EnumPopup("Texture Filter When Scaling", filterMode);
            defaultQuality = EditorGUILayout.IntSlider("Default Textures Quality", defaultQuality, 0, 100);

            uniformScale = EditorGUILayout.FloatField("Uniform Scale", uniformScale);

            exportLightmaps = GUILayout.Toggle(exportLightmaps, "Export Lightmaps");
            if (exportLightmaps)
            {
                maxLightMapResolution = Math.Max(32, EditorGUILayout.IntField("Max Lightmap Resolution", maxLightMapResolution));
                bakeMatLightMaps = GUILayout.Toggle(bakeMatLightMaps, "Bake Materials to Lightmaps");
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

                        GUI.DrawTexture(new Rect(size * 0.1f, r.y, size, size), tex.Texture);
                        GUI.Label(new Rect(size * 1.1f, r.y, half - size, size), tex.Texture.name);

                        GUI.Label(new Rect(half, r.y, half * 0.3f, last.height), "Format");
                        tex.Format = (ImageFormatEnum)EditorGUI.EnumPopup(new Rect(half * 1.3f, r.y, half * 0.7f, last.height), tex.Format);
                        GUI.Label(new Rect(half, r.y + last.height, half * 0.3f, last.height), "Resolution");
                        tex.Resolution = EditorGUI.IntSlider(new Rect(half * 1.3f, r.y + last.height, half * 0.7f, last.height), tex.Resolution, 32, tex.Texture.width);
                        if (SupportsQuality(tex.Format))
                        {
                            GUI.Label(new Rect(half, r.y + (last.height * 2), half * 0.3f, last.height), "Quality");
                            tex.Quality = EditorGUI.IntSlider(new Rect(half * 1.3f, r.y + (last.height * 2), half * 0.7f, last.height), tex.Quality, 0, 100);
                        }
                    }
                }

                perModelOptions = EditorGUILayout.Foldout(perModelOptions, "Per Model Options");
                if (perModelOptions)
                {
                    for (int i = 0; i < meshesExportedData.Count; i++)
                    {
                        MeshExportData model = meshesExportedData[i];
                        Rect r = GUILayoutUtility.GetRect(Screen.width, size * 1.05f);

                        GUI.DrawTexture(new Rect(size * 0.1f, r.y, size, size), model.Preview);
                        GUI.Label(new Rect(size * 1.1f, r.y, half - size, size), model.Mesh.name);

                        GUI.Label(new Rect(half, r.y, half * 0.3f, last.height), "Format");
                        model.Format = (ExportMeshFormat)EditorGUI.EnumPopup(new Rect(half * 1.3f, r.y, half * 0.7f, last.height), model.Format);
                    }
                }

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

        private void PreExport()
        {
            Clean();

            texturesExported = new List<Texture2D>();
            texturesExportedData = new List<TextureExportData>();

            meshesExported = new List<Mesh>();
            meshesExportedData = new List<MeshExportData>();

            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            lightmapped = new Dictionary<int, List<GameObject>>();
            exported = new ExportedData();

            for (int i = 0; i < roots.Length; i++)
            {
                RecursiveSearch(roots[i], exported);
            }

            if (exportLightmaps)
            {
                if (lightmapped.Count == 0)
                {
                    return;
                }

                string scenePath = scene.path;
                scenePath = Path.GetDirectoryName(scenePath);
                string lightMapsFolder = Path.Combine(scenePath, scene.name);

                if (bakeMatLightMaps)
                {
                    // only load shader now, so if the user is not exporting lightmaps
                    // he doesn't need to have it on his project folder
                    Shader lightMapShader = Shader.Find("Hidden/LightMapExtracter");
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

                            ExportedObject eobj = exported.exportedObjs.First(c => c.go == obj);
                            eobj.diffuseMapTex = tex;

                            Graphics.SetRenderTarget(null);
                            RenderTexture.ReleaseTemporary(renderTexture);

                            lmap++;
                        }
                    }
                    UnityEngine.Object.DestroyImmediate(lightMap);
                }
                else
                {
                    Shader lightMapShader = Shader.Find("Hidden/LightMapToScreen");
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
                        // but we can't! yay! So we have to render everything to a custom RenderTexture!
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

                            Material[] mats = renderer.sharedMaterials;
                            for (int j = 0; j < mats.Length; j++)
                            {
                                Material mat = mats[j];

                                lightMap.SetVector("_LightMapUV", renderer.lightmapScaleOffset);
                                lightMap.SetPass(0);
                                Graphics.DrawMeshNow(mesh, world, j);
                            }

                            ExportedObject eobj = exported.exportedObjs.First(c => c.go == obj);
                            eobj.lightMapTex = decTex;
                        }

                        decTex.ReadPixels(new Rect(0, 0, decTex.width, decTex.height), 0, 0);
                        decTex.Apply(); // send the data back to the GPU so we can draw it on the preview area

                        Graphics.SetRenderTarget(null);
                        RenderTexture.ReleaseTemporary(renderTexture);
                    }
                    UnityEngine.Object.DestroyImmediate(lightMap);
                }
            }

            for (int i = 0; i < texturesExported.Count; i++)
            {
                Texture2D tex = texturesExported[i];
                TextureExportData data = new TextureExportData();
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
                data.Format = this.defaultMeshFormat;
                data.Mesh = mesh;

                data.Preview = AssetPreview.GetAssetPreview(mesh);

                meshesExportedData.Add(data);
            }
        }

        private void RecursiveSearch(GameObject root, ExportedData data)
        {
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

                    ExportedObject exp = data.exportedObjs.FirstOrDefault(c => c.go == root);
                    if (exp == null)
                    {
                        exp = new ExportedObject();
                        exp.go = root;
                        data.exportedObjs.Add(exp);
                    }
                    exp.mesh = mesh;

                    // export textures
                    if (!bakeMatLightMaps) // if were baking we dont need the original textures
                    {
                        Material[] mats = meshRen.sharedMaterials;
                        for (int j = 0; j < mats.Length; j++)
                        {
                            Material mat = mats[j];
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

                                        exp.diffuseMapTex = tex;
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
                        // Render lightmaps for this object
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

                    ExportedObject exp = data.exportedObjs.FirstOrDefault(c => c.go == root);
                    if (exp == null)
                    {
                        exp = new ExportedObject();
                        exp.go = root;
                        data.exportedObjs.Add(exp);
                    }
                    exp.col = col;
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
                string path = AssetDatabase.GetAssetPath(tex.Texture);

                if (string.IsNullOrEmpty(path))
                {
                    // we made this, we delete this
                    GameObject.DestroyImmediate(tex.Texture);
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

        private Texture2D ScaleTexture(TextureExportData tex)
        {
            Texture2D texture = tex.Texture;
            // scale the texture
            Texture2D scaled = new Texture2D(tex.Resolution, tex.Resolution);

            Color[] source = texture.GetPixels();
            Color[] target = new Color[tex.Resolution * tex.Resolution];

            int scale = texture.width / tex.Resolution;
            float sca = scale * 2;

            switch (filterMode)
            {
                case TextureFilterMode.Average:
                    {
                        for (int x = 0; x < tex.Resolution; x++)
                        {
                            for (int y = 0; y < tex.Resolution; y++)
                            {
                                // sample neighbors
                                int xx = x * scale;
                                int yy = y * scale;

                                float r = 0, g = 0, b = 0, a = 0;

                                int ind = xx + (yy * texture.width);
                                for (int j = 0; j < scale; j++)
                                {
                                    Color col = source[ind + j];
                                    r += col.r;
                                    g += col.g;
                                    b += col.b;
                                    a += col.a;
                                }
                                ind = xx + ((yy + 1) * texture.width);
                                for (int j = 0; j < scale; j++)
                                {
                                    Color col = source[ind + j];
                                    r += col.r;
                                    g += col.g;
                                    b += col.b;
                                    a += col.a;
                                }

                                r = r / sca;
                                g = g / sca;
                                b = b / sca;
                                a = a / sca;

                                Color sampled = new Color(r, g, b, a);
                                target[x + (y * tex.Resolution)] = sampled;
                            }
                        }
                    }
                    break;
                case TextureFilterMode.Nearest:
                    {
                        for (int x = 0; x < tex.Resolution; x++)
                        {
                            for (int y = 0; y < tex.Resolution; y++)
                            {
                                // sample neighbors
                                int xx = x * scale;
                                int yy = y * scale;
                                int ind = xx + (yy * texture.width);

                                Color col = source[ind];
                                target[x + (y * tex.Resolution)] = col;
                            }
                        }
                    }
                    break;
            }

            scaled.SetPixels(target);
            scaled.Apply();
            return scaled;
        }

        private void DoExport()
        {
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
                        Texture2D scaled = ScaleTexture(tex);
                        ExportTexture(scaled, expPath, defaultTexFormat, null);
                        UnityEngine.Object.DestroyImmediate(scaled);
                    }
                    else
                    {
                        ExportTexture(tex.Texture, expPath, defaultTexFormat, null);
                    }
                }
                else
                {
                    TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
                    bool wasReadable = importer.isReadable;
                    TextureImporterFormat lastFormat = importer.textureFormat;
                    bool changed = false;
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
                        Texture2D scaled = ScaleTexture(tex);
                        ExportTexture(scaled, expPath, defaultTexFormat, null);
                        UnityEngine.Object.DestroyImmediate(scaled);
                    }
                    else
                    {
                        ExportTexture(tex.Texture, expPath, tex.Format, tex.Quality);
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

            for (int i = 0; i < meshesExportedData.Count; i++)
            {
                MeshExportData model = meshesExportedData[i];
                string expPath = Path.Combine(exportPath, model.Mesh.name);
                ExportMesh(model.Mesh, expPath, model.Format, null, exportLightmaps);
                model.ExportedPath = model.Mesh.name + GetMeshFormat(model.Format);
            }

            // Make the index.html file
            StringBuilder index = new StringBuilder("<html>\n\t<head>\n\t\t<title>Unreal Export</title>\n\t</head>\n\t<body>\n\t\t<FireBoxRoom>\n\t\t\t<Assets>");

            List<Texture2D> texWritten = new List<Texture2D>();
            List<Mesh> meshWritten = new List<Mesh>();

            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                if (obj.diffuseMapTex != null &&
                    !texWritten.Contains(obj.diffuseMapTex))
                {
                    string path = texturesExportedData.First(c => c.Texture == obj.diffuseMapTex).ExportedPath;
                    index.Append("\n\t\t\t\t<AssetImage id=\"" + Path.GetFileNameWithoutExtension(path) + "\" src=\"" + path + "\" />");
                    texWritten.Add(obj.diffuseMapTex);
                }
                if (obj.lightMapTex != null &&
                    !texWritten.Contains(obj.lightMapTex))
                {
                    string path = texturesExportedData.First(c => c.Texture == obj.lightMapTex).ExportedPath;
                    index.Append("\n\t\t\t\t<AssetImage id=\"" + Path.GetFileNameWithoutExtension(path) + "\" src=\"" + path + "\" />");
                    texWritten.Add(obj.lightMapTex);
                }
            }
            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                if (!meshWritten.Contains(obj.mesh))
                {
                    string path = meshesExportedData.First(c => c.Mesh == obj.mesh).ExportedPath;
                    index.Append("\n\t\t\t\t<AssetObject id=\"" + obj.mesh.name + "\" src=\"" + path + "\" />");
                    meshWritten.Add(obj.mesh);
                }
            }

            index.Append("\n\t\t\t</Assets>\n\t\t\t<Room>");

            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                GameObject go = obj.go;

                string diffuseID = "";
                string lmapID = "";
                if (obj.diffuseMapTex != null)
                {
                    diffuseID = Path.GetFileNameWithoutExtension(texturesExportedData.First(k => k.Texture == obj.diffuseMapTex).ExportedPath);
                }
                if (obj.lightMapTex != null)
                {
                    lmapID = Path.GetFileNameWithoutExtension(texturesExportedData.First(k => k.Texture == obj.lightMapTex).ExportedPath);
                }

                CultureInfo c = CultureInfo.InvariantCulture;

                if (string.IsNullOrEmpty(diffuseID))
                {
                    index.Append("\n\t\t\t\t<Object collision_id=\"" + obj.mesh.name + "\" id=\"" + obj.mesh.name + "\" lighting=\"true\" pos=\"");
                }
                else
                {
                    if (exportLightmaps && !bakeMatLightMaps)
                    {
                        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
                        Vector4 lmap = renderer.lightmapScaleOffset;
                        lmap.x = Mathf.Clamp(lmap.x, 0, 1);
                        lmap.y = Mathf.Clamp(lmap.y, 0, 1);
                        lmap.z = Mathf.Clamp(lmap.z, 0, 1);
                        lmap.w = Mathf.Clamp(lmap.w, 0, 1);

                        index.Append("\n\t\t\t\t<Object collision_id=\"" + obj.mesh.name + "\" id=\"" + obj.mesh.name + "\" lmapid=\"" + lmapID + "\" image_id=\"" + diffuseID + "\" lmapscale=\"");
                        index.Append(lmap.x.ToString(c) + " " + lmap.y.ToString(c) + " " + lmap.z.ToString(c) + " " + lmap.w.ToString(c));
                        index.Append("\" lighting=\"true\" pos=\"");
                    }
                    else
                    {
                        index.Append("\n\t\t\t\t<Object collision_id=\"" + obj.mesh.name + "\" id=\"" + obj.mesh.name + "\" image_id=\"" + diffuseID + "\" lighting=\"true\" pos=\"");
                    }
                }

                Transform trans = go.transform;
                Vector3 pos = trans.position;
                pos *= uniformScale;

                Quaternion rot = trans.rotation;
                Vector3 xDir = rot * Vector3.right;
                Vector3 yDir = rot * Vector3.up;
                Vector3 zDir = rot * Vector3.forward;

                Vector3 sca = trans.lossyScale;
                sca *= uniformScale;


                index.Append(pos.x.ToString(c) + " " + pos.y.ToString(c) + " " + pos.z.ToString(c));
                if (sca.x < 0 || sca.y < 0 || sca.z < 0)
                {
                    index.Append("\" cull_face=\"front");
                }

                index.Append("\" scale=\"");
                index.Append(sca.x.ToString(c) + " " + sca.y.ToString(c) + " " + sca.z.ToString(c));

                index.Append("\" xdir=\"");
                index.Append(xDir.x.ToString(c) + " " + xDir.y.ToString(c) + " " + xDir.z.ToString(c));

                index.Append("\" ydir=\"");
                index.Append(yDir.x.ToString(c) + " " + yDir.y.ToString(c) + " " + yDir.z.ToString(c));

                index.Append("\" zdir=\"");
                index.Append(zDir.x.ToString(c) + " " + zDir.y.ToString(c) + " " + zDir.z.ToString(c));

                index.Append("\" />");

            }

            index.Append("\n\t\t\t</Room>\n\t\t</FireBoxRoom>\n\t</body>\n</html>");

            string indexPath = Path.Combine(exportPath, "index.html");
            File.WriteAllText(indexPath, index.ToString());
        }

        private static void ExportMesh(Mesh mesh, string path, ExportMeshFormat format, object data, bool pog)
        {
            string formatName = GetMeshFormat(format);
            string finalPath = path + formatName;
            switch (format)
            {
                case ExportMeshFormat.FBX:
                    if (pog)
                    {
                        // for now janus doesnt support UV1 on FBX, so we export UV1 into 0
                        FBXExporter.ExportMeshPOG(mesh, finalPath);
                    }
                    else
                    {
                        FBXExporter.ExportMesh(mesh, finalPath);
                    }
                    break;
                case ExportMeshFormat.OBJ_NotWorking:
                    break;
            }
        }

        private static void ExportTexture(Texture2D tex, string path, ImageFormatEnum format, object data)
        {
            string formatName = GetImageFormatName(format);
            using (Stream output = File.OpenWrite(path + formatName))
            {
                TextureUtil.ExportTexture(tex, output, format, data);
            }
        }
    }
}

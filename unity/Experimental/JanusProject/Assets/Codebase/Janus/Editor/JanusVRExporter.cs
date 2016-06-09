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
            internal List<ExportedMesh> exportedMeshs;

            internal ExportedData()
            {
                exportedObjs = new List<ExportedObject>();
                exportedMeshs = new List<ExportedMesh>();
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
        private int maxLightMapResolution = 1024;
        [SerializeField]
        private float uniformScale = 1;
        [SerializeField]
        private ExportMeshFormat meshFormat;
        [SerializeField]
        private bool exportLightmaps = true;

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

            meshFormat = (ExportMeshFormat)EditorGUILayout.EnumPopup("Mesh Format", meshFormat);
            uniformScale = EditorGUILayout.FloatField("Uniform Scale", uniformScale);

            exportLightmaps = GUILayout.Toggle(exportLightmaps, "Export Lightmaps");
            if (exportLightmaps)
            {
                maxLightMapResolution = Math.Max(32, EditorGUILayout.IntField("Max Lightmap Resolution", maxLightMapResolution));
                bakeMatLightMaps = GUILayout.Toggle(bakeMatLightMaps, "Bake Materials to Lightmaps");
            }


            if (GUILayout.Button("Export"))
            {
                DoExport();
            }
        }

        private string GetMeshFormat()
        {
            switch (meshFormat)
            {
                case ExportMeshFormat.FBX:
                    return ".fbx";
                case ExportMeshFormat.OBJ:
                    return ".obj";
                default:
                    return "";
            }
        }

        private void DoExport()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            lightmapped = new Dictionary<int, List<GameObject>>();
            ExportedData exported = new ExportedData();

            for (int i = 0; i < roots.Length; i++)
            {
                RecursiveSearch(roots[i], exported);
            }


            if (exportLightmaps)
            {
                if (bakeMatLightMaps)
                {
                    if (lightmapped.Count == 0)
                    {
                        return;
                    }

                    // only load shader now, so if the user is not exporting lightmaps
                    // he doesn't need to have it on his project folder
                    Shader lightMapShader = Shader.Find("Hidden/LightMapExtracter");
                    Material lightMap = new Material(lightMapShader);
                    lightMap.SetPass(0);

                    string scenePath = scene.path;
                    scenePath = Path.GetDirectoryName(scenePath);
                    string lightMapsFolder = Path.Combine(scenePath, scene.name);

                    // export lightmaps
                    int exp = 0;
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
                            GL.Clear(true, true, Color.red);

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
                            byte[] bytes = tex.EncodeToPNG();
                            string lightName = "Lightmap" + exp;
                            File.WriteAllBytes(Path.Combine(exportPath, lightName + ".png"), bytes);

                            ExportedMesh me = new ExportedMesh();
                            me.mesh = mesh;
                            me.lightMapPath = lightName;
                            me.go = obj;
                            exported.exportedMeshs.Add(me);

                            Graphics.SetRenderTarget(null);
                            RenderTexture.ReleaseTemporary(renderTexture);
                            UnityEngine.Object.DestroyImmediate(tex);

                            exp++;
                        }
                    }
                    UnityEngine.Object.DestroyImmediate(lightMap);
                }
            }
            else
            {
                // export materials textures

            }


            // Make the index.html file
            StringBuilder index = new StringBuilder("<html>\n\t<head>\n\t\t<title>Unreal Export</title>\n\t</head>\n\t<body>\n\t\t<FireBoxRoom>\n\t\t\t<Assets>");

            List<string> written = new List<string>();

            for (int i = 0; i < exported.exportedMeshs.Count; i++)
            {
                ExportedMesh expo = exported.exportedMeshs[i];
                if (!written.Contains(expo.lightMapPath))
                {
                    index.Append("\n\t\t\t\t<AssetImage id=\"" + Path.GetFileNameWithoutExtension(expo.lightMapPath) + "\" src=\"" + expo.lightMapPath + ".png\" />");
                    written.Add(expo.lightMapPath);
                }
            }
            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                if (!written.Contains(obj.diffuseTexPath))
                {
                    index.Append("\n\t\t\t\t<AssetImage id=\"" + Path.GetFileNameWithoutExtension(obj.diffuseTexPath) + "\" src=\"" + obj.diffuseTexPath + "\" />");
                    written.Add(obj.diffuseTexPath);
                }
            }

            string format = GetMeshFormat();
            for (int i = 0; i < exported.exportedMeshs.Count; i++)
            {
                ExportedMesh expo = exported.exportedMeshs[i];
                if (!written.Contains(expo.mesh.name))
                {
                    index.Append("\n\t\t\t\t<AssetObject id=\"" + expo.mesh.name + "\" src=\"" + expo.mesh.name + format + "\" />");
                    written.Add(expo.mesh.name);
                }
            }
            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                if (!written.Contains(obj.mesh.name))
                {
                    index.Append("\n\t\t\t\t<AssetObject id=\"" + obj.mesh.name + "\" src=\"" + obj.mesh.name + format + "\" />");
                    written.Add(obj.mesh.name);
                }
            }
            index.Append("\n\t\t\t</Assets>\n\t\t\t<Room>");

            for (int i = 0; i < exported.exportedObjs.Count; i++)
            {
                ExportedObject obj = exported.exportedObjs[i];
                GameObject go = obj.go;

                ExportedMesh emesh = exported.exportedMeshs.FirstOrDefault(x => x.go == go);
                string imageID = "";
                if (emesh != null && emesh.mesh != null)
                {
                    imageID = Path.GetFileNameWithoutExtension(emesh.lightMapPath);
                }
                else
                {
                    imageID = obj.diffuseTexPath;
                }

                if (string.IsNullOrEmpty(imageID))
                {
                    index.Append("\n\t\t\t\t<Object collision_id=\"" + obj.mesh.name + "\" id=\"" + obj.mesh.name + "\" lighting=\"true\" pos=\"");
                }
                else
                {
                    index.Append("\n\t\t\t\t<Object collision_id=\"" + obj.mesh.name + "\" id=\"" + obj.mesh.name + "\" image_id=\"" + imageID + "\" lighting=\"true\" pos=\"");
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

                CultureInfo c = CultureInfo.InvariantCulture;

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
                    if (!data.exportedObjs.Any(c => c.mesh == mesh))
                    {
                        switch (meshFormat)
                        {
                            case ExportMeshFormat.FBX:
                                if (exportLightmaps)
                                {
                                    // for now janus doesnt support UV1, so we export UV1 into 0
                                    FBXExporter.ExportMeshPOG(mesh, Path.Combine(exportPath, mesh.name + ".fbx"));
                                }
                                else
                                {
                                    FBXExporter.ExportMesh(mesh, Path.Combine(exportPath, mesh.name + ".fbx"));
                                }
                                break;
                            case ExportMeshFormat.OBJ:
                                break;
                        }
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
                    if (exportLightmaps && !bakeMatLightMaps || !exportLightmaps)
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
                                        string texSource = AssetDatabase.GetAssetPath(tex);

                                        string texPath = Path.Combine(exportPath, Path.GetFileNameWithoutExtension(texSource)) + ".png";
                                        exp.diffuseTexPath = texPath.Replace(exportPath, "");
                                        if (exp.diffuseTexPath.StartsWith(@"\") ||
                                            exp.diffuseTexPath.StartsWith("/"))
                                        {
                                            exp.diffuseTexPath = exp.diffuseTexPath.Remove(0, 1);
                                        }

                                        if (!File.Exists(texPath))
                                        {
                                            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(texSource);
                                            bool wasReadable = importer.isReadable;
                                            TextureImporterFormat lastFormat = importer.textureFormat;
                                            importer.isReadable = true;
                                            importer.textureFormat = TextureImporterFormat.ARGB32;

                                            AssetDatabase.Refresh();
                                            AssetDatabase.ImportAsset(texSource);

                                            byte[] png = tex.EncodeToPNG();
                                            File.WriteAllBytes(texPath, png);

                                            importer.isReadable = wasReadable;
                                            importer.textureFormat = lastFormat;
                                            AssetDatabase.Refresh();
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
    }
}

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    public class MtlImporterWindow : EditorWindow
    {
        public MtlImporterWindow()
        {
            this.titleContent = new GUIContent("MTL Importer");
        }

        //[MenuItem("Assets/Import MTL")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:
            MtlImporterWindow window = EditorWindow.GetWindow<MtlImporterWindow>();
            window.Show();
        }

        private string importPath;
        private GameObject sourceModel;

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Export Path");
            importPath = EditorGUILayout.TextField(importPath);
            if (GUILayout.Button("..."))
            {
                importPath = EditorUtility.OpenFilePanel("MTL Importer", importPath, "mtl");
            }
            EditorGUILayout.EndHorizontal();

            sourceModel = (GameObject)EditorGUILayout.ObjectField("Source Model", sourceModel, typeof(GameObject), true);

            if (GUILayout.Button("Import"))
            {
                ImportMTL();
            }
        }

        private void ImportMTL()
        {
            string projectFolder = Application.dataPath;

            string rootFolder = AssetDatabase.GetAssetPath(sourceModel);
            rootFolder = Path.GetDirectoryName(rootFolder);

            string mtlRoot = Path.GetDirectoryName(importPath);
            string matsRoot = Path.Combine(rootFolder, "Materials");
            Directory.CreateDirectory(matsRoot);

            using (Stream stream = File.OpenRead(importPath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (line.StartsWith("newmtl"))
                        {
                            Material mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                            mat = UnityEngine.Object.Instantiate<Material>(mat);

                            string matPath = Path.Combine(matsRoot, line.Split(' ')[1] + ".mat");
                            AssetDatabase.CreateAsset(mat, matPath);

                            while (!reader.EndOfStream)
                            {
                                line = reader.ReadLine();

                                if (string.IsNullOrEmpty(line))
                                {
                                    break;
                                }

                                int mapPos = line.IndexOf("map");
                                if (mapPos != -1)
                                {
                                    // texture (but might be map on the texture name)
                                    int space = line.IndexOf(' ', mapPos) + 1;
                                    string path = line.Substring(space, line.Length - space);

                                    string finalTex = Path.Combine(rootFolder, path);

                                    bool present = true;
                                    if (!File.Exists(finalTex))
                                    {
                                        string source = Path.Combine(mtlRoot, path);
                                        if (File.Exists(source))
                                        {
                                            // import texture
                                            // just copy to our project folder, Unity will import it on its own
                                            File.Copy(source, finalTex);
                                        }
                                        else
                                        {
                                            present = false;
                                        }
                                    }

                                    if (present)
                                    {
                                        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(finalTex);
                                        string mapName = line.Substring(0, mapPos + space - 1);

                                        if (mapName.Contains("map_Kd"))
                                        {
                                            mat.SetTexture("_MainTex", tex);
                                        }
                                    }
                                }
                            }

                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            }
        }
    }
}
#endif
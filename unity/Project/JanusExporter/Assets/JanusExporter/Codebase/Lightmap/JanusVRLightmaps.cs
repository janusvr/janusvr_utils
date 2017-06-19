#if UNITY_EDITOR
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

namespace JanusVR
{
    public class JanusVRLightmaps
    {
        private SceneExportData parent;
        public Texture2D Preview { get; private set; }

        public JanusVRLightmaps(SceneExportData scene)
        {
            parent = scene;
        }

        private float lastExposure;
        private LightmapExportType lastType;
        private ColorSpace lastColorSpace;

        public bool BuildPreview(LightmapExportType type, float exposure)
        {
            if (Preview)
            {
                if (type == lastType &&
                    exposure == lastExposure &&
                    lastColorSpace == PlayerSettings.colorSpace ||
                    parent.IsSceneUnloaded())
                {
                    return true;
                }
            }

            // just speeds up things when the user changes color space of the project
            lastColorSpace = PlayerSettings.colorSpace;
            lastExposure = exposure;
            lastType = type;

            string lightMapsFolder = parent.GetLightmapsFolder();

            Shader exposureShader = Shader.Find("Hidden/ExposureShader");
            if (!JanusUtil.AssertShader(exposureShader))
            {
                return false;
            }

            DirectoryInfo sceneDir = new DirectoryInfo(lightMapsFolder);
            FileInfo[] maps = sceneDir.GetFiles("*.exr");
            FileInfo first = maps.FirstOrDefault(c => c.Name.Contains("_comp_light"));

            if (first == null)
            {
                return false;
            }

            Material exposureMat = new Material(exposureShader);

            string lightMapFile = Path.Combine(lightMapsFolder, first.Name);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(lightMapFile);
            exposureMat.SetTexture("_InputTex", texture);
            exposureMat.SetFloat("_Exposure", exposure);
            exposureMat.SetFloat("_IsLinear", PlayerSettings.colorSpace == ColorSpace.Linear ? 2 : 0);

            Texture2D decTex;
            if (Preview)
            {
                if (texture.width == Preview.width &&
                    texture.height == Preview.height)
                {
                    decTex = Preview;
                }
                else
                {
                    decTex = new Texture2D(texture.width, texture.height);
                }
            }
            else
            {
                decTex = new Texture2D(texture.width, texture.height);
            }
            decTex.name = "LightmapPreview";

            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height);
            Graphics.SetRenderTarget(renderTexture);
            GL.Clear(true, true, new Color(0, 0, 0, 0)); // clear to transparent

            exposureMat.SetPass(0);
            Graphics.DrawMeshNow(JanusResources.PlaneMesh, Matrix4x4.identity);

            decTex.ReadPixels(new Rect(0, 0, decTex.width, decTex.height), 0, 0);
            decTex.Apply(); // send the data back to the GPU so we can draw it on the preview area

            Graphics.SetRenderTarget(null);
            RenderTexture.ReleaseTemporary(renderTexture);

            Preview = decTex;
            return true;
        }

        public bool BuildLightmaps(LightmapExportType type)
        {
            string lightMapsFolder = parent.GetLightmapsFolder();
            switch (type)
            {
                case LightmapExportType.Packed:
                    {
                        Shader lightMapShader = Shader.Find("Hidden/LMapPacked");
                        if (!JanusUtil.AssertShader(lightMapShader))
                        {
                            return false;
                        }

                        Material lightMap = new Material(lightMapShader);
                        lightMap.SetPass(0);
                        lightMap.SetFloat("_IsLinear", PlayerSettings.colorSpace == ColorSpace.Linear ? 1 : 0);

                        // just pass the textures forward
                        // export lightmaps
                        foreach (var lightPair in parent.lightmapped)
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
                            parent.texturesExported.Add(decTex);

                            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height);
                            Graphics.SetRenderTarget(renderTexture);
                            GL.Clear(true, true, new Color(0, 0, 0, 0)); // clear to transparent

                            for (int i = 0; i < toRender.Count; i++)
                            {
                                GameObject obj = toRender[i];
                                MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
                                MeshFilter filter = obj.GetComponent<MeshFilter>();

                                Mesh mesh = filter.sharedMesh;
                                //Transform trans = obj.transform;
                                //Matrix4x4 world = Matrix4x4.TRS(trans.position, trans.rotation, trans.lossyScale);

                                lightMap.SetVector("_LightMapUV", renderer.lightmapScaleOffset);
                                lightMap.SetPass(0);
                                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);

                                ExportedObject eobj = parent.exportedObjs.First(c => c.GameObject == obj);
                                eobj.LightMapTex = decTex;
                            }

                            decTex.ReadPixels(new Rect(0, 0, decTex.width, decTex.height), 0, 0);
                            decTex.Apply(); // send the data back to the GPU so we can draw it on the preview area

                            Graphics.SetRenderTarget(null);
                            RenderTexture.ReleaseTemporary(renderTexture);
                        }

                        UObject.DestroyImmediate(lightMap);
                        return true;
                    }
            }

            return false;
        }
    }
}

#endif
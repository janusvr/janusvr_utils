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
    }
}
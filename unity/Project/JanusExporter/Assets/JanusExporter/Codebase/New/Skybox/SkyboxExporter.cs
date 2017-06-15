#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace JanusVR
{
    public class SkyboxExporter : Exporter
    {
        private JanusRoom room;

        private AssetImage assetLeft;
        private AssetImage assetRight;
        private AssetImage assetUp;
        private AssetImage assetDown;
        private AssetImage assetForward;
        private AssetImage assetBack;

        public override void Initialize(JanusRoom room)
        {
            this.room = room;
        }

        public override void PreExport()
        {

        }

        private bool renderedSkybox;
        public override void Export()
        {
            if (!room.SkyboxEnabled)
            {
                return;
            }

            // look for skybox and grab all 6 textures if it's 6-sided
            Material skybox = RenderSettings.skybox;
            if (skybox != null)
            {
                bool proceed = true;
                string[] skyboxTexNames = JanusGlobals.SkyboxTexNames;
                for (int i = 0; i < skyboxTexNames.Length; i++)
                {
                    if (!skybox.HasProperty(skyboxTexNames[i]))
                    {
                        proceed = false;
                    }
                }

                if (proceed)
                {
                    Texture2D skyBoxForward = (Texture2D)skybox.GetTexture("_FrontTex");
                    Texture2D skyBoxBack = (Texture2D)skybox.GetTexture("_BackTex");
                    Texture2D skyBoxLeft = (Texture2D)skybox.GetTexture("_LeftTex");
                    Texture2D skyBoxRight = (Texture2D)skybox.GetTexture("_RightTex");
                    Texture2D skyBoxUp = (Texture2D)skybox.GetTexture("_UpTex");
                    Texture2D skyBoxDown = (Texture2D)skybox.GetTexture("_DownTex");

                    assetForward = TryAddTexture(skyBoxForward);
                    assetBack = TryAddTexture(skyBoxBack);
                    assetLeft = TryAddTexture(skyBoxLeft);
                    assetRight = TryAddTexture(skyBoxRight);
                    assetUp = TryAddTexture(skyBoxUp);
                    assetDown = TryAddTexture(skyBoxDown);

                    renderedSkybox = false;
                }
                else
                {
                    // the skybox is not a 6-texture skybox
                    // lets render it to one then
                    GameObject temp = new GameObject("__TempSkyRender");
                    Camera cam = temp.AddComponent<Camera>();

                    cam.enabled = false;

                    int exportSkyboxResolution = room.SkyboxResolution;
                    RenderTexture tex = new RenderTexture(exportSkyboxResolution, exportSkyboxResolution, 0);
                    cam.targetTexture = tex;
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.cullingMask = 0;
                    cam.orthographic = true;

                    Texture2D skyBoxLeft = RenderSkyBoxSide(Vector3.left, "Left", tex, cam);
                    Texture2D skyBoxRight = RenderSkyBoxSide(Vector3.right, "Right", tex, cam);
                    Texture2D skyBoxForward = RenderSkyBoxSide(Vector3.forward, "Forward", tex, cam);
                    Texture2D skyBoxBack = RenderSkyBoxSide(Vector3.back, "Back", tex, cam);
                    Texture2D skyBoxUp = RenderSkyBoxSide(Vector3.up, "Up", tex, cam);
                    Texture2D skyBoxDown = RenderSkyBoxSide(Vector3.down, "Down", tex, cam);

                    cam.targetTexture = null;
                    Graphics.SetRenderTarget(null);

                    GameObject.DestroyImmediate(tex);
                    GameObject.DestroyImmediate(temp);

                    assetLeft = AddTexture(skyBoxLeft);
                    assetRight = AddTexture(skyBoxRight);
                    assetForward = AddTexture(skyBoxForward);
                    assetBack = AddTexture(skyBoxBack);
                    assetUp = AddTexture(skyBoxUp);
                    assetDown = AddTexture(skyBoxDown);

                    renderedSkybox = true;
                }
            }

            room.SkyboxLeft = assetLeft;
            room.SkyboxRight = assetRight;
            room.SkyboxFront = assetForward;
            room.SkyboxBack = assetBack;
            room.SkyboxUp = assetUp;
            room.SkyboxDown = assetDown;
        }

        private AssetImage TryAddTexture(Texture2D tex)
        {
            List<AssetImage> images = room.AssetImages;
            AssetImage image = images.FirstOrDefault(c => c.Texture == tex);

            if (image == null)
            {
                return AddTexture(tex);
            }
            return image;
        }

        private AssetImage AddTexture(Texture2D tex)
        {
            AssetImage image = new AssetImage();
            image.id = tex.name;
            image.src = tex.name;
            image.Texture = tex;
            room.AddAssetImage(image);

            return image;
        }

        private Texture2D RenderSkyBoxSide(Vector3 direction, string name, RenderTexture tmpTex, Camera cam)
        {
            cam.gameObject.transform.LookAt(direction);
            cam.Render();

            Graphics.SetRenderTarget(tmpTex);
            Texture2D left = new Texture2D(tmpTex.width, tmpTex.height, TextureFormat.ARGB32, false, false);
            left.ReadPixels(new Rect(0, 0, tmpTex.width, tmpTex.height), 0, 0);
            left.name = "SkyBox" + name;

            //texturesExported.Add(left);

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
                string[] skyboxTexNames = JanusGlobals.SkyboxTexNames;
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

        public override void Cleanup()
        {
            if (renderedSkybox)
            {
                UObject.DestroyImmediate(assetLeft.Texture);
                UObject.DestroyImmediate(assetRight.Texture);
                UObject.DestroyImmediate(assetUp.Texture);
                UObject.DestroyImmediate(assetDown.Texture);
                UObject.DestroyImmediate(assetForward.Texture);
                UObject.DestroyImmediate(assetBack.Texture);
            }
        }
    }
}
#endif
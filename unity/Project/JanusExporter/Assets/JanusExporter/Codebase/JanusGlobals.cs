#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UObject = UnityEngine.Object;

namespace JanusVR
{
    public static class JanusGlobals
    {
        public const decimal Version = 2.12M;
        public static int DecimalCasesPosition = 4;
        public static int DecimalCasesLightmap = 6;

        /// <summary>
        /// Lower case values that the exporter will consider for being the Main Texture on a shader
        /// </summary>
        public static readonly string[] SemanticsMainTex = new string[]
        {
            "_maintex"
        };

        /// <summary>
        /// Lower case values that the exporter will consider for being the Tiling
        /// </summary>
        public static readonly string[] SemanticsTiling = new string[]
        {
            "_maintex_st"
        };

        /// <summary>
        /// Lower case values that the exporter will consider for being the Color off a shader
        /// </summary>
        public static readonly string[] SemanticsColor = new string[]
        {
            "_color"
        };

        /// <summary>
        /// Lower case values that the exporter will consider for the shader using transparent textures
        /// </summary>
        public static readonly string[] SemanticsTransparent = new string[]
        {
            "transparent"
        };

        /// <summary>
        /// The semantic names for all the skybox 6-sided faces
        /// </summary>
        public static readonly string[] SkyboxTexNames = new string[]
        {
            "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex"
        };

        private static List<IJanusObject> objects;
        static JanusGlobals()
        {
            objects = new List<IJanusObject>();
        }


        public static void RegisterObject(IJanusObject obj)
        {
            objects.Add(obj);
        }

        public static void UpdateScale(float scale)
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

                obj.UpdateScale(scale);
            }
        }
    }
}
#endif
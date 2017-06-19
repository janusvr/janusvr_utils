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
    public class LightmapPreviewWindow : EditorWindow
    {
        public Texture2D Tex { get; set; }

        private void OnEnable()
        {
            // search for the icon file
            Texture2D icon = Resources.Load<Texture2D>("janusvricon");
            this.SetWindowTitle("Lightmap", icon);
        }

        private void OnGUI()
        {
            if (Tex != null)
            {
                Rect rect = this.position;
                GUI.DrawTexture(new Rect(0, 0, rect.width, rect.height), Tex);
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    public class TextureExportData
    {
        private Texture2D preview;
        private Texture2D texture;
        private ExportTextureFormat format;
        private int quality;
        private string exportedPath;
        private int resolution;
        private bool created;
        private bool exportAlpha;

        public bool Created
        {
            get { return created; }
            set { created = value; }
        }
        public int Resolution
        {
            get { return resolution; }
            set { resolution = value; }
        }

        public string ExportedPath
        {
            get { return exportedPath; }
            set { exportedPath = value; }
        }

        public int Quality
        {
            get { return quality; }
            set { quality = value; }
        }

        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        public Texture2D Preview
        {
            get { return preview; }
            set { preview = value; }
        }

        public ExportTextureFormat Format
        {
            get { return format; }
            set { format = value; }
        }

        public bool ExportAlpha
        {
            get { return exportAlpha; }
            set { exportAlpha = value; }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(texture.name))
            {
                return "No name: " + texture.width + " - " + texture.height;
            }
            return texture.name;
        }
    }
}
#endif
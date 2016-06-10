using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class TextureExportData
    {
        private Texture2D texture;
        private ImageFormatEnum format;
        private int quality;
        private string exportedPath;

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

        public ImageFormatEnum Format
        {
            get { return format; }
            set { format = value; }
        }
    }
}

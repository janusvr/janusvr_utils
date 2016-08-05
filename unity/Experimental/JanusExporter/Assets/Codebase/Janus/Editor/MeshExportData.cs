using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class MeshExportData
    {
        private Mesh mesh;
        private ExportMeshFormat format;
        private Texture2D preview;
        private string exportedPath;

        public string ExportedPath
        {
            get { return exportedPath; }
            set { exportedPath = value; }
        }

        public Texture2D Preview
        {
            get { return preview; }
            set { preview = value; }
        }

        public Mesh Mesh
        {
            get { return mesh; }
            set { mesh = value; }
        }

        public ExportMeshFormat Format
        {
            get { return format; }
            set { format = value; }
        }
    }
}

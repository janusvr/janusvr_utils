using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class BruteForceMeshExportData
    {
        public bool LightmapEnabled { get; set; }
        public Mesh Mesh { get; set; }
        public string MeshId { get; set; }
        public AssetObject Asset { get; set; }

        public string ExportedPath { get; set; }
        public Texture2D Preview { get; set; }
        public ExportMeshFormat Format { get; set; }
    }
}
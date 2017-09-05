using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public struct PerMaterialMeshExportDataObj
    {
        public bool LightmapEnabled { get; set; }
        public int MaterialId { get; set; }
        public Mesh Mesh { get; set; }
        public Transform Transform { get; set; }
        public MeshRenderer Renderer { get; set; }
    }
}

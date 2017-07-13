using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class PerMaterialMeshExportData
    {
        public RoomObject Object { get; set; }
        public List<PerMaterialMeshExportDataObj> Meshes { get; set; }
        public AssetObject Asset { get; set; }

        public PerMaterialMeshExportData()
        {
            Meshes = new List<PerMaterialMeshExportDataObj>();
        }
    }
}

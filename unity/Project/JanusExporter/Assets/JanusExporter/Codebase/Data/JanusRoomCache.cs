using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class JanusRoomCache
    {
        private List<Mesh> exportedMeshes;

        public JanusRoomCache()
        {
            exportedMeshes = new List<Mesh>();
        }

        public bool ExportMesh(Mesh mesh)
        {
            if (exportedMeshes.Contains(mesh))
            {
                return false;
            }

            exportedMeshes.Add(mesh);
            return true;
        }
    }
}

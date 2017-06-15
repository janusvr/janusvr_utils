#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public abstract class MeshExporter
    {
        public abstract void Initialize(JanusRoom room);

        public abstract string GetFormat();

        public abstract void ExportMesh(Mesh mesh, string exportPath, MeshExportParameters parameters);
    }
}
#endif
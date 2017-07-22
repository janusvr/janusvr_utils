using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanusVR
{
    /// <summary>
    /// Parameters for properly exporting meshes
    /// through the exporter's pipeline
    /// </summary>
    public class MeshExportParameters
    {
        private bool switchUv;
        private bool mirror;

        /// <summary>
        /// If the exporter should grab the UV1 layer
        /// and export as UV0 (for baked material option)
        /// </summary>
        public bool SwitchUV
        {
            get { return switchUv; }
        }

        /// <summary>
        /// If true, mirrors all vertices and normals on the X-axis,
        /// and changes the winding of the triangles (always true
        /// to match JanusVR OpenGL space)
        /// </summary>
        public bool Mirror
        {
            get { return mirror; }
        }

        public MeshExportParameters(bool switchUv, bool mirror)
        {
            this.switchUv = switchUv;
            this.mirror = mirror;
        }
    }
}
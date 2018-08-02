using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class MeshData
    {
        public bool Lightmapped { get; set; }
        public string Name { get; set; }
        public Vector3[] Vertices { get; set; }
        public Vector3[] Normals { get; set; }
        public int[] Triangles { get; set; }
        public Vector2[][] UV { get; set; }
        public float Scale { get; set; }

        public MeshData()
        {
            Scale = 1;
        }
    }
}

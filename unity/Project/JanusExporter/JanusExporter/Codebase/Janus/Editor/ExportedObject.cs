using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class ExportedObject
    {
        private Mesh mesh;
        private Texture2D lightMapTex;
        private Texture2D diffuseMapTex;
        private GameObject go;
        private Collider col;

        public Mesh Mesh
        {
            get { return mesh; }
            set { mesh = value; }
        }

        public Texture2D LightMapTex
        {
            get { return lightMapTex; }
            set { lightMapTex = value; }
        }

        public Texture2D DiffuseMapTex
        {
            get { return diffuseMapTex; }
            set { diffuseMapTex = value; }
        }

        public Texture Texture { get; set; }

        public GameObject GameObject
        {
            get { return go; }
            set { go = value; }
        }

        public Collider Col
        {
            get { return col; }
            set { col = value; }
        }

        public Color? Color { get; set; }

        public Vector4? Tiling { get; set; }
    }
}

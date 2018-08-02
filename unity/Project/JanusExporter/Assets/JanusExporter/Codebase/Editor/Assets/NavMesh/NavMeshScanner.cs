using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UObject = UnityEngine.Object;

namespace JanusVR
{
    public class NavMeshScanner
    {
        //private JanusRoom room;

        public NavMeshScanner(JanusRoom room)
        {
            //this.room = room;
        }

        public void Initialize()
        {
        }

        public AssetObject GetNavMeshAsset()
        {
            NavMeshTriangulation triangles = NavMesh.CalculateTriangulation();
            Mesh mesh = new Mesh();
            mesh.vertices = triangles.vertices;
            mesh.triangles = triangles.indices;
            mesh.UploadMeshData(false);
            mesh.name = "NavMesh";

            AssetObject obj = new AssetObject();
            obj.Mesh = mesh;

            return obj;
        }
    }
}
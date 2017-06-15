using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class RoomObject
    {
        private GameObject unityObj;
        public GameObject UnityObj
        {
            get { return unityObj; }
        }

        public Texture2D Preview { get; set; }

        /// <summary>
        /// Id of the AssetObject
        /// </summary>
        public AssetObject id { get; set; }
        public AssetImage image_id { get; set; }
        public AssetImage lmap_id { get; set; }
        public Vector4? lmap_sca { get; set; }

        public bool lighting { get; set; }
        public Color color { get; set; }
        public Vector3 pos { get; set; }
        public Vector3 scale { get; set; }
        public Vector3 xdir { get; set; }
        public Vector3 ydir { get; set; }
        public Vector3 zdir { get; set; }

        public void SetUnityObj(GameObject obj)
        {
            unityObj = obj;

            Transform trans = obj.transform;
            Vector3 position = trans.position;
            position.x *= -1;

            Quaternion rot = trans.rotation;
            Vector3 xDir = rot * Vector3.right;
            Vector3 yDir = rot * Vector3.up;
            Vector3 zDir = rot * Vector3.forward;
            xDir.x *= -1;
            yDir.x *= -1;
            zDir.x *= -1;

            xdir = xDir;
            ydir = yDir;
            zdir = zDir;
            pos = position;
            scale = trans.lossyScale;
        }
    }
}

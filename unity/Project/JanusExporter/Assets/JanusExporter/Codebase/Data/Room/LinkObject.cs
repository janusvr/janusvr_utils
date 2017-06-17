using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public class LinkObject : JanusRoomElement
    {
        public Vector3 pos;
        public Vector3 xDir;
        public Vector3 yDir;
        public Vector3 zDir;

        public Color col;
        public Vector3 scale;
        public string url;
        public string title;

        public AssetImage image_id;
    }
}

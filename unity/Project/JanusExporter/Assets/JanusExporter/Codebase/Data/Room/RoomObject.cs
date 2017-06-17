using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace JanusVR
{
    public class RoomObject : JanusRoomElement
    {
        private GameObject unityObj;

        [XmlIgnore]
        public GameObject UnityObj
        {
            get { return unityObj; }
        }

        [XmlIgnore]
        public Texture2D Preview { get; set; }

        /// <summary>
        /// Id of the AssetObject
        /// </summary>
        [XmlAttribute]
        public string id { get; set; }
        [XmlAttribute]
        public string image_id { get; set; }
        [XmlAttribute]
        public string lmap_id { get; set; }
        [XmlAttribute]
        public string lmap_sca { get; set; }

        [XmlAttribute]
        public bool lighting { get; set; }
        [XmlAttribute]
        public string col { get; set; }
        [XmlAttribute]
        public string pos { get; set; }
        [XmlAttribute]
        public string scale { get; set; }
        [XmlAttribute]
        public string xdir { get; set; }
        [XmlAttribute]
        public string ydir { get; set; }
        [XmlAttribute]
        public string zdir { get; set; }

        [XmlAttribute]
        public string cull_face { get; set; }

        public void SetUnityObj(GameObject obj, JanusRoom room)
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

            xdir = JanusUtil.FormatVector3(xDir, JanusGlobals.DecimalCasesPosition);
            ydir = JanusUtil.FormatVector3(yDir, JanusGlobals.DecimalCasesPosition);
            zdir = JanusUtil.FormatVector3(zDir, JanusGlobals.DecimalCasesPosition);
            pos = JanusUtil.FormatVector3(position, JanusGlobals.DecimalCasesPosition);

            Vector3 sca = trans.lossyScale;
            if (sca.x < 0 || sca.y < 0 || sca.z < 0)
            {
                cull_face = "front";
            }
            scale = JanusUtil.FormatVector3(trans.lossyScale, JanusGlobals.DecimalCasesPosition);
        }

        public void SetLightmap(Vector4 lightmap)
        {
            lmap_sca = JanusUtil.FormatVector4(lightmap, JanusGlobals.DecimalCasesLightmap);
        }
    }
}

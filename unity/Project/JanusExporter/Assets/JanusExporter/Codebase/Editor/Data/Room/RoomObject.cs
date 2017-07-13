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
        public bool? lighting { get; set; }
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
        public string rotation { get; set; }

        [XmlAttribute]
        public string cull_face { get; set; }
        [XmlAttribute]
        public string tiling { get; set; }
        [XmlAttribute]
        public string collision_id { get; set; }

        public void SetNoUnityObj(JanusRoom room)
        {
            //Vector3 xDir = Vector3.right;
            //Vector3 yDir = Vector3.up;
            //Vector3 zDir = Vector3.forward;
            //xDir.x *= -1;
            //yDir.x *= -1;
            //zDir.x *= -1;

            //xdir = JanusUtil.FormatVector3(xDir, JanusGlobals.DecimalCasesForTransforms);
            //ydir = JanusUtil.FormatVector3(yDir, JanusGlobals.DecimalCasesForTransforms);
            //zdir = JanusUtil.FormatVector3(zDir, JanusGlobals.DecimalCasesForTransforms);
            pos = JanusUtil.FormatVector3(Vector3.zero, JanusGlobals.DecimalCasesForTransforms);
            scale = JanusUtil.FormatVector3(Vector3.one * room.UniformScale, JanusGlobals.DecimalCasesForTransforms);
            lighting = room.LightmapType == LightmapExportType.None;            
        }

        public void SetUnityObj(GameObject obj, JanusRoom room)
        {
            unityObj = obj;

            Transform trans = obj.transform;
            Vector3 position = trans.position;

            Quaternion rot = trans.rotation;
            if (room.UseEulerRotations)
            {
                Vector3 euler = JanusUtil.ConvertEulerRotation(rot.eulerAngles);
                rotation = JanusUtil.FormatVector3(euler, JanusGlobals.DecimalCasesForTransforms);
            }
            else
            {
                Vector3 xDir = JanusUtil.ConvertDirection(rot * Vector3.right);
                Vector3 yDir = JanusUtil.ConvertDirection(rot * Vector3.up);
                Vector3 zDir = JanusUtil.ConvertDirection(rot * Vector3.forward);

                xdir = JanusUtil.FormatVector3(xDir, JanusGlobals.DecimalCasesForTransforms);
                ydir = JanusUtil.FormatVector3(yDir, JanusGlobals.DecimalCasesForTransforms);
                zdir = JanusUtil.FormatVector3(zDir, JanusGlobals.DecimalCasesForTransforms);
            }
            pos = JanusUtil.FormatVector3(JanusUtil.ConvertPosition(position, room.UniformScale), JanusGlobals.DecimalCasesForTransforms);

            Vector3 sca = trans.lossyScale;
            if (sca.x < 0 || sca.y < 0 || sca.z < 0)
            {
                cull_face = "front";
            }
            scale = JanusUtil.FormatVector3(trans.lossyScale * room.UniformScale, JanusGlobals.DecimalCasesForTransforms);

            if (obj.isStatic &&
                room.LightmapType != LightmapExportType.None)
            {
                lighting = false;
            }
            else
            {
                lighting = true;
            }
        }

        public void SetLightmap(Vector4 lightmap)
        {
            lmap_sca = JanusUtil.FormatVector4(lightmap, JanusGlobals.DecimalCasesForLightmaps);
        }
    }
}
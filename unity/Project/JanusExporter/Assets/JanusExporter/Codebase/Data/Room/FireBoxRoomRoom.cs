using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace JanusVR
{
    public class FireBoxRoomRoom
    {
        [XmlAttribute("far_dist")]
        public float? FarDistance { get; set; }

        [XmlElement(typeof(RoomObject), ElementName = "Object")]
        [XmlElement(typeof(LinkObject))]
        public JanusRoomElement[] Elements { get; set; }

        [XmlAttribute]
        public string skybox_front_id { get; set; }
        [XmlAttribute]
        public string skybox_back_id { get; set; }
        [XmlAttribute]
        public string skybox_left_id { get; set; }
        [XmlAttribute]
        public string skybox_right_id { get; set; }
        [XmlAttribute]
        public string skybox_up_id { get; set; }
        [XmlAttribute]
        public string skybox_down_id { get; set; }

        [XmlAttribute]
        public string cubemap_irradiance_id { get; set; }
        [XmlAttribute]
        public string cubemap_radiance_id { get; set; }

        [XmlAttribute]
        public string pos { get; set; }
        [XmlAttribute]
        public string xdir { get; set; }
        [XmlAttribute]
        public string ydir { get; set; }
        [XmlAttribute]
        public string zdir { get; set; }
    }
}

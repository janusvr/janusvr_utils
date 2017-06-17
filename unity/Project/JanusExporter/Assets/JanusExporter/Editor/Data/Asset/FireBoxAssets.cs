using JanusVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace JanusVR
{
    public class FireBoxAssets
    {
        [XmlElement(typeof(AssetImage))]
        [XmlElement(typeof(AssetObject))]
        public JanusAsset[] Assets { get; set; }
    }
}
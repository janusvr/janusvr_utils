#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace JanusVR
{
    public class AssetImage : JanusAsset
    {
        [XmlIgnore]
        public Texture2D Texture { get; set; }
    }
}
#endif
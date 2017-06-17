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

        [XmlIgnore]
        public bool Created { get; set; }

        public static implicit operator string(AssetImage asset)
        {
            if (asset == null)
            {
                return null;
            }
            return asset.id;
        }
    }
}
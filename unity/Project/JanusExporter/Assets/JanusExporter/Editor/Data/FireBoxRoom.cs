using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace JanusVR
{
    [XmlRoot("FireBoxRoom", IsNullable = false)]
    public class FireBoxRoom
    {
        private XmlSerializerNamespaces namespaces;

        [XmlElement("Assets")]
        public FireBoxAssets Assets { get; set; }

        [XmlElement("Room")]
        public FireBoxRoomRoom Room { get; set; }

        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces
        {
            get { return namespaces; }
        }

        public FireBoxRoom()
        {
            Assets = new FireBoxAssets();
            Room = new FireBoxRoomRoom();

            this.namespaces = new XmlSerializerNamespaces(new XmlQualifiedName[] {
                new XmlQualifiedName("", "") // Default Namespace
            });
        }
    }
}
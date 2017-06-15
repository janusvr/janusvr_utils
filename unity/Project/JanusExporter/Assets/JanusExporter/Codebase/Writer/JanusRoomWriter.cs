using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace JanusVR
{
    public class JanusRoomWriter
    {
        private JanusRoom room;

        private StringBuilder builder;
        private XmlWriter writer;

        public JanusRoomWriter(JanusRoom room)
        {
            this.room = room;
        }

        public void WriteHtml(string path)
        {
            LightmapExportType lightmapExportType = room.LightmapType;
            float uniformScale = room.UniformScale;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            builder = new StringBuilder();
            writer = XmlWriter.Create(builder, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("html");

            writer.WriteStartElement("head");
            writer.WriteStartElement("title");
            writer.WriteString("Janus Unity Exporter v" + JanusGlobals.Version);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("body");
            writer.WriteStartElement("FireBoxRoom");
            writer.WriteStartElement("Assets");

            // write all Asset Objects
            List<JanusAsset> allAssets = room.AllAssets;
            for (int i = 0; i < allAssets.Count; i++)
            {
                JanusAsset asset = allAssets[i];
                XmlSerializer serializer = new XmlSerializer(asset.GetType());
                serializer.Serialize(writer, asset, namespaces);
            }

            writer.WriteEndElement();
            writer.WriteStartElement("Room");

            if (room.SkyboxFront != null)
            {
                writer.WriteAttributeString("skybox_front_id", room.SkyboxFront.id);
            }
            if (room.SkyboxBack != null)
            {
                writer.WriteAttributeString("skybox_back_id", room.SkyboxBack.id);
            }
            if (room.SkyboxLeft != null)
            {
                writer.WriteAttributeString("skybox_left_id", room.SkyboxLeft.id);
            }
            if (room.SkyboxRight != null)
            {
                writer.WriteAttributeString("skybox_right_id", room.SkyboxRight.id);
            }
            if (room.SkyboxUp != null)
            {
                writer.WriteAttributeString("skybox_up_id", room.SkyboxUp.id);
            }
            if (room.SkyboxDown != null)
            {
                writer.WriteAttributeString("skybox_down_id", room.SkyboxDown.id);
            }
            if (room.CubemapIrradiance != null)
            {
                writer.WriteAttributeString("cubemap_irradiance_id", room.CubemapIrradiance.id);
            }
            if (room.CubemapRadiance != null)
            {
                writer.WriteAttributeString("cubemap_radiance_id", room.CubemapRadiance.id);
            }

            // write all Room Objects
            List<RoomObject> roomObjects = room.RoomObjects;
            for (int i = 0; i < roomObjects.Count; i++)
            {
                RoomObject obj = roomObjects[i];

                writer.WriteStartElement("Object");
                writer.WriteAttributeString("id", obj.id.id);
                writer.WriteAttributeString("lighting", "true");

                Vector3 sca = obj.scale;
                if (sca.x < 0 || sca.y < 0 || sca.z < 0)
                {
                    writer.WriteAttributeString("cull_face", "front");
                }
                writer.WriteAttributeString("scale", JanusUtil.FormatVector3(sca));

                writer.WriteAttributeString("pos", JanusUtil.FormatVector3(obj.pos));
                writer.WriteAttributeString("xdir", JanusUtil.FormatVector3(obj.xdir));
                writer.WriteAttributeString("ydir", JanusUtil.FormatVector3(obj.ydir));
                writer.WriteAttributeString("zdir", JanusUtil.FormatVector3(obj.zdir));

                if (obj.image_id != null &&
                    !string.IsNullOrEmpty(obj.image_id.id))
                {
                    writer.WriteAttributeString("image_id", obj.image_id.id);
                }

                if (obj.lmap_id != null &&
                    !string.IsNullOrEmpty(obj.lmap_id.id))
                {
                    writer.WriteAttributeString("lmap_id", obj.lmap_id.id);
                }

                if (obj.lmap_sca != null)
                {
                    writer.WriteAttributeString("lmap_sca", JanusUtil.FormatVector4(obj.lmap_sca.Value, 6));
                }                

                writer.WriteEndElement();
            }

            writer.WriteEndDocument();
            writer.Close();
            writer.Flush();

            string str = builder.ToString();
            UnityUtil.WriteAllText(path, builder.ToString());
        }
    }
}

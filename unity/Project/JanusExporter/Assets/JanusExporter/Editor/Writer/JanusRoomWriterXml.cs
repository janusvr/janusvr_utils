using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace JanusVR
{
    public class JanusRoomWriterXml
    {
        private JanusRoom room;

        private StringBuilder builder;
        private XmlTextWriter writer;

        public JanusRoomWriterXml(JanusRoom room)
        {
            this.room = room;
        }

        private FireBoxRoom MakeRoom()
        {
            FireBoxRoom fireBoxRoom = new FireBoxRoom();
            float uniformScale = room.UniformScale;

            // copy data
            fireBoxRoom.Assets.Assets = room.AllAssets.ToArray();
            fireBoxRoom.Room.Elements = room.RoomElements.ToArray();

            if (room.PortalPos != null)
            {
                fireBoxRoom.Room.pos = JanusUtil.FormatVector3(room.PortalPos.Value);
                fireBoxRoom.Room.xdir = JanusUtil.FormatVector3(room.PortalXDir.Value);
                fireBoxRoom.Room.ydir = JanusUtil.FormatVector3(room.PortalYDir.Value);
                fireBoxRoom.Room.zdir = JanusUtil.FormatVector3(room.PortalZDir.Value);
            }

            // implicit operators
            fireBoxRoom.Room.skybox_back_id = room.SkyboxBack;
            fireBoxRoom.Room.skybox_front_id = room.SkyboxFront;
            fireBoxRoom.Room.skybox_left_id = room.SkyboxLeft;
            fireBoxRoom.Room.skybox_right_id = room.SkyboxRight;
            fireBoxRoom.Room.skybox_up_id = room.SkyboxUp;
            fireBoxRoom.Room.skybox_down_id = room.SkyboxDown;

            return fireBoxRoom;
        }

        public void WriteHtml(string path)
        {
            LightmapExportType lightmapExportType = room.LightmapType;
            float uniformScale = room.UniformScale;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Encoding = Encoding.UTF8;

            builder = new StringBuilder();
            writer = (XmlTextWriter)XmlWriter.Create(builder, settings);
            writer.Formatting = Formatting.Indented;

            writer.WriteStartDocument();
            writer.WriteStartElement("html");

            writer.WriteStartElement("head");
            writer.WriteStartElement("title");
            writer.WriteString("Janus Unity Exporter v" + JanusGlobals.Version);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("body");

            FireBoxRoom fireBoxRoom = MakeRoom();
            XmlSerializer fireBoxRoomSerializer = new XmlSerializer(typeof(FireBoxRoom));
            fireBoxRoomSerializer.Serialize(writer, fireBoxRoom, fireBoxRoom.Namespaces);

            writer.WriteEndDocument();

            writer.Close();
            writer.Flush();

            string str = builder.ToString();
            UnityUtil.WriteAllText(path, builder.ToString());
            return;
        }
    }
}

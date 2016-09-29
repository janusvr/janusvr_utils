using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusExporter
{
    public class DDSExporter
    {
        public void Write(Stream stream, Texture2D[] faces)
        {
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((Int32)0x20534444);
            writer.Write((Int32)124); //dwSize

        }
    }
}

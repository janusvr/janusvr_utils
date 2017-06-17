#if GZIP


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.IO.Compression;

namespace JanusVR
{
    public static class GZipExporter
    {
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }

        public static void Save(string file, Stream str)
        {
            using (FileStream output = File.OpenWrite(file))
            {
                using (GZipStream stream = new GZipStream(output, CompressionMode.Compress, false))
                {
                    byte[] source = new byte[str.Length];
                    str.Read(source, 0, source.Length);
                    // this is memory heavy

                    stream.Write(source, 0, source.Length);
                }
            }
        }
    }
}
#endif

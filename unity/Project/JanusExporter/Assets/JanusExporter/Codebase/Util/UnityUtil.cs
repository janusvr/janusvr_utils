#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    public static class UnityUtil
    {
        public static void SetWindowTitle(this EditorWindow window, string title, Texture2D icon)
        {
#if UNITY_5_0
            window.title = "Janus";
#else
            window.titleContent = new GUIContent("Janus", icon);
#endif
        }

        public static void StartProcess(string exportPath)
        {
#if UNITY_5_3
            Process p = new Process();
            p.StartInfo.FileName = exportPath;
            p.Start();
#else
            System.Diagnostics.Process.Start(exportPath);
#endif
        }

        public static void WriteAllText(string exportPath, string text)
        {
#if UNITY_5_3
            using (Stream stream = File.OpenWrite(exportPath))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(text);
                    writer.Flush();
                }
                stream.Flush();
            }
#else
            File.WriteAllText(exportPath, text);
#endif
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    /// <summary>
    /// Class shown when the user first adds the Janus
    /// </summary>
    public class JanusVRWelcome : EditorWindow
    {
        [NonSerialized]
        private Rect border = new Rect(10, 5, 20, 15);

        //[MenuItem("Window/JanusVR Welcome")]
        public static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            JanusVRWelcome window = EditorWindow.GetWindow<JanusVRWelcome>();
            window.Show();
        }

        private void OnEnable()
        {
            // search for the icon file
            Texture2D icon = Resources.Load<Texture2D>("janusvricon");
            this.SetWindowTitle("Welcome", icon);
        }

        private void OnGUI()
        {
            Rect rect = this.position;
            GUILayout.BeginArea(new Rect(border.x, border.y, rect.width - border.width, rect.height - border.height));

            GUILayout.Label("JanusVR Unity Exporter Version " + (JanusGlobals.Version).ToString("F2"), EditorStyles.boldLabel);
            GUILayout.Label("Welcome!");
            GUILayout.Label("Open the exporter window by hitting Window -> JanusVR Exporter");
            GUILayout.Label("or clicking one of the buttons below:");

            if (GUILayout.Button("Check for Updates"))
            {
                //JanusVRUpdater.ShowWindow();
            }

            if (GUILayout.Button("Open JanusVR Exporter"))
            {
                JanusVRExporterWindow.ShowWindow();
            }

            GUILayout.EndArea();
        }
    }
}
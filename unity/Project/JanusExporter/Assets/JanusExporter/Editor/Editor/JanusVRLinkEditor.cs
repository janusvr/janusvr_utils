using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    [CustomEditor(typeof(JanusVRLink))]
    public class JanusVRLinkEditor : Editor
    {
        private JanusVRLink instance;

        public void OnEnable()
        {
            instance = (JanusVRLink)target;
        }

        public override void OnInspectorGUI()
        {
            //instance.Circular = EditorGUILayout.Toggle("Circular", instance.Circular);

            instance.draw_glow = EditorGUILayout.Toggle("Draw Glow", instance.draw_glow);
            instance.draw_text = EditorGUILayout.Toggle("Draw Text", instance.draw_text);
            instance.auto_load = EditorGUILayout.Toggle("Auto Load", instance.auto_load);
            instance.url = EditorGUILayout.TextField("URL", instance.url);
            instance.title = EditorGUILayout.TextField("Title", instance.title);

            instance.Color = EditorGUILayout.ColorField("Color", instance.Color);

            GUILayout.Label("For a custom texture modify the Link's material");
        }
    }
}
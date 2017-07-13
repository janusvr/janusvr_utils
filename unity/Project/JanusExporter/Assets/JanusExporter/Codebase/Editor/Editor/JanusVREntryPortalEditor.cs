using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    [CustomEditor(typeof(JanusVREntryPortal))]
    public class JanusVREntryPortalEditor : Editor
    {
        //private JanusVREntryPortal instance;

        public void OnEnable()
        {
            //instance = (JanusVREntryPortal)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Update size by changing");
            EditorGUILayout.LabelField("Uniform Scale on JanusVR Exporter window");
            //instance.Circular = EditorGUILayout.Toggle("Circular", instance.Circular);
        }
    }
}
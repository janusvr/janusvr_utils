using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace JanusVR
{
    //[Serializable]
    public class SceneExportData
    {
        public bool IsPreview { get; set; }

        public bool UpdateOnlyHTML { get; set; }

        public List<ExportedObject> exportedObjs;

        public List<JanusVRLink> exportedLinks;

        public JanusVREntryPortal entryPortal;

        public Cubemap environmentCubemap;
        public List<ExportedObject> exportedReflectionProbes;

        public Dictionary<int, List<GameObject>> lightmapped;
        public List<Texture2D> texturesExported;

#if UNITY_5_3_OR_NEWER
        public Scene Scene { get; private set; }
#endif

        public GameObject[] SceneRoots { get; private set; }
        public string ScenePath { get; private set; }
        public string SceneName { get; private set; }

        public JanusVRLightmaps Lightmaps { get; private set; }

        public string GetLightmapsFolder()
        {
            string scenePath = Path.GetDirectoryName(ScenePath);
            return Path.Combine(scenePath, SceneName);
        }

        internal SceneExportData()
        {
            exportedObjs = new List<ExportedObject>();
            exportedReflectionProbes = new List<ExportedObject>();

            exportedLinks = new List<JanusVRLink>();
            lightmapped = new Dictionary<int, List<GameObject>>();

            Lightmaps = new JanusVRLightmaps(this);

#if UNITY_5_3_OR_NEWER
            Scene = SceneManager.GetActiveScene();
            SceneRoots = Scene.GetRootGameObjects();
            ScenePath = Scene.path;
            SceneName = Scene.name;
#else
            GameObject[] roots = SceneRoots().ToArray();
            ScenePath = EditorApplication.currentScene;
            SceneName = Path.GetFileNameWithoutExtension(scenePath);
#endif

          
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusExporter
{
    [ExecuteInEditMode]
    public class JanusLink : MonoBehaviour
    {
        public string url;
        public string title;
        public Color color;
        public bool draw_glow = true;
        public bool draw_test = true;
        public bool auto_load = false;

        private void Start()
        {
            transform.localScale = new Vector3(1.8f, 2.5f, 1);
        }

        private void Update()
        {
            Vector3 sca = transform.localScale;
            sca.x = Math.Max(1.8f, sca.x);
            sca.y = Math.Max(2.5f, sca.y);
            transform.localScale = sca;
        }
    }
}

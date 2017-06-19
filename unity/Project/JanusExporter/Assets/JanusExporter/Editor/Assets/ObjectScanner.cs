using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public abstract class ObjectScanner
    {
        public abstract void Initialize(GameObject[] rootObjects);
        public abstract void ExportAssetImages();
        public abstract void ExportAssetObjects();
    }
}
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanusVR
{
    public abstract class Exporter
    {
        public abstract void Initialize(JanusRoom room);
        public abstract void PreExport();
        public abstract void Export();

        public abstract void Cleanup();
    }
}
#endif
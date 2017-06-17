using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanusVR
{
    public static class ObjectScannerFactory
    {
        public static ObjectScanner GetObjectScanner(AssetObjectSearchType type, JanusRoom room)
        {
            switch (type)
            {
                default:
                    return new BruteForceObjectScanner(room);
            }
        }
    }
}

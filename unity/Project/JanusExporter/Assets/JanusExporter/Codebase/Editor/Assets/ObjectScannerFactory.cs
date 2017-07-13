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
                case AssetObjectSearchType.EachMesh:
                    return new BruteForceObjectScanner();
                case AssetObjectSearchType.PerMaterial:
                    return new PerMaterialObjectScanner();
                case AssetObjectSearchType.PerLightmapId:
                    return new PerLightmapIDScanner();
                default:
                    return null;
            }
        }
    }
}

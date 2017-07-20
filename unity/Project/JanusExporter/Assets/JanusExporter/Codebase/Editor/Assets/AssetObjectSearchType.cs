using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanusVR
{
    public enum AssetObjectSearchType
    {
        // Each Mesh that is used inside Unity becomes a single model file outside
        EachMesh,
        // 
        PerMaterial,
        // Maintains the original model file hierarchy, while also re-exporting it
        //KeepHierarchy,
        // Modify model transformations to match Janus space while keeping the original model files
        //PassthroughKeepHierarchy,

        PerLightmapId
    }
}

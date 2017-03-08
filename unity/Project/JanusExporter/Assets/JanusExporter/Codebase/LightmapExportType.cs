#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JanusVR
{
    public enum LightmapExportType
    {
        // None: No lightmaps
        None,

        // BakedMaterial: Each object gets a texture
        // with the lightmap and the material baked in
        // (memory redundant, wastes space)
        BakedMaterial,

        // Packed: Default Unity configuration but converted to SDR,
        // exports all lightmaps on their original packed configuration
        Packed,

        // PackedSource: Exports exactly the HDR lighting data straight from Unity with no modifications
        PackedSourceEXR,

        // Unpacked: Exports all lightmaps, but 
        // each object gets its own texture instead of sharing one.
        // There's nothing lost in quality terms, just
        // memory (as they are forced power of 2)
        Unpacked
    }
}
#endif
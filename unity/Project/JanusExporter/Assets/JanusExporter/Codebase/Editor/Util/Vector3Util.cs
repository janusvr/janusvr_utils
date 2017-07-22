using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public static class Vector3Util
    {
        public static Vector3 Divide(Vector3 input, Vector3 divisor)
        {
            return new Vector3(input.x / divisor.x,
                input.y / divisor.y,
                input.z / divisor.z);
        }

        public static Vector3 Divide(Vector3 input, float divisor)
        {
            return new Vector3(input.x / divisor,
                input.y / divisor,
                input.z / divisor);
        }
    }
}

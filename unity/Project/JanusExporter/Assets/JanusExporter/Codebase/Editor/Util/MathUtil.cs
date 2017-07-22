using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public static class MathUtil
    {
        public static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Math.Abs(v.x), Math.Abs(v.y), Math.Abs(v.z));
        }

        /// <summary>
        /// Returns true if the value is a power of 2
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsPowerOf2(int value)
        {
            return (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Gets the next exponent of 2
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int NextPowerOf2(int value)
        {
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(value) / Math.Log(2)));
        }

        /// <summary>
        /// Gets the next exponent of 2
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetExponentOf2(int value)
        {
            int times = 1;
            for (int result = value; result > 2; result /= 2)
            {
                times++;
            }
            return times;
        }
    }
}
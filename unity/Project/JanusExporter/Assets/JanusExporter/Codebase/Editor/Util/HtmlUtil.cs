using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JanusVR
{
    public static class HtmlUtil
    {
        public static string ColorToHex(Color color)
        {
            int r = (int)(color.r * 255);
            int g = (int)(color.g * 255);
            int b = (int)(color.b * 255);
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }
    }
}
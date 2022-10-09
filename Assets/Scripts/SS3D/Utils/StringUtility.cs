using UnityEngine;

namespace SS3D.Utils
{
    public static class StringUtility
    {
        public static string Colorize(this string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }
    }
}
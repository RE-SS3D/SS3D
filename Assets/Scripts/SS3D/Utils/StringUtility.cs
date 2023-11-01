using System;
using UnityEngine;

namespace SS3D.Utils
{
    public static class StringUtility
    {
        public static string Colorize(this string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }

        /// <summary>
        /// Extension method to return an enum value of type T for the given string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}
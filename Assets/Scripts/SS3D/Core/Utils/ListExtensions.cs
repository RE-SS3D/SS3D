using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Core.Utils
{
    public static class ListExtensions       
    {
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool OneElementOnly<T>(this List<T> list)
        {
            return list.Count == 1;
        }
    }
}
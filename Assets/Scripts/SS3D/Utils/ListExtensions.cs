using JetBrains.Annotations;
using System.Collections.Generic;

namespace SS3D.Utils
{
    public static class ListExtensions
    {
        public static bool IsNullOrEmpty<T>([CanBeNull] this List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static bool OneElementOnly<T>([NotNull] this List<T> list)
        {
            return list.Count == 1;
        }
    }
}
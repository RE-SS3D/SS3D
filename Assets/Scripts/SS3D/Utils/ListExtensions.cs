using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

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

        /// <summary>
        /// Checks if the content of the list contains any element from another list. Slow for large lists.
        /// Be aware it's based upon the Equals method to compare equality.
        /// </summary>
        public static bool ContainsAny<T>([NotNull] this List<T> list, List<T> otherList)
        {
            return list.Any(x => otherList.Any(y => y.Equals(x)));
        }
    }
}
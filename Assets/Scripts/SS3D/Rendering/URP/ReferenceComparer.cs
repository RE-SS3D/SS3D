using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Identity comparer for dictionaries keyed by Unity objects on hot paths: <c>UnityEngine.Object.Equals</c>
    /// checks the native object on every lookup. A destroyed key stays findable until its owner prunes it.
    /// </summary>
    public sealed class ReferenceComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new();

        public bool Equals(T x, T y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}

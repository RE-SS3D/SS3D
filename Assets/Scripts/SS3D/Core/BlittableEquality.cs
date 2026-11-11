using System;
using System.Runtime.InteropServices;

namespace SS3D.Core
{
    /// <summary>
    /// Allocation-free equality for plain-data structs (tech-debt P6.2). FishNet compares a <c>[SyncVar]</c>'s old and new
    /// value with <c>EqualityComparer&lt;T&gt;.Default</c>; for a struct without <see cref="IEquatable{T}"/>, Mono boxes both
    /// values and compares them field by field through reflection, which allocates on every assignment. Structs synced
    /// every tick implement <see cref="IEquatable{T}"/> with these helpers instead.
    /// </summary>
    /// <remarks>
    /// Compares raw bytes, so it is stricter than field equality only where that is harmless: identical NaNs count as
    /// equal, and +0/-0 or differing padding count as a change (one extra sync).
    /// </remarks>
    public static class BlittableEquality
    {
        public static bool Equal<T>(T a, T b)
            where T : unmanaged
        {
            ReadOnlySpan<byte> left = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref a, 1));
            ReadOnlySpan<byte> right = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref b, 1));
            return left.SequenceEqual(right);
        }

        /// <summary>FNV-1a over the struct's bytes, consistent with <see cref="Equal{T}"/>.</summary>
        public static int Hash<T>(T value)
            where T : unmanaged
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash = (hash ^ bytes[i]) * 16777619;
                }

                return (int)hash;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Every live instance of <typeparamref name="T"/>, kept by the instances themselves (tech-debt P6.4): a component
    /// registers when it is enabled (or spawned) and unregisters when disabled (or despawned), so gameplay code reads the
    /// set instead of scanning the scene with <c>FindObjectsByType</c>. Main thread only.
    /// </summary>
    /// <remarks>
    /// Don't add or remove members while iterating <see cref="All"/> (destroying a member unregisters it): copy it
    /// first with <see cref="CopyTo"/>.
    /// </remarks>
    public static class SceneRegistry<T>
        where T : class
    {
        private static readonly List<T> Members = SceneRegistry.Track(new List<T>());

        public static IReadOnlyList<T> All => Members;

        public static int Count => Members.Count;

        /// <summary>Adds <paramref name="member"/> once; registering it again is a no-op.</summary>
        public static void Register(T member)
        {
            if (member != null && !Members.Contains(member))
            {
                Members.Add(member);
            }
        }

        public static void Unregister(T member) => Members.Remove(member);

        /// <summary>Replaces <paramref name="buffer"/>'s contents with the current members, for loops that change the set.</summary>
        public static void CopyTo(List<T> buffer)
        {
            buffer.Clear();
            buffer.AddRange(Members);
        }
    }

    /// <summary>Resets every <see cref="SceneRegistry{T}"/> between Play sessions (generic types can't take the attribute).</summary>
    public static class SceneRegistry
    {
        private static readonly List<Action> Clears = new();

        internal static List<T> Track<T>(List<T> members)
        {
            Clears.Add(members.Clear);
            return members;
        }

        /// <summary>Enter Play Mode without a domain reload must not keep the last session's destroyed members.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetForNewSession()
        {
            foreach (Action clear in Clears)
            {
                clear();
            }
        }
    }
}

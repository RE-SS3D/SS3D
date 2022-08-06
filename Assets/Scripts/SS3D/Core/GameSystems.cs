using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Core
{
    /// <summary>
    /// Class used to get game systems, I haven't tested with Photon yet but my attempts on the same idea
    /// with FishNet networking worked out fine. (Still useful for offline things in any case)
    /// </summary>
    public static class GameSystems
    {
        private static readonly Dictionary<Type, object> Systems = new();

        public static T Get<T>() where T : Object
        {
            if (Systems.TryGetValue(typeof(T), out object match))
            {
                return (T)match;
            }

            match = Object.FindObjectOfType<T>();

            if (match == null)
            {
                Debug.Log($"[{nameof(GameSystems)}] - Couldn't find system of {nameof(T)} in the scene");
            }

            Systems.Add(typeof(T), match);

            return (T)match;
        }
    }
}
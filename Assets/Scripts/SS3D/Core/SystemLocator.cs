using System;
using System.Collections.Generic;
using SS3D.Core.Logging;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Class used to get game systems, using generics and then making cache of said systems.
    /// </summary>
    public static class SystemLocator
    {
        /// <summary>
        /// A dictionary containing all the objects that registered themselves.
        /// </summary>
        private static readonly Dictionary<Type, object> Systems = new();

        /// <summary>
        /// Registers a system in the dictionary so we don't have to use find object of type.
        /// </summary>
        /// <param name="system">The object to be stored.</param>
        public static void Register(MonoBehaviour system)
        {
            Type type = system.GetType();

            if (!Systems.TryGetValue(type, out object _))
            {
                Systems.Add(type, system);
            }
        }

        /// <summary>
        /// Gets any system at runtime, make sure there's no duplicates of said system before using.
        /// </summary>
        /// <typeparam name="T">The Type of object you want to get.</typeparam>
        /// <returns></returns>
        public static T Get<T>() where T : MonoBehaviour
        {
            if (Systems.TryGetValue(typeof(T), out object match))
            {
                return (T)match;
            }

            string message = $"Couldn't find system of {typeof(T).Name} in the scene";
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Punpun.Error(typeof(SystemLocator), message, Logs.Important);

            return null;
        }
    }
}
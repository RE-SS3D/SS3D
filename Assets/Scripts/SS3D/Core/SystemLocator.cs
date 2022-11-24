using System;
using System.Collections.Generic;
using SS3D.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Core
{
    /// <summary>
    /// Class used to get game systems, using generics and then making cache of said systems
    /// </summary>
    public static class SystemLocator
    {
        private static readonly Dictionary<Type, object> Systems = new();

        /// <summary>
        /// Registers a system in the dictionary so we don't have to use find object of type
        /// </summary>
        /// <param name="system"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register(MonoBehaviour system)
        {
            Type type = system.GetType();

            if (!Systems.TryGetValue(type, out object _))
            {
                Systems.Add(type, system);   
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Gets any system at runtime, make sure there's no duplicates of said system before using.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Get<T>() where T : Object
        {
            if (Systems.TryGetValue(typeof(T), out object match))
            {
                return (T)match;
            }

            string message = $"Couldn't find system of {typeof(T).Name} in the scene";
            Punpun.Panic(typeof(SystemLocator), message, Logs.Important);

            return null;
        }
    }
}
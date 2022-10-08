using System;
using System.Collections.Generic;
using SS3D.Logging;
using SS3D.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Core
{
    /// <summary>
    /// Class used to get game systems, using generics and then making cache of said systems
    /// </summary>
    public static class GameSystems
    {
        private static readonly Dictionary<Type, object> Systems = new();

        /// <summary>
        /// Registers a system in the dictionary so we don't have to use find object of type
        /// </summary>
        /// <param name="system"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>(T system)
        {
            if (!Systems.TryGetValue(typeof(T), out object _))
            {
                Systems.Add(typeof(T), system);   
            }
        }

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

            match = Object.FindObjectOfType<T>();

            if (match == null)
            {
                string message = $"Couldn't find system of {typeof(T).Name} in the scene";

                Punpun.Panic(typeof(GameSystems), message, Logs.Important);
            }

            Systems.Add(typeof(T), match);

            return (T)match;
        }
    }
}
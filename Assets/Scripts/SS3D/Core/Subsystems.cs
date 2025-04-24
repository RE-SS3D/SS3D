using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using SS3D.Logging;
using SS3D.Core.Behaviours;

namespace SS3D.Core
{
    /// <summary>
    /// System locator class used to get game subsystems.
    /// Uses generics and then making cache of said subsystems.
    /// </summary>
    public static class SubSystems
    {
        /// <summary>
        /// A dictionary containing all the objects that registered themselves.
        /// </summary>
        private static readonly Dictionary<Type, ISubSystem> RegisteredSubsystems = new();

        /// <summary>
        /// Tries to get a subsystem at runtime, make sure there's no duplicates of said subsystem before using.
        /// </summary>
        /// <typeparam name="T">The Type of object you want to get.</typeparam>
        /// <returns>If the subsystem is found or not</returns>
        public static bool TryGet<T>([CanBeNull] out T subsystem) where T : class, ISubSystem
        {
            bool hasValue = RegisteredSubsystems.TryGetValue(typeof(T), out ISubSystem match);

            subsystem = match as T;
            return hasValue;
        }

        /// <summary>
        /// Gets any system at runtime, make sure there's no duplicates of said system before using.
        /// </summary>
        /// <typeparam name="T">The Type of object you want to get.</typeparam>
        /// <returns>The found subsystem</returns>
        public static T Get<T>() where T : class, ISubSystem
        {
            if (RegisteredSubsystems.TryGetValue(typeof(T), out ISubSystem match))
            {
                return match as T;
            }

            UnityEngine.Object subsystem = UnityEngine.Object.FindObjectOfType(typeof(T), true);

            if (subsystem != null)
            {
                Register(subsystem as T);
                return subsystem as T;
            }

            string message = $"Couldn't find subsystem of {typeof(T).Name} in the scene";

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Log.Error(typeof(SubSystems), message, Logs.Important);

            return null;
        }

        /// <summary>
        /// Registers a subsystem in the dictionary so we don't have to use find object of type.
        /// </summary>
        /// <param name="subSystem">The subsystem to register and be stored.</param>
        public static void Register([NotNull] ISubSystem subSystem)
        {
            Type type = subSystem.GetType();

            if (!RegisteredSubsystems.TryGetValue(type, out ISubSystem _))
            {
                Serilog.Log.Information($"{nameof(SubSystems)} - Registering {subSystem.GetType().Name}");
                RegisteredSubsystems.Add(type, subSystem);
            }
        }

        /// <summary>
        /// Unregister the subsystem from the dictionary. 
        /// </summary>
        /// <param name="subSystem">The subsystem to unregister.</param>
        public static void Unregister([NotNull] ISubSystem subSystem)
        {
            Serilog.Log.Information($"{nameof(SubSystems)} - Unregistering {subSystem.GetType().Name}");
            RegisteredSubsystems.Remove(subSystem.GetType());
        }
    }
}
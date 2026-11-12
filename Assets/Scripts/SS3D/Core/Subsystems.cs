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
        /// True once the application has started quitting. During teardown, GameObjects are destroyed in
        /// an arbitrary order, so a subsystem being unavailable to a late OnDestroy callback is expected
        /// rather than an error worth logging.
        /// </summary>
        private static bool _isQuitting;

        /// <summary>
        /// When true, missing <see cref="Get{T}"/> lookups stay silent (WaitingForServer / offline Empty
        /// after Boot unload — same class as teardown).
        /// </summary>
        private static bool _suppressMissingErrors;

        static SubSystems()
        {
            UnityEngine.Application.quitting += () => _isQuitting = true;
        }

        /// <summary>
        /// Entering Play Mode without a domain reload (the MCP / Test Runner path) keeps statics: without this reset a
        /// previous session's quit flag made every <see cref="Get{T}"/> return null, and its instances lingered.
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetForNewSession()
        {
            _isQuitting = false;
            _suppressMissingErrors = false;
            RegisteredSubsystems.Clear();
        }

        /// <summary>
        /// Suppresses missing-subsystem Errors while disconnected / waiting to reconnect.
        /// </summary>
        public static void SetSuppressMissingErrors(bool suppress) => _suppressMissingErrors = suppress;

        /// <summary>
        /// Tries to get a subsystem at runtime, make sure there's no duplicates of said subsystem before using.
        /// </summary>
        /// <typeparam name="T">The Type of object you want to get.</typeparam>
        /// <returns>If the subsystem is found or not</returns>
        public static bool TryGet<T>([CanBeNull] out T subsystem) where T : class, ISubSystem
        {
            subsystem = TryGetLive(typeof(T), out ISubSystem match) ? match as T : null;
            return subsystem != null;
        }

        /// <summary>
        /// <see cref="TryGet{T}"/> as an expression: the subsystem, or null with no error and no scene scan. Use it where a
        /// miss is expected (<c>SubSystems.GetOrNull&lt;T&gt;()?.Method()</c>); <see cref="Get{T}"/> logs an error on a miss.
        /// </summary>
        [CanBeNull]
        public static T GetOrNull<T>() where T : class, ISubSystem => TryGet(out T subsystem) ? subsystem : null;

        /// <summary>The registered instance for <paramref name="type"/>; a destroyed Unity object counts as missing and is dropped.</summary>
        private static bool TryGetLive(Type type, out ISubSystem match)
        {
            if (!RegisteredSubsystems.TryGetValue(type, out match))
            {
                return false;
            }

            if (match is UnityEngine.Object unityObject && unityObject == null)
            {
                RegisteredSubsystems.Remove(type);
                match = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets any system at runtime, make sure there's no duplicates of said system before using.
        /// Prefer <see cref="TryGet{T}"/>. FindObject fallback is legacy (TECH_DEBT §1.12) and skipped
        /// while quitting or waiting-for-server.
        /// </summary>
        /// <typeparam name="T">The Type of object you want to get.</typeparam>
        /// <returns>The found subsystem</returns>
        public static T Get<T>() where T : class, ISubSystem
        {
            if (TryGetLive(typeof(T), out ISubSystem match))
            {
                return match as T;
            }

            if (_isQuitting || _suppressMissingErrors)
            {
                return null;
            }

            UnityEngine.Object subsystem = UnityEngine.Object.FindObjectOfType(typeof(T), true);

            if (subsystem != null)
            {
                // Tech-debt P6.4: this scan goes once nothing hits it. A hit means T didn't register itself in time.
                Log.Warning(typeof(SubSystems), "SubSystems.Get<{type}> fell back to a scene scan; it should register itself", Logs.Important, typeof(T).Name);
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

            // A previously registered instance that was destroyed without calling Unregister() (e.g. an
            // exception skipped cleanup) must not permanently block re-registration of a fresh instance.
            if (!RegisteredSubsystems.TryGetValue(type, out ISubSystem existing) || existing is UnityEngine.Object existingObject && existingObject == null)
            {
                Log.Debug(typeof(SubSystems), "Registering subsystem {subSystemType}", Logs.Generic, subSystem.GetType().Name);
                RegisteredSubsystems[type] = subSystem;
            }
        }

        /// <summary>
        /// Unregister the subsystem from the dictionary. 
        /// </summary>
        /// <param name="subSystem">The subsystem to unregister.</param>
        public static void Unregister([NotNull] ISubSystem subSystem)
        {
            // Only the registered instance may unregister: a destroyed duplicate (a content-prefab copy next to the
            // bootstrap one) must not evict the live subsystem.
            Type type = subSystem.GetType();
            if (!RegisteredSubsystems.TryGetValue(type, out ISubSystem registered) || !ReferenceEquals(registered, subSystem))
            {
                return;
            }

            Log.Debug(typeof(SubSystems), "Unregistering subsystem {subSystemType}", Logs.Generic, type.Name);
            RegisteredSubsystems.Remove(type);
        }
    }
}

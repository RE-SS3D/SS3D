using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Class used to get game systems, using generics and then making cache of said systems.
    /// </summary>
    public static class ViewLocator
    {
        /// <summary>
        /// A dictionary containing all the objects that registered themselves.
        /// </summary>
        private static readonly Dictionary<Type, List<object>> Views = new();

        /// <summary>
        /// Registers a view into a view list.
        /// </summary>
        /// <param name="view">The object to be stored.</param>
        public static void Register([NotNull] MonoBehaviour view)
        {
            Type type = view.GetType();

            if (!Views.TryGetValue(type, out List<object> viewList))
            {
                Views.Add(type, new() { view, });
            }

            else
            {
                viewList.Add(view);
            }
        }

        /// <summary>
        /// Removes a view from the view list.
        /// </summary>
        /// <param name="view"></param>
        public static void Unregister([NotNull] MonoBehaviour view)
        {
            Type type = view.GetType();

            if (Views.TryGetValue(type, out List<object> viewList))
            {
                viewList.Remove(view);
            }
        }

        /// <summary>
        /// Gets a list of view of type T.
        /// </summary>
        /// <typeparam name="T">The Type of object you want to get.</typeparam>
        /// <returns></returns>
        [CanBeNull]
        public static List<T> Get<T>() where T : MonoBehaviour
        {
            if (Views.TryGetValue(typeof(T), out List<object> match))
            {
                return match.Cast<T>().ToList();
            }

            string message = $"No views of type {typeof(T).Name} found.";

            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Punpun.Error(typeof(Subsystems), message, Logs.Important);

            return null;
        }
    }
}
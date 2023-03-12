using JetBrains.Annotations;
using SS3D.Logging;
using System.Collections.Generic;

namespace SS3D.Core
{
    public static class ActorLocator
    {
        /// <summary>
        /// A dictionary containing all the Actors, being able to be searched by ID.
        /// </summary>
        private static readonly Dictionary<int, IActor> Actors = new();

        /// <summary>
        /// Registers an IActor in a dictionary with the GameObject's ID. 
        /// </summary>
        /// <param name="actor">The object to be stored.</param>
        public static void Register([NotNull] IActor actor)
        {
            int id = actor.GameObject.GetInstanceID();

            if (!Actors.TryGetValue(id, out IActor _))
            {
                Actors.Add(id, actor);
            }
        }

        /// <summary>
        /// Unregisters an actor.
        /// </summary>
        public static void Unregister([NotNull] IActor actor)
        {
            if (Actors.TryGetValue(actor.Id, out IActor _))
            {
                Actors.Remove(actor.Id);
            }
        }

        /// <summary>
        /// Gets any IActor at runtime, using the GameObject's ID.
        /// </summary>
        /// <returns></returns>
        public static IActor Get(int id)
        {
            if (Actors.TryGetValue(id, out IActor match))
            {
                return match;
            }

            string message = $"Couldn't find actor of id {id} in the scene";
            
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Punpun.Error(typeof(Subsystems), message, Logs.Important);

            return null;
        }
    }
}
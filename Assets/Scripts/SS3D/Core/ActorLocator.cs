using SS3D.Logging;
using System.Collections.Generic;
namespace SS3D.Core
{
    public static class ActorLocator
    {
        /// <summary>
        /// A dictionary containing all the objects that registered themselves.
        /// </summary>
        private static readonly Dictionary<int, IActor> Systems = new();

        /// <summary>
        /// Registers a system in the dictionary so we don't have to use find object of type.
        /// </summary>
        /// <param name="actor">The object to be stored.</param>
        public static void Register(IActor actor)
        {
            int id = actor.GameObject.GetInstanceID();

            if (!Systems.TryGetValue(id, out IActor _))
            {
                Systems.Add(id, actor);
            }
        }

        /// <summary>
        /// Unregisters an actor.
        /// </summary>
        public static void Unregister(IActor actor)
        {
            if (Systems.TryGetValue(actor.Id, out IActor _))
            {
                Systems.Remove(actor.Id);
            }
        }

        /// <summary>
        /// Gets any system at runtime, make sure there's no duplicates of said system before using.
        /// </summary>
        /// <typeparam name="T">The Type of object you want to get.</typeparam>
        /// <returns></returns>
        public static IActor Get(int id)
        {
            if (Systems.TryGetValue(id, out IActor match))
            {
                return match;
            }

            string message = $"Couldn't find actor of id {id} in the scene";
            
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Punpun.Panic(typeof(SystemLocator), message, Logs.Important);

            return null;
        }
    }
}
using Coimbra.Services.Events;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Core.Behaviours
{
    /// <summary>
    /// Actors are the representation of a GameObject with extra steps. The basic idea is to optimize Transform and GameObject manipulation,
    /// as Unity's getters are a bit slow since they do not cache the Transform and the GameObject. They also used for QOL on code usages,
    /// as the Unity doesn't provide some of them from the get-go.
    ///
    /// They will also be used for optimization with the Update calls, as Unity's method is slow and the UpdateEvent event solves that issue and guarantees performance.
    /// </summary>
    public class NetworkActorBase : NetworkBehaviour
    {
        internal GameObject GameObjectCache;
        internal Transform TransformCache;

        protected bool Initialized;

        protected readonly List<EventHandle> EventHandles = new();
    }
}
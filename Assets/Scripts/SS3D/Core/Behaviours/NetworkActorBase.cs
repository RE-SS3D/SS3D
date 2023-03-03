using Coimbra.Services.Events;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Core.Behaviours
{
    public class NetworkActorBase : NetworkBehaviour
    {
        protected GameObject GameObjectCache;
        protected Transform TransformCache;

        protected bool Initialized;

        protected readonly List<EventHandle> EventHandles = new();
    }
}
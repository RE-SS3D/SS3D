using Coimbra.Services.Events;
using System.Collections.Generic;
using UnityEngine;
namespace SS3D.Core.Behaviours
{
    public class ActorBase : MonoBehaviour
    {
        protected GameObject GameObjectCache;
        protected Transform TransformCache;

        protected bool Initialized;

        protected readonly List<EventHandle> EventHandles = new();
    }
}
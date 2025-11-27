using SS3D.Interactions.Extensions;
using System;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Interaction target for target game objects without their own interaction target
    /// </summary>
    public class InteractionTargetGameObject : IInteractionTarget, IGameObjectProvider
    {
        public InteractionTargetGameObject(GameObject gameObject)
        {
            GameObject = gameObject;
        }

        public GameObject GameObject { get; }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return Array.Empty<IInteraction>();
        }

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);

    }
}

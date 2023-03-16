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
            ProvidedGameObject = gameObject;
        }

        public GameObject ProvidedGameObject { get; }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return Array.Empty<IInteraction>();
        }
    }
}
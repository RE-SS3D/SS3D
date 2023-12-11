using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// A target that can be interacted with
    /// </summary>
    public abstract class InteractionTargetBehaviour : Actor, IInteractionTarget, IGameObjectProvider
    {
        public new GameObject GameObject => base.GameObject;
        public abstract IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent);
    }
}
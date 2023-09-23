using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// A base class for interaction targets which use the fishnet NetworkBehaviour
    /// </summary>
    public abstract class InteractionTargetNetworkBehaviour : NetworkActor, IInteractionTarget, IGameObjectProvider
    {
        public new GameObject GameObject => base.GameObject;

        public abstract IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent);
    }
}
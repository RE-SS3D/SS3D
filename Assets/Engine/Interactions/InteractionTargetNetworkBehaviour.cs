using Mirror;
using UnityEngine;

namespace SS3D.Engine.Interactions
{
    public abstract class InteractionTargetNetworkBehaviour : NetworkBehaviour, IInteractionTarget, IGameObjectProvider
    {
        public abstract IInteraction[] GenerateInteractions(InteractionEvent interactionEvent);
        public GameObject GameObject => gameObject;
    }
}
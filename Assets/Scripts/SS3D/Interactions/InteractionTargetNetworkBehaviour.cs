using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Interactions
{
    public abstract class InteractionTargetNetworkBehaviour : NetworkedSpessBehaviour, IInteractionTarget, IGameObjectProvider
    {
        public abstract IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent);
        public GameObject GameObject => GameObjectCache;
    }
}
using UnityEngine;

namespace SS3D.Engine.Interactions
{
    public abstract class InteractionTargetBehaviour : MonoBehaviour, IInteractionTarget, IGameObjectProvider
    {
        public GameObject GameObject => gameObject;
        public abstract IInteraction[] GenerateInteractions(InteractionEvent interactionEvent);
    }
}
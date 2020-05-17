using UnityEngine;

namespace SS3D.Engine.Interactions
{
    public class InteractionGameObject : IInteractionTarget, IGameObjectProvider
    {
        public InteractionGameObject(GameObject gameObject)
        {
            GameObject = gameObject;
        }

        public GameObject GameObject { get; }
        
        public IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[0];
        }
    }
}
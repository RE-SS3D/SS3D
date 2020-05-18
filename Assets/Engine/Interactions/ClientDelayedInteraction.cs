using SS3D.Engine.Interactions.Extensions;
using UnityEngine;

namespace SS3D.Engine.Interactions
{
    public class ClientDelayedInteraction : IClientInteraction
    {
        public float Delay { get; set; }
        public GameObject LoadingBarPrefab { get; set; }

        private static readonly Vector3 LoadingBarOffset = new Vector3(0, 2f, 0);
        private GameObject loadingBarInstance;

        public virtual bool ClientStart(InteractionEvent interactionEvent)
        {
            if (LoadingBarPrefab == null)
            {
                return false;
            }
            
            if (interactionEvent.Source.GetRootSource() is IGameObjectProvider source)
            {
                loadingBarInstance = Object.Instantiate(LoadingBarPrefab, source.GameObject.transform);
                loadingBarInstance.transform.localPosition = LoadingBarOffset;
                loadingBarInstance.GetComponent<LoadingBar>().Duration = Delay;
            }

            return true;
        }

        public virtual bool ClientUpdate(InteractionEvent interactionEvent)
        {
            return true;
        }

        public virtual void ClientCancel(InteractionEvent interactionEvent)
        {
            Object.Destroy(loadingBarInstance);
        }
    }
}
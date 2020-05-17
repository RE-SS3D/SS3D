using UnityEngine;

namespace SS3D.Engine.Interactions
{
    public class ClientDelayedInteraction : IClientInteraction
    {
        public float Delay { get; set; }
        public GameObject LoadingBarPrefab { get; set; }

        private GameObject loadingBarInstance;

        public virtual bool ClientStart(InteractionEvent interactionEvent)
        {
            if (LoadingBarPrefab == null)
            {
                return false;
            }
            
            if (interactionEvent.Source is IGameObjectProvider source)
            {
                loadingBarInstance = GameObject.Instantiate(LoadingBarPrefab, source.GameObject.transform);
                loadingBarInstance.transform.localPosition = Vector3.zero;
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
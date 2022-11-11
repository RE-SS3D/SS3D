using Coimbra;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    public sealed class ClientDelayedInteraction : IClientInteraction
    {
        public float Delay { get; set; }
        public GameObject LoadingBarPrefab { get; set; }

        private static readonly Vector3 LoadingBarOffset = new(0, 2f, 0);
        private GameObject _loadingBarInstance;

        public bool ClientStart(InteractionEvent interactionEvent)
        {
            if (LoadingBarPrefab == null)
            {
                return false;
            }

            if (interactionEvent.Source.GetRootSource() is not IGameObjectProvider source)
            {
                return true;
            }

            _loadingBarInstance = Object.Instantiate(LoadingBarPrefab, source.GameObject.transform);
            _loadingBarInstance.transform.localPosition = LoadingBarOffset;
            _loadingBarInstance.GetComponent<LoadingBar>().Duration = Delay;

            return true;
        }

        public bool ClientUpdate(InteractionEvent interactionEvent)
        {
            return true;
        }

        public void ClientCancel(InteractionEvent interactionEvent)
        {
            _loadingBarInstance.Destroy();
        }
    }
}
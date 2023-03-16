using Coimbra;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// /// A client-side interaction that shows a loading bar
    /// </summary>
    public sealed class ClientDelayedInteraction : IClientInteraction
    {
        /// <summary>
        /// The duration of the loading bar in seconds
        /// </summary>
        public float Delay { get; set; }

        /// <summary>
        /// The loading bar prefab
        /// </summary>
        public GameObject LoadingBarPrefab { get; set; }

        private static readonly Vector3 LoadingBarOffset = new(0, 2f, 0);
        private GameObject _loadingBarInstance;

        /// <summary>
        /// Starts the interaction on the client side
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <returns>True if started successfully</returns>
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

            _loadingBarInstance = Object.Instantiate(LoadingBarPrefab, source.ProvidedGameObject.transform);
            _loadingBarInstance.transform.localPosition = LoadingBarOffset;
            _loadingBarInstance.GetComponent<LoadingBar>().Duration = Delay;

            return true;
        }

        /// <inheritdoc />
        public bool ClientUpdate(InteractionEvent interactionEvent)
        {
            return true;
        }

        /// <inheritdoc />
        public void ClientCancel(InteractionEvent interactionEvent)
        {
            _loadingBarInstance.Dispose(true);
        }
    }
}
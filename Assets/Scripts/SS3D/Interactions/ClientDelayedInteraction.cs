using Coimbra;
using Serilog;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Data;
using SS3D.Data.Generated;

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

        private GameObject _loadingBarInstance;

        /// <summary>
        /// Starts the interaction on the client side
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <returns>True if started successfully</returns>
        public bool ClientStart(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source.GetRootSource() is not IGameObjectProvider source)
            {
                return true;
            }

            _loadingBarInstance = WorldSpaceUI.LoadingBar.New();

            _loadingBarInstance.transform.SetParent(source.GameObject.transform);
            _loadingBarInstance.GetComponent<LoadingBar>().Duration = Delay;

            Log.Information("spawned loading bar");
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
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
        private static readonly Vector3 LoadingBarOffset = new(0, 0.5f, 0);

        private LoadingBar _loadingBarInstance;

        /// <summary>
        /// The duration of the loading bar in seconds
        /// </summary>
        public float Delay { get; set; }

        /// <summary>
        /// Starts the interaction on the client side
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <returns>True if started successfully</returns>
        public bool ClientStart(InteractionEvent interactionEvent)
        {
            if (_loadingBarInstance != null)
            {
                _loadingBarInstance.GameObject.Dispose(true);
            }

            if (interactionEvent.Source.GetRootSource() is not IGameObjectProvider source)
            {
                return true;
            }
                                                             
            WorldSpaceUI.LoadingBar.CreateAs(out _loadingBarInstance, source.GameObject.transform);
            
            _loadingBarInstance.LocalPosition = LoadingBarOffset;
            _loadingBarInstance.Duration = Delay;

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
            if (_loadingBarInstance != null)
            {
                _loadingBarInstance.GameObject.Dispose(true);
            }
        }
    }
}
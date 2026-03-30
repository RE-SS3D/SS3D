using Coimbra;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Data;
using SS3D.Data.Generated;
using System;
using Object = UnityEngine.Object;

namespace SS3D.Interactions
{
    /// <summary>
    /// /// A client-side interaction that shows a loading bar
    /// </summary>
    public sealed class ClientDelayedInteraction : IClientInteraction
    {
        private static readonly Vector3 LoadingBarOffset = new(0, 0.5f, 0);

        private AssetHandle<LoadingBar> _loadingBarPrefabHandle;

        private LoadingBar _loadingBarInstance;
        
        private bool _isDisposed;

        public ClientDelayedInteraction()
        {
            AcquireLoadingBar();
        }
        
        ~ClientDelayedInteraction()
        {
            Cleanup();
        }

        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        private void Cleanup()
        {
            if (_isDisposed)
            {
                return;
            }

            _loadingBarInstance?.Dispose(true);
            _loadingBarPrefabHandle?.Dispose();
            
            _isDisposed = true;
        }

        /// <summary>
        /// The duration of the loading bar in seconds
        /// </summary>
        public float Delay { get; init; }

        private async void AcquireLoadingBar()
        {
            _loadingBarPrefabHandle = await new AssetRequest<LoadingBar>(WorldSpaceUI.LoadingBar).ExecuteAsync();

            if (!_loadingBarPrefabHandle)
            {
                _loadingBarPrefabHandle?.Dispose();
                _loadingBarPrefabHandle = null;
            }
        }

        /// <summary>
        /// Starts the interaction on the client side
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <returns>True if started successfully</returns>
        public bool ClientStart(InteractionEvent interactionEvent)
        {
            if (_loadingBarInstance)
            {
                _loadingBarInstance.GameObject.Dispose(true);
            }

            if (interactionEvent.Source.GetRootSource() is not IGameObjectProvider source)
            {
                return true;
            }

            // Check if loading bar prefab is valid or not.
            if (_loadingBarPrefabHandle is { IsValid: true })
            {
                _loadingBarInstance = Object.Instantiate(_loadingBarPrefabHandle.Asset, source.GameObject.transform);

                _loadingBarInstance.LocalPosition = LoadingBarOffset;
                _loadingBarInstance.Duration = Delay;
            }

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
            if (_loadingBarInstance)
            {
                _loadingBarInstance.GameObject.Dispose(true);
            }
        }
    }
}
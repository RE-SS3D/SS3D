using Coimbra;
using SS3D.Core;
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

        private static AssetHandle<LoadingBar> LoadingBarPrefabHandle;

        private static bool TryingToLoadPrefab;

        private LoadingBar _loadingBarInstance;

        public ClientDelayedInteraction()
        {
            if (LoadingBarPrefabHandle is not { IsValid: true })
            {
                AcquireLoadingBar();
            }

            Application.quitting += OnApplicationQuit;
        }

        /// <summary>
        /// The duration of the loading bar in seconds
        /// </summary>
        public float Delay { get; init; }

        private void OnApplicationQuit()
        {
            ReleaseLoadingBarHandle();
        }

        private async void AcquireLoadingBar()
        {
            if (TryingToLoadPrefab || !SubSystems.TryGet(out AssetSubSystem assetSubSystem))
            {
                return;
            }

            TryingToLoadPrefab = true;
            LoadingBarPrefabHandle = await assetSubSystem.AcquireAsync<LoadingBar>(WorldSpaceUI.LoadingBar);

            if (LoadingBarPrefabHandle is { IsValid: false })
            {
                LoadingBarPrefabHandle.Dispose();
                LoadingBarPrefabHandle = null;
            }

            TryingToLoadPrefab = false;
        }

        private void ReleaseLoadingBarHandle()
        {
            LoadingBarPrefabHandle?.Dispose();
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
            if (LoadingBarPrefabHandle is not { IsValid: true })
            {
                AcquireLoadingBar();

                return true;
            }

            _loadingBarInstance = Object.Instantiate(LoadingBarPrefabHandle.Asset, source.GameObject.transform);

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
            if (_loadingBarInstance)
            {
                _loadingBarInstance.GameObject.Dispose(true);
            }
        }
    }
}
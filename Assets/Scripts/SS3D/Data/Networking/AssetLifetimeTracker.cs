using System;
using FishNet.Object;
using UnityEngine;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Tracks the server-side lifetime of a spawned addressable network object so
    /// consumers can react when the instance stops representing an active spawned object.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class AssetLifetimeTracker : MonoBehaviour
    {
        /// <summary>
        /// Raised once when this tracked instance is no longer considered an active spawned user of its asset.
        /// </summary>
        internal static event Action<AssetKey> OnReleased;

        private AssetKey _assetKey;

        private bool _released;
        private NetworkObject _networkObject;

        /// <summary>
        /// Arms the tracker for the currently associated addressable asset and resets the one-shot release guard.
        /// </summary>
        /// <param name="assetKey">Identity of the tracked asset.</param>
        internal void Initialize(AssetKey assetKey)
        {
            _assetKey = assetKey;
            _released = false;
            _networkObject = GetComponent<NetworkObject>();
        }

        private void OnDisable()
        {
            // FishNet often despawns pooled or scene objects by disabling them rather than destroying them.
            if (!_networkObject || !_networkObject.IsSpawned)
            {
                Release();
            }
        }

        private void OnDestroy()
        {
            // Fallback for destroy paths that do not go through a disable first.
            Release();
        }

        private void Release()
        {
            // Disable and destroy can both fire for the same instance, so emission must be idempotent.
            if (_released)
            {
                return;
            }

            _released = true;
            OnReleased?.Invoke(_assetKey);
        }
    }
}

using SS3D.Data;
using UnityEngine;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Stamped onto every <see cref="FishNet.Object.NetworkObject"/> prefab at editor time by the generation pass.
    /// Carries the asset GUID so that dependency prefabs loaded implicitly by Addressables
    /// can be discovered at runtime via <see cref="Resources.FindObjectsOfTypeAll{T}"/>.
    /// Self-destructs on spawned instances since only the prefab asset reference is needed.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AssetIdentifier))]
    internal sealed class NetworkObjectTracker : MonoBehaviour
    {
        /// <summary>
        /// GUID used to place this prefab into the generated FishNet prefab collection.
        /// The value is owned by <see cref="AssetIdentifier"/> to avoid serializing the same GUID twice.
        /// </summary>
        internal string AssetGuid
        {
            get
            {
                if (TryGetComponent(out AssetIdentifier identifier) && !string.IsNullOrEmpty(identifier.AssetGuid))
                {
                    return identifier.AssetGuid;
                }

                return null;
            }
        }

        /// <summary>
        /// Set by <see cref="NetworkObjects"/> once the prefab has been registered into FishNet's
        /// runtime prefab slot. Used to skip this tracker on subsequent scans.
        /// </summary>
        [System.NonSerialized]
        internal bool Initialized;

        private void Awake()
        {
            // Scene instances no longer need editor discovery metadata after FishNet spawns them.
            if (gameObject.scene.IsValid())
            {
                Destroy(this);
            }
        }
    }
}

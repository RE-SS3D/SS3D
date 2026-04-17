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
    internal sealed class NetworkObjectTracker : MonoBehaviour
    {
#if UNITY_EDITOR
        [field: SS3D.Attributes.ReadOnly]
#endif
        [field: SerializeField]
        internal string Guid { get; private set; }

        private void Awake()
        {
            if (gameObject.scene.IsValid())
            {
                Destroy(this);
            }
        }

#if UNITY_EDITOR
        internal void SetGuid(string guid)
        {
            Guid = guid;
        }
#endif
    }
}

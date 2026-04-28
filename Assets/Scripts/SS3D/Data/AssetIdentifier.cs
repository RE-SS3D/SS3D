using SS3D.Attributes;
using UnityEngine;

namespace SS3D.Data
{
    /// <summary>
    /// Stores the asset GUID for a prefab that participates in the asset system.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal sealed class AssetIdentifier : MonoBehaviour
    {
#if UNITY_EDITOR
        [ReadOnly]
#endif
        [SerializeField]
        private string _assetGuid;

        /// <summary>
        /// GUID of the prefab asset this component is stamped on.
        /// </summary>
        internal string AssetGuid => _assetGuid;

        /// <summary>
        /// Updates the serialized asset GUID during editor stamping or migration.
        /// Runtime callers should prefer the stamped value instead of changing it.
        /// </summary>
        internal void SetGuid(string assetGuid)
        {
            _assetGuid = assetGuid;
        }
    }
}

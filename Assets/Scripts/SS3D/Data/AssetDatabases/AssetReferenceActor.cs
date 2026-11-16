using SS3D.Attributes;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// The current asset system requires prefabs to have a component implementing the IWorldObjectAsset interface to work.
    /// This script can be put at the root of any game objects that need to be part of the asset system, it is there only
    /// as a simple implementation for the IWorldObjedctAsset interface.
    /// </summary>
    public class AssetReferenceActor : MonoBehaviour, IWorldObjectAsset
    {

        [SerializeField]
#if UNITY_EDITOR
        [ReadOnly]
        [Header("This field is filled automatically by the AssetData system.")]
#endif
        private ObjectAssetReference _asset;

        public ObjectAssetReference Asset
        {
            get => _asset;
            set
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Log.Warning(this, "Field {fieldName} is being modified in runtime. This should not happen in normal conditions.", Logs.Generic, nameof(Asset));
                }
                _asset = value;
            }
        }
    }
}

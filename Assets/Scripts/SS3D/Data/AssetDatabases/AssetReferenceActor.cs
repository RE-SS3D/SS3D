using SS3D.Attributes;
using SS3D.Data.AssetDatabases;
using System.Collections;
using System.Collections.Generic;
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
        private WorldObjectAssetReference _asset;

        public WorldObjectAssetReference Asset
        {
            get => _asset;
            set
            {
                if (Application.isPlaying)
                {
                    Serilog.Log.Warning($"Field {nameof(Asset)} is being modified in runtime. This should not happen in normal conditions.");
                }
                _asset = value;
            }
        }
    }
}

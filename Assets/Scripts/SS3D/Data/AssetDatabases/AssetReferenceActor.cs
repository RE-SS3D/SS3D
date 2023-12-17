using SS3D.Attributes;
using SS3D.Data.AssetDatabases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

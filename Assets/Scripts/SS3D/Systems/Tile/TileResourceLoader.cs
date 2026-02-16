using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Loads assets used by the tilemap. Can be used to retrieve scriptableobjects from a name string.
    /// </summary>
    public sealed class TileResourceLoader : MonoBehaviour
    {
        public Sprite _missingIcon;

        public bool IsInitialized { get; private set; }

        public List<GenericObjectSo> Assets { get; private set; }

        public void Awake()
        {
            LoadAssets();
        }

        private void LoadAssets()
        {
            Assets = new();

			Log.Information(this, "Loading tilemaps content");

            GenericObjectSo[] tempAssets = Resources.LoadAll<GenericObjectSo>("");
            LoadAssetsWithIcon(tempAssets);
        }

        private void LoadAssetsWithIcon(GenericObjectSo[] assets)
        {
            RuntimePreviewGenerator.OrthographicMode = true;

            foreach (GenericObjectSo asset in assets)
            {
                asset.icon = asset.icon ? asset.icon : _missingIcon;
                Assets.Add(asset);
            }

            IsInitialized = true;
        }

        [CanBeNull]
        public GenericObjectSo GetAsset(string assetName)
        {
            GenericObjectSo genericObjectSo = Assets.FirstOrDefault(tileObject =>  tileObject.NameString.Equals(assetName, StringComparison.OrdinalIgnoreCase));
            
            if (!genericObjectSo)
            {
	            Log.Warning(this, "Requested tile asset {assetName} was not found.", Logs.Generic, assetName);
            }

            return genericObjectSo;
        }

        [CanBeNull]
        public GenericObjectSo GetAsset(ObjectAssetReference asset)
        {
            GenericObjectSo genericObjectSo = Assets.FirstOrDefault(tileObject => tileObject.PrefabAsset.Equals(asset));

            if (!genericObjectSo)
            {
                Log.Warning(this, "Requested tile asset {assetName} was not found.", Logs.Generic, asset);
            }

            return genericObjectSo;
        }
    }
}
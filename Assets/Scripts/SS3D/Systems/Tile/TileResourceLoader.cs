using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using SS3D.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Loads assets used by the tilemap. Can be used to retrieve scriptableobjects from a name string.
    /// </summary>
    public sealed class TileResourceLoader: MonoBehaviour
    {
        public Sprite _missingIcon;

        public bool IsInitialized { get; private set; }

        public List<GenericObjectSo> Assets { get; private set; }

        public TileAssetCatalog Catalog { get; } = new();

        private readonly Dictionary<string, GenericObjectSo> _assetsById = new(StringComparer.Ordinal);

        public void Awake()
        {
            LoadAssets();
        }

        private void LoadAssets()
        {
            Assets = new();

			Log.Debug(this, "Loading tilemaps content");

            GenericObjectSo[] tempAssets = Resources.LoadAll<GenericObjectSo>("");
            foreach (GenericObjectSo asset in tempAssets)
                Assets.Add(asset);

            _assetsById.Clear();
            foreach (GenericObjectSo asset in Assets)
            {
                string id = SavedAssetLookup.IdOf(asset);
                if (string.IsNullOrEmpty(id))
                    continue;

                if (!_assetsById.TryAdd(id, asset))
                    Log.Error(this, "Tile assets {first} and {second} share prefab id {id}; saved maps load the first",
                        Logs.Generic, _assetsById[id].name, asset.name, id);
            }

            Catalog.Build(Assets);
            IsInitialized = true;

#if !UNITY_SERVER
            // Icons are only used by client-side UI (construction/build menus). Generating them
            // requires rendering a camera through URP; that crashes on NullGfxDevice
            // (-batchmode -nographics multiplayer harness clients) with GraphicsBuffer/Blitter errors.
            if (CanGeneratePreviewIcons())
            {
                StartCoroutine(LoadAssetsWithIcon(tempAssets));
            }
#endif
        }

        private static bool CanGeneratePreviewIcons()
        {
            return !UnityEngine.Application.isBatchMode
                && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
        }

        private IEnumerator LoadAssetsWithIcon(GenericObjectSo[] assets)
        {
	        List<Texture2D> tempIcons = new List<Texture2D>();

	        foreach (GenericObjectSo asset in assets)
	        {
                GameObject prefab = Data.Assets.Get<GameObject>(asset.PrefabAsset);
                if (prefab == null)
                {
                    tempIcons.Add(null);
                    continue;
                }

		        Quaternion previousPreviewRotation = RuntimePreviewGenerator.PreviewRotation;
		        RuntimePreviewGenerator.PreviewRotation = prefab.transform.localRotation;
		        Texture2D texture;
		        try
		        {
			        texture = IconPreviewGenerator.Generate(prefab.transform, 128, 128, shouldCloneModel: true);
		        }
		        finally
		        {
			        RuntimePreviewGenerator.PreviewRotation = previousPreviewRotation;
		        }

		        tempIcons.Add(texture);
	        }

	        for (int i = 0; i < assets.Length; i++)
	        {
		        Assets[i].icon = tempIcons[i] != null
			        ? Sprite.Create(tempIcons[i], new Rect(0, 0, tempIcons[i].width, tempIcons[i].height), new Vector2(0.5f, 0.5f))
			        : _missingIcon;
	        }

	        yield return null;
        }

        [CanBeNull]
        public GenericObjectSo GetAsset(ushort assetId) => Catalog.GetAsset(assetId);

        [CanBeNull]
        public GenericObjectSo GetAsset(string assetName)
        {
            GenericObjectSo genericObjectSo = Assets.FirstOrDefault(tileObject =>  tileObject.NameString.Equals(assetName, StringComparison.OrdinalIgnoreCase));
            
            if (genericObjectSo == null)
            {
	            Log.Warning(this, "Requested tile asset {assetName} was not found.", Logs.Generic, assetName);
            }

            return genericObjectSo;
        }

        /// <summary>
        /// The asset whose prefab has this <see cref="ObjectAssetReference.Id"/>, or null. Logs nothing, so a caller can fall back.
        /// </summary>
        [CanBeNull]
        public GenericObjectSo GetAssetById(string id) =>
            id != null && _assetsById.TryGetValue(id, out GenericObjectSo asset) ? asset : null;

        /// <summary>
        /// Resolves a saved object: by id, then by name through <see cref="RenamedTileObjects"/>.
        /// </summary>
        [CanBeNull]
        public GenericObjectSo GetSavedAsset(string id, string savedName) =>
            SavedAssetLookup.Resolve(id, savedName, GetAssetById, GetAsset);

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
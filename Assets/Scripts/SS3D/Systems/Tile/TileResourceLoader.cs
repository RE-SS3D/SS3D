using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
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
	        RuntimePreviewGenerator.OrthographicMode = true;

	        foreach (GenericObjectSo asset in assets)
	        {
                GameObject prefab = Data.Assets.Get<GameObject>(asset.PrefabAsset);
                Transform prefabTransform = prefab.transform;
		        Shader shader = Shader.Find("Unlit/ObjectIcon");

		        Texture2D texture = RuntimePreviewGenerator.GenerateModelPreviewWithShader(prefabTransform, shader, null, 128, 128, true);

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
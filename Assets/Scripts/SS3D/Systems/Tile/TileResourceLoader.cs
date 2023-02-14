using SS3D.Core.Behaviours;
using SS3D.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Tile
{

    public class TileResourceLoader: MonoBehaviour
    {
        private List<TileObjectSo> _tileAssets;
        private List<ItemObjectSo> _itemAssets;
        private bool _initialized = false;

        public void Start()
        {
            LoadAssets();
        }

        private void LoadAssets()
        {
            LoadTileAssets();
            LoadItemAssets();
        }

        private void LoadTileAssets()
        {
            _tileAssets = new List<TileObjectSo>();

            TileObjectSo[] tempAssets = Resources.LoadAll<TileObjectSo>("");

            StartCoroutine(LoadAssetsWithIcon(tempAssets));
        }

        private IEnumerator LoadAssetsWithIcon(TileObjectSo[] assets)
        {
            List<Texture2D> tempIcons = new List<Texture2D>();

#if UNITY_EDITOR
            foreach (var asset in assets)
            {
                tempIcons.Add(AssetPreview.GetAssetPreview(asset.prefab));
            }

            while (AssetPreview.IsLoadingAssetPreviews())
            {
                yield return null;
            }
#endif
            
            for (int i = 0; i < assets.Length; i++)
            {
#if UNITY_EDITOR
                assets[i].icon = Sprite.Create(tempIcons[i], new Rect(0, 0, tempIcons[i].width, tempIcons[i].height), new Vector2(0.5f, 0.5f));
#endif
                _tileAssets.Add(assets[i]);
            }

            _initialized = true;
            yield return null;
        }

        private void LoadItemAssets()
        {
            _itemAssets = new List<ItemObjectSo>();
            ItemObjectSo[] tempAssets = Resources.LoadAll<ItemObjectSo>("");

            foreach (var asset in tempAssets)
            {
                _itemAssets.Add(asset);
            }
        }

        public bool IsInitialized()
        {
            return _initialized;
        }

        public TileObjectSo GetTileAsset(string assetName)
        {
            TileObjectSo tileObjectSo = _tileAssets.FirstOrDefault(tileObject => tileObject.nameString == assetName);
            if (tileObjectSo == null)
                Punpun.Yell(this, $"Requested tile asset {assetName} was not found.");

            return tileObjectSo;
        }

        public ItemObjectSo GetItemAsset(string assetName)
        {
            ItemObjectSo itemObjectSo = _itemAssets.FirstOrDefault(tileObject => tileObject.nameString == assetName);
            if (itemObjectSo == null)
                Punpun.Yell(this, $"Requested tile asset {assetName} was not found.");

            return itemObjectSo;
        }

        public List<TileObjectSo> GetAllTileAssets()
        {
            return _tileAssets;
        }

        public List<ItemObjectSo> GetAllItemAssets()
        {
            return _itemAssets;
        }
    }
}
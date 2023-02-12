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

            foreach (var asset in tempAssets)
            {
                StartCoroutine(LoadAssetWithIcon(asset));
            }
        }

        private IEnumerator LoadAssetWithIcon(TileObjectSo asset)
        {
            Texture2D texture = AssetPreview.GetAssetPreview(asset.prefab);

            yield return new WaitUntil(() => AssetPreview.IsLoadingAssetPreview(asset.GetInstanceID()) == false);

            asset.icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            _tileAssets.Add(asset);
            Debug.Log("Asset added");
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
using SS3D.Core.Behaviours;
using SS3D.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public class TileResourceLoader
    {
        private List<TileObjectSo> _tileAssets;
        private List<ItemObjectSo> _itemAssets;

        public TileResourceLoader()
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
            _tileAssets.AddRange(tempAssets);
        }

        private void LoadItemAssets()
        {
            _itemAssets = new List<ItemObjectSo>();
            ItemObjectSo[] tempAssets = Resources.LoadAll<ItemObjectSo>("");
            _itemAssets.AddRange(tempAssets);
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
    }
}
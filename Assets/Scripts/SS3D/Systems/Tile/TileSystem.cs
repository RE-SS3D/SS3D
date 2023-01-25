using SS3D.Core.Behaviours;
using SS3D.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public class TileSystem : NetworkSystem
    {
        private List<TileObjectSo> _tileAssets;

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        private void Setup()
        {
            LoadTileAssets();
        }

        private void LoadTileAssets()
        {
            _tileAssets = new List<TileObjectSo>();
            TileObjectSo[] tempAssets = Resources.LoadAll<TileObjectSo>("");
            _tileAssets.AddRange(tempAssets);
        }

        public TileObjectSo GetTileAsset(string assetName)
        {
            TileObjectSo tileObjectSo = _tileAssets.FirstOrDefault(tileObject => tileObject.nameString == assetName);
            if (tileObjectSo == null)
                Punpun.Yell(this, $"Requested tile asset {assetName} was not found.");
            
            return tileObjectSo;
        }
    }
}
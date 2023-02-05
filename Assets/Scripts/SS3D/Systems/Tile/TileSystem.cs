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
        private List<ItemObjectSo> _itemAssets;
        private TileMap _currentMap;

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        private void Setup()
        {
            LoadTileAssets();
            LoadItemAssets();

            CreateMap("Test map");
            // Load();

            Punpun.Say(this, "All tiles loaded successfully");
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

        private void CreateMap(string mapName)
        {
            if (_currentMap == null)
            {
                TileMap map = TileMap.Create(mapName);
                map.transform.SetParent(transform);
                _currentMap = map;
            }
        }

        public bool PlaceTileObject(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir)
        {
            return _currentMap.PlaceTileObject(tileObjectSo, placePosition, dir, false);
        }

        public void PlaceItemObject(ItemObjectSo itemObjectSo, Vector3 placePosition, Quaternion rotation)
        {
            _currentMap.PlaceItemObject(placePosition, rotation, itemObjectSo);
        }

        public bool CanBuild(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir)
        {
            return _currentMap.CanBuild(tileObjectSo, placePosition, dir);
        }

        public void Save()
        {
            var mapSave = _currentMap.Save();
            SaveSystem.SaveObject(mapSave);
        }

        public void Load()
        {
            var mapSave = SaveSystem.LoadMostRecentObject<TileMap.MapSaveObject>();
            _currentMap.Load(mapSave);
        }
    }
}
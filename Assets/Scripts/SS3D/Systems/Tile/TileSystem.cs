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
        private TileMap _currentMap;
        private TileResourceLoader _loader;

        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        private void Setup()
        {
            _loader = GetComponent<TileResourceLoader>();
            CreateMap("Test map");
            // Load();

            Punpun.Say(this, "All tiles loaded successfully");
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

        public TileObjectSo GetTileAsset(string assetName)
        {
            return _loader.GetTileAsset(assetName);
        }

        public ItemObjectSo GetItemAsset(string assetName)
        {
            return _loader.GetItemAsset(assetName);
        }

        public TileResourceLoader GetLoader()
        {
            return _loader;
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

        public void ResetSave()
        {
            _currentMap.Clear();
            Save();
            Punpun.Yell(this, "Tilemap resetted. Existing savefile has been wiped");
        }
    }
}
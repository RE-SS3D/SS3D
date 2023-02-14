using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using System;
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

        private IEnumerator WaitForResourcesLoad()
        {
            while (!_loader.IsInitialized())
            {
                yield return null;
            }

            Load();
        }


        private void Setup()
        {
            _loader = GetComponent<TileResourceLoader>();

            // Server only loads the map
            if (IsServer)
            {
                CreateMap("Test map");
                StartCoroutine(WaitForResourcesLoad());
                Punpun.Say(this, "All tiles loaded successfully");
            }
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

        private bool PlaceTileObject(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir)
        {
            return _currentMap.PlaceTileObject(tileObjectSo, placePosition, dir, false);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RpcPlaceTileObject(string tileObjectSoName, Vector3 placePosition, Direction dir)
        {
            TileObjectSo tileObjectSo = GetTileAsset(tileObjectSoName);
            PlaceTileObject(tileObjectSo, placePosition, dir);
        }

        private void ClearTileObject(TileObjectSo tileObjectSo, Vector3 placePosition)
        {
            _currentMap.ClearTileObject(placePosition, tileObjectSo.layer);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RpcClearTileObject(string tileObjectSoName, Vector3 placePosition)
        {
            TileObjectSo tileObjectSo = GetTileAsset(tileObjectSoName);
            _currentMap.ClearTileObject(placePosition, tileObjectSo.layer);
        }

        private void PlaceItemObject(ItemObjectSo itemObjectSo, Vector3 placePosition, Quaternion rotation)
        {
            _currentMap.PlaceItemObject(placePosition, rotation, itemObjectSo);
        }

        public bool CanBuild(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir)
        {
            return _currentMap.CanBuild(tileObjectSo, placePosition, dir);
        }

        /*
        [ServerRpc(RequireOwnership = false)]
        public void RpcCanBuild(string tileObjectSoName, Vector3 placePosition, Direction dir)
        {
            TileObjectSo tileObjectSo = GetTileAsset(tileObjectSoName);

            bool canBuild = _currentMap.CanBuild(tileObjectSo, placePosition, dir);
            RpcReceiveCanBuild(canBuild);
        }
        */
       

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
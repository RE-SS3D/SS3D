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

        public GenericObjectSo GetAsset(string assetName)
        {
            return _loader.GetAsset(assetName);
        }

        public TileResourceLoader GetLoader()
        {
            return _loader;
        }

        private bool PlaceObject(GenericObjectSo genericObjectSo, Vector3 placePosition, Direction dir)
        {
            if (genericObjectSo is TileObjectSo)
            {
                return _currentMap.PlaceTileObject((TileObjectSo)genericObjectSo, placePosition, dir, false);
            }
            else if (genericObjectSo is ItemObjectSo)
            {
                _currentMap.PlaceItemObject(placePosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0), (ItemObjectSo)genericObjectSo);
            }

            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RpcPlaceObject(string genericObjectSoName, Vector3 placePosition, Direction dir)
        {
            GenericObjectSo tileObjectSo = GetAsset(genericObjectSoName);
            PlaceObject(tileObjectSo, placePosition, dir);
        }

        private void ClearTileObject(TileObjectSo tileObjectSo, Vector3 placePosition)
        {
            _currentMap.ClearTileObject(placePosition, tileObjectSo.layer);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RpcClearTileObject(string tileObjectSoName, Vector3 placePosition)
        {
            GenericObjectSo tileObjectSo = GetAsset(tileObjectSoName);
            _currentMap.ClearTileObject(placePosition, ((TileObjectSo)tileObjectSo).layer);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RpcClearItemObject(string itemObjectSoName, Vector3 placePosition)
        {
            ItemObjectSo itemObjectSo = (ItemObjectSo)GetAsset(itemObjectSoName);
            _currentMap.ClearItemObject(placePosition, itemObjectSo);
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
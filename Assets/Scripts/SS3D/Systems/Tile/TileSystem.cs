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
 
    /// <summary>
    /// Manages and keeps an inventory of all placed tiles. This is where all others scripts that use the tilemap should interact with.
    /// </summary>
    public class TileSystem : NetworkSystem
    {
        public TileResourceLoader Loader { get; private set; }

        private TileMap _currentMap;

        [ServerOrClient]
        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        [ServerOrClient]
        private IEnumerator WaitForResourcesLoad()
        {
            while (!Loader.IsInitialized)
            {
                yield return null;
            }

            Load();
        }

        [ServerOrClient]
        private void Setup()
        {
            Loader = GetComponent<TileResourceLoader>();

            // Server only loads the map
            if (IsServer)
            {
                CreateMap("Test map");
                StartCoroutine(WaitForResourcesLoad());
                Punpun.Information(this, "All tiles loaded successfully");
            }
        }

        [ServerOrClient]
        private void CreateMap(string mapName)
        {
            if (_currentMap == null)
            {
                TileMap map = TileMap.Create(mapName);
                map.transform.SetParent(transform);
                _currentMap = map;
            }
        }

        [ServerOrClient]
        public GenericObjectSo GetAsset(string assetName)
        {
            return Loader.GetAsset(assetName);
        }

        [Server]
        private bool PlaceObject(GenericObjectSo genericObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting)
        {
            if (genericObjectSo is TileObjectSo)
            {
                return _currentMap.PlaceTileObject((TileObjectSo)genericObjectSo, placePosition, dir, false, replaceExisting);
            }
            else if (genericObjectSo is ItemObjectSo)
            {
                _currentMap.PlaceItemObject(placePosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0), (ItemObjectSo)genericObjectSo);
            }

            return true;
        }

        [Client]
        [ServerRpc(RequireOwnership = false)] // No ownership required since clients are allowed to place/remove objects. Should be removed when construction is in.
        public void RpcPlaceObject(string genericObjectSoName, Vector3 placePosition, Direction dir, bool replaceExisting)
        {
            GenericObjectSo tileObjectSo = GetAsset(genericObjectSoName);
            PlaceObject(tileObjectSo, placePosition, dir, replaceExisting);
        }

        [Server]
        private void ClearTileObject(TileObjectSo tileObjectSo, Vector3 placePosition)
        {
            _currentMap.ClearTileObject(placePosition, tileObjectSo.layer);
        }

        [Client]
        [ServerRpc(RequireOwnership = false)] // No ownership required since clients are allowed to place/remove objects. Should be removed when construction is in.
        public void RpcClearTileObject(string tileObjectSoName, Vector3 placePosition)
        {
            GenericObjectSo tileObjectSo = GetAsset(tileObjectSoName);
            _currentMap.ClearTileObject(placePosition, ((TileObjectSo)tileObjectSo).layer);
        }

        [Client]
        [ServerRpc(RequireOwnership = false)] // No ownership required since clients are allowed to place/remove objects. Should be removed when construction is in.
        public void RpcClearItemObject(string itemObjectSoName, Vector3 placePosition)
        {
            ItemObjectSo itemObjectSo = (ItemObjectSo)GetAsset(itemObjectSoName);
            _currentMap.ClearItemObject(placePosition, itemObjectSo);
        }

        [Server]
        public bool CanBuild(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting)
        {
            return _currentMap.CanBuild(tileObjectSo, placePosition, dir, replaceExisting);
        }

        [Server]
        public void Save()
        {
            var mapSave = _currentMap.Save();
            SaveSystem.SaveObject(mapSave);
        }

        [Server]
        public void Load()
        {
            var mapSave = SaveSystem.LoadMostRecentObject<SavedTileMap>();
            _currentMap.Load(mapSave);
        }

        [Server]
        public void ResetSave()
        {
            _currentMap.Clear();
            Save();
            Punpun.Warning(this, "Tilemap resetted. Existing savefile has been wiped");
        }
    }
}
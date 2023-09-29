﻿using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Data.Management;
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
	    public const string SavePath = "/Tilemaps";

	    public const string UnnamedMapName = "UnnamedMap";

        public TileResourceLoader Loader { get; private set; }
 
        private TileMap _currentMap;

        [ServerOrClient]
        protected override void OnStart()
        {
            base.OnStart();
            Setup();
        }

        [ServerOrClient]
        private async UniTask WaitForResourcesLoad()
        {
	        await UniTask.WaitUntil(() => Loader.IsInitialized);

            Load();
        }

        [Server]
        private async void Setup()
        {
	        Loader = GetComponent<TileResourceLoader>();

	        // Server only loads the map
	        if (!IsServer)
	        {
		        return;
	        }

	        CreateMap(UnnamedMapName);

	        await WaitForResourcesLoad();

            Log.Information(this, "All tiles loaded successfully");
        }

        [ServerOrClient]
        private void CreateMap(string mapName)
        {
	        if (_currentMap != null)
	        {
                Log.Warning(this, $"A map is already loaded. {mapName}");
		        return;
	        }

			Log.Information(this, $"Creating new tilemap {mapName}");

	        TileMap map = TileMap.Create(mapName);
	        map.transform.SetParent(transform);
	        _currentMap = map;
        }

        [ServerOrClient]
        public GenericObjectSo GetAsset(string assetName) => Loader.GetAsset(assetName);

        [Server]
        private bool PlaceObject(GenericObjectSo genericObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting)
        {
	        switch (genericObjectSo)
	        {
		        case TileObjectSo so:
			        return _currentMap.PlaceTileObject(so, placePosition, dir, false, replaceExisting);
		        case ItemObjectSo so:
			        _currentMap.PlaceItemObject(placePosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0), so);
			        break;
	        }

	        return true;
        }

        // No ownership required since clients are allowed to place/remove objects. Should be removed when construction is in.
        [Client]
        [ServerRpc(RequireOwnership = false)]
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

        // No ownership required since clients are allowed to place/remove objects. Should be removed when construction is in.
        [Client]
        [ServerRpc(RequireOwnership = false)]
        public void RpcClearTileObject(string tileObjectSoName, Vector3 placePosition)
        {
            GenericObjectSo tileObjectSo = GetAsset(tileObjectSoName);
            _currentMap.ClearTileObject(placePosition, ((TileObjectSo)tileObjectSo).layer);
        }

        // No ownership required since clients are allowed to place/remove objects. Should be removed when construction is in.
        [Client]
        [ServerRpc(RequireOwnership = false)]
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
			Log.Information(this, $"Saving tilemap {_currentMap.name}");

            SavedTileMap mapSave = _currentMap.Save();
												    
            SaveSystem.SaveObject(SavePath + "/" + _currentMap.name, mapSave);
        }

        [Server]
        public void Load()
        {
            Log.Information(this, "Loading most recent tilemap");
            
	        SavedTileMap mapSave = SaveSystem.LoadMostRecentObject<SavedTileMap>(SavePath);

            _currentMap.Load(mapSave);
        }

        [Server]
        public void ResetSave()
        {
            _currentMap.Clear();
            Save();
            Log.Warning(this, "Tilemap resetted. Existing savefile has been wiped");
        }
    }
}
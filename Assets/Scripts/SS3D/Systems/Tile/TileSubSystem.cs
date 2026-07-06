using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
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
    public class TileSubSystem : NetworkSubSystem
    {
	    public const string savePath = "/Tilemaps";

	    public const string unnamedMapName = "UnnamedMap";

        public TileResourceLoader Loader { get; private set; }
 
        private TileMap _currentMap;
        private TileQueryService _queryService;
        private ConstructionService _constructionService;
        public TileMap CurrentMap => _currentMap;
        public ITileQueryService QueryService => _queryService;
        public IConstructionService Construction => _constructionService;

        public string SavePath => savePath;


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

	        CreateMap(unnamedMapName);

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
	        _queryService = new TileQueryService(map);
	        _constructionService = new ConstructionService(map, _queryService);
        }

        public void RegisterTileMutationObserver(ITileMutationObserver observer)
        {
            _currentMap?.RegisterMutationObserver(observer);
        }

        public void UnregisterTileMutationObserver(ITileMutationObserver observer)
        {
            _currentMap?.UnregisterMutationObserver(observer);
        }

        [ServerOrClient]
        public GenericObjectSo GetAsset(string assetName) => Loader.GetAsset(assetName);

        [ServerOrClient]
        public GenericObjectSo GetAsset(ushort assetId) => Loader?.GetAsset(assetId);

        [ServerOrClient]
        public ushort TryGetAssetId(GenericObjectSo asset) => Loader?.Catalog.TryGetAssetId(asset) ?? TileAssetCatalog.InvalidAssetId;

        [ServerOrClient]
        public GenericObjectSo GetAsset(ObjectAssetReference asset) => Loader.GetAsset(asset);

        [Server]
        private bool PlaceObject(GenericObjectSo genericObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting)
        {
	        switch (genericObjectSo)
	        {
		        case TileObjectSo so:
			        return _constructionService.TryPlaceTile(so, placePosition, dir, replaceExisting).Success;
		        case ItemObjectSo so:
			        return _constructionService.TryPlaceItem(so, placePosition,
                        Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0)).Success;
	        }

	        return false;
        }

        /// <summary>
        /// TileMap Creator place RPC. Requires <see cref="ServerRoleTypes.Administrator"/> on the server.
        /// </summary>
        [Client]
        [ServerRpc(RequireOwnership = false)]
        public void RpcPlaceObject(string genericObjectSoName, Vector3 placePosition, Direction dir, bool replaceExisting,
            NetworkConnection conn = null)
        {
            if (!TileMapEditorPermissions.TryAuthorize(conn))
                return;

            GenericObjectSo tileObjectSo = GetAsset(genericObjectSoName);
            PlaceObject(tileObjectSo, placePosition, dir, replaceExisting);
        }

        /// <summary>
        /// TileMap Creator clear-tile RPC. Requires <see cref="ServerRoleTypes.Administrator"/> on the server.
        /// </summary>
        [Client]
        [ServerRpc(RequireOwnership = false)]
        public void RpcClearTileObject(string tileObjectSoName, Vector3 placePosition, Direction dir,
            NetworkConnection conn = null)
        {
            if (!TileMapEditorPermissions.TryAuthorize(conn))
                return;

            GenericObjectSo tileObjectSo = GetAsset(tileObjectSoName);
            _constructionService.TryClearTile(placePosition, ((TileObjectSo)tileObjectSo).layer, dir);
        }

        /// <summary>
        /// TileMap Creator clear-item RPC. Requires <see cref="ServerRoleTypes.Administrator"/> on the server.
        /// </summary>
        [Client]
        [ServerRpc(RequireOwnership = false)]
        public void RpcClearItemObject(string itemObjectSoName, Vector3 placePosition, NetworkConnection conn = null)
        {
            if (!TileMapEditorPermissions.TryAuthorize(conn))
                return;

            ItemObjectSo itemObjectSo = (ItemObjectSo)GetAsset(itemObjectSoName);
            _constructionService.TryClearItem(placePosition, itemObjectSo);
        }

        [Server]
        public bool CanBuild(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting)
        {
            return _constructionService.TryPreviewTile(tileObjectSo, placePosition, dir, replaceExisting).CanBuild;
        }

        [Server]
        public void Save(string mapName, bool overwrite)
        {
			Log.Information(this, $"Saving tilemap {mapName}");

            SavedTileMap mapSave = _currentMap.Save();
												    
            LocalStorage.SaveObject(SavePath + "/" + mapName, mapSave, overwrite);
        }

        [Server]
        public void Load()
        {
            Log.Information(this, "Loading most recent tilemap");
            
	        SavedTileMap mapSave = LocalStorage.LoadMostRecentObject<SavedTileMap>(SavePath);

            _currentMap.Load(mapSave);
        }

        [Server]
        public void Load(string mapName)
        {
            Log.Information(this, "Loading most recent tilemap");

            SavedTileMap mapSave = LocalStorage.LoadObject<SavedTileMap>(mapName);

            _currentMap.Load(mapSave);
        }

        [Server]
        public void ResetSave()
        {
            _currentMap.Clear();
            Save("UnnamedMap", true);
            Log.Warning(this, "Tilemap resetted. Existing savefile has been wiped");
        }

        public bool MapNameAlreadyExist(string name)
        {
            return LocalStorage.FolderAlreadyContainsName(savePath, name);
        }
    }
}
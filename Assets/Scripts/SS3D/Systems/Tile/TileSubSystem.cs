using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Management;
using SS3D.Data.Persistence;
using SS3D.Logging;
using SS3D.Systems.Persistence;
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
	    public const string savePath = PersistencePaths.StationTemplates;

	    public const string legacySavePath = PersistencePaths.LegacyTilemaps;

	    public const string unnamedMapName = "UnnamedMap";

        public TileResourceLoader Loader { get; private set; }
 
        private TileMap _currentMap;
        private TileQueryService _queryService;
        private ConstructionService _constructionService;
        public TileMap CurrentMap => _currentMap;
        public ITileQueryService QueryService => _queryService;
        public IConstructionService Construction => _constructionService;

        public event Action OnMapCreated;

        public string SavePath => savePath;

        // Placed objects spawned on a remote client before the client-local map was created.
        private readonly List<PlacedTileObject> _pendingClientPlaced = new();


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

        [ServerOrClient]
        private async void Setup()
        {
	        Loader = GetComponent<TileResourceLoader>();

	        // Both peers hold a map instance. On a remote client it starts empty and is populated by
	        // replicated placed objects; only the server loads persisted tiles from disk.
	        CreateMap(unnamedMapName);
	        FlushPendingClientPlaced();

	        if (!IsServer)
	        {
		        return;
	        }

            if (SubSystems.TryGet(out PersistenceSubSystem persistenceSubSystem))
            {
                persistenceSubSystem.LoadServerMeta();
            }

	        await WaitForResourcesLoad();

            Log.Debug(this, "All tiles loaded successfully");
        }

        [ServerOrClient]
        private void CreateMap(string mapName)
        {
	        if (_currentMap != null)
	        {
                Log.Warning(this, $"A map is already loaded. {mapName}");
		        return;
	        }

			Log.Debug(this, $"Creating new tilemap {mapName}");

	        TileMap map = TileMap.Create(mapName);
	        map.transform.SetParent(transform);
	        _currentMap = map;
	        _queryService = new TileQueryService(map);
	        _constructionService = new ConstructionService(map, _queryService);
            OnMapCreated?.Invoke();
        }

        public void RegisterTileMutationObserver(ITileMutationObserver observer)
        {
            _currentMap?.RegisterMutationObserver(observer);
        }

        public void UnregisterTileMutationObserver(ITileMutationObserver observer)
        {
            _currentMap?.UnregisterMutationObserver(observer);
        }

        /// <summary>
        /// Called by a placed tile object once its synced identity is applied on a remote client. Mirrors
        /// the object into the client-local map, queueing it if the map has not been created yet.
        /// </summary>
        public void NotifyClientPlacedObjectStarted(PlacedTileObject placed)
        {
            if (placed == null)
                return;

            if (_currentMap != null)
                _currentMap.AddClientPlacedObject(placed);
            else if (!_pendingClientPlaced.Contains(placed))
                _pendingClientPlaced.Add(placed);
        }

        /// <summary>
        /// Called by a placed tile object despawning on a remote client.
        /// </summary>
        public void NotifyClientPlacedObjectStopped(PlacedTileObject placed)
        {
            if (placed == null)
                return;

            _pendingClientPlaced.Remove(placed);
            _currentMap?.RemoveClientPlacedObject(placed);
        }

        /// <summary>
        /// Notifies the tilemap that a tile cell's runtime state changed (e.g. door open/close).
        /// </summary>
        public void NotifyTileStateChanged(Vector3 worldPosition)
        {
            _currentMap?.NotifyTileStateChanged(worldPosition);
        }

        private void FlushPendingClientPlaced()
        {
            if (_currentMap == null || _pendingClientPlaced.Count == 0)
                return;

            foreach (PlacedTileObject placed in _pendingClientPlaced)
            {
                if (placed != null)
                    _currentMap.AddClientPlacedObject(placed);
            }

            _pendingClientPlaced.Clear();
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
			Log.Debug(this, $"Saving station template {mapName}");

            if (SubSystems.TryGet(out PersistenceSubSystem persistenceSubSystem))
            {
                persistenceSubSystem.SaveStationTemplate(mapName, overwrite);
                return;
            }

            SavedTileMap mapSave = _currentMap.Save();
            LocalStorage.SaveObject(legacySavePath + "/" + mapName, mapSave, overwrite);
        }

        [Server]
        public void Load()
        {
            Log.Debug(this, "Loading most recent station template");

            if (SubSystems.TryGet(out PersistenceSubSystem persistenceSubSystem))
            {
                persistenceSubSystem.LoadMostRecentStationTemplate();
                return;
            }

	        SavedTileMap mapSave = LocalStorage.LoadMostRecentObject<SavedTileMap>(legacySavePath);
            _currentMap.Load(mapSave);
        }

        [Server]
        public void Load(string mapName)
        {
            Log.Debug(this, $"Loading station template {mapName}");

            if (SubSystems.TryGet(out PersistenceSubSystem persistenceSubSystem))
            {
                persistenceSubSystem.LoadStationTemplate(mapName);
                return;
            }

            SavedTileMap mapSave = LocalStorage.LoadObject<SavedTileMap>(legacySavePath + "/" + mapName);
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
            if (SubSystems.TryGet(out PersistenceSubSystem persistenceSubSystem))
            {
                return persistenceSubSystem.StationTemplateExists(name);
            }

            return LocalStorage.FolderAlreadyContainsName(savePath, name)
                || LocalStorage.FolderAlreadyContainsName(legacySavePath, name);
        }
    }
}
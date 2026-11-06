using Coimbra;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Application;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Management;
using SS3D.Data.Persistence;
using SS3D.Logging;
using SS3D.Systems.Area;
using SS3D.Systems.Persistence;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile.FloorVisuals;
using SS3D.Systems.Tile.SpawnPoints;
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
    public partial class TileSubSystem : NetworkSubSystem
    {
	    public const string savePath = PersistencePaths.StationTemplates;

	    public const string legacySavePath = PersistencePaths.LegacyTilemaps;

	    public const string unnamedMapName = "UnnamedMap";

        public TileResourceLoader Loader { get; private set; }
 
        private TileMap _currentMap;
        private TileQueryService _queryService;
        private ConstructionService _constructionService;
        private readonly SpawnPointRegistry _spawnPoints = new();
        public TileMap CurrentMap => _currentMap;
        public ITileQueryService QueryService => _queryService;
        public IConstructionService Construction => _constructionService;
        public SpawnPointRegistry SpawnPoints => _spawnPoints;

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
        private void Setup()
        {
            Loader = GetComponent<TileResourceLoader>();
            // Keep an empty map container for authoring and client AOI registration. A station is
            // restored only by Start Round (or an explicit Map Editor load), never by session boot.
            CreateMap(unnamedMapName);
            FlushPendingClientPlaced();
            if (IsServer && SubSystems.TryGet(out WorldReadiness.WorldReadinessSubSystem readiness))
                readiness.NotifyStationTemplateRestoreBeginning();
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
            map.RegisterMutationObserver(new WreckageMutationObserver(this));
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
        private bool PlaceObject(GenericObjectSo genericObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting,
            bool skipBuildCheck = false)
        {
	        switch (genericObjectSo)
	        {
		        case TileObjectSo so:
			        return _constructionService.TryPlaceTile(so, placePosition, dir, replaceExisting, skipBuildCheck).Success;
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
            PlaceObject(tileObjectSo, placePosition, dir, replaceExisting, skipBuildCheck: false);
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

        /// <summary>
        /// TileMap Creator place floor-decal RPC. Requires administrator. Writes sparse ushort id, not a PlacedTileObject.
        /// </summary>
        [Client]
        [ServerRpc(RequireOwnership = false)]
        public void RpcSetFloorDecal(ushort decalId, Vector3 placePosition, NetworkConnection conn = null)
        {
            if (!TileMapEditorPermissions.TryAuthorize(conn))
                return;

            if (_currentMap == null)
                return;

            if (_currentMap.TrySetFloorDecal(placePosition, decalId))
                SyncFloorDecalsToClients();
        }

        [Client]
        [ServerRpc(RequireOwnership = false)]
        public void RpcClearFloorDecal(Vector3 placePosition, NetworkConnection conn = null)
        {
            if (!TileMapEditorPermissions.TryAuthorize(conn))
                return;

            if (_currentMap == null)
                return;

            if (_currentMap.TryClearFloorDecal(placePosition))
                SyncFloorDecalsToClients();
        }

        [Server]
        public void SyncFloorDecalsToClients()
        {
            if (_currentMap == null)
                return;

            var chunks = new List<SyncedFloorDecalChunk>();
            foreach (TileChunk chunk in _currentMap.GetAllChunks())
            {
                ushort[] ids = chunk.CopyFloorDecalIds();
                if (ids == null)
                    continue;

                Vector2Int key = _currentMap.GetKey(chunk.GetWorldPosition(0, 0));
                chunks.Add(new SyncedFloorDecalChunk
                {
                    chunkKeyX = key.x,
                    chunkKeyY = key.y,
                    floorDecalIds = ids,
                });
            }

            RpcSyncFloorDecals(chunks.ToArray());
        }

        [ObserversRpc(BufferLast = true)]
        private void RpcSyncFloorDecals(SyncedFloorDecalChunk[] chunks)
        {
            FloorDecalView view = FindFirstObjectByType<FloorDecalView>();
            if (view == null)
                return;

            var list = new List<(Vector2Int chunkKey, ushort[] decalIds)>();
            if (chunks != null)
            {
                foreach (SyncedFloorDecalChunk chunk in chunks)
                {
                    list.Add((
                        new Vector2Int(chunk.chunkKeyX, chunk.chunkKeyY),
                        chunk.floorDecalIds));
                }
            }

            view.ReplaceClientChunks(list);
        }

        /// <summary>
        /// Server entry for blast detonation VFX. One-shot — do not BufferLast.
        /// </summary>
        [Server]
        public void ServerNotifyBlastDetonated(Vector3 worldPosition, float yield)
        {
            RpcBlastDetonated(worldPosition, yield);
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcBlastDetonated(Vector3 worldPosition, float yield)
        {
            BlastVfxPresenter.Play(worldPosition, yield);
            if (IsClient && _structuralAudio != null)
                _structuralAudio.OnBlast(worldPosition, yield);
        }

        [Server]
        public void ServerNotifyStructuralImpact(Vector3 point, Vector3 normal, StructuralMaterial material,
            StructuralImpactCue cue)
        {
            RpcStructuralImpact(point, normal, material, cue);
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcStructuralImpact(Vector3 point, Vector3 normal, StructuralMaterial material,
            StructuralImpactCue cue)
        {
            if (!IsClient)
                return;

            // A zero normal means no measured surface, so this impact is sound only.
            if (normal.sqrMagnitude > 0.0001f)
                SteelWallDamageVfx.PlayImpact(point, normal, (byte)material);

            if (_structuralAudio != null)
                _structuralAudio.OnImpact(point, material, cue);
        }

        [Serializable]
        private struct SyncedFloorDecalChunk
        {
            public int chunkKeyX;
            public int chunkKeyY;
            public ushort[] floorDecalIds;
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
            LoadAsync().Forget();
        }

        [Server]
        public async UniTask LoadAsync()
        {
            string editorTemplate = UnityEngine.Application.isEditor
                ? ScriptableSettings.GetOrFind<ApplicationSettings>().EditorStationTemplate?.Trim()
                : null;

            if (SubSystems.TryGet(out PersistenceSubSystem persistenceSubSystem))
            {
                // Editor playtests can pin a small template; a missing one falls back to most recent.
                if (!string.IsNullOrEmpty(editorTemplate))
                {
                    Log.Debug(this, $"Loading editor station template {editorTemplate}");
                    if (await persistenceSubSystem.LoadStationTemplateAsync(editorTemplate))
                    {
                        SyncFloorDecalsToClients();
                        return;
                    }

                    Log.Warning(this, $"Editor station template '{editorTemplate}' not found; loading most recent");
                }

                Log.Debug(this, "Loading most recent station template");
                if (!await persistenceSubSystem.LoadMostRecentStationTemplateAsync())
                {
                    NotifyEmptyMapReady();
                }

                SyncFloorDecalsToClients();
                return;
            }

	        SavedTileMap mapSave = LocalStorage.LoadMostRecentObject<SavedTileMap>(legacySavePath);
            await LoadLegacyMapAsync(mapSave);
            SyncFloorDecalsToClients();
        }

        [Server]
        public void Load(string mapName)
        {
            LoadAsync(mapName).Forget();
        }

        [Server]
        public async UniTask LoadAsync(string mapName)
        {
            Log.Debug(this, $"Loading station template {mapName}");

            if (SubSystems.TryGet(out PersistenceSubSystem persistenceSubSystem))
            {
                if (!await persistenceSubSystem.LoadStationTemplateAsync(mapName))
                {
                    NotifyEmptyMapReady();
                }

                SyncFloorDecalsToClients();
                return;
            }

            SavedTileMap mapSave = LocalStorage.LoadObject<SavedTileMap>(legacySavePath + "/" + mapName);
            await LoadLegacyMapAsync(mapSave);
            SyncFloorDecalsToClients();
        }

        /// <summary>
        /// Legacy flat-JSON load path (no PersistenceSubSystem). Still must defer area flood
        /// until every tile object has been placed.
        /// </summary>
        private void LoadLegacyMap(SavedTileMap mapSave)
        {
            IEnumerator routine = LoadLegacyMapRoutine(mapSave, timeSlice: false);
            while (routine.MoveNext())
            {
            }
        }

        private async UniTask LoadLegacyMapAsync(SavedTileMap mapSave)
        {
            IEnumerator routine = LoadLegacyMapRoutine(mapSave, timeSlice: true);
            while (routine.MoveNext())
                await UniTask.Yield();
        }

        private IEnumerator LoadLegacyMapRoutine(SavedTileMap mapSave, bool timeSlice)
        {
            bool hasElectricity = SubSystems.TryGet(out Electricity.ElectricitySubSystem electricity);
            bool hasAtmos = SubSystems.TryGet(out Atmospherics.AtmosSubSystem atmos);
            bool hasDisposal = SubSystems.TryGet(out Furniture.Disposal.DisposalSubSystem disposal);
            bool priorAtmosPaused = false;

            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                areaSubSystem.BeginDeferredAreaFlood();
            }

            if (hasElectricity)
                electricity.SuspendCircuitUpdates(true);
            if (hasAtmos)
            {
                priorAtmosPaused = atmos.SimulationPaused;
                atmos.SimulationPaused = true;
            }

            if (hasDisposal)
                disposal.BeginDeferredNetworkRebuild();

            _currentMap?.BeginBulkMutation();

            try
            {
                IEnumerator load = _currentMap.LoadRoutine(mapSave, invokeMapLoadedEvent: true, yieldFrames: timeSlice);
                while (load.MoveNext())
                    yield return load.Current;
                if ((mapSave.hasAuthoredAreas || mapSave.savedAreas is { Length: > 0 }) &&
                    SubSystems.TryGet(out AreaSubSystem restoredAreas))
                {
                    restoredAreas.BeginTemplateRestore(mapSave.savedAreas);
                    restoredAreas.RestoreFromSave(mapSave.savedAreas, mapSave.hasAuthoredAreas);
                }
            }
            finally
            {
                _currentMap?.EndBulkMutation();

                if (hasDisposal)
                    disposal.EndDeferredNetworkRebuild();
                if (hasElectricity)
                    electricity.SuspendCircuitUpdates(false);
                if (hasAtmos)
                    atmos.SimulationPaused = priorAtmosPaused;

                if (SubSystems.TryGet(out AreaSubSystem areaAfterLoad))
                {
                    areaAfterLoad.EndDeferredAreaFlood();
                }

                if (SubSystems.TryGet(out WorldReadiness.WorldReadinessSubSystem readiness))
                {
                    readiness.NotifyLegacyMapLoadComplete();
                }
            }
        }

        private static void NotifyEmptyMapReady()
        {
            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                // No BeginDeferred was paired; still signal flood-complete / empty ready.
                areaSubSystem.EndDeferredAreaFlood();
            }

            if (SubSystems.TryGet(out WorldReadiness.WorldReadinessSubSystem readiness))
            {
                readiness.NotifyLegacyMapLoadComplete();
            }
        }

        [Server]
        public void ResetSave()
        {
            _currentMap.Clear();
            _spawnPoints.Clear();
            Save("UnnamedMap", true);
            Log.Warning(this, "Tilemap resetted. Existing savefile has been wiped");
        }

        /// <summary>
        /// Clears authored spawn and gas markers. Called from <see cref="TileMap.Clear"/> so map wipe /
        /// template restore cannot leave stale markers when a template lacks their chunk.
        /// </summary>
        public void ClearMarkers()
        {
            _spawnPoints.Clear();
            _gasMarkers.Clear();
            if (IsServer) _steelWreckage.Clear();
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
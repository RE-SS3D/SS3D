using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.WorldReadiness;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using SS3D.Systems.Electricity;
using SS3D.Systems.Atmospherics;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// APC-seeded area flood-fill and per-tile area-id registry.
    /// </summary>
    public sealed partial class AreaSubSystem : NetworkSubSystem, ITileMutationObserver, IAreaLightingStateSource, IWorldReady
    {
        public event Action WhenReady;

        public event Action<AreaId, AreaLightingState> OnAreaLightingStateChanged;

        public event Action<AreaId, bool> OnAreaLightingSwitchChanged;

        public event Action OnAreaVisualsDirty;

        public bool IsReady { get; private set; }

        public AreaFloorVisualCache FloorVisualCache { get; } = new();

        private readonly AreaRegistry _registry = new();
        private readonly List<IAreaApcOrigin> _registeredApcs = new();
        private readonly HashSet<IAreaApcOrigin> _overlapFlaggedApcs = new();
        private readonly Dictionary<AreaId, AreaLightingState> _lightingStates = new();
        private readonly Dictionary<AreaId, bool> _equipmentPowered = new();
        private readonly Dictionary<AreaId, bool> _lightingSwitchOn = new();
        private readonly Dictionary<PlacedTileObject, AreaId> _deviceAreaIdCache = new();
        private int _deviceAreaCacheStructureVersion = -1;
        private Dictionary<Vector3, SavedAreaRecord> _pendingSavedByApcPosition;

        private TileMap _map;
        private ITileQueryService _query;
        private AreaFloodFillService _floodFill;
        private bool _electricityTickSubscribed;
        private bool _templateRestoreActive;
        private bool _mapWired;

        /// <summary>
        /// When true, <see cref="RegisterApc"/> queues APCs without flooding. Used while
        /// <see cref="TileMap.Load"/> is still placing tiles across chunks — flooding mid-load
        /// only claims tiles that already exist (often "front and right", never the unloaded side).
        /// </summary>
        private bool _deferAreaFlood;

        /// <summary>
        /// TileMap notifies <see cref="OnTileCleared"/> before the occupant is removed; defer
        /// reflood to the next update so occupancy matches the post-clear world (same pitfall as atmos).
        /// </summary>
        private bool _pendingLiveBoundaryRecompute;

        public override void OnStartServer()
        {
            base.OnStartServer();

            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            tileSubSystem.OnMapCreated += HandleTileMapCreated;

            if (tileSubSystem.CurrentMap != null)
                CompleteSetup(tileSubSystem);

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsServer)
                FloorVisualCache.OnDirty += HandleFloorVisualCacheDirty;
        }

        protected override void OnDestroyed()
        {
            if (SubSystems.TryGet(out TileSubSystem tileSubSystem))
                tileSubSystem.OnMapCreated -= HandleTileMapCreated;

            FloorVisualCache.OnDirty -= HandleFloorVisualCacheDirty;
            if (_painting != null) _painting.Changed -= HandleAuthoredAreasChanged;
            UnsubscribeElectricityTicks();
            SubSystems.Get<TileSubSystem>()?.UnregisterTileMutationObserver(this);
            base.OnDestroyed();
        }

        private void HandleFloorVisualCacheDirty() => OnAreaVisualsDirty?.Invoke();

        private void HandleTileMapCreated()
        {
            if (_mapWired)
                return;

            if (SubSystems.TryGet(out TileSubSystem tileSubSystem))
                CompleteSetup(tileSubSystem);
        }

        private void CompleteSetup(TileSubSystem tileSubSystem)
        {
            if (_mapWired || tileSubSystem.CurrentMap == null)
                return;

            _map = tileSubSystem.CurrentMap;
            _query = tileSubSystem.QueryService;
            _floodFill = new AreaFloodFillService(_map, _query);
            _painting = new AreaPaintingService(_map, _query, _registry, _registeredApcs);
            _painting.Changed += HandleAuthoredAreasChanged;

            tileSubSystem.RegisterTileMutationObserver(this);

            _mapWired = true;
            GameplayLightGuard.DisableOrphanSceneLights();
            SubscribeElectricityTicks();
        }

        public bool TryGetEffectiveApcForDevice(IElectricDevice device, out IApcChannelSource apc)
        {
            apc = null;
            if (device?.TileObject == null)
            {
                return false;
            }

            if (!TryGetAreaForDevice(device.TileObject, out AreaRecord record))
            {
                return false;
            }

            return TryGetAreaApc(record.Id, out apc);
        }

        public bool TryGetLightingState(AreaId areaId, out AreaLightingState state)
        {
            return _lightingStates.TryGetValue(areaId, out state);
        }

        /// <summary>
        /// Re-derives Normal/Emergency/Dark from APC channels and stats immediately (server).
        /// Use after APC channel toggles instead of waiting for the next electricity tick.
        /// </summary>
        [Server]
        public void RefreshAreaLightingStates()
        {
            UpdateAreaLightingStates();
        }

        public bool TryGetLightingStateForTile(TileCoord coord, out AreaLightingState state)
        {
            state = default;
            if (!TryGetAreaForTile(coord, out AreaRecord record))
            {
                return false;
            }

            return TryGetLightingState(record.Id, out state);
        }

        /// <summary>False only when the area is known to have no Equipment power. Works on clients.</summary>
        public bool IsAreaEquipmentPowered(AreaId areaId) =>
            !_equipmentPowered.TryGetValue(areaId, out bool powered) || powered;

        public bool TryGetAreaLightingSwitchOn(AreaId areaId, out bool on)
        {
            if (_lightingSwitchOn.TryGetValue(areaId, out on))
            {
                return true;
            }

            if (IsServer && _registry.TryGet(areaId, out AreaRecord record))
            {
                on = record.LightingSwitchOn;
                return true;
            }

            on = true;
            return false;
        }

        [Server]
        public bool ToggleAreaLightingSwitch(AreaId areaId)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
            {
                return false;
            }

            record.LightingSwitchOn = !record.LightingSwitchOn;
            ApplyLightingSwitchChange(areaId, record.LightingSwitchOn);
            UpdateAreaLightingStates(pushObservers: false);
            // One snapshot after switch + re-derived state so clients never see switch/state skew.
            PushLightingSnapshotToObservers();
            return true;
        }

        public bool TryGetAreaForTile(TileCoord coord, out AreaRecord record)
        {
            record = null;
            if (_map == null || !_map.TryGetAreaId(coord, out ushort areaId))
                return false;

            return _registry.TryGet(new AreaId(areaId), out record);
        }

        public bool TryGetArea(AreaId areaId, out AreaRecord record)
        {
            record = null;
            if (areaId.IsNone)
            {
                return false;
            }

            return _registry.TryGet(areaId, out record);
        }

        public bool TryGetAreaForDevice(PlacedTileObject tileObject, out AreaRecord record)
        {
            record = null;
            if (tileObject == null)
            {
                return false;
            }

            EnsureDeviceAreaCacheCurrent();
            if (_deviceAreaIdCache.TryGetValue(tileObject, out AreaId cachedId))
            {
                if (cachedId.IsNone)
                {
                    return false;
                }

                return _registry.TryGet(cachedId, out record);
            }

            bool found = ResolveAreaForDeviceUncached(tileObject, out record);
            _deviceAreaIdCache[tileObject] = found && record != null ? record.Id : default;
            return found;
        }

        /// <summary>
        /// Clears cached device→area ids (tile structure change or area reflood).
        /// </summary>
        public void InvalidateDeviceAreaCache()
        {
            _deviceAreaIdCache.Clear();
            _deviceAreaCacheStructureVersion = -1;
        }

        private void EnsureDeviceAreaCacheCurrent()
        {
            int version = _map != null ? _map.StructureVersion : -1;
            if (version == _deviceAreaCacheStructureVersion)
            {
                return;
            }

            _deviceAreaIdCache.Clear();
            _deviceAreaCacheStructureVersion = version;
        }

        private bool ResolveAreaForDeviceUncached(PlacedTileObject tileObject, out AreaRecord record)
        {
            record = null;

            // Wall-mounted devices can share the same wall tile on opposite sides of a wall.
            // In that case the wall tile's stored area (if any) is ambiguous; the correct area is
            // always the tile "in front" of the device's facing direction.
            if (tileObject.Layer == TileLayer.WallMountHigh || tileObject.Layer == TileLayer.WallMountLow)
            {
                TileCoord inFrontTile = AreaDeviceTileResolver.GetTileInFront(tileObject);
                return TryGetAreaForTile(inFrontTile, out record);
            }

            TileCoord origin = AreaDeviceTileResolver.GetOriginTile(tileObject);
            if (TryGetAreaForTile(origin, out record))
            {
                return true;
            }

            TileCoord inFront = AreaDeviceTileResolver.GetTileInFront(tileObject);
            return TryGetAreaForTile(inFront, out record);
        }

        /// <summary>
        /// Resolves area id for a device on host (live registry) or pure clients (floor-cache snapshot).
        /// On server/host with a live map, <see cref="TryGetAreaForDevice"/> is authoritative —
        /// including sticky None for unassigned tiles. Do not fall through to the floor cache on
        /// miss; that double-resolves forever for devices outside any area.
        /// </summary>
        public bool TryResolveAreaIdForDevice(PlacedTileObject tileObject, out AreaId areaId)
        {
            areaId = default;
            if (tileObject == null)
            {
                return false;
            }

            if (_map != null && IsServer)
            {
                if (TryGetAreaForDevice(tileObject, out AreaRecord record))
                {
                    areaId = record.Id;
                    return true;
                }

                return false;
            }

            return AreaDeviceTileResolver.TryResolveAreaIdFromFloorCache(FloorVisualCache, tileObject, out areaId);
        }

        /// <summary>
        /// Departmental tint from live registry (host) or floor-cache snapshot (clients).
        /// </summary>
        public bool TryGetDepartmentalLightTint(AreaId areaId, out Color tint)
        {
            if (_registry.TryGet(areaId, out AreaRecord record) && record.HasDepartmentalLightTint)
            {
                tint = record.DepartmentalLightTint;
                return true;
            }

            return FloorVisualCache.TryGetTint(areaId.Value, out tint);
        }

        /// <summary>
        /// Ambience track id (audio.md §2) from live registry (host) or floor-cache snapshot
        /// (clients). Empty/missing means the area has no authored ambience track.
        /// </summary>
        public bool TryGetAmbienceProfileId(AreaId areaId, out string profileId)
        {
            if (_registry.TryGet(areaId, out AreaRecord record) && !string.IsNullOrEmpty(record.EffectiveAmbienceProfileId))
            {
                profileId = record.EffectiveAmbienceProfileId;
                return true;
            }

            return FloorVisualCache.TryGetAmbienceProfileId(areaId.Value, out profileId);
        }

        public bool TryGetAreaApc(AreaId areaId, out IApcChannelSource apc)
        {
            apc = null;
            if (!_registry.TryGet(areaId, out AreaRecord record) || record.Apc == null)
                return false;

            if (record.Apc is IApcChannelSource channelSource)
            {
                apc = channelSource;
                return true;
            }

            return false;
        }

        public IReadOnlyList<AreaRecord> GetAllAreas() => _registry.GetAllAreas();

        /// <summary>
        /// Suppress flood-fill while the station template is still placing tiles. Call
        /// <see cref="EndDeferredAreaFlood"/> after load/restore completes.
        /// </summary>
        [Server]
        public void BeginDeferredAreaFlood()
        {
            _deferAreaFlood = true;
        }

        /// <summary>
        /// Recompute per-tile area ids from every registered APC now that the map is complete.
        /// Preserves existing <see cref="AreaRecord"/> metadata (names, tags, tints, access, switches).
        /// Always notifies world readiness (including no-op / early-out paths).
        /// </summary>
        [Server]
        public void EndDeferredAreaFlood()
        {
            if (_deferAreaFlood)
            {
                _deferAreaFlood = false;

                if (_floodFill != null && _map != null && _registeredApcs.Count > 0)
                {
                    // Template restore may have linked APCs to saved records without flooding.
                    // Mid-load RegisterApc may have queued APCs with no records yet.
                    bool anyLinked = false;
                    foreach (IAreaApcOrigin apc in _registeredApcs)
                    {
                        if (_registry.TryGetApcArea(apc, out _))
                        {
                            anyLinked = true;
                            break;
                        }
                    }

                    if (HasAuthoredAreas)
                    {
                        // Membership is loaded; refreshed below, without geometric flood fill.
                    }
                    else if (anyLinked)
                    {
                        RefloodAllAreaTilesPreservingMetadata();
                    }
                    else
                    {
                        RebuildAllAreasFromApcs();
                    }
                }
            }

            if (HasAuthoredAreas) _painting.NotifyChanged();
            MarkAreasReady();
        }

        private void MarkAreasReady()
        {
            if (!_mapWired)
            {
                if (SubSystems.TryGet(out WorldReadiness.WorldReadinessSubSystem readinessEarly))
                {
                    readinessEarly.NotifyAreasFlooded();
                }

                return;
            }

            if (!IsReady)
            {
                IsReady = true;
                WhenReady?.Invoke();
            }

            if (SubSystems.TryGet(out WorldReadiness.WorldReadinessSubSystem readiness))
            {
                readiness.NotifyAreasFlooded();
            }
        }

        [Server]
        public void RegisterApc(IAreaApcOrigin apc)
        {
            if (apc == null || _registeredApcs.Contains(apc))
                return;

            _registeredApcs.Add(apc);

            if (_templateRestoreActive && TryLinkApcDuringTemplateRestore(apc))
            {
                UpdateOverlapWarnings();
                TryCompleteTemplateRestore();
                InvalidateAreaMembershipIndexes();
                NotifyAreaVisualsChanged();
                return;
            }

            // Map load still placing tiles — queue only; EndDeferredAreaFlood will flood once.
            if (_deferAreaFlood)
            {
                return;
            }

            if (HasAuthoredAreas)
            {
                _painting.NotifyChanged();
                return;
            }

            if (_registry.TryGetApcArea(apc, out AreaId existingArea))
            {
                _floodFill.ClearAreaTiles(existingArea);
                _registry.Unregister(existingArea);
            }

            AreaId areaId = RegisterStartingArea(apc);

            var claimedTiles = BuildClaimedTilesExcluding(areaId);
            _floodFill.FloodFromApc(apc, areaId, claimedTiles);
            _floodFill.AssignDoorTileAreas();
            UpdateOverlapWarnings();
            InvalidateAreaMembershipIndexes();
            NotifyAreaVisualsChanged();
        }

        [Server]
        public void UnregisterApc(IAreaApcOrigin apc)
        {
            if (apc == null || !_registeredApcs.Remove(apc))
                return;

            if (HasAuthoredAreas)
            {
                apc.SetMultipleApcsInArea(false);
                _painting.NotifyChanged();
                return;
            }

            if (!_registry.TryGetApcArea(apc, out AreaId areaId))
                return;

            _floodFill.ClearAreaTiles(areaId);
            _registry.Unregister(areaId);
            _lightingStates.Remove(areaId);
            _lightingSwitchOn.Remove(areaId);
            _overlapFlaggedApcs.Remove(apc);
            apc.SetMultipleApcsInArea(false);
            UpdateOverlapWarnings();
            InvalidateAreaMembershipIndexes();
            NotifyAreaVisualsChanged();
        }

        [Server]
        public void RebuildAllAreasFromApcs()
        {
            if (HasAuthoredAreas) { _painting.NotifyChanged(); return; }
            _map.ClearAllAreaIds();
            _registry.Clear();
            _overlapFlaggedApcs.Clear();
            _lightingStates.Clear();
            _equipmentPowered.Clear();
            _lightingSwitchOn.Clear();

            List<IAreaApcOrigin> apcs = _registeredApcs
                .OrderBy(apc => apc.OriginTile.Grid.x)
                .ThenBy(apc => apc.OriginTile.Grid.y)
                .ToList();

            var claimedTiles = new HashSet<TileCoord>();

            foreach (IAreaApcOrigin apc in apcs)
            {
                AreaId areaId = RegisterStartingArea(apc);
                _floodFill.FloodFromApc(apc, areaId, claimedTiles);
            }

            _floodFill.AssignDoorTileAreas();
            UpdateOverlapWarnings();
            InvalidateAreaMembershipIndexes();
            NotifyAreaVisualsChanged();
        }

        [Server]
        public void RebuildAreaFromApc(IAreaApcOrigin apc)
        {
            if (HasAuthoredAreas) { _painting.NotifyChanged(); return; }
            if (apc == null)
                return;

            if (_registry.TryGetApcArea(apc, out AreaId existingArea))
            {
                _floodFill.ClearAreaTiles(existingArea);
                _registry.Unregister(existingArea);
            }

            AreaId areaId = RegisterStartingArea(apc);

            var claimedTiles = BuildClaimedTilesExcluding(areaId);
            _floodFill.FloodFromApc(apc, areaId, claimedTiles);
            _floodFill.AssignDoorTileAreas();
            UpdateOverlapWarnings();
            InvalidateAreaMembershipIndexes();
            NotifyAreaVisualsChanged();
        }

        /// <summary>
        /// Clears tile area ids and floods again from registered APCs, keeping existing AreaRecords.
        /// </summary>
        [Server]
        public void RefloodAllAreaTilesPreservingMetadata()
        {
            if (HasAuthoredAreas) { _painting.NotifyChanged(); return; }
            if (_floodFill == null || _map == null)
            {
                return;
            }

            _map.ClearAllAreaIds();
            _overlapFlaggedApcs.Clear();

            List<IAreaApcOrigin> apcs = _registeredApcs
                .OrderBy(apc => apc.OriginTile.Grid.x)
                .ThenBy(apc => apc.OriginTile.Grid.y)
                .ToList();

            var claimedTiles = new HashSet<TileCoord>();

            foreach (IAreaApcOrigin apc in apcs)
            {
                if (!_registry.TryGetApcArea(apc, out AreaId areaId))
                {
                    // APC registered during deferred load without a saved record — allocate one.
                    areaId = RegisterStartingArea(apc);
                }

                _floodFill.FloodFromApc(apc, areaId, claimedTiles);
            }

            _floodFill.AssignDoorTileAreas();
            UpdateOverlapWarnings();
            InvalidateAreaMembershipIndexes();
        }

        /// <summary>A flooded area gets a neutral name and no department: an APC's label says nothing about the room (area.md §3).</summary>
        private AreaId RegisterStartingArea(IAreaApcOrigin apc)
        {
            AreaId areaId = _registry.AllocateId();
            var record = new AreaRecord
            {
                Id = areaId,
                DisplayName = _painting.NextRoomName(),
                ParentTag = string.Empty,
                Apc = apc,
            };
            _registry.Register(record);
            _lightingSwitchOn[areaId] = record.LightingSwitchOn;
            return areaId;
        }

        [Server]
        public void RenameArea(AreaId areaId, string displayName)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.DisplayName = displayName;
            SyncAreaNames();
        }

        [Server]
        public void SetParentTag(AreaId areaId, string tag)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.ParentTag = AreaDepartments.Normalize(tag);
            SyncAreaAmbience();
        }

        [Server]
        public void SetDepartmentalLightTint(AreaId areaId, Color tint)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.HasDepartmentalLightTint = true;
            record.DepartmentalLightTint = tint;
            NotifyAreaVisualsChanged();
        }

        /// <summary>
        /// Authors the area's ambience override (audio.md §2). Empty goes back to the department.
        /// No Map Editor UI yet; the <c>areaambience</c> console command reaches it.
        /// </summary>
        [Server]
        public void SetAreaAmbienceProfileId(AreaId areaId, string profileId)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.AmbienceProfileId = profileId ?? string.Empty;
            SyncAreaAmbience();
        }

        [Server]
        public void SetDefaultRequiredAccess(AreaId areaId, IdAccess.AccessMask requiredAccess)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
            {
                return;
            }

            record.DefaultRequiredAccess = requiredAccess;
        }

        public bool TryGetAreaForWorldPosition(Vector3 worldPosition, out AreaRecord record)
        {
            record = null;
            if (_query == null)
            {
                return false;
            }

            TileCoord coord = _query.WorldToTile(worldPosition);
            return TryGetAreaForTile(coord, out record);
        }

        /// <summary>
        /// Resolves an area id for a world position on host (live registry via
        /// <see cref="TryGetAreaForWorldPosition"/>) or a pure client (<see cref="FloorVisualCache"/>
        /// snapshot, same world-grid math <see cref="ITileQueryService.WorldToTile"/> uses since it
        /// has no registry). Used for client-local presentation that tracks a moving world position
        /// (e.g. ambience crossfade, audio.md §2) rather than a fixed device tile.
        /// </summary>
        public bool TryResolveAreaIdForWorldPosition(Vector3 worldPosition, out AreaId areaId)
        {
            if (TryGetAreaForWorldPosition(worldPosition, out AreaRecord record))
            {
                areaId = record.Id;
                return true;
            }

            Vector2Int worldGrid = new(Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));
            if (FloorVisualCache.TryGetAreaIdForWorldGrid(worldGrid, out ushort raw) && raw != AreaId.None)
            {
                areaId = new AreaId(raw);
                return true;
            }

            areaId = default;
            return false;
        }

        public void ClearDepartmentalLightTint(AreaId areaId)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.HasDepartmentalLightTint = false;
            record.DepartmentalLightTint = default;
            if (IsServer)
                NotifyAreaVisualsChanged();
        }

        public SavedAreaRecord[] BuildSavedAreaRecords() => _painting?.CaptureRecords() ?? Array.Empty<SavedAreaRecord>();

        public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
        {
            if (ShouldRecomputeForOccupant(occupant))
                QueueLiveBoundaryRecompute();
        }

        public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
        {
            if (layer == TileLayer.Turf && ShouldRecomputeForOccupant(occupant))
                QueueLiveBoundaryRecompute();
        }

        public void OnChunkCreated(TileChunkRef chunk) { }

        public void OnTileStateChanged(TileCoord coord)
        {
            // Integrity Cracked changes airtightness for atmos, not Area boundaries (wall still present).
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (!_pendingLiveBoundaryRecompute)
                return;

            _pendingLiveBoundaryRecompute = false;
            RequestLiveBoundaryRecompute();
        }

        private void QueueLiveBoundaryRecompute()
        {
            _pendingLiveBoundaryRecompute = true;
        }

        /// <summary>
        /// Phase 1: full reflood preserving AreaRecord metadata. True local-region flood is a follow-up.
        /// </summary>
        [Server]
        public void RequestLiveBoundaryRecompute()
        {
            if (_deferAreaFlood || _floodFill == null || _map == null)
                return;

            if (_registeredApcs.Count == 0)
                return;

            RefloodAllAreaTilesPreservingMetadata();
            NotifyAreaVisualsChanged();
        }

        private static bool ShouldRecomputeForOccupant(ITileOccupant occupant)
        {
            if (occupant is not PlacedTileObject placed)
                return false;

            return placed.GenericType == TileObjectGenericType.Wall
                || placed.GenericType == TileObjectGenericType.Door;
        }

        [Server]
        public void BeginTemplateRestore(IReadOnlyList<SavedAreaRecord> savedAreas)
        {
            _templateRestoreActive = true;
            _pendingSavedByApcPosition = new Dictionary<Vector3, SavedAreaRecord>();

            if (savedAreas == null)
            {
                return;
            }

            foreach (SavedAreaRecord saved in savedAreas)
            {
                if (!saved.noApc) _pendingSavedByApcPosition[saved.apcWorldPosition] = saved;
            }
        }

        [Server]
        public void RestoreFromSave(IReadOnlyList<SavedAreaRecord> savedAreas, bool authored = false)
        {
            if ((savedAreas == null || savedAreas.Count == 0) && !authored)
            {
                return;
            }

            RestoreRegistryFromSave(savedAreas ?? Array.Empty<SavedAreaRecord>(), authored);
            LinkRegisteredApcsDuringTemplateRestore();
            UpdateAreaLightingStates();
            TryCompleteTemplateRestore();
            NotifyAreaVisualsChanged();
        }

        [Server]
        public void EndTemplateRestore()
        {
            _templateRestoreActive = false;
            _pendingSavedByApcPosition = null;
        }

        private bool TryLinkApcDuringTemplateRestore(IAreaApcOrigin apc)
        {
            if (_pendingSavedByApcPosition == null || _query == null)
            {
                return false;
            }

            Vector3 world = _query.TileToWorld(apc.OriginTile);
            if (!_pendingSavedByApcPosition.TryGetValue(world, out SavedAreaRecord saved))
            {
                return false;
            }

            var areaId = new AreaId(saved.id);
            if (!_registry.TryGet(areaId, out AreaRecord record))
            {
                return false;
            }

            record.Apc = apc;
            _registry.Register(record);
            _lightingSwitchOn[areaId] = record.LightingSwitchOn;
            return true;
        }

        private void LinkRegisteredApcsDuringTemplateRestore()
        {
            foreach (IAreaApcOrigin apc in _registeredApcs)
            {
                TryLinkApcDuringTemplateRestore(apc);
            }

            UpdateOverlapWarnings();
        }

        private void TryCompleteTemplateRestore()
        {
            if (!_templateRestoreActive)
            {
                return;
            }

            if (_pendingSavedByApcPosition == null || _pendingSavedByApcPosition.Count == 0)
            {
                EndTemplateRestore();
                return;
            }

            foreach (SavedAreaRecord saved in _pendingSavedByApcPosition.Values)
            {
                var areaId = new AreaId(saved.id);
                if (!_registry.TryGet(areaId, out AreaRecord record) || record.Apc == null)
                {
                    return;
                }
            }

            EndTemplateRestore();
        }

        private void RestoreRegistryFromSave(IReadOnlyList<SavedAreaRecord> savedAreas, bool authored)
        {
            _painting.RestoreRecords(savedAreas, authored);
            _lightingSwitchOn.Clear();
            foreach (AreaRecord room in _registry.GetAllAreas()) _lightingSwitchOn[room.Id] = room.LightingSwitchOn;
        }

        private HashSet<TileCoord> BuildClaimedTilesExcluding(AreaId excludeAreaId)
        {
            var claimed = new HashSet<TileCoord>();

            foreach (TileChunk chunk in _map.GetAllChunks())
            {
                for (int x = 0; x < TileChunk.ChunkSize; x++)
                {
                    for (int y = 0; y < TileChunk.ChunkSize; y++)
                    {
                        ushort areaId = chunk.GetAreaId(x, y);
                        if (areaId == AreaId.None || areaId == excludeAreaId.Value)
                            continue;

                        claimed.Add(_query.WorldToTile(chunk.GetWorldPosition(x, y), _map.MapId));
                    }
                }
            }

            return claimed;
        }

        private void UpdateOverlapWarnings()
        {
            var apcsByArea = new Dictionary<ushort, List<IAreaApcOrigin>>();

            foreach (IAreaApcOrigin apc in _registeredApcs)
            {
                if (!_registry.TryGetApcArea(apc, out AreaId areaId))
                    continue;

                if (!_map.TryGetAreaId(AreaDeviceTileResolver.GetApcRoomTile(apc, _query), out ushort originAreaId) || originAreaId == AreaId.None)
                    continue;

                if (!apcsByArea.TryGetValue(originAreaId, out List<IAreaApcOrigin> list))
                {
                    list = new List<IAreaApcOrigin>();
                    apcsByArea[originAreaId] = list;
                }

                list.Add(apc);
            }

            _overlapFlaggedApcs.Clear();

            foreach (List<IAreaApcOrigin> group in apcsByArea.Values)
            {
                if (group.Count <= 1)
                    continue;

                foreach (IAreaApcOrigin apc in group)
                    _overlapFlaggedApcs.Add(apc);
            }

            foreach (IAreaApcOrigin apc in _registeredApcs)
                apc.SetMultipleApcsInArea(_overlapFlaggedApcs.Contains(apc));

            if (IsServer)
            {
                UpdateAreaLightingStates();
            }
        }

        private void SubscribeElectricityTicks()
        {
            if (_electricityTickSubscribed || !SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            electricitySubSystem.OnTick += HandleElectricityTick;
            _electricityTickSubscribed = true;
        }

        private void UnsubscribeElectricityTicks()
        {
            if (!_electricityTickSubscribed || !SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            electricitySubSystem.OnTick -= HandleElectricityTick;
            _electricityTickSubscribed = false;
        }

        private void HandleElectricityTick()
        {
            if (!IsServer)
            {
                return;
            }

            UpdateAreaLightingStates();
        }

        private void UpdateAreaLightingStates(bool pushObservers = true)
        {
            if (!SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            bool anyChanged = false;
            foreach (AreaRecord record in _registry.GetAllAreas())
            {
                if (record.Apc is not IApcChannelSource areaApc || record.Apc is not IElectricDevice)
                {
                    if (HasAuthoredAreas && ApplyLightingStateChange(record.Id, AreaLightingState.Dark)) anyChanged = true;
                    if (HasAuthoredAreas && ApplyEquipmentPowerChange(record.Id, false)) anyChanged = true;
                    continue;
                }

                IPowerStorage apcStorage = record.Apc as IPowerStorage;
                if (!electricitySubSystem.TryGetApcCircuitStats(areaApc, apcStorage, out CircuitStats stats))
                {
                    continue;
                }

                AreaLightingState newState = AreaLightingStateDeriver.Derive(stats, areaApc.Channels, record.LightingSwitchOn);
                if (ApplyLightingStateChange(record.Id, newState))
                {
                    anyChanged = true;
                }

                if (ApplyEquipmentPowerChange(record.Id, AreaLightingStateDeriver.DeriveEquipmentPowered(stats, areaApc.Channels)))
                {
                    anyChanged = true;
                }
            }

            if (pushObservers && anyChanged && IsServer)
            {
                PushLightingSnapshotToObservers();
            }
        }

        private void ApplyLightingSwitchChange(AreaId areaId, bool on)
        {
            _lightingSwitchOn[areaId] = on;
            OnAreaLightingSwitchChanged?.Invoke(areaId, on);
        }

        /// <returns>True when the stored state changed.</returns>
        private bool ApplyLightingStateChange(AreaId areaId, AreaLightingState newState)
        {
            if (_lightingStates.TryGetValue(areaId, out AreaLightingState previousState) && previousState == newState)
            {
                return false;
            }

            _lightingStates[areaId] = newState;
            OnAreaLightingStateChanged?.Invoke(areaId, newState);
            return true;
        }

        /// <returns>True when the stored state changed.</returns>
        private bool ApplyEquipmentPowerChange(AreaId areaId, bool powered)
        {
            if (_equipmentPowered.TryGetValue(areaId, out bool previous) && previous == powered)
            {
                return false;
            }

            _equipmentPowered[areaId] = powered;
            return true;
        }

        [Server]
        private void PushLightingSnapshotToObservers()
        {
            var areaIds = new HashSet<AreaId>(_lightingStates.Keys);
            foreach (AreaId switchAreaId in _lightingSwitchOn.Keys)
            {
                areaIds.Add(switchAreaId);
            }

            var entries = new SyncedAreaLighting[areaIds.Count];
            int index = 0;
            foreach (AreaId areaId in areaIds)
            {
                if (!_lightingStates.TryGetValue(areaId, out AreaLightingState state))
                {
                    state = AreaLightingState.Dark;
                }

                if (!_lightingSwitchOn.TryGetValue(areaId, out bool switchOn))
                {
                    switchOn = true;
                }

                entries[index++] = new SyncedAreaLighting
                {
                    areaId = areaId.Value,
                    state = state,
                    lightingSwitchOn = switchOn,
                    equipmentPowered = IsAreaEquipmentPowered(areaId),
                };
            }

            RpcSyncAreaLighting(entries);
        }

        /// <summary>
        /// Full lighting + wall-switch snapshot. BufferLast so late joiners get current fixture state
        /// (per-area RPCs would only retain the last area).
        /// </summary>
        [ObserversRpc(BufferLast = true)]
        private void RpcSyncAreaLighting(SyncedAreaLighting[] entries)
        {
            if (IsServer)
            {
                return;
            }

            var nextStates = new Dictionary<AreaId, AreaLightingState>();
            var nextSwitches = new Dictionary<AreaId, bool>();
            _equipmentPowered.Clear();
            if (entries != null)
            {
                foreach (SyncedAreaLighting entry in entries)
                {
                    var areaId = new AreaId(entry.areaId);
                    nextStates[areaId] = entry.state;
                    nextSwitches[areaId] = entry.lightingSwitchOn;
                    _equipmentPowered[areaId] = entry.equipmentPowered;
                }
            }

            foreach (KeyValuePair<AreaId, AreaLightingState> pair in nextStates)
            {
                if (_lightingStates.TryGetValue(pair.Key, out AreaLightingState previous) && previous == pair.Value)
                {
                    continue;
                }

                _lightingStates[pair.Key] = pair.Value;
                OnAreaLightingStateChanged?.Invoke(pair.Key, pair.Value);
            }

            foreach (AreaId removed in _lightingStates.Keys.Where(id => !nextStates.ContainsKey(id)).ToList())
            {
                _lightingStates.Remove(removed);
            }

            foreach (KeyValuePair<AreaId, bool> pair in nextSwitches)
            {
                if (_lightingSwitchOn.TryGetValue(pair.Key, out bool previous) && previous == pair.Value)
                {
                    continue;
                }

                _lightingSwitchOn[pair.Key] = pair.Value;
                OnAreaLightingSwitchChanged?.Invoke(pair.Key, pair.Value);
            }

            foreach (AreaId removed in _lightingSwitchOn.Keys.Where(id => !nextSwitches.ContainsKey(id)).ToList())
            {
                _lightingSwitchOn.Remove(removed);
            }
        }

        private void InvalidateAreaMembershipIndexes()
        {
            InvalidateDeviceAreaCache();
            if (SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                electricitySubSystem.InvalidateAreaConsumerIndex();
            }

            if (SubSystems.TryGet(out AtmosSubSystem atmosSubSystem))
            {
                atmosSubSystem.InvalidateAreaPortIndex();
            }
        }

        [Server]
        private void NotifyAreaVisualsChanged()
        {
            if (_map == null)
                return;

            SyncAreaNames();
            var tintList = new List<(ushort areaId, bool hasTint, Color tint)>();
            foreach (AreaRecord record in _registry.GetAllAreas())
            {
                tintList.Add((
                    record.Id.Value,
                    record.HasDepartmentalLightTint,
                    record.DepartmentalLightTint));
            }

            var chunkList = new List<(Vector2Int chunkKey, ushort[] areaIds)>();
            var rpcChunks = new List<SyncedChunkAreaIds>();
            foreach (TileChunk chunk in _map.GetAllChunks())
            {
                ushort[] areaIds = chunk.CopyAreaIds();
                if (areaIds == null)
                    continue;

                Vector2Int chunkKey = _map.GetKey(chunk.GetWorldPosition(0, 0));
                chunkList.Add((chunkKey, areaIds));
                rpcChunks.Add(new SyncedChunkAreaIds
                {
                    chunkKeyX = chunkKey.x,
                    chunkKeyY = chunkKey.y,
                    areaIds = areaIds,
                });
            }

            FloorVisualCache.ReplaceAll(tintList, chunkList);
            OnAreaVisualsDirty?.Invoke();

            var rpcTints = new SyncedAreaTint[tintList.Count];
            for (int i = 0; i < tintList.Count; i++)
            {
                (ushort areaId, bool hasTint, Color tint) = tintList[i];
                rpcTints[i] = new SyncedAreaTint
                {
                    areaId = areaId,
                    hasTint = hasTint,
                    tint = tint,
                };
            }

            RpcSyncAreaFloorVisuals(rpcTints, rpcChunks.ToArray());

            SyncAreaAmbience();
        }

        /// <summary>
        /// Separate broadcast channel (audio.md §2) from tint and chunk data, so tagging a room
        /// doesn't resend the floor snapshot. Clients get the resolved id and no department data.
        /// </summary>
        [Server]
        private void SyncAreaAmbience()
        {
            if (_map == null)
                return;

            var profileList = new List<(ushort areaId, string profileId)>();
            var rpcAmbience = new List<SyncedAreaAmbience>();
            foreach (AreaRecord record in _registry.GetAllAreas())
            {
                string profileId = record.EffectiveAmbienceProfileId;
                if (string.IsNullOrEmpty(profileId))
                    continue;

                profileList.Add((record.Id.Value, profileId));
                rpcAmbience.Add(new SyncedAreaAmbience { areaId = record.Id.Value, profileId = profileId });
            }

            FloorVisualCache.ReplaceAmbienceProfileIds(profileList);
            RpcSyncAreaAmbience(rpcAmbience.ToArray());
        }

        [Server]
        private void SyncAreaNames()
        {
            HashSet<ushort> spatial = new();
            if (_map != null)
                foreach (TileChunk chunk in _map.GetAllChunks())
                    for (int y = 0; y < TileConstants.ChunkSize; y++)
                        for (int x = 0; x < TileConstants.ChunkSize; x++)
                        {
                            ushort id = chunk.GetAreaId(x, y);
                            if (id == AreaId.None || spatial.Contains(id)) continue;
                            TileCoord coord = _query.WorldToTile(chunk.GetWorldPosition(x, y), _map.MapId);
                            if (AreaBoundaryEvaluator.IsWalkable(_query, coord)) spatial.Add(id);
                        }
            var records = _registry.GetAllAreas().Where(room => spatial.Contains(room.Id.Value)).ToArray();
            ushort[] ids = new ushort[records.Length];
            string[] names = new string[records.Length];
            int i = 0;
            foreach (AreaRecord record in records)
            {
                ids[i] = record.Id.Value;
                names[i++] = record.DisplayName;
            }
            FloorVisualCache.ReplaceNames(ids, names);
            RpcSyncAreaNames(ids, names);
        }

        [ObserversRpc(BufferLast = true)]
        private void RpcSyncAreaNames(ushort[] ids, string[] names)
        {
            if (!IsServer) FloorVisualCache.ReplaceNames(ids, names);
        }

        [ObserversRpc(BufferLast = true)]
        private void RpcSyncAreaAmbience(SyncedAreaAmbience[] profiles)
        {
            if (IsServer)
                return;

            var profileList = new List<(ushort areaId, string profileId)>();
            if (profiles != null)
            {
                foreach (SyncedAreaAmbience profile in profiles)
                    profileList.Add((profile.areaId, profile.profileId));
            }

            FloorVisualCache.ReplaceAmbienceProfileIds(profileList);
        }

        [ObserversRpc(BufferLast = true)]
        private void RpcSyncAreaFloorVisuals(SyncedAreaTint[] tints, SyncedChunkAreaIds[] chunks)
        {
            if (IsServer)
                return;

            var tintList = new List<(ushort areaId, bool hasTint, Color tint)>();
            if (tints != null)
            {
                foreach (SyncedAreaTint tint in tints)
                    tintList.Add((tint.areaId, tint.hasTint, tint.tint));
            }

            var chunkList = new List<(Vector2Int chunkKey, ushort[] areaIds)>();
            if (chunks != null)
            {
                foreach (SyncedChunkAreaIds chunk in chunks)
                {
                    chunkList.Add((
                        new Vector2Int(chunk.chunkKeyX, chunk.chunkKeyY),
                        chunk.areaIds));
                }
            }

            FloorVisualCache.ReplaceAll(tintList, chunkList);
            OnAreaVisualsDirty?.Invoke();
        }

        [Serializable]
        private struct SyncedAreaTint
        {
            public ushort areaId;
            public bool hasTint;
            public Color tint;
        }

        [Serializable]
        private struct SyncedChunkAreaIds
        {
            public int chunkKeyX;
            public int chunkKeyY;
            public ushort[] areaIds;
        }

        [Serializable]
        private struct SyncedAreaLighting
        {
            public ushort areaId;
            public AreaLightingState state;
            public bool lightingSwitchOn;
            public bool equipmentPowered;
        }

        [Serializable]
        private struct SyncedAreaAmbience
        {
            public ushort areaId;
            public string profileId;
        }
    }
}

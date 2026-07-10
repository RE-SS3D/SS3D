using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using System.Electricity;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// APC-seeded area flood-fill and per-tile area-id registry.
    /// </summary>
    public sealed class AreaSubSystem : NetworkSubSystem, ITileMutationObserver, IAreaLightingStateSource
    {
        public event Action OnSystemSetUp;

        public event Action<AreaId, AreaLightingState> OnAreaLightingStateChanged;

        public bool IsSetUp { get; private set; }

        private readonly AreaRegistry _registry = new();
        private readonly List<IAreaApcOrigin> _registeredApcs = new();
        private readonly HashSet<IAreaApcOrigin> _overlapFlaggedApcs = new();
        private readonly Dictionary<AreaId, AreaLightingState> _lightingStates = new();

        private TileMap _map;
        private ITileQueryService _query;
        private AreaFloodFillService _floodFill;
        private bool _electricityTickSubscribed;

        public override void OnStartServer()
        {
            base.OnStartServer();

            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            tileSubSystem.OnMapCreated += HandleTileMapCreated;

            if (tileSubSystem.CurrentMap != null)
                CompleteSetup(tileSubSystem);
        }

        protected override void OnDestroyed()
        {
            if (SubSystems.TryGet(out TileSubSystem tileSubSystem))
                tileSubSystem.OnMapCreated -= HandleTileMapCreated;

            if (_map != null)
                _map.OnMapLoaded -= HandleMapLoaded;

            UnsubscribeElectricityTicks();
            SubSystems.Get<TileSubSystem>()?.UnregisterTileMutationObserver(this);
            base.OnDestroyed();
        }

        private void HandleTileMapCreated()
        {
            if (IsSetUp)
                return;

            if (SubSystems.TryGet(out TileSubSystem tileSubSystem))
                CompleteSetup(tileSubSystem);
        }

        private void CompleteSetup(TileSubSystem tileSubSystem)
        {
            if (IsSetUp || tileSubSystem.CurrentMap == null)
                return;

            _map = tileSubSystem.CurrentMap;
            _query = tileSubSystem.QueryService;
            _floodFill = new AreaFloodFillService(_map, _query);

            tileSubSystem.RegisterTileMutationObserver(this);
            _map.OnMapLoaded += HandleMapLoaded;

            if (_map.LoadedAreaRecords.Count > 0 && _registeredApcs.Count == 0)
                RestoreRegistryFromSave(_map.LoadedAreaRecords);

            IsSetUp = true;
            OnSystemSetUp?.Invoke();
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

        public bool TryGetLightingStateForTile(TileCoord coord, out AreaLightingState state)
        {
            state = default;
            if (!TryGetAreaForTile(coord, out AreaRecord record))
            {
                return false;
            }

            return TryGetLightingState(record.Id, out state);
        }

        public bool TryGetAreaForTile(TileCoord coord, out AreaRecord record)
        {
            record = null;
            if (_map == null || !_map.TryGetAreaId(coord, out ushort areaId))
                return false;

            return _registry.TryGet(new AreaId(areaId), out record);
        }

        public bool TryGetAreaForDevice(PlacedTileObject tileObject, out AreaRecord record)
        {
            record = null;
            if (tileObject == null)
            {
                return false;
            }

            TileCoord origin = AreaDeviceTileResolver.GetOriginTile(tileObject);
            if (TryGetAreaForTile(origin, out record))
            {
                return true;
            }

            TileCoord inFront = AreaDeviceTileResolver.GetTileInFront(tileObject);
            return TryGetAreaForTile(inFront, out record);
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

        [Server]
        public void RegisterApc(IAreaApcOrigin apc)
        {
            if (apc == null || _registeredApcs.Contains(apc))
                return;

            _registeredApcs.Add(apc);

            if (_registry.TryGetApcArea(apc, out AreaId existingArea))
            {
                _floodFill.ClearAreaTiles(existingArea);
                _registry.Unregister(existingArea);
            }

            AreaId areaId = _registry.AllocateId();
            var record = new AreaRecord
            {
                Id = areaId,
                DisplayName = string.IsNullOrWhiteSpace(apc.DisplayName) ? "Unnamed Area" : apc.DisplayName,
                ParentTag = string.Empty,
                Apc = apc,
            };
            _registry.Register(record);

            var claimedTiles = BuildClaimedTilesExcluding(areaId);
            _floodFill.FloodFromApc(apc, areaId, claimedTiles);
            _floodFill.AssignDoorTileAreas();
            UpdateOverlapWarnings();
        }

        [Server]
        public void UnregisterApc(IAreaApcOrigin apc)
        {
            if (apc == null || !_registeredApcs.Remove(apc))
                return;

            if (!_registry.TryGetApcArea(apc, out AreaId areaId))
                return;

            _floodFill.ClearAreaTiles(areaId);
            _registry.Unregister(areaId);
            _lightingStates.Remove(areaId);
            _overlapFlaggedApcs.Remove(apc);
            apc.SetMultipleApcsInArea(false);
            UpdateOverlapWarnings();
        }

        [Server]
        public void RebuildAllAreasFromApcs()
        {
            _map.ClearAllAreaIds();
            _registry.Clear();
            _overlapFlaggedApcs.Clear();
            _lightingStates.Clear();

            List<IAreaApcOrigin> apcs = _registeredApcs
                .OrderBy(apc => apc.OriginTile.Grid.x)
                .ThenBy(apc => apc.OriginTile.Grid.y)
                .ToList();

            var claimedTiles = new HashSet<TileCoord>();

            foreach (IAreaApcOrigin apc in apcs)
            {
                AreaId areaId = _registry.AllocateId();
                var record = new AreaRecord
                {
                    Id = areaId,
                    DisplayName = string.IsNullOrWhiteSpace(apc.DisplayName) ? "Unnamed Area" : apc.DisplayName,
                    ParentTag = string.Empty,
                    Apc = apc,
                };
                _registry.Register(record);
                _floodFill.FloodFromApc(apc, areaId, claimedTiles);
            }

            _floodFill.AssignDoorTileAreas();
            UpdateOverlapWarnings();
        }

        [Server]
        public void RebuildAreaFromApc(IAreaApcOrigin apc)
        {
            if (apc == null)
                return;

            if (_registry.TryGetApcArea(apc, out AreaId existingArea))
            {
                _floodFill.ClearAreaTiles(existingArea);
                _registry.Unregister(existingArea);
            }

            AreaId areaId = _registry.AllocateId();
            var record = new AreaRecord
            {
                Id = areaId,
                DisplayName = string.IsNullOrWhiteSpace(apc.DisplayName) ? "Unnamed Area" : apc.DisplayName,
                ParentTag = string.Empty,
                Apc = apc,
            };
            _registry.Register(record);

            var claimedTiles = BuildClaimedTilesExcluding(areaId);
            _floodFill.FloodFromApc(apc, areaId, claimedTiles);
            _floodFill.AssignDoorTileAreas();
            UpdateOverlapWarnings();
        }

        [Server]
        public void RenameArea(AreaId areaId, string displayName)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.DisplayName = displayName;
        }

        [Server]
        public void SetParentTag(AreaId areaId, string tag)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.ParentTag = tag ?? string.Empty;
        }

        [Server]
        public void SetDepartmentalLightTint(AreaId areaId, Color tint)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.HasDepartmentalLightTint = true;
            record.DepartmentalLightTint = tint;
        }

        [Server]
        public void ClearDepartmentalLightTint(AreaId areaId)
        {
            if (!_registry.TryGet(areaId, out AreaRecord record))
                return;

            record.HasDepartmentalLightTint = false;
            record.DepartmentalLightTint = default;
        }

        public SavedAreaRecord[] BuildSavedAreaRecords()
        {
            var saved = new List<SavedAreaRecord>();
            foreach (AreaRecord record in _registry.GetAllAreas())
            {
                if (record.Apc == null)
                    continue;

                Vector3 world = _query.TileToWorld(record.Apc.OriginTile);
                saved.Add(new SavedAreaRecord
                {
                    id = record.Id.Value,
                    displayName = record.DisplayName,
                    parentTag = record.ParentTag,
                    apcWorldPosition = world,
                    hasDepartmentalLightTint = record.HasDepartmentalLightTint,
                    departmentalLightTint = record.DepartmentalLightTint,
                });
            }

            return saved.ToArray();
        }

        public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
        {
            // Live boundary recompute deferred.
        }

        public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
        {
            // Live boundary recompute deferred.
        }

        public void OnChunkCreated(TileChunkRef chunk) { }

        private void HandleMapLoaded(object sender, EventArgs args)
        {
            if (_registeredApcs.Count > 0)
            {
                RebuildAllAreasFromApcs();
                return;
            }

            if (_map.LoadedAreaRecords.Count > 0)
                RestoreRegistryFromSave(_map.LoadedAreaRecords);
        }

        private void RestoreRegistryFromSave(IReadOnlyList<SavedAreaRecord> savedAreas)
        {
            _registry.Clear();
            ushort highestId = 0;

            foreach (SavedAreaRecord saved in savedAreas)
            {
                highestId = Math.Max(highestId, saved.id);
                _registry.Register(new AreaRecord
                {
                    Id = new AreaId(saved.id),
                    DisplayName = saved.displayName,
                    ParentTag = saved.parentTag,
                    Apc = null,
                    HasDepartmentalLightTint = saved.hasDepartmentalLightTint,
                    DepartmentalLightTint = saved.departmentalLightTint,
                });
            }

            _registry.EnsureNextIdAbove(highestId);
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

                if (!_map.TryGetAreaId(apc.OriginTile, out ushort originAreaId) || originAreaId == AreaId.None)
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

        private void UpdateAreaLightingStates()
        {
            if (!SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            foreach (AreaRecord record in _registry.GetAllAreas())
            {
                if (record.Apc is not IApcChannelSource areaApc || record.Apc is not IElectricDevice apcDevice)
                {
                    continue;
                }

                IPowerStorage apcStorage = record.Apc as IPowerStorage;
                if (!electricitySubSystem.TryGetApcCircuitStats(areaApc, apcStorage, out CircuitStats stats))
                {
                    continue;
                }

                AreaLightingState newState = AreaLightingStateDeriver.Derive(stats, areaApc.Channels);
                ApplyLightingStateChange(record.Id, newState);
            }
        }

        private void ApplyLightingStateChange(AreaId areaId, AreaLightingState newState)
        {
            if (_lightingStates.TryGetValue(areaId, out AreaLightingState previousState) && previousState == newState)
            {
                return;
            }

            _lightingStates[areaId] = newState;
            OnAreaLightingStateChanged?.Invoke(areaId, newState);

            if (IsServer)
            {
                RpcAreaLightingStateChanged(areaId.Value, newState);
            }
        }

        [ObserversRpc]
        private void RpcAreaLightingStateChanged(ushort areaIdValue, AreaLightingState state)
        {
            if (IsServer)
            {
                return;
            }

            var areaId = new AreaId(areaIdValue);
            _lightingStates[areaId] = state;
            OnAreaLightingStateChanged?.Invoke(areaId, state);
        }
    }
}

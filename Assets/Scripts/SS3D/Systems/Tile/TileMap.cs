using FishNet;
using FishNet.Connection;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Class used for storing and modifying a tile map. Coordinates on the tile map follows the following :
    /// - South North  is the Y axis with North going toward positives.
    /// - East West is the X axis with east going toward positives.
    /// </summary>
    public class TileMap : NetworkBehaviour
    {
        private Dictionary<Vector2Int, TileChunk> _chunks;
        private List<PlacedItemObject> _items;
        private readonly List<ITileMutationObserver> _mutationObservers = new();
        private readonly List<SavedAreaRecord> _loadedAreaRecords = new();
        private AdjacencyEngine _adjacencyEngine;
        private string _mapName;
        private int _bulkMutationDepth;
        private int _structureVersion;

        /// <summary>Placements between yields during time-sliced <see cref="LoadRoutine"/> / DMM apply.</summary>
        public const int BulkLoadYieldEveryPlacements = 64;

        public int MapId { get; private set; }

        public AdjacencyEngine AdjacencyEngine => _adjacencyEngine;

        public int ChunkCount => _chunks.Count;

        public IReadOnlyList<PlacedItemObject> PlacedItems => _items;

        public IReadOnlyList<SavedAreaRecord> LoadedAreaRecords => _loadedAreaRecords;

        private readonly Dictionary<string, int> _lastLoadSkipped = new();

        /// <summary>
        /// Asset names the last <see cref="LoadRoutine"/> could not resolve, with how many placements
        /// each dropped. Non-empty means the map was saved by a build with objects this build lacks;
        /// saving it now would make the loss permanent.
        /// </summary>
        public IReadOnlyDictionary<string, int> LastLoadSkipped => _lastLoadSkipped;

        private void CountSkipped(string assetName)
        {
            string key = string.IsNullOrEmpty(assetName) ? "(unnamed)" : assetName;
            _lastLoadSkipped[key] = _lastLoadSkipped.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        public void SetLoadedAreaRecords(IEnumerable<SavedAreaRecord> records)
        {
            _loadedAreaRecords.Clear();
            if (records == null)
            {
                return;
            }

            _loadedAreaRecords.AddRange(records);
        }

        public event EventHandler OnMapLoaded;

        public static TileMap Create(string name, int mapId = 0)
        {
            GameObject mapObject = new GameObject(name);

            TileMap map = mapObject.AddComponent<TileMap>();
            map.Setup(name, mapId);

            if (InstanceFinder.ServerManager != null && mapObject.GetComponent<NetworkObject>() != null)
            {
                InstanceFinder.ServerManager.Spawn(mapObject);
            }

            return map;
        }

        private void Setup(string mapName, int mapId)
        {
            _chunks = new Dictionary<Vector2Int, TileChunk>();
            _items = new List<PlacedItemObject>();
            name = mapName;
            _mapName = mapName;
            MapId = mapId;
            _adjacencyEngine = new AdjacencyEngine(this);
        }

        /// <summary>
        /// Inserts an already-spawned <see cref="PlacedTileObject"/> into the logical map. Used on a remote
        /// client to mirror server-side placement into a client-local map (chunks are created on demand),
        /// so read-only systems such as vision can query occupancy. Does not run adjacency processing;
        /// clients receive derived adjacency through the objects' own synced state.
        /// </summary>
        public void AddClientPlacedObject(PlacedTileObject placed)
        {
            if (placed == null || placed.tileObjectSO == null)
                return;

            Vector3 origin = new Vector3(placed.WorldOrigin.x, 0, placed.WorldOrigin.y);

            foreach (Vector2Int gridOffset in placed.GridOffsetList)
            {
                Vector3 cell = origin + new Vector3(gridOffset.x, 0, gridOffset.y);
                GetOrCreateTileLocation(placed.Layer, cell).AddPlacedObject(placed, placed.Direction);
            }

            NotifyTilePlaced(placed, origin);

            if (placed.Layer == TileLayer.Turf)
                TileUnderfloorVisibility.RefreshUnderfloorForTurf(this, placed);
        }

        /// <summary>
        /// Removes a placed object from the client-local map (e.g. when it despawns).
        /// </summary>
        public void RemoveClientPlacedObject(PlacedTileObject placed)
        {
            if (placed == null || placed.tileObjectSO == null)
                return;

            TileLayer layer = placed.Layer;
            Vector2Int worldOrigin = placed.WorldOrigin;
            List<Vector2Int> gridOffsets = placed.GridOffsetList;
            Vector3 origin = new Vector3(worldOrigin.x, 0, worldOrigin.y);

            foreach (Vector2Int gridOffset in gridOffsets)
            {
                Vector3 cell = origin + new Vector3(gridOffset.x, 0, gridOffset.y);
                if (!TryGetTileLocation(placed.Layer, cell, out ITileLocation location))
                    continue;

                // FishNet is already despawning it. DestroySelf would call the server-only Despawn,
                // and one warning per tile froze the client when a chunk left its view.
                location.TryForgetPlacedObject(placed.Direction);
            }

            NotifyTileCleared(placed, origin, layer);

            if (layer == TileLayer.Turf)
            {
                foreach (Vector2Int gridOffset in gridOffsets)
                {
                    Vector3 cell = origin + new Vector3(gridOffset.x, 0, gridOffset.y);
                    TileUnderfloorVisibility.RefreshUnderfloorAt(this, cell);
                }
            }
        }

        public void RegisterMutationObserver(ITileMutationObserver observer)
        {
            if (observer != null && !_mutationObservers.Contains(observer))
                _mutationObservers.Add(observer);
        }

        public void UnregisterMutationObserver(ITileMutationObserver observer)
        {
            _mutationObservers.Remove(observer);
        }

        /// <summary>
        /// Suppress place/clear mutation notifies (atmos UpdateCell, cable graph refresh, etc.)
        /// during bulk place. Nestable. Chunk-created notifies still fire so late observers can
        /// track structure; prefer <see cref="GetChunkRefs"/> seeding after restore.
        /// </summary>
        public void BeginBulkMutation() => _bulkMutationDepth++;

        public void EndBulkMutation()
        {
            if (_bulkMutationDepth <= 0)
                return;

            _bulkMutationDepth--;
            if (_bulkMutationDepth == 0)
                BumpStructureVersion();
        }

        public bool IsBulkMutationActive => _bulkMutationDepth > 0;

        /// <summary>
        /// Monotonic counter bumped when place/clear/chunk/clear-map changes occupancy.
        /// Not bumped for door open/close (<see cref="NotifyTileStateChanged"/>) — area membership
        /// ignores door state. Consumers cache device→area resolves against this version.
        /// </summary>
        public int StructureVersion => _structureVersion;

        public void BumpStructureVersion() => _structureVersion++;

        /// <summary>
        /// Returns the chunk key to be used based on a world position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2Int GetKey(Vector3 worldPosition)
        {
            int x = (int)Math.Floor(worldPosition.x / TileChunk.ChunkSize);
            int y = (int)Math.Floor(worldPosition.z / TileChunk.ChunkSize);

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Create a new chunk. Internal use only
        /// </summary>
        /// <param name="chunkKey">Unique key to use</param>
        /// <param name="origin">Origin position of the chunk</param>
        /// <returns></returns>
        private TileChunk CreateChunk(Vector2Int chunkKey, Vector3 origin)
        {
            // An async station load can outlive its map (Play Mode exit, scene unload). Creating the
            // chunk first would orphan it in whatever scene is active - in the editor, the saved Game scene.
            if (this == null)
            {
                throw new OperationCanceledException("Tile map was destroyed while loading.");
            }

            TileChunk chunk = TileChunk.Create(chunkKey, origin);
            chunk.transform.SetParent(transform);

            return chunk;
        }

        /// <summary>
        /// Returns chunk based the world position. Will create a new chunk if it doesn't exist.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        private TileChunk GetOrCreateChunk(Vector3 worldPosition)
        {
            TileChunk chunk = GetChunk(worldPosition);
            if (chunk == null)
            {
                Vector2Int key = GetKey(worldPosition);
                Vector3 origin = new Vector3 { x = key.x * TileChunk.ChunkSize, z = key.y * TileChunk.ChunkSize };
                chunk = CreateChunk(key, origin);
                _chunks[key] = chunk;
                NotifyChunkCreated(key, origin);
            }

            return chunk;
        }

        public TileChunk GetChunk(Vector3 worldPosition)
        {
            Vector2Int key = GetKey(worldPosition);

            if (_chunks.TryGetValue(key, out TileChunk _))
            {
                return _chunks[key];
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<TileChunk> GetAllChunks() => _chunks.Values;

        public bool TryGetAreaId(TileCoord coord, out ushort areaId)
        {
            areaId = 0;
            if (coord.MapId != MapId)
                return false;

            Vector3 world = new Vector3(coord.Grid.x, 0, coord.Grid.y);
            TileChunk chunk = GetChunk(world);
            if (chunk == null)
                return false;

            Vector2Int local = chunk.GetXY(world);
            areaId = chunk.GetAreaId(local.x, local.y);
            return true;
        }

        public bool TrySetAreaId(TileCoord coord, ushort areaId)
        {
            if (coord.MapId != MapId)
                return false;

            Vector3 world = new Vector3(coord.Grid.x, 0, coord.Grid.y);
            TileChunk chunk = GetOrCreateChunk(world);
            Vector2Int local = chunk.GetXY(world);
            chunk.SetAreaId(local.x, local.y, areaId);
            return true;
        }

        public bool TryGetFloorDecalId(Vector3 worldPosition, out ushort decalId)
        {
            decalId = 0;
            TileChunk chunk = GetChunk(worldPosition);
            if (chunk == null)
                return false;

            Vector2Int local = chunk.GetXY(worldPosition);
            decalId = chunk.GetFloorDecalId(local.x, local.y);
            return true;
        }

        public bool TrySetFloorDecal(Vector3 worldPosition, ushort decalId)
        {
            if (!HasPlenumAt(worldPosition))
                return false;

            TileChunk chunk = GetOrCreateChunk(worldPosition);
            Vector2Int local = chunk.GetXY(worldPosition);
            chunk.SetFloorDecalId(local.x, local.y, decalId);
            OnFloorDecalsChanged?.Invoke(GetKey(worldPosition));
            return true;
        }

        public bool TryClearFloorDecal(Vector3 worldPosition) => TrySetFloorDecal(worldPosition, 0);

        public event Action<Vector2Int> OnFloorDecalsChanged;

        private bool HasPlenumAt(Vector3 worldPosition)
        {
            if (!TryGetTileLocation(TileLayer.Plenum, worldPosition, out ITileLocation plenumLocation))
                return false;

            return !plenumLocation.IsFullyEmpty();
        }

        public void ClearAllAreaIds()
        {
            foreach (TileChunk chunk in _chunks.Values)
                chunk.ClearAreaIds();
        }

        public ITileLocation GetTileLocation(TileLayer layer, Vector3 worldPosition)
        {
            return GetOrCreateTileLocation(layer, worldPosition);
        }

        public ITileLocation GetOrCreateTileLocation(TileLayer layer, Vector3 worldPosition)
        {
            TileChunk chunk = GetOrCreateChunk(worldPosition);
            return chunk.GetTileObject(layer, worldPosition);
        }

        public bool TryGetTileLocation(TileLayer layer, Vector3 worldPosition, out ITileLocation location)
        {
            TileChunk chunk = GetChunk(worldPosition);
            if (chunk == null)
            {
                location = CreateEmptyTileLocation(layer, worldPosition);
                return false;
            }

            location = chunk.GetTileObject(layer, worldPosition);
            return true;
        }

        public bool TryGetTileLocations(Vector3 worldPosition, out ITileLocation[] tileLocations)
        {
            tileLocations = new ITileLocation[TileHelper.GetTileLayers().Length];
            TileChunk chunk = GetChunk(worldPosition);

            if (chunk == null)
            {
                foreach (TileLayer layer in TileHelper.GetTileLayers())
                    tileLocations[(int)layer] = CreateEmptyTileLocation(layer, worldPosition);

                return false;
            }

            foreach (TileLayer layer in TileHelper.GetTileLayers())
                tileLocations[(int)layer] = chunk.GetTileObject(layer, worldPosition);

            return true;
        }

        public ITileLocation[] GetTileLocations(Vector3 worldPosition)
        {
            ITileLocation[] tileObjects = new ITileLocation[TileHelper.GetTileLayers().Length];

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                tileObjects[(int)layer] = GetOrCreateTileLocation(layer, worldPosition);
            }

            return tileObjects;
        }

        private static ITileLocation CreateEmptyTileLocation(TileLayer layer, Vector3 worldPosition)
        {
            Vector2Int local = GetLocalTileCoordinates(worldPosition);
            return TileHelper.CreateTileLocation(layer, local.x, local.y);
        }

        private static Vector2Int GetLocalTileCoordinates(Vector3 worldPosition)
        {
            int chunkX = (int)Math.Floor(worldPosition.x / TileChunk.ChunkSize);
            int chunkY = (int)Math.Floor(worldPosition.z / TileChunk.ChunkSize);
            Vector3 chunkOrigin = new Vector3(chunkX * TileChunk.ChunkSize, 0, chunkY * TileChunk.ChunkSize);

            return new Vector2Int(
                (int)Math.Round(worldPosition.x - chunkOrigin.x),
                (int)Math.Round(worldPosition.z - chunkOrigin.z));
        }

        /// <summary>
        /// Returns an array of the 8 neighbouring placed objects of a given tile location.
        /// It should be noted that if the tile location is not a single object tile location (e.g a cardinal tile location),
        /// then it checks if on the tile next to it, an item is present in the opposite direction.
        /// Therefore it doesn't consider neighbours two objects on adjacent tiles, unless they're placed in opposite
        /// direction. For single object tile location, direction does not matter of course.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public PlacedTileObject[] GetNeighbourPlacedObjects(TileLayer layer, Vector3 worldPosition)
        {
            PlacedTileObject[] adjacentObjects = new PlacedTileObject[8];

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
                Vector3 neighbourPosition = worldPosition + new Vector3(vector.Item1, 0, vector.Item2);
                TryGetTileLocation(layer, neighbourPosition, out ITileLocation neighbourLocation);
                neighbourLocation.TryGetPlacedObject(out PlacedTileObject neighbourObject, TileHelper.GetOpposite(direction));
                adjacentObjects[(int)direction] = neighbourObject;
            }
            return adjacentObjects;
        }


        public List<PlacedTileObject> GetCardinalNeighbourPlacedObjects(TileLayer layer, Vector3 worldPosition)
        {
            List<PlacedTileObject> adjacentObjects = new();

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction+= 2)
            {
                Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
                Vector3 neighbourPosition = worldPosition + new Vector3(vector.Item1, 0, vector.Item2);
                TryGetTileLocation(layer, neighbourPosition, out ITileLocation neighbourLocation);
                adjacentObjects.AddRange(neighbourLocation.GetAllPlacedObject());
            }

            return adjacentObjects;
        }


        /// <summary>
        /// Returns whether the specified object can be successfully build for a given position and direction.
        /// </summary>
        /// <param name="tileObjectSo">Object to place</param>
        /// <param name="placePosition">World position to place the object</param>
        /// <param name="dir">Direction the object is facing</param>
        /// <param name="replaceExisting">Replace an existing object</param>
        /// <returns></returns>
        public bool CanBuild(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting) =>
            EvaluateBuild(tileObjectSo, placePosition, dir, replaceExisting).Length == 0;

        /// <summary>
        /// Aggregates <see cref="BuildChecker"/> failures across every cell the object occupies.
        /// </summary>
        public BuildFailReason[] EvaluateBuild(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir, bool replaceExisting)
        {
            List<Vector2Int> gridPositionList = tileObjectSo.GetGridOffsetList(dir);
            var failures = new List<BuildFailReason>();

            foreach (Vector2Int gridOffset in gridPositionList)
            {
                Vector3 gridPosition = new(placePosition.x + gridOffset.x, 0, placePosition.z + gridOffset.y);
                TryGetTileLocations(gridPosition, out ITileLocation[] tileLocations);

                BuildFailReason[] cellFailures = BuildChecker.Evaluate(
                    tileLocations,
                    tileObjectSo,
                    dir,
                    gridPosition,
                    GetNeighbourPlacedObjects(TileLayer.Turf, gridPosition),
                    replaceExisting);

                foreach (BuildFailReason failure in cellFailures)
                {
                    if (!failures.Contains(failure))
                        failures.Add(failure);
                }
            }

            return failures.Count == 0
                ? System.Array.Empty<BuildFailReason>()
                : failures.ToArray();
        }

        public bool PlaceTileObject(TileObjectSo tileObjectSo, Vector3 placePosition, Direction dir,
            bool skipBuildCheck, bool replaceExisting, bool skipAdjacency, out GameObject placedObjectGo)
        {
            bool canBuild = skipBuildCheck || CanBuild(tileObjectSo, placePosition, dir, replaceExisting);
            placedObjectGo = null;

            if (canBuild || skipBuildCheck)
            {
                TileChunk chunk = GetOrCreateChunk(placePosition);
                Vector2Int origin = chunk.GetXY(placePosition);
                PlacedTileObject placedObject = PlacedTileObject.Create(placePosition, origin, dir, tileObjectSo, MapId);
                placedObject.transform.SetParent(chunk.transform);

                foreach (Vector2Int gridOffset in tileObjectSo.GetGridOffsetList(dir))
                {
                    Vector3 gridPosition = new(placePosition.x + gridOffset.x, 0, placePosition.z + gridOffset.y);
                    chunk = GetOrCreateChunk(gridPosition);

                    // Remove an existing object if there
                    if (replaceExisting)
                        ClearTileObject(gridPosition, tileObjectSo.layer, dir);

                    // Place new object
                    chunk.GetTileObject(tileObjectSo.layer, gridPosition).AddPlacedObject(placedObject, dir);
                }

                // Handle Adjacency connectors, can skip it particulary when loading the map.
                if (!skipAdjacency)
                {
                    UpdateAdjacenciesFor(placedObject);
                    _adjacencyEngine.ProcessQueue();
                }

                placedObjectGo = placedObject.gameObject;
                NotifyTilePlaced(placedObject, placePosition);

                if (tileObjectSo.layer == TileLayer.Turf)
                    TileUnderfloorVisibility.RefreshUnderfloorForTurf(this, placedObject);
            }

            return canBuild;
        }

        public void ClearTileObject(Vector3 placePosition, TileLayer layer, Direction dir)
        {
            TryGetTileLocations(placePosition, out ITileLocation[] tileLocations);
            ITileLocation tileLocation = tileLocations[(int)layer];
            tileLocation.TryGetPlacedObject(out PlacedTileObject placed, dir);

            // Capture turf footprint before DestroySelf so underfloor can reappear after clear.
            List<Vector3> underfloorRefreshCells = null;
            if (layer == TileLayer.Turf && placed != null)
            {
                underfloorRefreshCells = new List<Vector3>();
                Vector3 turfOrigin = new Vector3(placed.WorldOrigin.x, 0f, placed.WorldOrigin.y);
                foreach (Vector2Int gridOffset in placed.GridOffsetList)
                    underfloorRefreshCells.Add(turfOrigin + new Vector3(gridOffset.x, 0f, gridOffset.y));
            }

            if (placed != null)
            {
                NotifyTileCleared(placed, placePosition, layer);

                if (placed.TryGetComponent(out IAdjacencyConnector connector))
                {
                    List<PlacedTileObject> neighbours = connector.GetNeighbours();
                    ResetAdjacencies(placed, tileLocation, neighbours);
                }
                else
                {
                    tileLocation.TryClearPlacedObject(dir);
                }
            }

            // Remove any invalid tile combinations
            List<ITileLocation> toClearLocations = BuildChecker.GetToBeClearedLocations(tileLocations);

            foreach (ITileLocation clearLocation in toClearLocations)
            {
                var allPlaced = clearLocation.GetAllPlacedObject();

                foreach (PlacedTileObject placedToClear in allPlaced)
                {
                    if (placed != null && placedToClear.TryGetComponent(out IAdjacencyConnector connectorToClear))
                    {
                        List<PlacedTileObject> neighbours = connectorToClear.GetNeighbours();
                        ResetAdjacencies(placed, clearLocation, neighbours);
                    }
                }

                clearLocation.ClearAllPlacedObject();
            }

            if (underfloorRefreshCells != null)
            {
                for (int i = 0; i < underfloorRefreshCells.Count; i++)
                    TileUnderfloorVisibility.RefreshUnderfloorAt(this, underfloorRefreshCells[i]);
            }
        }

        private void ResetAdjacencies(PlacedTileObject placed, ITileLocation location, List<PlacedTileObject> neighbours)
        {
            List<Direction> neighboursAtDirection= new List<Direction>();

            // First get the directions of all neighbours, relative to this placed object.
            // Direction is not always relevant (e.g disposal pipes with disposal furnitures)
            // but it is in most cases. It's up to the connectors to choose what they do with this info.
            foreach (PlacedTileObject neighbour in neighbours)
            {
               placed.NeighbourAtDirectionOf(neighbour, out var dir);
               neighboursAtDirection.Add(dir);
            }
            // then destroy this placed object. It's important to do it here, before updating
            // adjacencies, as some connectors might be looking for it.
            location.ClearAllPlacedObject();

            // Then update all neighbours, using their directions.
            int i = 0;
            foreach (PlacedTileObject neighbour in neighbours)
            {
                Direction dir = neighboursAtDirection[i];
                neighbour?.UpdateSingleAdjacency(TileHelper.GetOpposite(dir), null, false);
                i++;
            }
        }

        public void PlaceItemObject(Vector3 worldPosition, Quaternion rotation, ItemObjectSo itemObjectSo, GameObject existingItem = null)
        {
            // Handle existing items that already have a PlacedItemObject component
            if (existingItem != null)
            {
                PlacedItemObject existingPlacedItem = existingItem.GetComponent<PlacedItemObject>();
                if (existingPlacedItem != null)
                {
                    if (_items.Contains(existingPlacedItem))
                    {
                        // Item is already tracked, just update its position
                        existingPlacedItem.UpdatePosition(worldPosition, rotation);
                        return;
                    }
                    else
                    {
                        // Item has PlacedItemObject but not tracked, remove the old component
                        DestroyImmediate(existingPlacedItem);
                    }
                }
            }
            
            // Create new PlacedItemObject and add to tracking
            PlacedItemObject placedItem = PlacedItemObject.Create(worldPosition, rotation, itemObjectSo, existingItem);
            placedItem.transform.SetParent(transform);
            _items.Add(placedItem);
        }

        public void ClearItemObject(Vector3 worldPosition, ItemObjectSo itemObjectSo)
        {
            List<PlacedItemObject> placedItems = _items.FindAll(item => item.NameString == itemObjectSo.NameString);
            PlacedItemObject toRemove = null;

            foreach (PlacedItemObject item in placedItems)
            {
                if (Vector3.Distance(item.transform.position, worldPosition) < 1f)
                {
                    toRemove = item;
                }
            }

            toRemove?.DestroySelf();
            _items.Remove(toRemove);
        }

        /// <summary>
        /// Remove a specific PlacedItemObject from TileMap tracking without destroying the GameObject.
        /// This is used when items are picked up and placed in containers.
        /// </summary>
        /// <param name="placedItemObject">The PlacedItemObject to remove from tracking</param>
        public void RemovePlacedItemFromTracking(PlacedItemObject placedItemObject)
        {
            if (placedItemObject != null && _items.Contains(placedItemObject))
            {
                _items.Remove(placedItemObject);
            }
        }

        public void Clear()
        {
            if (SubSystems.TryGet(out TileSubSystem owner) && owner.CurrentMap == this &&
                SubSystems.TryGet(out Area.AreaSubSystem areas)) areas.ResetForMapClear();
            foreach (TileChunk chunk in _chunks.Values)
            {
                chunk.Clear();
            }

            _chunks.Clear();
            BumpStructureVersion();

            // Clear items list safely, checking for null references
            while (_items.Count > 0)
            {
                PlacedItemObject item = _items.First();
                if (item != null && item.gameObject != null)
                {
                    item.DestroySelf();
                }
                
                _items.RemoveAt(0);
            }

            // Template restore skips missing spawn chunks — wipe markers with the map so stale
            // points cannot survive a load that has no spawn-points contributor payload.
            if (SubSystems.TryGet(out TileSubSystem tileSubSystem) && tileSubSystem.CurrentMap == this)
                tileSubSystem.ClearSpawnPoints();
        }

        /// <summary>
        /// Returns a new SaveObject for storing the entire map.
        /// Note: Items in player inventory (containers) are not saved as they have their PlacedItemObject
        /// component removed when picked up. Only items placed in the world are saved.
        /// </summary>
        /// <returns></returns>
        public SavedTileMap Save()
        {
            List<SavedTileChunk> chunkObjectSaveList = new List<SavedTileChunk>();
            List<SavedPlacedItemObject> itemSaveList = new List<SavedPlacedItemObject>();

            foreach (TileChunk chunk in _chunks.Values)
            {
                chunkObjectSaveList.Add(chunk.Save());
            }

            foreach (PlacedItemObject item in _items)
            {
                itemSaveList.Add(item.Save());
            }

            return new SavedTileMap
            {
                mapName = _mapName,
                hasAuthoredAreas = SubSystems.TryGet(out Area.AreaSubSystem areas) && areas.HasAuthoredAreas,
                savedChunkList = chunkObjectSaveList.ToArray(),
                savedItemList = itemSaveList.ToArray(),
                savedAreas = BuildSavedAreas(),
            };
        }

        private SavedAreaRecord[] BuildSavedAreas()
        {
            if (SubSystems.TryGet(out Area.AreaSubSystem areaSubSystem))
                return areaSubSystem.BuildSavedAreaRecords();

            return Array.Empty<SavedAreaRecord>();
        }

        public void Load([CanBeNull] SavedTileMap saveObject, bool invokeMapLoadedEvent = true)
        {
            IEnumerator routine = LoadRoutine(saveObject, invokeMapLoadedEvent, yieldFrames: false);
            while (routine.MoveNext())
            {
            }
        }

        /// <summary>
        /// Time-sliced station template restore. Yield every
        /// <see cref="BulkLoadYieldEveryPlacements"/> so FishNet heartbeats keep ticking on MetaStation.
        /// Pair with <see cref="BeginBulkMutation"/> and deferred area/disposal rebuilds.
        /// </summary>
        public IEnumerator LoadRoutine(
            [CanBeNull] SavedTileMap saveObject,
            bool invokeMapLoadedEvent = true,
            bool yieldFrames = true)
        {
            if (saveObject == null)
            {
                Log.Warning(this, "The intended save object is null");
                yield break;
            }

            // Clear TileMap data first (this clears the _items list)
            Clear();
            _loadedAreaRecords.Clear();
            _lastLoadSkipped.Clear();

            // Then clear all items in the scene, not just those tracked by TileMap
            ClearUntrackedItems();
            if (yieldFrames)
                yield return null;

            SubSystems.TryGet(out TileSubSystem tileSystem);

            SavedTileChunk[] savedChunks = saveObject.savedChunkList ?? Array.Empty<SavedTileChunk>();
            int sinceYield = 0;
            int chunkIndex = 0;

            foreach (SavedTileChunk savedChunk in savedChunks)
            {
                chunkIndex++;
                TileChunk chunk = GetOrCreateChunk(savedChunk.originPosition);
                if (savedChunk.areaIds != null)
                    chunk.SetAreaIds(savedChunk.areaIds);
                if (savedChunk.floorDecalIds != null)
                    chunk.SetFloorDecalIds(savedChunk.floorDecalIds);

                if (tileSystem == null)
                    continue;

                ISavedTileLocation[] savedTiles = savedChunk.savedTiles ?? Array.Empty<ISavedTileLocation>();

                foreach (var savedTile in savedTiles)
                {
                    foreach (SavedPlacedTileObject savedObject in savedTile.GetPlacedObjects())
                    {
                        TileObjectSo toBePlaced = tileSystem.GetAsset(savedObject.tileObjectSOName) as TileObjectSo;
                        if (toBePlaced == null)
                        {
                            Log.Warning(this,
                                "Skipping unknown or removed tile asset '{assetName}' while loading map",
                                Logs.Generic,
                                savedObject.tileObjectSOName);
                            CountSkipped(savedObject.tileObjectSOName);
                            continue;
                        }

                        Vector3 placePosition = chunk.GetWorldPosition(savedTile.Location.x, savedTile.Location.y);

                        // Skipping build check here to allow loading tile objects in a non-valid order
                        PlaceTileObject(toBePlaced, placePosition, savedObject.dir, true, false, true, out GameObject placedObject);
                        sinceYield++;

                        if (yieldFrames && sinceYield >= BulkLoadYieldEveryPlacements)
                        {
                            sinceYield = 0;
                            yield return null;
                        }
                    }
                }
            }

            if (saveObject.savedAreas != null)
                _loadedAreaRecords.AddRange(saveObject.savedAreas);

            if (tileSystem == null)
            {
                if (invokeMapLoadedEvent)
                {
                    OnMapLoaded?.Invoke(this, EventArgs.Empty);
                }

                yield break;
            }

            SavedPlacedItemObject[] savedItems = saveObject.savedItemList ?? Array.Empty<SavedPlacedItemObject>();
            foreach (SavedPlacedItemObject savedItem in savedItems)
            {
                if (tileSystem.GetAsset(savedItem.itemName) is not ItemObjectSo toBePlaced)
                {
                    Log.Warning(this, "Skipping unknown or removed item asset '{assetName}' while loading map",
                        Logs.Generic, savedItem.itemName);
                    CountSkipped(savedItem.itemName);
                    continue;
                }

                PlaceItemObject(savedItem.worldPosition, savedItem.rotation, toBePlaced);
                sinceYield++;
                if (yieldFrames && sinceYield >= BulkLoadYieldEveryPlacements)
                {
                    sinceYield = 0;
                    yield return null;
                }
            }

            if (invokeMapLoadedEvent)
            {
                OnMapLoaded?.Invoke(this, EventArgs.Empty);
            }

            if (yieldFrames)
                yield return null;
            RefreshAllAdjacencies();

            if (yieldFrames)
                yield return null;
            // Template load skips per-tile observer churn; re-apply AOI + underfloor occlusion now
            // that every covering turf is present.
            RefreshAllHostVisibility();

            // Host may have had empty Observers during the first pass (debug: all outOfAoi).
            // Rebuild then refresh so in-AOI cells take the cover-hide branch after AOI show.
            if (InstanceFinder.ServerManager != null
                && InstanceFinder.IsClient
                && InstanceFinder.ClientManager != null)
            {
                NetworkConnection conn = InstanceFinder.ClientManager.Connection;
                if (conn != null && conn.IsValid)
                {
                    InstanceFinder.ServerManager.Objects.RebuildObservers(conn, timedOnly: false);
                    if (yieldFrames)
                        yield return null;
                    RefreshAllHostVisibility();
                }
            }
        }

        /// <summary>
        /// Recompute adjacency for every placed tile that has a connector.
        /// Used after bulk load/import that skipped per-tile adjacency.
        /// </summary>
        public void RefreshAllAdjacencies()
        {
            foreach (TileChunk chunk in _chunks.Values)
            {
                foreach (PlacedTileObject obj in chunk.GetAllTilePlacedObjects())
                {
                    if (obj.HasAdjacencyConnector)
                        UpdateAdjacenciesFor(obj);
                }
            }

            _adjacencyEngine.ProcessQueue();
        }

        /// <summary>
        /// Re-apply host MeshRenderer visibility for every placed tile (Map Editor open → all
        /// visible; closed → HashGrid AOI). See <see cref="PlacedTileObject.RefreshHostVisibility"/>.
        /// </summary>
        public void RefreshAllHostVisibility()
        {
            foreach (TileChunk chunk in _chunks.Values)
            {
                foreach (PlacedTileObject obj in chunk.GetAllTilePlacedObjects())
                {
                    if (obj != null)
                        obj.RefreshHostVisibility();
                }
            }
        }

        private void UpdateAdjacenciesFor(PlacedTileObject placedObject)
        {
            if (placedObject.TryGetComponent<IEngineDrivenAdjacency>(out _))
                _adjacencyEngine.QueueCascadeFrom(placedObject);
            else
                placedObject.UpdateAdjacencies();
        }

        /// <summary>
        /// Clear untracked items in the scene that are not in containers and don't have a PlacedItemObject component
        /// </summary>
        private void NotifyChunkCreated(Vector2Int chunkKey, Vector3 origin)
        {
            TileChunkRef chunkRef = new TileChunkRef
            {
                MapId = MapId,
                ChunkKey = chunkKey,
                Origin = origin,
            };

            foreach (ITileMutationObserver observer in _mutationObservers)
                observer.OnChunkCreated(chunkRef);

            BumpStructureVersion();
        }

        /// <summary>
        /// Enumerates the chunks that currently exist on the map. Lets observers that register
        /// after chunks were created (e.g. atmospherics) seed themselves from existing state.
        /// </summary>
        public IEnumerable<TileChunkRef> GetChunkRefs()
        {
            foreach (KeyValuePair<Vector2Int, TileChunk> pair in _chunks)
            {
                yield return new TileChunkRef
                {
                    MapId = MapId,
                    ChunkKey = pair.Key,
                    Origin = new Vector3(pair.Key.x * TileChunk.ChunkSize, 0, pair.Key.y * TileChunk.ChunkSize),
                };
            }
        }

        private void NotifyTilePlaced(PlacedTileObject placedObject, Vector3 worldPosition)
        {
            if (_bulkMutationDepth > 0)
                return;

            TileCoord coord = new TileCoord(MapId, Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));

            foreach (ITileMutationObserver observer in _mutationObservers)
                observer.OnTilePlaced(placedObject, coord);

            BumpStructureVersion();
        }

        private void NotifyTileCleared(PlacedTileObject placedObject, Vector3 worldPosition, TileLayer layer)
        {
            if (_bulkMutationDepth > 0)
                return;

            TileCoord coord = new TileCoord(MapId, Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));

            foreach (ITileMutationObserver observer in _mutationObservers)
                observer.OnTileCleared(placedObject, coord, layer);

            BumpStructureVersion();
        }

        /// <summary>
        /// Notifies observers that a tile cell's runtime state changed (e.g. door open/close).
        /// </summary>
        public void NotifyTileStateChanged(Vector3 worldPosition)
        {
            TileCoord coord = new TileCoord(MapId, Mathf.RoundToInt(worldPosition.x), Mathf.RoundToInt(worldPosition.z));

            foreach (ITileMutationObserver observer in _mutationObservers)
                observer.OnTileStateChanged(coord);
        }

        private void ClearUntrackedItems()
        {
            // Find all Item components in the scene
            Item[] allItems = FindObjectsOfType<Item>();
            
            foreach (Item item in allItems)
            {
                // Skip items that are in containers (player inventory, etc.)
                // and items that already have PlacedItemObject (they're already tracked)
                if (item.Container != null || item.GetComponent<PlacedItemObject>() != null)
                {
                    continue;
                }
                
                // Destroy items that are in the world but not tracked by TileMap
                if (item.gameObject != null)
                {
                    Log.Warning(this, "Destroying untracked item: {itemName} at position {position}",
                        Logs.Generic, item.gameObject.name, item.gameObject.transform.position);
                    DestroyImmediate(item.gameObject);
                }
            }
        }
    }
}
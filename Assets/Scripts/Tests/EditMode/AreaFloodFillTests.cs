using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EditorTests
{
    public class AreaFloodFillTests
    {
        [Test]
        public void SingleApcInEnclosedRoom_ClaimsAllInteriorTiles()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(origin: new Vector3(10, 0, 10), width: 5, height: 5);
            TestApc apc = context.PlaceApc(new Vector3(12, 0, 12));

            context.RebuildAll();

            AreaId areaId = context.GetApcAreaId(apc);
            Assert.IsFalse(areaId.IsNone);

            for (int x = 11; x <= 13; x++)
            {
                for (int z = 11; z <= 13; z++)
                {
                    TileCoord coord = new TileCoord(context.Map.MapId, x, z);
                    Assert.AreEqual(areaId.Value, context.GetAreaId(coord), $"Tile ({x},{z}) should belong to APC area.");
                }
            }
        }

        [Test]
        public void TwoApcsInSeparateWalledRooms_CreateDistinctAreas()
        {
            AreaTestContext left = AreaTestContext.CreateRoom(new Vector3(0, 0, 0), 5, 5);
            AreaTestContext right = AreaTestContext.CreateRoom(new Vector3(8, 0, 0), 5, 5, map: left.Map, query: left.Query);

            TestApc leftApc = left.PlaceApc(new Vector3(2, 0, 2));
            TestApc rightApc = right.PlaceApc(new Vector3(10, 0, 2));

            left.RebuildAll();

            AreaId leftArea = left.GetApcAreaId(leftApc);
            AreaId rightArea = left.GetApcAreaId(rightApc);

            Assert.AreNotEqual(leftArea, rightArea);
            Assert.AreEqual(leftArea.Value, left.GetAreaId(new TileCoord(left.Map.MapId, 2, 2)));
            Assert.AreEqual(rightArea.Value, left.GetAreaId(new TileCoord(left.Map.MapId, 10, 2)));
        }

        [Test]
        public void TwoApcsInOpenSpace_FirstApcWinsAndSecondGetsOverlapWarning()
        {
            AreaTestContext context = AreaTestContext.CreateOpenFloor(origin: new Vector3(0, 0, 0), width: 6, height: 4);
            TestApc firstApc = context.PlaceApc(new Vector3(1, 0, 2));
            TestApc secondApc = context.PlaceApc(new Vector3(4, 0, 2));

            context.RebuildAll();

            AreaId firstArea = context.GetApcAreaId(firstApc);
            AreaId secondArea = context.GetApcAreaId(secondApc);

            Assert.IsFalse(firstArea.IsNone);
            Assert.IsFalse(secondArea.IsNone);
            Assert.AreNotEqual(firstArea, secondArea);
            Assert.AreEqual(firstArea.Value, context.GetAreaId(secondApc.OriginTile));
            Assert.IsTrue(firstApc.MultipleApcsInArea);
            Assert.IsTrue(secondApc.MultipleApcsInArea);
        }

        [Test]
        public void DoorBetweenTwoApcRooms_EachSideGetsOwnAreaAndDoorMatchesNeighbor()
        {
            AreaTestContext context = AreaTestContext.CreateTwoRoomsWithDoor(
                leftOrigin: new Vector3(0, 0, 0),
                rightOrigin: new Vector3(5, 0, 0),
                roomSize: 5);

            TestApc leftApc = context.PlaceApc(new Vector3(2, 0, 2));
            TestApc rightApc = context.PlaceApc(new Vector3(7, 0, 2));

            context.RebuildAll();

            AreaId leftArea = context.GetApcAreaId(leftApc);
            AreaId rightArea = context.GetApcAreaId(rightApc);
            TileCoord doorCoord = new TileCoord(context.Map.MapId, 4, 2);

            Assert.AreNotEqual(leftArea, rightArea);
            Assert.AreEqual(leftArea.Value, context.GetAreaId(doorCoord));
            Assert.AreEqual(leftArea.Value, context.GetAreaId(leftApc.OriginTile));
            Assert.AreEqual(rightArea.Value, context.GetAreaId(rightApc.OriginTile));
        }

        [Test]
        public void RoomWithoutApc_RemainsUnassigned()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(new Vector3(0, 0, 0), 4, 4);

            context.RebuildAll();

            Assert.AreEqual(AreaId.None, context.GetAreaId(new TileCoord(context.Map.MapId, 2, 2)));
        }

        [Test]
        public void ApcRemoved_ClearsClaimedTiles()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(new Vector3(0, 0, 0), 4, 4);
            TestApc apc = context.PlaceApc(new Vector3(2, 0, 2));
            context.RebuildAll();

            AreaId areaId = context.GetApcAreaId(apc);
            Assert.AreNotEqual(AreaId.None, areaId.Value);

            context.UnregisterApc(apc);

            Assert.AreEqual(AreaId.None, context.GetAreaId(apc.OriginTile));
        }

        [Test]
        public void SaveLoad_PreservesAreaIdsAndMetadata()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(new Vector3(0, 0, 0), 4, 4);
            TestApc apc = context.PlaceApc(new Vector3(2, 0, 2));
            context.RebuildAll();
            context.AreaSubSystem.RenameArea(context.GetApcAreaId(apc), "Engineering");
            context.AreaSubSystem.SetParentTag(context.GetApcAreaId(apc), "eng");

            SavedTileMap saved = context.Map.Save();
            saved.savedAreas = context.AreaSubSystem.BuildSavedAreaRecords();

            TileMap loadedMap = TileMap.Create("LoadedAreaMap");
            loadedMap.Load(saved);

            var loadedQuery = new TileQueryService(loadedMap);
            var loadedAreaSubSystem = new AreaSubSystemHarness(loadedMap, loadedQuery);
            loadedAreaSubSystem.HandleMapLoaded();

            TileCoord origin = apc.OriginTile;
            Assert.AreEqual(context.GetAreaId(origin), GetRawAreaId(loadedMap, origin));
            Assert.AreEqual(1, loadedMap.LoadedAreaRecords.Count);
            Assert.AreEqual("Engineering", loadedMap.LoadedAreaRecords[0].displayName);
            Assert.AreEqual("eng", loadedMap.LoadedAreaRecords[0].parentTag);
        }

        private static ushort GetRawAreaId(TileMap map, TileCoord coord)
        {
            map.TryGetAreaId(coord, out ushort areaId);
            return areaId;
        }

        private sealed class TestApc : IAreaApcOrigin
        {
            public TileCoord OriginTile { get; init; }

            public string DisplayName { get; init; } = "Test APC";

            public bool MultipleApcsInArea { get; private set; }

            public void SetMultipleApcsInArea(bool value) => MultipleApcsInArea = value;
        }

        private sealed class AreaTestContext
        {
            private readonly List<TestApc> _apcs = new();
            private readonly AreaSubSystemHarness _areaSubSystem;

            private AreaTestContext(TileMap map, TileQueryService query, AreaSubSystemHarness areaSubSystem)
            {
                Map = map;
                Query = query;
                _areaSubSystem = areaSubSystem;
            }

            public TileMap Map { get; }

            public TileQueryService Query { get; }

            public AreaSubSystemHarness AreaSubSystem => _areaSubSystem;

            public static AreaTestContext CreateRoom(Vector3 origin, int width, int height, TileMap map = null, TileQueryService query = null)
            {
                AreaTestContext context = CreateBase(map, query);

                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        Vector3 position = origin + new Vector3(x, 0, z);
                        bool isPerimeter = x == 0 || z == 0 || x == width - 1 || z == height - 1;
                        PlacePlenum(context, position);
                        if (isPerimeter)
                            PlaceTurf(context, position, TileObjectGenericType.Wall);
                    }
                }

                return context;
            }

            public static AreaTestContext CreateOpenFloor(Vector3 origin, int width, int height)
            {
                AreaTestContext context = CreateBase(null, null);

                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                        PlacePlenum(context, origin + new Vector3(x, 0, z));
                }

                return context;
            }

            public static AreaTestContext CreateTwoRoomsWithDoor(Vector3 leftOrigin, Vector3 rightOrigin, int roomSize)
            {
                AreaTestContext context = CreateBase(null, null);
                Vector3 doorPosition = new Vector3(leftOrigin.x + roomSize - 1, 0, leftOrigin.z + roomSize / 2);

                BuildRoom(context, leftOrigin, roomSize, doorPosition);
                BuildRoom(context, rightOrigin, roomSize, Vector3.positiveInfinity);

                PlacePlenum(context, doorPosition);
                PlaceTurf(context, doorPosition, TileObjectGenericType.Door);

                return context;
            }

            public TestApc PlaceApc(Vector3 position)
            {
                TileCoord coord = Query.WorldToTile(position, Map.MapId);
                var apc = new TestApc
                {
                    OriginTile = coord,
                    DisplayName = $"APC {coord.Grid.x},{coord.Grid.y}",
                };
                _apcs.Add(apc);
                _areaSubSystem.RegisterApc(apc);
                return apc;
            }

            public void RebuildAll() => _areaSubSystem.RebuildAllAreasFromApcs();

            public void UnregisterApc(TestApc apc) => _areaSubSystem.UnregisterApc(apc);

            public AreaId GetApcAreaId(TestApc apc)
            {
                Assert.IsTrue(_areaSubSystem.TryGetApcArea(apc, out AreaId areaId));
                return areaId;
            }

            public ushort GetAreaId(TileCoord coord)
            {
                Map.TryGetAreaId(coord, out ushort areaId);
                return areaId;
            }

            private static AreaTestContext CreateBase(TileMap map, TileQueryService query)
            {
                TileMapTestUtilities.EnsureTestAssetsRegistered();
                if (map == null)
                {
                    map = TileMap.Create("AreaFloodFillTests");
                    query = new TileQueryService(map);
                }

                var areaSubSystem = new AreaSubSystemHarness(map, query);
                return new AreaTestContext(map, query, areaSubSystem);
            }

            private static void BuildRoom(AreaTestContext context, Vector3 origin, int size, Vector3 doorToSkip)
            {
                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        Vector3 position = origin + new Vector3(x, 0, z);
                        bool isPerimeter = x == 0 || z == 0 || x == size - 1 || z == size - 1;
                        if (isPerimeter && doorToSkip != Vector3.positiveInfinity
                            && Vector3.Distance(position, doorToSkip) < 0.1f)
                        {
                            continue;
                        }

                        PlacePlenum(context, position);
                        if (isPerimeter)
                            PlaceTurf(context, position, TileObjectGenericType.Wall);
                    }
                }
            }

            private static void PlacePlenum(AreaTestContext context, Vector3 position)
            {
                TileObjectSo plenumSo = TileMapTestUtilities.CreateTileSo(TileLayer.Plenum, "AreaTestPlenum");
                bool success = context.Map.PlaceTileObject(
                    plenumSo,
                    position,
                    Direction.North,
                    skipBuildCheck: false,
                    replaceExisting: false,
                    skipAdjacency: true,
                    out _);
                Assert.IsTrue(success, $"Expected plenum placement at {position}.");
            }

            private static void PlaceTurf(AreaTestContext context, Vector3 position, TileObjectGenericType genericType)
            {
                TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, $"AreaTest_{genericType}");
                turfSo.genericType = genericType;
                bool success = context.Map.PlaceTileObject(
                    turfSo,
                    position,
                    Direction.North,
                    skipBuildCheck: false,
                    replaceExisting: false,
                    skipAdjacency: true,
                    out _);
                Assert.IsTrue(success, $"Expected turf placement at {position}.");
            }
        }

        private sealed class AreaSubSystemHarness
        {
            private readonly AreaRegistry _registry = new();
            private readonly List<IAreaApcOrigin> _registeredApcs = new();
            private readonly HashSet<IAreaApcOrigin> _overlapFlaggedApcs = new();
            private readonly TileMap _map;
            private readonly ITileQueryService _query;
            private readonly AreaFloodFillService _floodFill;

            public AreaSubSystemHarness(TileMap map, ITileQueryService query)
            {
                _map = map;
                _query = query;
                _floodFill = new AreaFloodFillService(map, query);
            }

            public void HandleMapLoaded()
            {
                if (_registeredApcs.Count > 0)
                {
                    RebuildAllAreasFromApcs();
                    return;
                }

                if (_map.LoadedAreaRecords.Count > 0)
                    RestoreRegistryFromSave(_map.LoadedAreaRecords);
            }

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
                _registry.Register(new AreaRecord
                {
                    Id = areaId,
                    DisplayName = apc.DisplayName,
                    Apc = apc,
                });

                var claimedTiles = BuildClaimedTilesExcluding(areaId);
                _floodFill.FloodFromApc(apc, areaId, claimedTiles);
                _floodFill.AssignDoorTileAreas();
                UpdateOverlapWarnings();
            }

            public void UnregisterApc(IAreaApcOrigin apc)
            {
                if (apc == null || !_registeredApcs.Remove(apc))
                    return;

                if (!_registry.TryGetApcArea(apc, out AreaId areaId))
                    return;

                _floodFill.ClearAreaTiles(areaId);
                _registry.Unregister(areaId);
                apc.SetMultipleApcsInArea(false);
                UpdateOverlapWarnings();
            }

            public void RebuildAllAreasFromApcs()
            {
                _map.ClearAllAreaIds();
                _registry.Clear();
                _overlapFlaggedApcs.Clear();

                List<IAreaApcOrigin> apcs = _registeredApcs
                    .OrderBy(apc => apc.OriginTile.Grid.x)
                    .ThenBy(apc => apc.OriginTile.Grid.y)
                    .ToList();

                var claimedTiles = new HashSet<TileCoord>();

                foreach (IAreaApcOrigin apc in apcs)
                {
                    AreaId areaId = _registry.AllocateId();
                    _registry.Register(new AreaRecord
                    {
                        Id = areaId,
                        DisplayName = apc.DisplayName,
                        Apc = apc,
                    });
                    _floodFill.FloodFromApc(apc, areaId, claimedTiles);
                }

                _floodFill.AssignDoorTileAreas();
                UpdateOverlapWarnings();
            }

            public bool TryGetApcArea(IAreaApcOrigin apc, out AreaId areaId) =>
                _registry.TryGetApcArea(apc, out areaId);

            public void RenameArea(AreaId areaId, string displayName)
            {
                if (_registry.TryGet(areaId, out AreaRecord record))
                    record.DisplayName = displayName;
            }

            public void SetParentTag(AreaId areaId, string tag)
            {
                if (_registry.TryGet(areaId, out AreaRecord record))
                    record.ParentTag = tag;
            }

            public SavedAreaRecord[] BuildSavedAreaRecords()
            {
                return _registeredApcs
                    .Select(apc =>
                    {
                        _registry.TryGetApcArea(apc, out AreaId areaId);
                        AreaRecord record = _registry.ById[areaId.Value];
                        return new SavedAreaRecord
                        {
                            id = record.Id.Value,
                            displayName = record.DisplayName,
                            parentTag = record.ParentTag,
                            apcWorldPosition = _query.TileToWorld(apc.OriginTile),
                        };
                    })
                    .ToArray();
            }

            private void RestoreRegistryFromSave(IReadOnlyList<SavedAreaRecord> savedAreas)
            {
                _registry.Clear();
                ushort highestId = 0;

                foreach (SavedAreaRecord saved in savedAreas)
                {
                    highestId = (ushort)Mathf.Max(highestId, saved.id);
                    _registry.Register(new AreaRecord
                    {
                        Id = new AreaId(saved.id),
                        DisplayName = saved.displayName,
                        ParentTag = saved.parentTag,
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
            }
        }
    }
}

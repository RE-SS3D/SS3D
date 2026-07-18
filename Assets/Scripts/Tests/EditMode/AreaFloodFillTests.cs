using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    public class AreaFloodFillTests
    {
        private List<GameObject> _instantiated;

        [SetUp]
        public void SetUp() => _instantiated = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated)
                Object.DestroyImmediate(go);
            _instantiated.Clear();
        }

        [Test]
        public void DeferredFlood_RegisterDuringIncompleteMap_ClaimsFullRoomAfterEnd()
        {
            // East column only — simulates an APC spawning before west chunks load.
            AreaTestContext context = AreaTestContext.CreateOpenFloor(
                _instantiated,
                origin: new Vector3(2, 0, 0),
                width: 1,
                height: 3);

            context.AreaSubSystem.BeginDeferredAreaFlood();
            TestApc apc = context.PlaceApc(new Vector3(2, 0, 1), Direction.West);

            Assert.AreEqual(0, context.GetAreaId(new TileCoord(context.Map.MapId, 2, 1)),
                "Deferred RegisterApc must not flood before the map is complete.");

            // Later chunks: west columns appear.
            for (int x = 0; x <= 1; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    context.PlaceOpenFloorTile(new Vector3(x, 0, z));
                }
            }

            context.AreaSubSystem.EndDeferredAreaFlood();

            AreaId areaId = context.GetApcAreaId(apc);
            Assert.IsFalse(areaId.IsNone);

            for (int x = 0; x <= 2; x++)
            {
                for (int z = 0; z <= 2; z++)
                {
                    Assert.AreEqual(
                        areaId.Value,
                        context.GetAreaId(new TileCoord(context.Map.MapId, x, z)),
                        $"Tile ({x},{z}) should be claimed after EndDeferredAreaFlood.");
                }
            }
        }

        [Test]
        public void FloodWithoutDefer_OnIncompleteMap_MissesUnplacedWestTiles()
        {
            // Documents the game-start failure mode: flood while west tiles missing.
            AreaTestContext context = AreaTestContext.CreateOpenFloor(
                _instantiated,
                origin: new Vector3(2, 0, 0),
                width: 1,
                height: 3);

            TestApc apc = context.PlaceApc(new Vector3(2, 0, 1), Direction.West);
            AreaId areaId = context.GetApcAreaId(apc);

            Assert.AreEqual(areaId.Value, context.GetAreaId(new TileCoord(context.Map.MapId, 2, 1)));

            for (int x = 0; x <= 1; x++)
            {
                for (int z = 0; z < 3; z++)
                {
                    context.PlaceOpenFloorTile(new Vector3(x, 0, z));
                }
            }

            // Without rebuild, newly placed west tiles stay unclaimed (live mutation deferred).
            Assert.AreEqual(0, context.GetAreaId(new TileCoord(context.Map.MapId, 0, 1)));
            Assert.AreEqual(0, context.GetAreaId(new TileCoord(context.Map.MapId, 1, 1)));
        }

        [Test]
        public void DeferredFlood_PreservesRenamedAreaMetadata()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, new Vector3(0, 0, 0), 4, 4);
            TestApc apc = context.PlaceApc(new Vector3(2, 0, 2));
            context.RebuildAll();
            AreaId areaId = context.GetApcAreaId(apc);
            context.AreaSubSystem.RenameArea(areaId, "Engineering");
            context.AreaSubSystem.SetParentTag(areaId, "eng");

            // Simulate post-load reflood: defer flag was set, then ended with APCs already linked.
            context.AreaSubSystem.BeginDeferredAreaFlood();
            context.AreaSubSystem.EndDeferredAreaFlood();

            Assert.IsTrue(context.AreaSubSystem.TryGetAreaForApc(apc, out AreaRecord record));
            Assert.AreEqual("Engineering", record.DisplayName);
            Assert.AreEqual("eng", record.ParentTag);
            Assert.AreEqual(areaId.Value, context.GetAreaId(new TileCoord(context.Map.MapId, 2, 2)));
        }

        [Test]
        public void SingleApcInEnclosedRoom_ClaimsAllInteriorTiles()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, origin: new Vector3(10, 0, 10), width: 5, height: 5);
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
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, new Vector3(0, 0, 0), 5, 5);
            context.AddRoom(new Vector3(8, 0, 0), 5, 5);

            TestApc leftApc = context.PlaceApc(new Vector3(2, 0, 2));
            TestApc rightApc = context.PlaceApc(new Vector3(10, 0, 2));

            context.RebuildAll();

            AreaId leftArea = context.GetApcAreaId(leftApc);
            AreaId rightArea = context.GetApcAreaId(rightApc);

            Assert.AreNotEqual(leftArea, rightArea);
            Assert.AreEqual(leftArea.Value, context.GetAreaId(new TileCoord(context.Map.MapId, 2, 2)));
            Assert.AreEqual(rightArea.Value, context.GetAreaId(new TileCoord(context.Map.MapId, 10, 2)));
        }

        [Test]
        public void TwoApcsInOpenSpace_FirstApcWinsAndSecondGetsOverlapWarning()
        {
            AreaTestContext context = AreaTestContext.CreateOpenFloor(_instantiated, origin: new Vector3(0, 0, 0), width: 6, height: 4);
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
                _instantiated,
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
        public void WallMountedApcOnPerimeter_FloodsInteriorTiles()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, origin: new Vector3(10, 0, 10), width: 5, height: 5);
            TestApc apc = context.PlaceApc(new Vector3(10, 0, 12), Direction.East);

            context.RebuildAll();

            AreaId areaId = context.GetApcAreaId(apc);
            Assert.IsFalse(areaId.IsNone);

            for (int x = 11; x <= 13; x++)
            {
                for (int z = 11; z <= 13; z++)
                {
                    TileCoord coord = new TileCoord(context.Map.MapId, x, z);
                    Assert.AreEqual(areaId.Value, context.GetAreaId(coord), $"Tile ({x},{z}) should belong to wall-mounted APC area.");
                }
            }

            Assert.AreEqual(areaId.Value, context.GetAreaId(apc.OriginTile));
        }

        [Test]
        public void WallMountedDevice_ResolvesAreaFromTileInFront()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, origin: new Vector3(10, 0, 10), width: 5, height: 5);
            TestApc apc = context.PlaceApc(new Vector3(10, 0, 12), Direction.East);
            context.RebuildAll();

            AreaId areaId = context.GetApcAreaId(apc);
            PlacedTileObject wallLight = CreateWallMountedDevice(new Vector2Int(10, 12), Direction.East);

            Assert.IsTrue(context.AreaSubSystem.TryGetAreaForDevice(wallLight, out AreaRecord record));
            Assert.AreEqual(areaId, record.Id);
        }

        [Test]
        public void WallMountedDevice_UsesTileInFrontWhenOriginHasAreaId()
        {
            AreaTestContext context = AreaTestContext.CreateTwoAdjacentRooms(_instantiated, new Vector3(0, 0, 0), roomSize: 5);
            TestApc leftApc = context.PlaceApc(new Vector3(4, 0, 2), Direction.West);
            TestApc rightApc = context.PlaceApc(new Vector3(5, 0, 2), Direction.East);
            context.RebuildAll();

            AreaId leftArea = context.GetApcAreaId(leftApc);
            AreaId rightArea = context.GetApcAreaId(rightApc);
            Assert.IsFalse(leftArea.IsNone);
            Assert.IsFalse(rightArea.IsNone);
            Assert.AreNotEqual(leftArea, rightArea);

            // Shared wall tile between the two rooms.
            // We'll force an area id onto the wall tile to simulate the ambiguous case.
            var wallMounted = CreateWallMountedDevice(new Vector2Int(4, 2), Direction.East);
            TileCoord origin = AreaDeviceTileResolver.GetOriginTile(wallMounted);
            Assert.IsTrue(context.Map.TrySetAreaId(origin, leftArea.Value));

            Assert.IsTrue(context.AreaSubSystem.TryGetAreaForDevice(wallMounted, out AreaRecord record));
            Assert.AreEqual(rightArea, record.Id, "Wall-mounted devices must resolve area from the tile in front, not the wall tile origin.");
        }

        [Test]
        public void WallMountedApcFacingIntoRoom_DoesNotFloodRoomBehindWall()
        {
            AreaTestContext context = AreaTestContext.CreateTwoAdjacentRooms(_instantiated, new Vector3(0, 0, 0), roomSize: 5);
            TestApc apc = context.PlaceApc(new Vector3(4, 0, 2), Direction.West);

            context.RebuildAll();

            AreaId areaId = context.GetApcAreaId(apc);
            Assert.IsFalse(areaId.IsNone);
            Assert.AreEqual(areaId.Value, context.GetAreaId(new TileCoord(context.Map.MapId, 2, 2)));
            Assert.AreEqual(AreaId.None, context.GetAreaId(new TileCoord(context.Map.MapId, 7, 2)));
        }

        [Test]
        public void WallMountedApcFacingAwayFromRoom_DoesNotFloodEitherRoom()
        {
            AreaTestContext context = AreaTestContext.CreateTwoAdjacentRooms(_instantiated, new Vector3(0, 0, 0), roomSize: 5);
            TestApc apc = context.PlaceApc(new Vector3(4, 0, 2), Direction.East);

            context.RebuildAll();

            Assert.AreEqual(AreaId.None, context.GetAreaId(new TileCoord(context.Map.MapId, 2, 2)));
            Assert.AreEqual(AreaId.None, context.GetAreaId(new TileCoord(context.Map.MapId, 7, 2)));
        }

        [Test]
        public void RoomWithoutApc_RemainsUnassigned()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, new Vector3(0, 0, 0), 4, 4);

            context.RebuildAll();

            Assert.AreEqual(AreaId.None, context.GetAreaId(new TileCoord(context.Map.MapId, 2, 2)));
        }

        [Test]
        public void ApcRemoved_ClearsClaimedTiles()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, new Vector3(0, 0, 0), 4, 4);
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
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, new Vector3(0, 0, 0), 4, 4);
            TestApc apc = context.PlaceApc(new Vector3(2, 0, 2));
            context.RebuildAll();
            context.AreaSubSystem.RenameArea(context.GetApcAreaId(apc), "Engineering");
            context.AreaSubSystem.SetParentTag(context.GetApcAreaId(apc), "eng");

            SavedTileMap saved = context.Map.Save();
            saved.savedAreas = context.AreaSubSystem.BuildSavedAreaRecords();

            TileMap loadedMap = TileMap.Create("LoadedAreaMap");
            _instantiated.Add(loadedMap.gameObject);
            loadedMap.Load(saved);

            var loadedQuery = new TileQueryService(loadedMap);
            var loadedAreaSubSystem = new AreaSubSystemHarness(loadedMap, loadedQuery);
            loadedAreaSubSystem.BeginTemplateRestore(saved.savedAreas);
            loadedAreaSubSystem.RestoreFromSave(saved.savedAreas);

            TileCoord origin = apc.OriginTile;
            Assert.AreEqual(context.GetAreaId(origin), GetRawAreaId(loadedMap, origin));
            Assert.AreEqual(1, loadedMap.LoadedAreaRecords.Count);
            Assert.AreEqual("Engineering", loadedMap.LoadedAreaRecords[0].displayName);
            Assert.AreEqual("eng", loadedMap.LoadedAreaRecords[0].parentTag);
        }

        [Test]
        public void TemplateRestore_WithRegisteredApc_PreservesSavedMetadata()
        {
            AreaTestContext context = AreaTestContext.CreateRoom(_instantiated, new Vector3(0, 0, 0), 4, 4);
            TestApc apc = context.PlaceApc(new Vector3(2, 0, 2));
            context.RebuildAll();
            AreaId areaId = context.GetApcAreaId(apc);
            context.AreaSubSystem.RenameArea(areaId, "Engineering");
            context.AreaSubSystem.SetParentTag(areaId, "eng");

            SavedAreaRecord[] saved = context.AreaSubSystem.BuildSavedAreaRecords();
            saved[0].lightingSwitchOn = false;

            TileMap loadedMap = TileMap.Create("LoadedApcMap");
            _instantiated.Add(loadedMap.gameObject);
            loadedMap.Load(context.Map.Save(), invokeMapLoadedEvent: false);
            loadedMap.SetLoadedAreaRecords(saved);

            var loadedQuery = new TileQueryService(loadedMap);
            var loadedAreaSubSystem = new AreaSubSystemHarness(loadedMap, loadedQuery);
            loadedAreaSubSystem.BeginTemplateRestore(saved);
            loadedAreaSubSystem.RestoreFromSave(saved);
            loadedAreaSubSystem.RegisterApc(apc);

            Assert.IsTrue(loadedAreaSubSystem.TryGetAreaForApc(apc, out AreaRecord record));
            Assert.AreEqual("Engineering", record.DisplayName);
            Assert.AreEqual("eng", record.ParentTag);
            Assert.IsFalse(record.LightingSwitchOn);
        }

        private static ushort GetRawAreaId(TileMap map, TileCoord coord)
        {
            map.TryGetAreaId(coord, out ushort areaId);
            return areaId;
        }

        private PlacedTileObject CreateWallMountedDevice(Vector2Int worldOrigin, Direction direction)
        {
            var go = new GameObject("WallLightTest");
            _instantiated.Add(go);
            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            // A wall-mounted device must have a TileObjectSo so PlacedTileObject.Layer can be evaluated.
            // We use WallMountHigh to force the resolver to look at the tile in front of the wall.
            TileObjectSo so = TileMapTestUtilities.CreateTileSo(TileLayer.WallMountHigh, "AreaTestWallMountDevice");
            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_dir", direction);
            SetPrivateField(placed, "_mapId", 0);
            return placed;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private sealed class TestApc : IAreaApcOrigin
        {
            public TileCoord OriginTile { get; init; }

            public Direction FacingDirection { get; init; } = Direction.North;

            public string DisplayName { get; init; } = "Test APC";

            public bool MultipleApcsInArea { get; private set; }

            public void SetMultipleApcsInArea(bool value) => MultipleApcsInArea = value;
        }

        private sealed class AreaTestContext
        {
            private readonly List<TestApc> _apcs = new();
            private readonly AreaSubSystemHarness _areaSubSystem;

            private AreaTestContext(TileMap map, TileQueryService query, ConstructionService construction, AreaSubSystemHarness areaSubSystem)
            {
                Map = map;
                Query = query;
                Construction = construction;
                _areaSubSystem = areaSubSystem;
            }

            public TileMap Map { get; }

            public TileQueryService Query { get; }

            public ConstructionService Construction { get; }

            public AreaSubSystemHarness AreaSubSystem => _areaSubSystem;

            public static AreaTestContext CreateRoom(List<GameObject> instantiated, Vector3 origin, int width, int height)
            {
                AreaTestContext context = CreateBase(instantiated);
                context.FillRoom(origin, width, height);
                return context;
            }

            public void AddRoom(Vector3 origin, int width, int height) => FillRoom(origin, width, height);

            public static AreaTestContext CreateOpenFloor(List<GameObject> instantiated, Vector3 origin, int width, int height)
            {
                AreaTestContext context = CreateBase(instantiated);

                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                        PlacePlenum(context, origin + new Vector3(x, 0, z));
                }

                return context;
            }

            public static AreaTestContext CreateTwoRoomsWithDoor(List<GameObject> instantiated, Vector3 leftOrigin, Vector3 rightOrigin, int roomSize)
            {
                AreaTestContext context = CreateBase(instantiated);
                Vector3 doorPosition = new Vector3(leftOrigin.x + roomSize - 1, 0, leftOrigin.z + roomSize / 2);

                context.BuildRoom(leftOrigin, roomSize, doorPosition);
                context.BuildRoom(rightOrigin, roomSize, Vector3.positiveInfinity);

                PlacePlenum(context, doorPosition);
                PlaceTurf(context, doorPosition, TileObjectGenericType.Door);

                return context;
            }

            public static AreaTestContext CreateTwoAdjacentRooms(List<GameObject> instantiated, Vector3 leftOrigin, int roomSize)
            {
                AreaTestContext context = CreateRoom(instantiated, leftOrigin, roomSize, roomSize);
                context.AddRoom(leftOrigin + new Vector3(roomSize, 0, 0), roomSize, roomSize);
                return context;
            }

            public TestApc PlaceApc(Vector3 position, Direction facingDirection = Direction.North)
            {
                TileCoord coord = Query.WorldToTile(position, Map.MapId);
                var apc = new TestApc
                {
                    OriginTile = coord,
                    FacingDirection = facingDirection,
                    DisplayName = $"APC {coord.Grid.x},{coord.Grid.y}",
                };
                _apcs.Add(apc);
                _areaSubSystem.RegisterApc(apc);
                return apc;
            }

            public void RebuildAll() => _areaSubSystem.RebuildAllAreasFromApcs();

            public void PlaceOpenFloorTile(Vector3 position) => PlacePlenum(this, position);

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

            private static AreaTestContext CreateBase(List<GameObject> instantiated)
            {
                TileMapTestUtilities.EnsureTestAssetsRegistered();
                TileMap map = TileMap.Create("AreaFloodFillTests");
                instantiated.Add(map.gameObject);
                var query = new TileQueryService(map);
                var construction = new ConstructionService(map, query);
                var areaSubSystem = new AreaSubSystemHarness(map, query);
                return new AreaTestContext(map, query, construction, areaSubSystem);
            }

            private void FillRoom(Vector3 origin, int width, int height)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int z = 0; z < height; z++)
                    {
                        Vector3 position = origin + new Vector3(x, 0, z);
                        bool isPerimeter = x == 0 || z == 0 || x == width - 1 || z == height - 1;
                        PlacePlenum(this, position);
                        if (isPerimeter)
                            PlaceTurf(this, position, TileObjectGenericType.Wall);
                    }
                }
            }

            private void BuildRoom(Vector3 origin, int size, Vector3 doorToSkip)
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

                        PlacePlenum(this, position);
                        if (isPerimeter)
                            PlaceTurf(this, position, TileObjectGenericType.Wall);
                    }
                }
            }

            private static void PlacePlenum(AreaTestContext context, Vector3 position)
            {
                TileObjectSo plenumSo = TileMapTestUtilities.CreateTileSo(TileLayer.Plenum, "AreaTestPlenum");
                PlaceResult result = context.Construction.TryPlaceTile(plenumSo, position, Direction.North, replaceExisting: false);
                Assert.IsTrue(result.Success, $"Expected plenum placement at {position}.");
            }

            private static void PlaceTurf(AreaTestContext context, Vector3 position, TileObjectGenericType genericType)
            {
                TileObjectSo turfSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, $"AreaTest_{genericType}");
                turfSo.genericType = genericType;
                bool success = context.Map.PlaceTileObject(
                    turfSo,
                    position,
                    Direction.North,
                    skipBuildCheck: true,
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
            private Dictionary<Vector3, SavedAreaRecord> _pendingSavedByApcPosition;
            private bool _templateRestoreActive;
            private bool _deferAreaFlood;

            public AreaSubSystemHarness(TileMap map, ITileQueryService query)
            {
                _map = map;
                _query = query;
                _floodFill = new AreaFloodFillService(map, query);
            }

            public void HandleMapLoaded()
            {
                if (_templateRestoreActive)
                {
                    if (_map.LoadedAreaRecords.Count > 0)
                    {
                        RestoreFromSave(_map.LoadedAreaRecords);
                    }

                    return;
                }

                if (_registeredApcs.Count > 0)
                {
                    RebuildAllAreasFromApcs();
                    return;
                }

                if (_map.LoadedAreaRecords.Count > 0)
                    RestoreFromSave(_map.LoadedAreaRecords);
            }

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
                    _pendingSavedByApcPosition[saved.apcWorldPosition] = saved;
                }
            }

            public void RestoreFromSave(IReadOnlyList<SavedAreaRecord> savedAreas)
            {
                if (savedAreas == null || savedAreas.Count == 0)
                {
                    return;
                }

                RestoreRegistryFromSave(savedAreas);
                LinkRegisteredApcsDuringTemplateRestore();
            }

            public bool TryGetAreaForApc(IAreaApcOrigin apc, out AreaRecord record)
            {
                record = null;
                return _registry.TryGetApcArea(apc, out AreaId areaId) && _registry.TryGet(areaId, out record);
            }

            public bool TryGetAreaForDevice(PlacedTileObject tileObject, out AreaRecord record)
            {
                record = null;
                if (tileObject == null)
                {
                    return false;
                }

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

            private bool TryGetAreaForTile(TileCoord coord, out AreaRecord record)
            {
                record = null;
                if (!_map.TryGetAreaId(coord, out ushort areaId))
                {
                    return false;
                }

                return _registry.TryGet(new AreaId(areaId), out record);
            }

            public void RegisterApc(IAreaApcOrigin apc)
            {
                if (apc == null || _registeredApcs.Contains(apc))
                    return;

                _registeredApcs.Add(apc);

                if (_templateRestoreActive && TryLinkApcDuringTemplateRestore(apc))
                {
                    UpdateOverlapWarnings();
                    return;
                }

                if (_deferAreaFlood)
                {
                    return;
                }

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

            public void BeginDeferredAreaFlood()
            {
                _deferAreaFlood = true;
            }

            public void EndDeferredAreaFlood()
            {
                if (!_deferAreaFlood)
                {
                    return;
                }

                _deferAreaFlood = false;

                if (_registeredApcs.Count == 0)
                {
                    return;
                }

                bool anyLinked = false;
                foreach (IAreaApcOrigin apc in _registeredApcs)
                {
                    if (_registry.TryGetApcArea(apc, out _))
                    {
                        anyLinked = true;
                        break;
                    }
                }

                if (anyLinked)
                {
                    RefloodAllAreaTilesPreservingMetadata();
                }
                else
                {
                    RebuildAllAreasFromApcs();
                }
            }

            public void RefloodAllAreaTilesPreservingMetadata()
            {
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
                        areaId = _registry.AllocateId();
                        _registry.Register(new AreaRecord
                        {
                            Id = areaId,
                            DisplayName = apc.DisplayName,
                            Apc = apc,
                        });
                    }

                    _floodFill.FloodFromApc(apc, areaId, claimedTiles);
                }

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
                            lightingSwitchOn = record.LightingSwitchOn,
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
                        LightingSwitchOn = saved.lightingSwitchOn,
                    });
                }

                _registry.EnsureNextIdAbove(highestId);
            }

            private bool TryLinkApcDuringTemplateRestore(IAreaApcOrigin apc)
            {
                if (_pendingSavedByApcPosition == null)
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

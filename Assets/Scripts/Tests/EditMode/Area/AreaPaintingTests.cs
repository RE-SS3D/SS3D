using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Area;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Electricity;
using SS3D.Systems.Persistence;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using UnityEngine;

namespace EditorTests
{
    public class AreaPaintingTests
    {
        private readonly List<GameObject> _objects = new();
        private TileMapTestUtilities.MapContext _context;
        private AreaRegistry _registry;
        private List<IAreaApcOrigin> _apcs;
        private AreaPaintingService _painting;

        [SetUp]
        public void SetUp()
        {
            TileMapTestUtilities.EnsureTestAssetsRegistered();
            _context = TileMapTestUtilities.CreateContext(_objects);
            _registry = new AreaRegistry();
            _apcs = new List<IAreaApcOrigin>();
            _painting = new AreaPaintingService(_context.Map, _context.Query, _registry, _apcs);
            for (int x = 0; x < 4; x++) TileMapTestUtilities.PlacePlenum(_context, new Vector3(x, 0, 0));
            _registry.Register(new AreaRecord { Id = new AreaId(1), DisplayName = "Engineering", ParentTag = "Engineering" });
            _registry.Register(new AreaRecord { Id = new AreaId(2), DisplayName = "Vault", ParentTag = "Command", DefaultRequiredAccess = new AccessMask(AccessLevel.Armory) });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private ushort At(int x)
        {
            _context.Map.TryGetAreaId(new TileCoord(_context.Map.MapId, x, 0), out ushort id);
            return id;
        }

        [Test]
        public void StrokeTransfersMembershipAndUndoesAsOneEdit()
        {
            _context.Map.TrySetAreaId(new TileCoord(_context.Map.MapId, 0, 0), 1);
            Assert.IsTrue(_painting.TryPaint(2, new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.zero }, out var stroke, out string error), error);
            Assert.AreEqual(2, At(0));
            Assert.AreEqual(2, At(1));
            Assert.IsTrue(_painting.HasAuthoredAreas);
            stroke.Undo();
            Assert.AreEqual(1, At(0));
            Assert.AreEqual(0, At(1));
            Assert.IsFalse(_painting.HasAuthoredAreas);
            stroke.Redo();
            Assert.AreEqual(2, At(0));
            Assert.AreEqual(2, At(1));
        }

        [Test]
        public void DeletingLastTileRetiresRoomAndUndoRestoresMetadata()
        {
            _painting.TryPaint(2, new[] { Vector2Int.zero }, out _, out _);
            Assert.IsTrue(_painting.TryPaint(0, new[] { Vector2Int.zero }, out var erase, out _));
            Assert.AreEqual(0, At(0));
            Assert.IsFalse(_registry.TryGet(new AreaId(2), out _));
            erase.Undo();
            Assert.AreEqual(2, At(0));
            Assert.IsTrue(_registry.TryGet(new AreaId(2), out AreaRecord room));
            Assert.AreEqual("Vault", room.DisplayName);
            Assert.AreEqual(new AccessMask(AccessLevel.Armory), room.DefaultRequiredAccess);
            erase.Redo();
            Assert.IsFalse(_registry.TryGet(new AreaId(2), out _));
        }

        [Test]
        public void PartialEraseKeepsRoomAndReplacingLastTileRetiresIt()
        {
            _painting.TryPaint(1, new[] { Vector2Int.zero, Vector2Int.right }, out _, out _);
            _painting.TryPaint(0, new[] { Vector2Int.zero }, out _, out _);
            Assert.IsTrue(_registry.TryGet(new AreaId(1), out _));
            _painting.TryPaint(2, new[] { Vector2Int.right }, out var replacement, out _);
            Assert.IsFalse(_registry.TryGet(new AreaId(1), out _));
            replacement.Undo();
            Assert.IsTrue(_registry.TryGet(new AreaId(1), out _));
            Assert.AreEqual(1, At(1));
        }

        [Test]
        public void EmptyAuthoredMapRetainsItsModeThroughPersistenceAndLegacyMigration()
        {
            _registry.Unregister(new AreaId(1));
            _painting.TryPaint(2, new[] { Vector2Int.zero }, out _, out _);
            _painting.TryPaint(0, new[] { Vector2Int.zero }, out _, out _);
            SavedAreaChunkPayload payload = new() { authored = _painting.HasAuthoredAreas, records = _painting.CaptureRecords() };
            SavedAreaChunkPayload loaded = JsonUtility.FromJson<SavedAreaChunkPayload>(JsonUtility.ToJson(payload));
            Assert.IsTrue(loaded.authored);
            Assert.IsEmpty(loaded.records);
            _painting.RestoreRecords(loaded.records, loaded.authored);
            Assert.IsTrue(_painting.HasAuthoredAreas);
            SavedTileMap legacy = new() { mapName = "Empty authored", hasAuthoredAreas = true, savedAreas = loaded.records };
            var envelope = LegacyTileMapMigrator.BuildEnvelope(legacy, legacy.mapName);
            var areaChunk = envelope.chunks.Single(chunk => chunk.contributorId == AreaPersistenceContributor.ContributorIdValue);
            Assert.IsTrue(JsonUtility.FromJson<SavedAreaChunkPayload>(areaChunk.payloadJson).authored);
        }

        [Test]
        public void AmbienceOverrideSurvivesSaveAndLoad()
        {
            Assert.IsTrue(_registry.TryGet(new AreaId(2), out AreaRecord vault));
            vault.AmbienceProfileId = "morgue";
            _painting.TryPaint(2, new[] { Vector2Int.zero }, out _, out _);

            SavedAreaChunkPayload payload = new() { authored = true, records = _painting.CaptureRecords() };
            SavedAreaChunkPayload loaded = JsonUtility.FromJson<SavedAreaChunkPayload>(JsonUtility.ToJson(payload));
            _painting.RestoreRecords(loaded.records, loaded.authored);

            Assert.IsTrue(_registry.TryGet(new AreaId(2), out AreaRecord restored));
            Assert.AreEqual("morgue", restored.AmbienceProfileId);
        }

        [TestCase("ENGINEERING", "Engineering")]
        [TestCase("Civilian", "")]
        [TestCase("eng", "")]
        public void SavedDepartmentLoadsAsAListedOneOrNone(string saved, string expected)
        {
            _painting.RestoreRecords(new[] { new SavedAreaRecord { id = 5, displayName = "Bay", parentTag = saved } });

            Assert.IsTrue(_registry.TryGet(new AreaId(5), out AreaRecord room));
            Assert.AreEqual(expected, room.ParentTag);
        }

        [Test]
        public void CenteringCameraPreservesAngleAndZoom()
        {
            GameObject go = new("Room camera test");
            _objects.Add(go);
            Camera camera = go.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 10, -10);
            MapEditorSession session = new();
            session.Enter(camera, camera.transform.position);
            try
            {
                session.RotateStep(30);
                session.ZoomStep(4);
                Plane floor = new(Vector3.up, 0);
                Ray before = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                Assert.IsTrue(floor.Raycast(before, out float beforeDistance));
                float orbitDistance = Vector3.Distance(camera.transform.position, before.GetPoint(beforeDistance));
                Quaternion rotation = camera.transform.rotation;
                session.CenterOn(new Vector3(30, 8, 40));
                Ray after = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                Assert.IsTrue(floor.Raycast(after, out float afterDistance));
                Assert.Less(Vector3.Distance(new Vector3(30, 0, 40), after.GetPoint(afterDistance)), 0.001f);
                Assert.Less(Quaternion.Angle(rotation, camera.transform.rotation), 0.001f);
                Assert.AreEqual(orbitDistance, Vector3.Distance(camera.transform.position, after.GetPoint(afterDistance)), 0.001f);
            }
            finally { session.Exit(); }
        }

        [Test]
        public void PaintingSkipsWallsAndSpaceAndRejectsUnknownRoom()
        {
            TileMapTestUtilities.PlaceAirtightWall(_context, Vector3.right);
            Assert.IsTrue(_painting.TryPaint(2, new[] { Vector2Int.zero, Vector2Int.right, new Vector2Int(100, 100) }, out _, out _));
            Assert.AreEqual(2, At(0));
            Assert.AreEqual(0, At(1));
            Assert.IsFalse(_painting.TryPaint(999, new[] { Vector2Int.zero }, out _, out _));
        }

        [Test]
        public void ApcMovesBetweenRoomsAndRemovalPreservesPaintedTiles()
        {
            TestApc apc = new() { OriginTile = new TileCoord(_context.Map.MapId, 0, 0) };
            _apcs.Add(apc);
            Assert.IsTrue(_painting.TryPaint(1, new[] { Vector2Int.zero }, out _, out _));
            Assert.IsTrue(_registry.TryGetApcArea(apc, out AreaId first));
            Assert.AreEqual(1, first.Value);
            Assert.IsTrue(_painting.TryPaint(2, new[] { Vector2Int.zero }, out var transfer, out _));
            Assert.IsTrue(_registry.TryGetApcArea(apc, out AreaId second));
            Assert.AreEqual(2, second.Value);
            transfer.Undo();
            Assert.IsTrue(_registry.TryGetApcArea(apc, out AreaId restored));
            Assert.AreEqual(1, restored.Value);
            _apcs.Clear();
            _painting.RefreshApcLinks();
            Assert.AreEqual(1, At(0));
            Assert.IsTrue(_registry.TryGet(new AreaId(1), out AreaRecord room));
            Assert.IsNull(room.Apc);
        }

        [Test]
        public void StrokeCannotMergeTwoApcsIntoOneRoomAndIsAtomic()
        {
            _apcs.Add(new TestApc { OriginTile = new TileCoord(_context.Map.MapId, 0, 0) });
            _apcs.Add(new TestApc { OriginTile = new TileCoord(_context.Map.MapId, 1, 0) });
            Assert.IsFalse(_painting.TryPaint(2, new[] { Vector2Int.zero, Vector2Int.right }, out _, out string error));
            StringAssert.Contains("one APC", error);
            Assert.AreEqual(0, At(0));
            Assert.AreEqual(0, At(1));
            Assert.IsFalse(_painting.HasAuthoredAreas);
        }

        [Test]
        public void AuthoredRoomWithoutApcRoundTripsMetadataAndChunkMembership()
        {
            _painting.TryPaint(2, new[] { Vector2Int.zero, new Vector2Int(3, 0) }, out _, out _);
            SavedTileMap saved = _context.Map.Save();
            saved.savedAreas = _painting.CaptureRecords();
            SavedTileMap decoded = JsonUtility.FromJson<SavedTileMap>(JsonUtility.ToJson(saved));
            Assert.IsTrue(decoded.savedAreas.All(room => room.authored));
            Assert.IsTrue(decoded.savedAreas.All(room => room.noApc));
            TileMap loaded = TileMap.Create("PaintedReload");
            _objects.Add(loaded.gameObject);
            loaded.Load(decoded, invokeMapLoadedEvent: false);
            AreaRegistry registry = new();
            AreaPaintingService restored = new(loaded, new TileQueryService(loaded), registry, new List<IAreaApcOrigin>());
            restored.RestoreRecords(decoded.savedAreas);
            Assert.IsTrue(restored.HasAuthoredAreas);
            Assert.IsTrue(registry.TryGet(new AreaId(2), out AreaRecord room));
            Assert.AreEqual("Vault", room.DisplayName);
            Assert.AreEqual(new AccessMask(AccessLevel.Armory), room.DefaultRequiredAccess);
            loaded.TryGetAreaId(new TileCoord(loaded.MapId, 0, 0), out ushort first);
            loaded.TryGetAreaId(new TileCoord(loaded.MapId, 3, 0), out ushort last);
            Assert.AreEqual(2, first);
            Assert.AreEqual(2, last);
        }

        [Test]
        public void LegacyMetadataKeepsApcDefaultMode()
        {
            SavedAreaRecord legacy = JsonUtility.FromJson<SavedAreaRecord>("{\"id\":1,\"displayName\":\"Legacy room\"}");
            _painting.RestoreRecords(new[] { legacy });
            Assert.IsFalse(_painting.HasAuthoredAreas);
            Assert.IsFalse(legacy.noApc);
        }

        [Test]
        public void CreatedRoomNamesAreNumberedAndAvoidExistingNames()
        {
            Assert.AreEqual("Room 1", _painting.NextRoomName());
            _registry.Register(new AreaRecord { Id = new AreaId(3), DisplayName = "Room 1" });
            Assert.AreEqual("Room 2", _painting.NextRoomName());
            _registry.Register(new AreaRecord { Id = new AreaId(4), DisplayName = "room 2" });
            Assert.AreEqual("Room 3", _painting.NextRoomName());
        }

        [Test]
        public void WallMountedApcUsesFacingRoomEvenWithoutAWall()
        {
            TileObjectSo mount = TileMapTestUtilities.CreateTileSo(TileLayer.WallMountHigh, "MountedApcTest");
            Assert.IsTrue(_context.Map.PlaceTileObject(mount, Vector3.zero, Direction.East,
                skipBuildCheck: true, replaceExisting: false, skipAdjacency: true, out GameObject go));
            TestApc apc = new()
            {
                OriginTile = new TileCoord(_context.Map.MapId, 0, 0), FacingDirection = Direction.East,
                TileObject = go.GetComponent<PlacedTileObject>(),
            };
            _apcs.Add(apc);
            _context.Map.TrySetAreaId(new TileCoord(_context.Map.MapId, 0, 0), 1);
            _context.Map.TrySetAreaId(new TileCoord(_context.Map.MapId, 1, 0), 2);
            _painting.RefreshApcLinks();
            Assert.AreEqual(new TileCoord(_context.Map.MapId, 1, 0), _painting.GetApcMembershipTile(apc));
            Assert.IsTrue(_registry.TryGetApcArea(apc, out AreaId room));
            Assert.AreEqual(2, room.Value);
            Assert.AreEqual(new TileCoord(_context.Map.MapId, 0, 0), apc.OriginTile,
                "Wiring and persistence still use the physical origin.");
        }

        [Test]
        public void LoadingOldAuthoredWallMembershipRemovesPhantomRoom()
        {
            TileMapTestUtilities.PlaceAirtightWall(_context, Vector3.zero);
            _context.Map.TrySetAreaId(new TileCoord(_context.Map.MapId, 0, 0), 1);
            _context.Map.TrySetAreaId(new TileCoord(_context.Map.MapId, 1, 0), 2);
            _painting.RestoreRecords(new[]
            {
                new SavedAreaRecord { id = 1, authored = true, displayName = "APC wall only" },
                new SavedAreaRecord { id = 2, authored = true, noApc = true, displayName = "Room 1" },
            });
            Assert.AreEqual(0, At(0));
            Assert.AreEqual(2, At(1));
            Assert.IsFalse(_registry.TryGet(new AreaId(1), out _));
            Assert.IsTrue(_registry.TryGet(new AreaId(2), out _));
        }

        [Test]
        public void StrokeGeometryIncludesEndpointsAndRejectsOversizedRectangles()
        {
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), Vector2Int.zero },
                MapEditorSubSystem.BuildAreaStroke(Vector2Int.zero, new Vector2Int(-1, -1), true));
            Vector2Int[] line = MapEditorSubSystem.BuildAreaStroke(Vector2Int.zero, new Vector2Int(3, 2), false);
            Assert.AreEqual(Vector2Int.zero, line[0]);
            Assert.AreEqual(new Vector2Int(3, 2), line[line.Length - 1]);
            Assert.IsEmpty(MapEditorSubSystem.BuildAreaStroke(Vector2Int.zero, new Vector2Int(1000, 1000), true));
        }

        private sealed class TestApc : IAreaApcOrigin, IElectricDevice
        {
            public TileCoord OriginTile { get; set; }
            public Direction FacingDirection { get; set; } = Direction.North;
            public PlacedTileObject TileObject { get; set; }
            public string DisplayName => "APC";
            public void SetMultipleApcsInArea(bool value) { }
        }
    }
}

using EditorTests;
using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public sealed class BuildCheckerPreviewTests
    {
        private readonly List<GameObject> _instantiated = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _instantiated.Count - 1; i >= 0; i--)
            {
                if (_instantiated[i] != null)
                    Object.DestroyImmediate(_instantiated[i]);
            }

            _instantiated.Clear();
        }

        // A sits west of B. A mount's direction is the side it sticks out of, into the next cell.
        private static readonly Vector3 CellA = new(20, 0, 20);
        private static readonly Vector3 CellB = new(21, 0, 20);

        [Test]
        public void Preview_WallFacingNeighbourMount_FailsWithNeighbourMountBlocks()
        {
            TileMapTestUtilities.MapContext context = WallBesideMountedWall(Direction.West);

            PreviewResult preview = context.Construction.TryPreviewTile(
                CreateWall("WallIntoMount"), CellA, Direction.North, replaceExisting: false);

            Assert.IsFalse(preview.CanBuild);
            CollectionAssert.Contains(preview.Failures, BuildFailReason.NeighbourWallMountBlocksWall);
        }

        [Test]
        public void Preview_WallBesideMountFacingAway_Succeeds()
        {
            TileMapTestUtilities.MapContext context = WallBesideMountedWall(Direction.East);

            PreviewResult preview = context.Construction.TryPreviewTile(
                CreateWall("WallBesideMount"), CellA, Direction.North, replaceExisting: false);

            Assert.IsTrue(preview.CanBuild, string.Join(", ", preview.Failures));
        }

        [Test]
        public void Preview_ReplaceWallInMountedRow_Succeeds()
        {
            // A wall row where B carries a mount facing away from A: swapping A's wall must not be refused.
            TileMapTestUtilities.MapContext context = WallBesideMountedWall(Direction.North);
            TileMapTestUtilities.PlaceAirtightWall(context, CellA);

            PreviewResult preview = context.Construction.TryPreviewTile(
                CreateWall("ReinforcedRowWall"), CellA, Direction.North, replaceExisting: true);

            Assert.IsTrue(preview.CanBuild, string.Join(", ", preview.Failures));
        }

        [Test]
        public void Preview_MountFacingNeighbourWall_FailsWithBlockedByNeighbourWall()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, CellA);
            TileMapTestUtilities.PlacePlenum(context, CellB);
            TileMapTestUtilities.PlaceAirtightWall(context, CellA);
            TileMapTestUtilities.PlaceAirtightWall(context, CellB);

            TileObjectSo mount = TileMapTestUtilities.CreateTileSo(TileLayer.WallMountHigh, "MountIntoWall");

            PreviewResult intoWall = context.Construction.TryPreviewTile(mount, CellB, Direction.West, replaceExisting: false);
            PreviewResult away = context.Construction.TryPreviewTile(mount, CellB, Direction.East, replaceExisting: false);

            Assert.IsFalse(intoWall.CanBuild);
            CollectionAssert.Contains(intoWall.Failures, BuildFailReason.WallMountBlockedByNeighbourWall);
            Assert.IsTrue(away.CanBuild, string.Join(", ", away.Failures));
        }

        // Playtest bug "Noclipping doors": a door or wall built on the builder's own tile spawned its
        // colliders on the body, which got pushed into the next wall and could walk out anywhere.
        [TestCase(TileObjectGenericType.Wall)]
        [TestCase(TileObjectGenericType.Door)]
        public void Preview_SolidTurfOnABody_FailsWithBodyInTheWay(TileObjectGenericType type)
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, CellA);
            context.Map.IsBodyInCell = cell => cell == CellA;

            TileObjectSo solid = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "SolidOnBody");
            solid.genericType = type;
            PreviewResult preview = context.Construction.TryPreviewTile(solid, CellA, Direction.North, replaceExisting: false);

            Assert.IsFalse(preview.CanBuild);
            CollectionAssert.Contains(preview.Failures, BuildFailReason.BodyInTheWay);
        }

        [Test]
        public void Preview_FloorUnderABody_Succeeds()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, CellA);
            context.Map.IsBodyInCell = cell => cell == CellA;

            TileObjectSo floor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "FloorUnderBody");
            floor.genericType = TileObjectGenericType.Floor;
            PreviewResult preview = context.Construction.TryPreviewTile(floor, CellA, Direction.North, replaceExisting: false);

            Assert.IsTrue(preview.CanBuild, string.Join(", ", preview.Failures));
        }

        [Test]
        public void Preview_WallBesideABody_Succeeds()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, CellA);
            context.Map.IsBodyInCell = cell => cell == CellB;

            PreviewResult preview = context.Construction.TryPreviewTile(
                CreateWall("WallBesideBody"), CellA, Direction.North, replaceExisting: false);

            Assert.IsTrue(preview.CanBuild, string.Join(", ", preview.Failures));
        }

        private TileMapTestUtilities.MapContext WallBesideMountedWall(Direction mountFacing)
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, CellA);
            TileMapTestUtilities.PlacePlenum(context, CellB);
            TileMapTestUtilities.PlaceAirtightWall(context, CellB);

            TileObjectSo mount = TileMapTestUtilities.CreateTileSo(TileLayer.WallMountHigh, "NeighbourMount");
            Assert.IsTrue(context.Map.PlaceTileObject(mount, CellB, mountFacing,
                skipBuildCheck: true, replaceExisting: false, skipAdjacency: true, out GameObject _));
            return context;
        }

        private static TileObjectSo CreateWall(string name)
        {
            TileObjectSo wall = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, name);
            wall.genericType = TileObjectGenericType.Wall;
            return wall;
        }

        [Test]
        public void Preview_FloorWithoutPlenum_FailsWithMissingPlenum()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileObjectSo floor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "SteelFloor");
            floor.genericType = TileObjectGenericType.Floor;

            PreviewResult preview = context.Construction.TryPreviewTile(
                floor, new Vector3(3, 0, 3), Direction.North, replaceExisting: false);

            Assert.IsFalse(preview.CanBuild);
            CollectionAssert.Contains(preview.Failures, BuildFailReason.MissingOrInvalidPlenum);
            Assert.AreEqual("Needs a plenum or catwalk underneath", preview.PrimaryMessage);
        }

        [Test]
        public void Preview_FloorWithPlenum_Succeeds()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(4, 0, 4);
            TileMapTestUtilities.PlacePlenum(context, pos);

            TileObjectSo floor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "SteelFloor");
            floor.genericType = TileObjectGenericType.Floor;

            PreviewResult preview = context.Construction.TryPreviewTile(floor, pos, Direction.North, replaceExisting: false);

            Assert.IsTrue(preview.CanBuild);
            Assert.IsEmpty(preview.Failures);
        }

        [Test]
        public void Preview_OccupiedLayerWithoutReplace_FailsWithLayerOccupied()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new(5, 0, 5);
            TileMapTestUtilities.PlacePlenum(context, pos);

            TileObjectSo floor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "SteelFloor");
            floor.genericType = TileObjectGenericType.Floor;
            Assert.IsTrue(context.Construction.TryPlaceTile(floor, pos, Direction.North, replaceExisting: false).Success);

            TileObjectSo otherFloor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "WoodFloor");
            otherFloor.genericType = TileObjectGenericType.Floor;

            PreviewResult preview = context.Construction.TryPreviewTile(
                otherFloor, pos, Direction.North, replaceExisting: false);

            Assert.IsFalse(preview.CanBuild);
            CollectionAssert.Contains(preview.Failures, BuildFailReason.LayerOccupied);
        }

        [Test]
        public void Preview_ReplaceOnOccupiedLayer_WithoutPlenumStillFails()
        {
            ITileLocation[] locations = CreateEmptyLocations();
            TileObjectSo floor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "SteelFloor");
            floor.genericType = TileObjectGenericType.Floor;

            // Occupy turf without a valid plenum underneath.
            var turf = (SingleTileLocation)locations[(int)TileLayer.Turf];
            GameObject go = new("ExistingFloor");
            _instantiated.Add(go);
            turf.PlacedObject = go.AddComponent<PlacedTileObject>();

            BuildFailReason[] failures = BuildChecker.Evaluate(
                locations, floor, Direction.North, Vector3.zero, new PlacedTileObject[8], replaceExisting: true);

            CollectionAssert.Contains(failures, BuildFailReason.MissingOrInvalidPlenum);
            CollectionAssert.DoesNotContain(failures, BuildFailReason.LayerOccupied);
        }

        [Test]
        public void Format_KnownReasons_AreNonEmpty()
        {
            foreach (BuildFailReason reason in System.Enum.GetValues(typeof(BuildFailReason)))
                Assert.IsFalse(string.IsNullOrWhiteSpace(BuildFailMessages.Format(reason)));
        }

        private static ITileLocation[] CreateEmptyLocations()
        {
            TileLayer[] layers = TileHelper.GetTileLayers();
            var locations = new ITileLocation[layers.Length];
            foreach (TileLayer layer in layers)
                locations[(int)layer] = TileHelper.CreateTileLocation(layer, 0, 0);
            return locations;
        }
    }
}

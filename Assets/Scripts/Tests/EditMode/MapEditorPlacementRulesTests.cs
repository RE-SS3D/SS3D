using EditorTests;
using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// The map editor's placement rules: auto subfloor, paint-over, silent repeats, door facing and the room tool's cells.
    /// </summary>
    public sealed class MapEditorPlacementRulesTests
    {
        private readonly List<GameObject> _instantiated = new();
        private readonly Dictionary<string, GenericObjectSo> _assets = new();

        private static readonly Vector3 Cell = new(30, 0, 30);

        [TearDown]
        public void TearDown()
        {
            for (int i = _instantiated.Count - 1; i >= 0; i--)
            {
                if (_instantiated[i] != null)
                    Object.DestroyImmediate(_instantiated[i]);
            }

            _instantiated.Clear();
            _assets.Clear();
        }

        [TestCase(TurfKind.Floor, TurfKind.Floor, true)]
        [TestCase(TurfKind.Floor, TurfKind.Wall, false)]
        [TestCase(TurfKind.Floor, TurfKind.Door, false)]
        [TestCase(TurfKind.Wall, TurfKind.Floor, true)]
        [TestCase(TurfKind.Wall, TurfKind.Wall, true)]
        [TestCase(TurfKind.Wall, TurfKind.Door, false)]
        [TestCase(TurfKind.Door, TurfKind.Floor, true)]
        [TestCase(TurfKind.Door, TurfKind.Wall, true)]
        [TestCase(TurfKind.Door, TurfKind.Door, true)]
        [TestCase(TurfKind.None, TurfKind.Floor, false)]
        public void PaintsOver_FloorsRepaintFloors_StructureGoesOverFloors_DoorsAreKept(
            TurfKind placing, TurfKind existing, bool expected)
        {
            Assert.AreEqual(expected, MapEditorPlacementRules.PaintsOver(placing, existing));
        }

        [TestCase(TurfKind.Wall, TurfKind.None, true)]
        [TestCase(TurfKind.Floor, TurfKind.None, true)]
        [TestCase(TurfKind.Wall, TurfKind.Floor, true)]
        [TestCase(TurfKind.Wall, TurfKind.Wall, false)]
        [TestCase(TurfKind.Wall, TurfKind.Door, false)]
        [TestCase(TurfKind.Floor, TurfKind.Floor, false)]
        [TestCase(TurfKind.Floor, TurfKind.Wall, false)]
        public void RoomBuildsOver_OnlyAdds(TurfKind placing, TurfKind existing, bool expected)
        {
            Assert.AreEqual(expected, MapEditorPlacementRules.RoomBuildsOver(placing, existing));
        }

        [Test]
        public void FloorInSpace_GetsASubfloor()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileObjectSo floor = Tile(TileLayer.Turf, "RulesFloor", TileObjectGenericType.Floor);

            PlacementPlan plan = MapEditorPlacementRules.Plan(context.Map, floor, Cell, Direction.North, false, autoSubfloor: true);

            Assert.IsTrue(plan.CanPlace, string.Join(", ", plan.Failures));
            Assert.AreEqual(1, plan.Subfloor.Count);
            Assert.AreEqual(Cell, plan.Subfloor[0].Position);
            Assert.IsFalse(plan.Subfloor[0].ReplacesGrating);
        }

        [Test]
        public void FloorInSpace_WithAutoSubfloorOff_StillNeedsAPlenum()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileObjectSo floor = Tile(TileLayer.Turf, "RulesFloorNoAuto", TileObjectGenericType.Floor);

            PlacementPlan plan = MapEditorPlacementRules.Plan(context.Map, floor, Cell, Direction.North, false, autoSubfloor: false);

            Assert.AreEqual(PlacementOutcome.Blocked, plan.Outcome);
            CollectionAssert.Contains(plan.Failures, BuildFailReason.TurfNeedsPlenum);
        }

        [Test]
        public void WallOnACatwalk_SwapsTheCatwalkForASubfloor()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            Place(context, Tile(TileLayer.Plenum, "Catwalk", TileObjectGenericType.Plenum), Cell);
            TileObjectSo wall = Tile(TileLayer.Turf, "RulesWall", TileObjectGenericType.Wall);

            PlacementPlan plan = MapEditorPlacementRules.Plan(context.Map, wall, Cell, Direction.North, false, autoSubfloor: true);

            Assert.IsTrue(plan.CanPlace, string.Join(", ", plan.Failures));
            Assert.AreEqual(1, plan.Subfloor.Count);
            Assert.IsTrue(plan.Subfloor[0].ReplacesGrating);
        }

        [Test]
        public void CableOnACatwalk_LeavesTheCatwalk()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            Place(context, Tile(TileLayer.Plenum, "Catwalk", TileObjectGenericType.Plenum), Cell);
            TileObjectSo cable = Tile(TileLayer.Wire, "RulesCable", TileObjectGenericType.Cable);

            PlacementPlan plan = MapEditorPlacementRules.Plan(context.Map, cable, Cell, Direction.North, false, autoSubfloor: true);

            Assert.IsTrue(plan.CanPlace, string.Join(", ", plan.Failures));
            Assert.AreEqual(0, plan.Subfloor.Count);
        }

        [Test]
        public void WallMountInSpace_GetsNoSubfloor()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileObjectSo mount = Tile(TileLayer.WallMountHigh, "RulesMount", TileObjectGenericType.None);

            PlacementPlan plan = MapEditorPlacementRules.Plan(context.Map, mount, Cell, Direction.North, false, autoSubfloor: true);

            Assert.AreEqual(PlacementOutcome.Blocked, plan.Outcome, "a subfloor alone can't hold a wall mount");
            Assert.AreEqual(0, plan.Subfloor.Count);
        }

        [Test]
        public void SameFloorAgain_IsAlreadyThere()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileMapTestUtilities.PlacePlenum(context, Cell);
            TileObjectSo floor = Tile(TileLayer.Turf, "RepeatFloor", TileObjectGenericType.Floor);
            Place(context, floor, Cell);

            PlacementPlan plan = MapEditorPlacementRules.Plan(context.Map, floor, Cell, Direction.East, false, autoSubfloor: true);

            Assert.AreEqual(PlacementOutcome.AlreadyThere, plan.Outcome);
        }

        [Test]
        public void FloorOverAnotherFloor_RepaintsWithoutAlt()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileMapTestUtilities.PlacePlenum(context, Cell);
            Place(context, Tile(TileLayer.Turf, "OldFloor", TileObjectGenericType.Floor), Cell);
            TileObjectSo floor = Tile(TileLayer.Turf, "NewFloor", TileObjectGenericType.Floor);

            PlacementPlan plan = MapEditorPlacementRules.Plan(context.Map, floor, Cell, Direction.North, false, autoSubfloor: true);

            Assert.IsTrue(plan.CanPlace, string.Join(", ", plan.Failures));
            Assert.IsTrue(plan.Replace);
        }

        [Test]
        public void FloorOverAWall_NeedsAlt()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileMapTestUtilities.PlacePlenum(context, Cell);
            TileMapTestUtilities.PlaceAirtightWall(context, Cell);
            TileObjectSo floor = Tile(TileLayer.Turf, "FloorOnWall", TileObjectGenericType.Floor);

            PlacementPlan plain = MapEditorPlacementRules.Plan(context.Map, floor, Cell, Direction.North, false, autoSubfloor: true);
            PlacementPlan alt = MapEditorPlacementRules.Plan(context.Map, floor, Cell, Direction.North, true, autoSubfloor: true);

            Assert.AreEqual(PlacementOutcome.Blocked, plain.Outcome);
            CollectionAssert.Contains(plain.Failures, BuildFailReason.LayerOccupied);
            Assert.IsTrue(alt.CanPlace, string.Join(", ", alt.Failures));
        }

        [Test]
        public void DoorIntoAWall_ReplacesItWithoutAlt()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileMapTestUtilities.PlacePlenum(context, Cell);
            TileMapTestUtilities.PlaceAirtightWall(context, Cell);
            TileObjectSo door = Tile(TileLayer.Turf, "RulesDoor", TileObjectGenericType.Door);

            PlacementPlan plan = MapEditorPlacementRules.Plan(context.Map, door, Cell, Direction.South, false, autoSubfloor: true);

            Assert.IsTrue(plan.CanPlace, string.Join(", ", plan.Failures));
            Assert.IsTrue(plan.Replace);
        }

        [Test]
        public void FloorInSpace_PlacesSubfloorThenFloor_AndOneUndoTakesBoth()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            Tile(TileLayer.Plenum, MapEditorPlacementRules.SubfloorAssetName, TileObjectGenericType.Plenum);
            TileObjectSo floor = Tile(TileLayer.Turf, "AutoFloor", TileObjectGenericType.Floor);
            MapEditorCommandService service = CreateService(context);

            var commands = new List<IMapEditorCommand>();
            Assert.IsTrue(MapEditorCommandFactory.TryCreatePlaceCommands(PlaceDto(floor, autoSubfloor: true), service.Context, commands));
            Assert.AreEqual(2, commands.Count, "the subfloor, then the floor");

            Assert.IsTrue(service.ExecuteCompound(commands));
            Assert.AreEqual(MapEditorPlacementRules.SubfloorAssetName, OccupantName(context, TileLayer.Plenum));
            Assert.AreEqual("AutoFloor", OccupantName(context, TileLayer.Turf));
            Assert.AreEqual(1, service.UndoDepth);

            Assert.IsTrue(service.Undo());
            Assert.IsNull(OccupantName(context, TileLayer.Turf));
            Assert.IsNull(OccupantName(context, TileLayer.Plenum), "the subfloor laid for the floor goes with it");
        }

        [Test]
        public void FloorInSpace_WithAutoSubfloorOff_IsRefusedByTheServer()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            Tile(TileLayer.Plenum, MapEditorPlacementRules.SubfloorAssetName, TileObjectGenericType.Plenum);
            TileObjectSo floor = Tile(TileLayer.Turf, "NoAutoFloor", TileObjectGenericType.Floor);
            MapEditorCommandService service = CreateService(context);

            var commands = new List<IMapEditorCommand>();
            Assert.IsFalse(MapEditorCommandFactory.TryCreatePlaceCommands(PlaceDto(floor, autoSubfloor: false), service.Context, commands));
            Assert.AreEqual(0, commands.Count);
        }

        [Test]
        public void DoorBetweenEastAndWestWalls_FacesSouth_AndBetweenNorthAndSouth_FacesEast()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileMapTestUtilities.PlaceAirtightWall(context, Cell + Vector3.right);
            TileMapTestUtilities.PlaceAirtightWall(context, Cell + Vector3.left);
            Vector3 other = Cell + new Vector3(10, 0, 0);
            TileMapTestUtilities.PlaceAirtightWall(context, other + Vector3.forward);
            TileMapTestUtilities.PlaceAirtightWall(context, other + Vector3.back);

            Assert.IsTrue(MapEditorPlacementRules.TryInferDoorDirection(context.Map, Cell, out Direction eastWest));
            Assert.AreEqual(Direction.South, eastWest);
            Assert.IsTrue(MapEditorPlacementRules.TryInferDoorDirection(context.Map, other, out Direction northSouth));
            Assert.AreEqual(Direction.East, northSouth);
        }

        [Test]
        public void DoorAtACornerOrInTheOpen_KeepsItsRotation()
        {
            TileMapTestUtilities.MapContext context = CreateContext();
            TileMapTestUtilities.PlaceAirtightWall(context, Cell + Vector3.right);
            TileMapTestUtilities.PlaceAirtightWall(context, Cell + Vector3.forward);

            Assert.IsFalse(MapEditorPlacementRules.TryInferDoorDirection(context.Map, Cell, out _), "corner");
            Assert.IsFalse(MapEditorPlacementRules.TryInferDoorDirection(context.Map, Cell + new Vector3(20, 0, 0), out _), "open");
        }

        [Test]
        public void RoomCells_SplitBorderFromInterior()
        {
            var border = new List<Vector3>();
            var interior = new List<Vector3>();

            MapEditorPlacementRules.RoomCells(new Vector2Int(0, 0), new Vector2Int(3, 2), border, interior);
            Assert.AreEqual(10, border.Count);
            Assert.AreEqual(2, interior.Count);
            CollectionAssert.AreEquivalent(new[] { new Vector3(1, 0, 1), new Vector3(2, 0, 1) }, interior);

            MapEditorPlacementRules.RoomCells(new Vector2Int(5, 5), new Vector2Int(5, 9), border, interior);
            Assert.AreEqual(5, border.Count, "a one-tile-wide room is all wall");
            Assert.AreEqual(0, interior.Count);
        }

        [Test]
        public void RoomTool_TakesAWindowOnlyWhenPickedWithIt()
        {
            var viewModel = new SS3D.Systems.Tile.MapEditor.UI.MapEditorViewModel();
            TileObjectSo wall = Tile(TileLayer.Turf, "RoomWall", TileObjectGenericType.Wall);
            TileObjectSo window = Tile(TileLayer.Turf, "SteelWindow", TileObjectGenericType.Wall);

            viewModel.SelectEntry(null, wall);
            viewModel.SelectEntry(null, window);
            Assert.AreSame(wall, viewModel.RoomWall, "placing windows with Construct leaves the room's wall alone");

            Assert.IsTrue(viewModel.RememberRoomPart(null, window, allowWindow: true));
            Assert.AreSame(window, viewModel.RoomWall, "a window picked with the room tool on becomes its wall");
        }

        [Test]
        public void CardinalFrom_PointsAtTheNearestSide()
        {
            Assert.AreEqual(Direction.North, MapEditorPlacementRules.CardinalFrom(new Vector3(0.1f, 0, 0.4f)));
            Assert.AreEqual(Direction.East, MapEditorPlacementRules.CardinalFrom(new Vector3(0.4f, 0, -0.1f)));
            Assert.AreEqual(Direction.South, MapEditorPlacementRules.CardinalFrom(new Vector3(-0.1f, 0, -0.4f)));
            Assert.AreEqual(Direction.West, MapEditorPlacementRules.CardinalFrom(new Vector3(-0.4f, 0, 0.2f)));
        }

        private TileMapTestUtilities.MapContext CreateContext() => TileMapTestUtilities.CreateContext(_instantiated);

        private TileObjectSo Tile(TileLayer layer, string name, TileObjectGenericType type)
        {
            TileObjectSo so = TileMapTestUtilities.CreateTileSo(layer, name);
            so.genericType = type;
            _assets[so.NameString] = so;
            return so;
        }

        private static void Place(TileMapTestUtilities.MapContext context, TileObjectSo tile, Vector3 position)
        {
            bool placed = context.Map.PlaceTileObject(tile, position, Direction.North, skipBuildCheck: true,
                replaceExisting: false, skipAdjacency: false, out GameObject _);
            Assert.IsTrue(placed, $"Expected {tile.NameString} to be placed.");
        }

        private static MapEditorCommandDto PlaceDto(TileObjectSo tile, bool autoSubfloor) => new()
        {
            Kind = MapEditorCommandKind.PlaceTile,
            AssetName = tile.NameString,
            Position = Cell,
            Direction = Direction.North,
            AutoSubfloor = autoSubfloor,
        };

        private MapEditorCommandService CreateService(TileMapTestUtilities.MapContext context)
        {
            var service = new MapEditorCommandService();
            service.Bind(context.Map, loader: null, context.Construction);
            service.Context.AssetResolver = name => _assets.TryGetValue(name, out GenericObjectSo asset) ? asset : null;
            return service;
        }

        private static string OccupantName(TileMapTestUtilities.MapContext context, TileLayer layer) =>
            context.Map.TryGetTileLocation(layer, Cell, out ITileLocation location)
            && location.TryGetPlacedObject(out PlacedTileObject placed)
            && placed != null
                ? placed.NameString
                : null;
    }
}

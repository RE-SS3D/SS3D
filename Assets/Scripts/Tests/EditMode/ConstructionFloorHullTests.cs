using System;
using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Construction;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    /// <summary>
    /// S2 of 2026-11_construction-mvp1: cell and object rules for floors and hull, nothing spent when a
    /// step cannot finish, and a blasted hull hole that turns airtight again once plated.
    /// </summary>
    public class ConstructionFloorHullTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private static ConstructionRule Rule(ConstructionStep step) => ConstructionRecipes.Get(step);

        private static TileObjectSo Plenum() => TileMapTestUtilities.CreateTileSo(TileLayer.Plenum, "Plenum");

        private static PlacedTileObject PlaceFloor(TileMapTestUtilities.MapContext context, Vector3 position)
        {
            TileObjectSo floor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "TestFloor");
            Assert.True(context.Map.PlaceTileObject(floor, position, Direction.North, true, false, false, out GameObject go));
            return go.GetComponent<PlacedTileObject>();
        }

        private static PlacedTileObject PlenumAt(TileMapTestUtilities.MapContext context, Vector3 position)
        {
            Assert.True(context.Query.TryGetOccupant(context.Query.WorldToTile(position), TileLayer.Plenum, Direction.North, out ITileOccupant occupant));
            return (PlacedTileObject)occupant;
        }

        private static void SetArea(TileMapTestUtilities.MapContext context, Vector3 position, ushort area) =>
            Assert.True(context.Map.TrySetAreaId(context.Query.WorldToTile(position), area));

        [Test]
        public void HullHole_MatchesOnlyACellWithoutPlating()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.PlacePlenum(context, Vector3.zero);
            ConstructionRule patch = Rule(ConstructionStep.PlacePlating);

            Assert.False(ConstructionCellRules.Matches(context.Map, patch, Vector3.zero), "Plated cell is not a hole");
            Assert.True(ConstructionCellRules.Matches(context.Map, patch, new Vector3(1, 0, 0)), "Empty cell in a chunk is a hole");
        }

        [Test]
        public void HullHole_OutsideTheStation_IsRefusedOnTheServer()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.PlacePlenum(context, Vector3.zero);
            ConstructionRule patch = Rule(ConstructionStep.PlacePlating);
            Vector3 hole = new(1, 0, 0);

            Assert.AreEqual("Outside the station", ConstructionCellRules.Validate(context.Map, patch, hole, Plenum(), checkStation: true));
            Assert.IsNull(ConstructionCellRules.Validate(context.Map, patch, hole, Plenum(), checkStation: false), "Clients cannot see areas");

            SetArea(context, hole, 3);
            Assert.IsNull(ConstructionCellRules.Validate(context.Map, patch, hole, Plenum(), checkStation: true));
        }

        [Test]
        public void FloorTile_GoesOnBarePlatingOnly()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.PlacePlenum(context, Vector3.zero);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(1, 0, 0));
            PlaceFloor(context, new Vector3(1, 0, 0));
            ConstructionRule lay = Rule(ConstructionStep.PlaceFloorTile);

            Assert.True(ConstructionCellRules.Matches(context.Map, lay, Vector3.zero), "Bare plating takes a tile");
            Assert.False(ConstructionCellRules.Matches(context.Map, lay, new Vector3(1, 0, 0)), "Already floored");
            Assert.False(ConstructionCellRules.Matches(context.Map, lay, new Vector3(2, 0, 0)), "No plating underneath");
        }

        [Test]
        public void ObjectRules_PryFloor_RepairOnlyDamagedPlating()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.PlacePlenum(context, Vector3.zero);
            PlacedTileObject floor = PlaceFloor(context, Vector3.zero);
            PlacedTileObject plating = PlenumAt(context, Vector3.zero);
            plating.tileObjectSO.genericType = TileObjectGenericType.Plenum;

            Assert.True(ConstructionObjectRules.Matches(Rule(ConstructionStep.PryFloorTile), floor));
            Assert.False(ConstructionObjectRules.Matches(Rule(ConstructionStep.PryFloorTile), plating));

            ConstructionRule repair = Rule(ConstructionStep.RepairPlating);
            plating.ServerSetIntegrity(200f, StructuralIntegrityStage.Intact);
            Assert.False(ConstructionObjectRules.Matches(repair, plating), "Intact plating needs no weld");
            plating.ServerSetIntegrity(40f, StructuralIntegrityStage.Cracked);
            Assert.True(ConstructionObjectRules.Matches(repair, plating));
            Assert.False(ConstructionObjectRules.Matches(repair, floor), "The plating rule never touches the floor layer");
        }

        // Guards: girder, floor tile and wreckage steps are offered on the hovered floor or plating (D14), not on an empty cell
        [Test]
        public void CellSteps_AreOfferedOnTheFloorOrPlatingYouHover()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Vector3 bare = Vector3.zero, floored = new(1, 0, 0);
            TileMapTestUtilities.PlacePlenum(context, bare);
            TileMapTestUtilities.PlacePlenum(context, floored);
            PlacedTileObject floor = PlaceFloor(context, floored);
            floor.tileObjectSO.genericType = TileObjectGenericType.Floor;
            PlacedTileObject barePlating = PlenumAt(context, bare);
            PlacedTileObject flooredPlating = PlenumAt(context, floored);
            barePlating.tileObjectSO.genericType = TileObjectGenericType.Plenum;
            flooredPlating.tileObjectSO.genericType = TileObjectGenericType.Plenum;

            TileCoord bareCoord = ConstructionCellRules.CoordOf(context.Map, bare);
            bool wreckage = false;
            Func<TileCoord, TileLayer, bool> lookup = (c, layer) => wreckage && c == bareCoord && layer == TileLayer.Turf;

            ConstructionRule lay = Rule(ConstructionStep.PlaceFloorTile);
            ConstructionRule girder = Rule(ConstructionStep.PlaceGirder);
            ConstructionRule clear = Rule(ConstructionStep.ClearWreckage);
            Assert.False(lay.IsCellAction || girder.IsCellAction || clear.IsCellAction, "Only the hull hole keeps a ghost");
            Assert.True(lay.ActsOnCell && girder.ActsOnCell && clear.ActsOnCell);
            Assert.True(Rule(ConstructionStep.PlacePlating).IsCellAction);

            Assert.True(ConstructionObjectRules.Matches(lay, barePlating, context.Map, lookup), "Lay a tile on bare plating");
            Assert.False(ConstructionObjectRules.Matches(lay, flooredPlating, context.Map, lookup), "Already floored");
            Assert.False(ConstructionObjectRules.Matches(lay, floor, context.Map, lookup), "Not on the floor tile itself");

            Assert.True(ConstructionObjectRules.Matches(girder, floor, context.Map, lookup), "Construct a girder on a floor tile");
            Assert.True(ConstructionObjectRules.Matches(girder, barePlating, context.Map, lookup), "or on bare plating");

            Assert.False(ConstructionObjectRules.Matches(clear, barePlating, context.Map, lookup), "No wreckage, nothing to clear");
            wreckage = true;
            Assert.True(ConstructionObjectRules.Matches(clear, barePlating, context.Map, lookup), "Clear wreckage from its plating");
        }

        // Guards: a damaged floor tile is replaced by prying it up and laying a new one; a Cracked one is scrap
        [Test]
        public void PryingACrackedTile_ReturnsNothing()
        {
            ConstructionRule pry = Rule(ConstructionStep.PryFloorTile);
            Assert.True(pry.NoReturnWhenCracked);
            Assert.AreEqual(ConstructionNames.FloorTileItem, pry.ReturnItemName, "An intact or damaged tile comes back");
        }

        // Guards: a pried floor returns its own kind of tile, and that tile lays the same floor back (D17)
        [Test]
        public void FloorTileKinds_RoundTrip()
        {
            foreach ((string turf, string item, string _) in ConstructionFloorTiles.Kinds)
            {
                Assert.AreEqual(item, ConstructionFloorTiles.ItemFor(turf));
                Assert.AreEqual(turf, ConstructionFloorTiles.TurfFor(item));
            }

            Assert.AreEqual("FloorTileWhite", ConstructionFloorTiles.ItemFor("TileWhite"));
            Assert.IsNull(ConstructionFloorTiles.ItemFor("TileGrass"), "Grass is not a steel tile");
            Assert.AreEqual(ConstructionNames.FloorTurf, ConstructionFloorTiles.TurfFor("Unknown"), "Unknown tiles lay grey");
            Assert.AreEqual(string.Empty, ConstructionApply.ReturnNameFor(Rule(ConstructionStep.PryFloorTile), null));
        }

        [Test]
        public void Reach_IsOneCellAroundTheBody()
        {
            Assert.True(ConstructionCellRules.InReach(new Vector3(0.4f, 0f, 0.2f), new Vector3(1f, 0f, 1f)));
            Assert.False(ConstructionCellRules.InReach(Vector3.zero, new Vector3(3f, 0f, 0f)));
        }

        [Test]
        public void StepThatCannotApply_SpendsNothing()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.PlacePlenum(context, Vector3.zero);
            PlacedTileObject floor = PlaceFloor(context, Vector3.zero);

            GameObject go = new("FloorTiles");
            _objects.Add(go);
            Item tiles = go.AddComponent<Item>();
            tiles.Init("FloorTile", 1f, new List<SS3D.Systems.Trait>());
            int before = tiles.StackCount;

            ConstructionRule lay = Rule(ConstructionStep.PlaceFloorTile);
            Assert.False(ConstructionApply.TryApplyCell(lay, Vector3.zero, Plenum(), tiles, null), "No TileSubSystem in EditMode");
            Assert.False(ConstructionApply.TryApplyObject(Rule(ConstructionStep.PryFloorTile), floor, tiles, null));
            new ConstructionStepInteraction(Rule(ConstructionStep.PryFloorTile), floor).Cancel(null, new InteractionReference(1));
            Assert.AreEqual(before, tiles.StackCount, "A step that does not finish costs nothing");
        }

        [Test]
        public void BlastedHullHole_IsAirtightAgainOncePlated()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation simulation = EditorTests.Atmospherics.AtmosTestFixtures.CreateSealedRoomSimulation(context, 4, out int map);
            context.Map.RegisterMutationObserver(new AtmosTileObserver(simulation));
            Vector3 position = new(2, 0, 2);
            TileCoord tile = new(map, 2, 2);
            SetArea(context, position, 1);

            StructuralDamageService service = new(context.Query, context.Construction);
            service.ApplyBlastFloorDamage(tile, 90f);
            service.ApplyBlastFloorDamage(tile, 110f);
            Assert.False(context.Query.TryGetOccupant(tile, TileLayer.Plenum, Direction.North, out _), "Two blasts open the hull");

            ConstructionRule patch = Rule(ConstructionStep.PlacePlating);
            Assert.True(ConstructionCellRules.Matches(context.Map, patch, position), "The breach is a hull hole");
            Assert.IsNull(ConstructionCellRules.Validate(context.Map, patch, position, Plenum(), checkStation: true),
                "The hole kept its area, so it can be patched");

            for (int i = 0; i < 100; i++) simulation.Tick(AtmosConstants.TickInterval);
            Assert.True(context.Construction.TryPlaceTile(Plenum(), position, Direction.North, replaceExisting: false).Success);
            Assert.True(context.Query.TryGetOccupancy(tile, out TileOccupancy occupancy) && occupancy.HasPlenum);

            for (int i = 0; i < 20; i++) simulation.Tick(AtmosConstants.TickInterval);
            float settled = simulation.GetTotalMoles();
            for (int i = 0; i < 200; i++) simulation.Tick(AtmosConstants.TickInterval);
            Assert.That(simulation.GetTotalMoles(), Is.EqualTo(settled).Within(settled * 0.01f + 0.1f),
                "Once plated the room stops venting");
        }
    }
}

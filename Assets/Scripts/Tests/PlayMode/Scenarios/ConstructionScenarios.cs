using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Construction;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// S6 of 2026-11_construction-mvp1 in the test room: weld cracked plating, patch a blasted hull hole,
    /// and rebuild a destroyed wall from its wreckage, each through the same server RPCs a player's click sends.
    /// </summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Construction)]
    public class ConstructionScenarios : ScenarioFixture
    {
        private const string SteelSheet = "4cb99cf2fee96f944abbacc51b0b701c";
        private const string FloorTile = "404443f7c306fc8469698745624fa3bf";
        private const string Welder = "06d944205e6b6dd489af078fa85fb878";
        private const string Crowbar = "7becfab153276864f9fc7636a839d837";
        private const string Magboots = "2ba59995b958400888f44509d7530731";

        private const float C4Yield = 180f;
        private const float C4Falloff = 45f;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly Vector3 Epicenter = new(0f, 0f, -1f);

        /// <summary>The wall between the main room (x −2..2) and the side room (x 4..6).</summary>
        private const int DividingWallX = 3;

        private static TileSubSystem Tiles => SubSystems.Get<TileSubSystem>();

        // Guards: the welder offers Repair on cracked plating under a stripped floor, and floor tiles relay it (S2) · Not cheaper because: object steps dispatch through the plating's Selectable and the client interaction path
        [UnityTest]
        public IEnumerator CrackedPlating_WeldedBackToIntact_ThenRetiled()
        {
            ChangesRoom = true;
            EntityInvincibility.SetServer(Body.Owner.ClientId, true);
            try
            {
                Teleport(Body, new Vector3(5f, Body.transform.position.y, 1f));
                SubSystems.Get<StructuralDamageSubSystem>().ResolveBlast(Coord(Epicenter), C4Yield, C4Falloff);
                yield return Hold(1f);
                PlacedTileObject plating = Plenum(Epicenter);
                Assert.AreEqual(StructuralIntegrityStage.Cracked, plating.IntegrityStage, "One charge cracks the plating");
                Assert.IsFalse(HasTurf(Epicenter), "and strips the floor above it");

                yield return StandBeside(Epicenter);
                Hand hand = HoldNew(Welder);
                yield return Hold(0.3f);
                yield return RunObjectStep(plating.gameObject, "ConstructionRepairPlating");
                yield return WaitFor(() => Plenum(Epicenter).IntegrityStage == StructuralIntegrityStage.Intact,
                    "the welder repairs the plating", ConstructionRecipes.Get(ConstructionStep.RepairPlating).Seconds + 3f);

                hand.ItemInHand.Delete();
                yield return Hold(0.3f);
                Item tiles = HoldNew(FloorTile).ItemInHand;
                int before = tiles.StackCount;
                yield return Hold(0.3f);
                yield return RunCellStep(ConstructionStep.PlaceFloorTile, Epicenter);
                yield return WaitFor(() => HasTurf(Epicenter), "a floor tile goes down on the bare plating",
                    ConstructionRecipes.Get(ConstructionStep.PlaceFloorTile).Seconds + 3f);
                Assert.AreEqual(before - 1, tiles.StackCount, "Laying one tile spends one");
            }
            finally
            {
                EntityInvincibility.ClearServer(Body.Owner.ClientId);
            }
        }

        // Guards: steel sheets patch a hole in the hull and the room holds air again (S2, closes M8's patch) · Not cheaper because: needs live atmos, the blast resolver and the cell-action RPC together
        [UnityTest]
        public IEnumerator HullHole_PatchedWithPlating_HoldsAirAgain()
        {
            ChangesRoom = true;
            EntityInvincibility.SetServer(Body.Owner.ClientId, true);
            try
            {
                Teleport(Body, new Vector3(5f, Body.transform.position.y, 1f));
                TileCoord epicenter = Coord(Epicenter);
                StructuralDamageSubSystem damage = SubSystems.Get<StructuralDamageSubSystem>();
                // One charge strips the floor and cracks the plating without breaking the walls; a second
                // would hole the walls too, and this test is about the hull. Take the cracked cell out directly.
                damage.ResolveBlast(epicenter, C4Yield, C4Falloff);
                yield return Hold(1f);
                Assert.IsTrue(Tiles.Construction.TryClearTile(Epicenter, TileLayer.Plenum, Direction.North).Success,
                    "The cracked plating comes away");
                Assert.IsFalse(IsPlated(Epicenter), "The hull is open");

                AtmosSimulation simulation = SubSystems.Get<AtmosSubSystem>().Simulation;
                yield return WaitFor(() => RoomPressure(simulation) < 60f, "the room vents to space", 30f);

                // An engineer patching a breach wears magboots: the wind into the hole would drag the body off the step.
                yield return WearMagboots();

                // Work in from the hole's edge: each patched cell is somewhere to stand for the next.
                Item sheets = HoldNew(SteelSheet).ItemInHand;
                yield return Hold(0.3f);
                Assert.AreEqual(20, sheets.StackCount, "Steel sheets spawn as a stack of 20");
                float seconds = ConstructionRecipes.Get(ConstructionStep.PlacePlating).Seconds + 3f;
                int patchedCells = 0;
                while (TryFindHoleAtEdge(out Vector3 cell))
                {
                    Assert.Less(patchedCells, 15, "The hole is no bigger than the room");
                    yield return StandBeside(cell);
                    yield return RunCellStep(ConstructionStep.PlacePlating, cell);
                    Vector3 start = Body.transform.position;
                    float until = Time.time + seconds;
                    while (!IsPlated(cell) && Time.time < until)
                    {
                        yield return null;
                    }

                    Assert.IsTrue(IsPlated(cell), $"The hole at {cell} is plated over; the body moved {start} -> {Body.transform.position}, "
                        + $"{Body.GetComponent<SS3D.Systems.Entities.Humanoid.Ragdoll>().Presentation}, wind {AtmosWind.SampleAt(simulation, Body.transform.position)}, room {RoomPressure(simulation)} kPa");
                    patchedCells++;
                }

                Assert.IsTrue(IsPlated(Epicenter), "The whole hole is patched");
                Assert.AreEqual(20 - patchedCells, sheets.StackCount, "Plating spends one sheet a cell");

                // The patched cell starts with station air (D8); with the hull shut it is not lost to space.
                yield return Hold(2f);
                float patchedPressure = RoomPressure(simulation);
                Assert.Greater(patchedPressure, 1f, "The patched cell's air reaches the room");
                yield return Hold(3f);
                Assert.GreaterOrEqual(RoomPressure(simulation), patchedPressure - 0.5f,
                    $"and the room keeps it ({patchedPressure} kPa after patching {patchedCells} cells)");
            }
            finally
            {
                EntityInvincibility.ClearServer(Body.Owner.ClientId);
            }
        }

        // Guards: a destroyed wall leaves wreckage that blocks building, a crowbar clears it for steel, and girder → plated → welded seals the rooms apart again (S4) · Not cheaper because: needs wreckage records, networked stage prefabs and atmos edges on a live host
        [UnityTest]
        public IEnumerator DestroyedWall_ClearedAndRebuilt_SealsTheRoomsApart()
        {
            ChangesRoom = true;
            EntityInvincibility.SetServer(Body.Owner.ClientId, true);
            try
            {
                Vector3 wall = FindDividingWall();
                TileCoord coord = Coord(wall);
                StructuralDamageSubSystem damage = SubSystems.Get<StructuralDamageSubSystem>();
                for (int hit = 0; hit < 20 && TryGetTurf(wall, out _); hit++)
                {
                    damage.TryApplyStructuralDamage(coord, 1000f, StructuralDamageSource.Console);
                }

                Assert.IsFalse(TryGetTurf(wall, out _), "The wall is destroyed");
                Assert.IsTrue(Tiles.HasWreckage(coord, TileLayer.Turf), "A steel wall leaves wreckage");
                yield return Hold(0.5f);

                yield return StandBeside(wall);
                Hand hand = HoldNew(Crowbar);
                yield return Hold(0.3f);
                yield return RunCellStep(ConstructionStep.ClearWreckage, wall);
                yield return WaitFor(() => !Tiles.HasWreckage(coord, TileLayer.Turf), "the crowbar clears the wreckage",
                    ConstructionRecipes.Get(ConstructionStep.ClearWreckage).Seconds + 3f);

                hand.ItemInHand.Delete();
                yield return Hold(0.3f);
                Item sheets = HoldNew(SteelSheet).ItemInHand;
                yield return Hold(0.3f);
                yield return RunCellStep(ConstructionStep.PlaceGirder, wall);
                yield return WaitFor(() => TurfNamed(wall, ConstructionNames.Girder), "a girder goes up",
                    ConstructionRecipes.Get(ConstructionStep.PlaceGirder).Seconds + 3f);
                Assert.IsTrue(Tiles.QueryService.TryGetOccupancy(coord, out TileOccupancy girder) && girder.BlockedEdges == 0,
                    "Air passes a bare girder");

                Assert.IsTrue(TryGetTurf(wall, out PlacedTileObject placed));
                yield return RunObjectStep(placed.gameObject, "ConstructionPlateGirder");
                yield return WaitFor(() => TurfNamed(wall, ConstructionNames.GirderPlated), "the girder is plated",
                    ConstructionRecipes.Get(ConstructionStep.PlateGirder).Seconds + 3f);
                Assert.AreEqual(16, sheets.StackCount, "Girder and plate cost two sheets each");

                hand.ItemInHand.Delete();
                yield return Hold(0.3f);
                HoldNew(Welder);
                yield return Hold(0.3f);
                Assert.IsTrue(TryGetTurf(wall, out placed));
                yield return RunObjectStep(placed.gameObject, "ConstructionWeldSeams");
                yield return WaitFor(() => TurfNamed(wall, ConstructionNames.Wall), "welding the seams makes a wall",
                    ConstructionRecipes.Get(ConstructionStep.WeldPlated).Seconds + 3f);
                Assert.IsTrue(Tiles.QueryService.TryGetOccupancy(coord, out TileOccupancy sealedWall) && sealedWall.BlockedEdges != 0,
                    "The rebuilt wall blocks air again");
            }
            finally
            {
                EntityInvincibility.ClearServer(Body.Owner.ClientId);
            }
        }

        /// <summary>Swaps the loadout shoes for magboots and switches them on.</summary>
        private IEnumerator WearMagboots()
        {
            EvaGearController gear = Body.GetComponent<EvaGearController>();
            AttachedContainer feet = Body.GetComponent<HumanInventory>().Containers.Single(worn => worn.ContainerType == ContainerType.ShoeLeft);
            feet.Dump();
            SubSystems.Get<ItemSubSystem>().SpawnItemInContainer(Magboots, feet);
            yield return WaitFor(() => gear.MagbootsAvailable, "the pair offers magboots");
            gear.ServerSetMagboots(true);
            yield return WaitFor(() => gear.MagbootsAnchored, "the boots hold");
        }

        /// <summary>A plating hole in the main room with plating beside it to stand on.</summary>
        private static bool TryFindHoleAtEdge(out Vector3 cell)
        {
            RectInt room = ScenarioAtmos.MainRoom;
            Vector3[] sides = { Vector3.left, Vector3.back, Vector3.forward, Vector3.right };
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int z = room.yMin; z < room.yMax; z++)
                {
                    cell = new Vector3(x, 0f, z);
                    if (IsPlated(cell))
                    {
                        continue;
                    }

                    foreach (Vector3 side in sides)
                    {
                        if (IsPlated(cell + side))
                        {
                            return true;
                        }
                    }
                }
            }

            cell = default;
            return false;
        }

        private static bool IsPlated(Vector3 cell) =>
            Tiles.QueryService.TryGetOccupancy(Coord(cell), out TileOccupancy tile) && tile.HasPlenum;

        /// <summary>A plain steel wall cell on the dividing line, not a door or window.</summary>
        private static Vector3 FindDividingWall()
        {
            for (int z = -3; z <= 1; z++)
            {
                var candidate = new Vector3(DividingWallX, 0f, z);
                if (TurfNamed(candidate, ConstructionNames.Wall))
                {
                    return candidate;
                }
            }

            Assert.Fail("The test room has a plain steel wall between its rooms");
            return default;
        }

        /// <summary>Stands the body on the main-room side of <paramref name="cell"/>, on plating, facing it.</summary>
        private IEnumerator StandBeside(Vector3 cell)
        {
            Vector3[] offsets = { Vector3.left, Vector3.back, Vector3.forward, Vector3.right };
            foreach (Vector3 offset in offsets)
            {
                Vector3 stand = cell + offset;
                if (Tiles.QueryService.TryGetOccupancy(Coord(stand), out TileOccupancy tile) && tile.HasPlenum && tile.BlockedEdges == 0)
                {
                    Teleport(Body, new Vector3(stand.x, Body.transform.position.y, stand.z));
                    Body.transform.rotation = Quaternion.LookRotation(-offset);
                    yield return Hold(0.5f);
                    Physics.SyncTransforms();
                    yield break;
                }
            }

            Assert.Fail($"Somewhere to stand beside {cell}");
        }

        /// <summary>Empties both hands and puts a fresh <paramref name="itemId"/> in the selected one.</summary>
        private Hand HoldNew(string itemId)
        {
            EmptyHands(Body);
            Hands hands = Body.GetComponent<Hands>();
            Hand hand = hands.SelectedHand;
            Item item = SubSystems.Get<ItemSubSystem>().SpawnItem(itemId, Body.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(item), $"{item.name} goes in the hand");
            return hand;
        }

        /// <summary>The ServerRpc a primary click with a valid ghost sends.</summary>
        private IEnumerator RunCellStep(ConstructionStep step, Vector3 cell)
        {
            ConstructionPlacementSubSystem placement = SubSystems.Get<ConstructionPlacementSubSystem>();
            Hands hands = Body.GetComponent<Hands>();
            object[] validate = { ConstructionRecipes.Get(step), cell, hands, hands.SelectedHand.ItemInHand, null };
            string refusal = (string)typeof(ConstructionPlacementSubSystem).GetMethod("ServerValidate", Private).Invoke(placement, validate);
            Assert.IsNull(refusal, $"{step} at {cell} from {Body.transform.position}: {refusal}");
            typeof(ConstructionPlacementSubSystem).GetMethod("CmdRequestCellAction", Private)
                .Invoke(placement, new object[] { (byte)step, cell, null });
            yield return null;
        }

        private IEnumerator RunObjectStep(GameObject target, string genericName)
        {
            InteractionController interactions = Body.GetComponent<InteractionController>();
            string refusal = interactions.RunInteractionForTesting(target, target.transform.position + Vector3.up * 0.1f,
                genericName, IntentType.Help);
            Assert.IsNull(refusal, refusal);
            yield return null;
        }

        private static float RoomPressure(AtmosSimulation simulation) =>
            AtmosAreaSampler.TrySampleTile(Coord(new Vector3(1f, 0f, 1f)), simulation, out AtmosAreaSample sample)
                ? sample.AveragePressureKpa
                : float.NaN;

        private static TileCoord Coord(Vector3 position) => Tiles.QueryService.WorldToTile(position, Tiles.CurrentMap.MapId);

        private static bool HasTurf(Vector3 position) => TryGetTurf(position, out _);

        private static bool TryGetTurf(Vector3 position, out PlacedTileObject placed)
        {
            placed = null;
            if (!Tiles.QueryService.TryGetOccupant(Coord(position), TileLayer.Turf, Direction.North, out ITileOccupant occupant))
            {
                return false;
            }

            placed = occupant as PlacedTileObject;
            return placed != null;
        }

        private static bool TurfNamed(Vector3 position, string name) =>
            TryGetTurf(position, out PlacedTileObject placed) && placed.NameString == name;

        private static PlacedTileObject Plenum(Vector3 position)
        {
            Assert.IsTrue(Tiles.QueryService.TryGetOccupant(Coord(position), TileLayer.Plenum, Direction.North, out ITileOccupant occupant),
                $"A plenum exists at {position}");
            return (PlacedTileObject)occupant;
        }
    }
}

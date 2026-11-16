using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    /// <summary>
    /// A tile change reshapes the grid; it must not hand out air. These run a room that scrubbers
    /// have emptied, then open it up the way a player does.
    /// </summary>
    public class AtmosTileChangeTests
    {
        private const int RoomSize = 6;
        private const float StationMoles = AtmosConstants.StationOxygenMoles + AtmosConstants.StationNitrogenMoles;

        // An opened tile holds what its neighbours hold, so the room gains one tile of its own air at most.
        private const float OneMoreTile = 1f + 1f / (RoomSize * RoomSize) + 0.001f;

        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [TestCase(0.01f)]
        [TestCase(0.1f)]
        public void HullWallShotOut_OfAnEmptiedRoom_AddsNoAirAndKnocksNobodyDown(float airLeft)
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int map);
            Empty(sim, map, airLeft);
            float before = sim.GetTotalMoles();

            context.Construction.TryClearTile(new Vector3(RoomSize + 1, 0, 3), TileLayer.Turf, Direction.North);
            sim.QueueCellRefresh(new TileCoord(map, RoomSize + 1, 3));

            float peakWind = 0f;
            for (int tick = 0; tick < 50; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                Assert.That(sim.GetTotalMoles(), Is.LessThanOrEqualTo(before * OneMoreTile), "the hole brought air with it");
                peakWind = Mathf.Max(peakWind, PeakWind(sim));
            }

            Debug.Log($"[tile-change] airLeft={airLeft} peakWind={peakWind:F2}");
            Assert.That(peakWind, Is.LessThan(AtmosWind.KnockdownSpeed), "thin air knocked a body down");
        }

        [Test]
        public void HullWallShotOut_OfAFullRoom_StillKnocksDownBesideTheHole()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int map);

            context.Construction.TryClearTile(new Vector3(RoomSize + 1, 0, 3), TileLayer.Turf, Direction.North);
            sim.QueueCellRefresh(new TileCoord(map, RoomSize + 1, 3));

            float peakWind = 0f;
            for (int tick = 0; tick < 25; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                peakWind = Mathf.Max(peakWind, PeakWind(sim));
            }

            Assert.That(peakWind, Is.GreaterThanOrEqualTo(AtmosWind.KnockdownSpeed));
        }

        [Test]
        public void DoorCycle_InAnEmptiedRoom_AddsNoAir()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int map);
            var door = new Vector3(3, 0, 3);
            var coord = new TileCoord(map, 3, 3);
            Empty(sim, map, 0.01f);
            float before = sim.GetTotalMoles();

            // A door swap in place: airtight while shut, open air while not.
            TileMapTestUtilities.PlaceAirtightWall(context, door);
            sim.UpdateCell(coord);
            context.Construction.TryClearTile(door, TileLayer.Turf, Direction.North);
            sim.UpdateCell(coord);

            Assert.That(sim.GetTotalMoles(), Is.LessThanOrEqualTo(before * OneMoreTile), "the door tile refilled itself");
        }

        [Test]
        public void TileChange_OnOpenFloor_LeavesItsGasAlone()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int map);
            var coord = new TileCoord(map, 3, 3);
            Empty(sim, map, 0.01f);
            float before = sim.GetTotalMoles();

            sim.UpdateCell(coord);

            Assert.That(sim.GetTotalMoles(), Is.EqualTo(before).Within(0.1f).Percent, "a placed object refilled the tile");
        }

        [Test]
        public void DoorOpening_InAFullRoom_StartsLevelWithIt()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int map);
            var door = new Vector3(3, 0, 3);
            var coord = new TileCoord(map, 3, 3);
            TileMapTestUtilities.PlaceAirtightWall(context, door);
            sim.UpdateCell(coord);

            context.Construction.TryClearTile(door, TileLayer.Turf, Direction.North);
            sim.UpdateCell(coord);

            Assert.That(sim.GetCellPressure(coord), Is.EqualTo(AtmosConstants.StandardPressure).Within(1f));
        }

        private static void Empty(AtmosSimulation sim, int map, float airLeft)
        {
            for (int x = 1; x <= RoomSize; x++)
            {
                for (int z = 1; z <= RoomSize; z++)
                {
                    var coord = new TileCoord(map, x, z);
                    float keep = 1f - airLeft;
                    sim.TryRemoveMoles(coord, AtmosConstants.Oxygen, AtmosConstants.StationOxygenMoles * keep, out _, out _);
                    sim.TryRemoveMoles(coord, AtmosConstants.Nitrogen, AtmosConstants.StationNitrogenMoles * keep, out _, out _);
                }
            }

            for (int tick = 0; tick < 5; tick++)
                sim.Tick(AtmosConstants.TickInterval);
            Assert.That(sim.GetTotalMoles(), Is.EqualTo(airLeft * StationMoles * RoomSize * RoomSize).Within(1f).Percent);
        }

        private static float PeakWind(AtmosSimulation sim)
        {
            float peak = 0f;
            for (int index = 0; index < sim.CellCount; index++)
                peak = Mathf.Max(peak, AtmosWind.SampleCell(sim, index).magnitude);
            return peak;
        }
    }
}

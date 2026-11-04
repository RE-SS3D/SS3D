using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    /// <summary>
    /// Phase 1 of Documents/architecture/2026-11_atmos-edge-flow.md: bulk transport by per-edge flow
    /// conserves gas, stays stable, settles and respects doors and chunk borders.
    /// </summary>
    public class AtmosEdgeFlowTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void SealedRoom_ConservesMolesAndEnergy()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = CreateRoom(context, 6, 6, out int map);
            sim.DebugAddMoles(new TileCoord(map, 1, 1), AtmosConstants.Nitrogen, 300f);
            sim.DebugSetTemperature(new TileCoord(map, 6, 6), 900f);

            float moles = sim.GetTotalMoles();
            float energy = sim.GetTotalThermalEnergy();
            Run(sim, 30f);

            Assert.That(sim.GetTotalMoles(), Is.EqualTo(moles).Within(0.1f).Percent);
            Assert.That(sim.GetTotalThermalEnergy(), Is.EqualTo(energy).Within(0.5f).Percent);
        }

        [Test]
        public void StepChange_DoesNotReverseOrOvershoot()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = CreateRoom(context, 12, 6, out int map);
            List<TileCoord> west = Cells(map, 1, 6, 1, 6);
            List<TileCoord> east = Cells(map, 7, 6, 1, 6);
            Evacuate(sim, east);
            Assert.That(sim.TryGetCellIndex(new TileCoord(map, 6, 3), out int edgeOwner), Is.True);

            float startGap = Mean(sim, west) - Mean(sim, east);
            int reversals = 0;
            float lastSign = 0f;
            float deepestOvershoot = 0f;
            float peakFlow = 0f;
            for (int tick = 0; tick < 200; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                float flow = sim.EdgeFlow[edgeOwner].East;
                peakFlow = Mathf.Max(peakFlow, Mathf.Abs(flow));
                if (Mathf.Abs(flow) >= AtmosFluxConstants.EdgeFlowSettledFlow)
                {
                    if (lastSign != 0f && Mathf.Sign(flow) != lastSign) reversals++;
                    lastSign = Mathf.Sign(flow);
                }

                deepestOvershoot = Mathf.Min(deepestOvershoot, Mean(sim, west) - Mean(sim, east));
            }

            Assert.That(peakFlow, Is.GreaterThan(AtmosFluxConstants.EdgeFlowSubstepFlow), "the step drove no bulk flow");
            Assert.That(reversals, Is.Zero, "flow changed direction");
            Assert.That(-deepestOvershoot, Is.LessThan(startGap * 0.02f), "pressure sloshed past equal");
        }

        [Test]
        public void SealedRoom_SettlesAndSleeps()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = CreateRoom(context, 8, 8, out int map);

            // Station mix, so only bulk flow has to settle: a composition gradient keeps a room
            // awake on purpose until it has mixed.
            var pulse = new TileCoord(map, 2, 2);
            sim.DebugAddMoles(pulse, AtmosConstants.Oxygen, AtmosConstants.StationOxygenMoles * 4f);
            sim.DebugAddMoles(pulse, AtmosConstants.Nitrogen, AtmosConstants.StationNitrogenMoles * 4f);

            float seconds = 0f;
            bool ranSubsteps = false;
            for (; seconds < 60f && (seconds < 1f || sim.ActiveCellCount > 0); seconds += AtmosConstants.TickInterval)
            {
                sim.Tick(AtmosConstants.TickInterval);
                ranSubsteps |= sim.LastSubsteps == AtmosFluxConstants.MaxBreachSubsteps;
            }

            TestContext.WriteLine($"[edge-flow] settled in {seconds:F1}s");
            Assert.That(ranSubsteps, Is.True, "the pulse should have needed substeps");
            Assert.That(sim.ActiveCellCount, Is.Zero, "room never slept");

            List<TileCoord> room = Cells(map, 1, 8, 1, 8);
            foreach (TileCoord coord in room)
            {
                Assert.That(sim.TryGetCellIndex(coord, out int index), Is.True);
                Assert.That(sim.EdgeFlow[index].East, Is.Zero, "flow left on a sleeping cell");
                Assert.That(sim.EdgeFlow[index].North, Is.Zero, "flow left on a sleeping cell");
                Assert.That(sim.GetCellPressure(coord), Is.EqualTo(Mean(sim, room)).Within(2f), "pressure left unequal");
            }
        }

        [Test]
        public void CalmRoom_RunsOneSubstep()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = CreateRoom(context, 4, 4, out int map);
            sim.DebugAddHeat(new TileCoord(map, 2, 2), 0.01f);

            sim.Tick(AtmosConstants.TickInterval);

            Assert.That(sim.LastSubsteps, Is.EqualTo(1));
        }

        [Test]
        public void ClosingDoor_ZeroesFlow_AndSealsBothSides()
        {
            const int roomSize = 5;
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = CreateRoom(context, roomSize * 2 + 1, roomSize, out int map);

            int dividerX = roomSize + 1;
            const int doorZ = 3;
            for (int z = 1; z <= roomSize; z++)
            {
                if (z == doorZ) continue;
                TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(dividerX, 0, z));
                sim.UpdateCell(new TileCoord(map, dividerX, z));
            }

            List<TileCoord> west = Cells(map, 1, roomSize, 1, roomSize);
            List<TileCoord> east = Cells(map, dividerX + 1, roomSize, 1, roomSize);
            Evacuate(sim, east);

            var door = new TileCoord(map, dividerX, doorZ);
            var westOfDoor = new TileCoord(map, dividerX - 1, doorZ);
            Assert.That(sim.TryGetCellIndex(westOfDoor, out int westIndex), Is.True);
            Assert.That(sim.TryGetCellIndex(door, out int doorIndex), Is.True);
            Run(sim, 1.2f);
            Assert.That(sim.EdgeFlow[westIndex].East, Is.GreaterThan(AtmosFluxConstants.EdgeFlowSubstepFlow), "no flow through the open door");

            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(dividerX, 0, doorZ));
            sim.UpdateCell(door);
            Assert.That(sim.EdgeFlow[westIndex].East, Is.Zero, "closing left flow on the west edge");
            Assert.That(sim.EdgeFlow[doorIndex].East, Is.Zero, "closing left flow on the east edge");

            sim.Tick(AtmosConstants.TickInterval);
            float westMoles = Moles(sim, west);
            float eastMoles = Moles(sim, east);
            Run(sim, 10f);

            Assert.That(Moles(sim, west), Is.EqualTo(westMoles).Within(0.1f).Percent, "gas left through a closed door");
            Assert.That(Moles(sim, east), Is.EqualTo(eastMoles).Within(0.1f).Percent, "gas arrived through a closed door");
            Assert.That(sim.EdgeFlow[westIndex].East, Is.Zero);
        }

        [TestCase(0, 1, true)]
        [TestCase(1, 0, true)]
        [TestCase(0, 1, false)]
        [TestCase(1, 0, false)]
        public void ChunkBorder_CarriesFlowBothWays(int firstChunk, int secondChunk, bool evacuateEast)
        {
            int border = AtmosConstants.ChunkSize - 1;
            int originX = border - 3;
            var context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.BuildWalledRoom(context, originX, 0, 6, 2);

            int map = context.Map.MapId;
            using var sim = new AtmosSimulation(context.Query, map, AtmosConstants.DefaultGasCount);
            sim.CreateChunk(ChunkRef(map, firstChunk));
            sim.CreateChunk(ChunkRef(map, secondChunk));
            for (int x = 0; x < 8; x++)
            {
                for (int z = 0; z < 4; z++)
                    sim.UpdateCell(new TileCoord(map, originX + x, z));
            }

            List<TileCoord> west = Cells(map, originX + 1, 3, 1, 2);
            List<TileCoord> east = Cells(map, border + 1, 3, 1, 2);
            List<TileCoord> emptied = evacuateEast ? east : west;
            Evacuate(sim, emptied);
            float moles = sim.GetTotalMoles();

            Assert.That(sim.TryGetCellIndex(new TileCoord(map, border, 1), out int edgeOwner), Is.True);
            float peakFlow = 0f;
            for (int tick = 0; tick < 5; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                float flow = sim.EdgeFlow[edgeOwner].East;
                if (Mathf.Abs(flow) > Mathf.Abs(peakFlow)) peakFlow = flow;
            }

            Assert.That(Mathf.Sign(peakFlow), Is.EqualTo(evacuateEast ? 1f : -1f), "flow runs toward the evacuated side");
            Assert.That(Mathf.Abs(peakFlow), Is.GreaterThan(AtmosFluxConstants.EdgeFlowSubstepFlow));
            Assert.That(Mean(sim, emptied), Is.GreaterThan(10f), "gas stopped at the border");

            Run(sim, 20f);
            Assert.That(Mean(sim, west), Is.EqualTo(Mean(sim, east)).Within(2f), "sides did not equalise");
            Assert.That(sim.GetTotalMoles(), Is.EqualTo(moles).Within(0.1f).Percent);
        }

        [Test]
        public void Breach_DrainsWithoutSeparatingGases_OrGoingNegative()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = OpenHole(context, 6, 6, out int map);
            List<TileCoord> room = Cells(map, 1, 6, 1, 6);
            float start = Mean(sim, room);
            float startRatio = Gas(sim, room, AtmosConstants.Oxygen) / Gas(sim, room, AtmosConstants.Nitrogen);

            bool halved = false;
            for (float seconds = 0f; seconds < 120f && !halved; seconds += AtmosConstants.TickInterval)
            {
                sim.Tick(AtmosConstants.TickInterval);
                halved = Mean(sim, room) <= start * 0.5f;
                AssertSaneGas(sim, room);
            }

            Assert.That(halved, Is.True, "room never reached half pressure");
            float ratio = Gas(sim, room, AtmosConstants.Oxygen) / Gas(sim, room, AtmosConstants.Nitrogen);
            Assert.That(ratio, Is.EqualTo(startRatio).Within(1f).Percent, "bulk flow separated the gases");
            Assert.That(sim.LastSubsteps, Is.EqualTo(AtmosFluxConstants.MaxBreachSubsteps));
        }

        [Test]
        public void Breach_EmptiesAndSleeps()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = OpenHole(context, 6, 6, out int map);
            List<TileCoord> room = Cells(map, 1, 6, 1, 6);

            float seconds = 0f;
            for (; seconds < 300f && (seconds < 1f || sim.ActiveCellCount > 0); seconds += AtmosConstants.TickInterval)
                sim.Tick(AtmosConstants.TickInterval);

            TestContext.WriteLine($"[edge-flow] 6x6 emptied and slept in {seconds:F1}s");
            Assert.That(sim.ActiveCellCount, Is.Zero, "room never slept");
            Assert.That(Mean(sim, room), Is.LessThan(AtmosFluxConstants.MinSimulationPressure));
            Assert.That(seconds, Is.LessThan(150f), "thin gas kept the room awake");
        }

        [Test]
        public void HotCell_StaysStableAtFullSubstepAndCalmSteps()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = CreateRoom(context, 6, 6, out int map);
            var hot = new TileCoord(map, 3, 3);
            sim.DebugSetTemperature(hot, 4000f);
            List<TileCoord> room = Cells(map, 1, 6, 1, 6);
            float peak = sim.GetCellPressure(hot);

            for (int tick = 0; tick < 200; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                AssertSaneGas(sim, room);
                foreach (TileCoord coord in room)
                    Assert.That(sim.GetCellPressure(coord), Is.LessThanOrEqualTo(peak * 1.05f), "pressure grew past the start");
            }
        }

        [Test]
        public void EqualPressurePocket_StillDiffuses()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = CreateRoom(context, 3, 3, out int map);
            Run(sim, 2f);

            var centre = new TileCoord(map, 2, 2);
            var neighbour = new TileCoord(map, 3, 2);
            Assert.That(sim.TryRemoveMoles(centre, AtmosConstants.Oxygen, 5f, out float removed, out float temperature), Is.True);
            Assert.That(sim.TryAddMolesAtTemperature(centre, AtmosConstants.CarbonDioxide, removed, temperature), Is.True);
            float moles = sim.GetTotalMoles();

            Run(sim, 16f);

            Assert.That(sim.DebugGetMoles(neighbour, AtmosConstants.CarbonDioxide), Is.GreaterThan(0.01f), "breath pocket did not spread");
            Assert.That(sim.DebugGetMoles(centre, AtmosConstants.CarbonDioxide), Is.LessThan(removed));
            Assert.That(sim.GetTotalMoles(), Is.EqualTo(moles).Within(0.5f).Percent);
        }

        private static AtmosSimulation CreateRoom(TileMapTestUtilities.MapContext context, int width, int depth, out int map)
        {
            return AtmosTestFixtures.CreateSealedRoomSimulation(context, width, depth, out map);
        }

        // A real hull hole: the wall goes, the plenum stays, so an air tile sits between room and space.
        private static AtmosSimulation OpenHole(TileMapTestUtilities.MapContext context, int width, int depth, out int map)
        {
            AtmosSimulation sim = CreateRoom(context, width, depth, out map);
            int holeZ = depth / 2;
            context.Construction.TryClearTile(new Vector3(width + 1, 0, holeZ), TileLayer.Turf, Direction.North);
            sim.QueueCellRefresh(new TileCoord(map, width + 1, holeZ));
            sim.Tick(AtmosConstants.TickInterval);
            return sim;
        }

        private static TileChunkRef ChunkRef(int map, int chunkX) => new()
        {
            MapId = map,
            ChunkKey = new Vector2Int(chunkX, 0),
            Origin = new Vector3(chunkX * AtmosConstants.ChunkSize, 0f, 0f),
        };

        private static void Run(AtmosSimulation sim, float seconds)
        {
            int ticks = Mathf.CeilToInt(seconds / AtmosConstants.TickInterval);
            for (int tick = 0; tick < ticks; tick++)
                sim.Tick(AtmosConstants.TickInterval);
        }

        private static void Evacuate(AtmosSimulation sim, List<TileCoord> cells)
        {
            foreach (TileCoord coord in cells)
            {
                for (ushort gas = 0; gas < AtmosConstants.DefaultGasCount; gas++)
                    sim.TryRemoveMoles(coord, new GasId(gas), float.MaxValue, out _, out _);
            }
        }

        private static List<TileCoord> Cells(int map, int minX, int width, int minZ, int depth)
        {
            var cells = new List<TileCoord>(width * depth);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                    cells.Add(new TileCoord(map, minX + x, minZ + z));
            }

            return cells;
        }

        private static float Mean(AtmosSimulation sim, List<TileCoord> cells)
        {
            float total = 0f;
            foreach (TileCoord coord in cells) total += sim.GetCellPressure(coord);
            return total / cells.Count;
        }

        private static float Gas(AtmosSimulation sim, List<TileCoord> cells, GasId gas)
        {
            float total = 0f;
            foreach (TileCoord coord in cells) total += sim.DebugGetMoles(coord, gas);
            return total;
        }

        private static float Moles(AtmosSimulation sim, List<TileCoord> cells)
        {
            float total = 0f;
            for (ushort gas = 0; gas < AtmosConstants.DefaultGasCount; gas++)
                total += Gas(sim, cells, new GasId(gas));
            return total;
        }

        private static void AssertSaneGas(AtmosSimulation sim, List<TileCoord> cells)
        {
            foreach (TileCoord coord in cells)
            {
                float pressure = sim.GetCellPressure(coord);
                Assert.That(float.IsNaN(pressure) || float.IsInfinity(pressure), Is.False, $"bad pressure at {coord.Grid}");
                for (ushort gas = 0; gas < AtmosConstants.DefaultGasCount; gas++)
                    Assert.That(sim.DebugGetMoles(coord, new GasId(gas)), Is.GreaterThanOrEqualTo(0f), $"negative gas at {coord.Grid}");
            }
        }
    }
}

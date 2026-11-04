using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    /// <summary>
    /// Room-scale targets for gas transport, run on a real <see cref="AtmosSimulation"/>: how fast a
    /// breach drains a room and how far its wind reaches. Numbers are logged as [edge-flow-ref].
    /// See Documents/architecture/2026-11_atmos-edge-flow.md.
    /// </summary>
    public class AtmosEdgeFlowReferenceTests
    {
        private const float MaxSeconds = 1200f;
        private const float OnsetDropKilopascals = 1f;

        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        // Decision 5's drain time and Decision 7's knockdown line: the design's "a tug across the room,
        // irresistible close to a breach".
        [Test]
        public void Room10x10_OneTileHole_DrainsAndPullsToDesign()
        {
            Metrics metrics = RunBreach(10, 10, "room 10x10");
            Assert.That(metrics.SecondsToHalf, Is.InRange(15f, 20f), "time to half pressure");
            Assert.That(metrics.SecondsToTenth, Is.LessThan(80f), "a breached room empties");
            Assert.That(metrics.FarOnset, Is.LessThan(3f), "the far wall feels the breach within seconds");
            Assert.That(metrics.PeakByDistance[1], Is.GreaterThanOrEqualTo(AtmosWind.KnockdownSpeed), "beside the hole is irresistible");
            Assert.That(metrics.PeakByDistance[3], Is.LessThan(AtmosWind.KnockdownSpeed), "three tiles out can still be fought");
            Assert.That(metrics.PeakByDistance[5], Is.GreaterThan(AtmosWind.MinSpeed * 2f), "mid-room feels a tug");
        }

        [Test]
        public void Corridor40x8_OneTileHole_DrainsSlowerThanARoomButStillPulls()
        {
            Metrics metrics = RunBreach(40, 8, "corridor 40x8");
            Assert.That(metrics.SecondsToHalf, Is.LessThan(100f), "time to half pressure");
            Assert.That(metrics.FarOnset, Is.LessThan(15f), "the far end feels the breach within seconds");
            Assert.That(metrics.PeakByDistance[5], Is.GreaterThan(AtmosWind.MinSpeed * 2f), "five tiles out feels a tug");
        }

        [Test]
        public void TwoRooms_DoorOpensOntoEvacuatedRoom()
        {
            const int roomSize = 6;
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, roomSize * 2 + 1, roomSize, out int map);

            int dividerX = roomSize + 1;
            for (int z = 1; z <= roomSize; z++)
            {
                TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(dividerX, 0, z));
                sim.UpdateCell(new TileCoord(map, dividerX, z));
            }

            List<TileCoord> full = Interior(map, 1, roomSize, roomSize);
            List<TileCoord> empty = Interior(map, dividerX + 1, roomSize, roomSize);
            foreach (TileCoord coord in empty)
            {
                sim.TryRemoveMoles(coord, AtmosConstants.Oxygen, float.MaxValue, out _, out _);
                sim.TryRemoveMoles(coord, AtmosConstants.Nitrogen, float.MaxValue, out _, out _);
            }

            int doorZ = roomSize / 2;
            var door = new TileCoord(map, dividerX, doorZ);
            context.Construction.TryClearTile(new Vector3(dividerX, 0, doorZ), TileLayer.Turf, Direction.North);
            sim.QueueCellRefresh(door);

            var farEmpty = new TileCoord(map, roomSize * 2 + 1, doorZ);
            var nearDoor = new TileCoord(map, dividerX - 1, doorZ);
            float settled = -1f, onset = -1f, peakWind = 0f;
            int ticks = Mathf.CeilToInt(MaxSeconds / AtmosConstants.TickInterval);
            for (int tick = 1; tick <= ticks && settled < 0f; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                float seconds = tick * AtmosConstants.TickInterval;
                peakWind = Mathf.Max(peakWind, WindSpeed(sim, nearDoor));
                if (onset < 0f && sim.GetCellPressure(farEmpty) >= OnsetDropKilopascals) onset = seconds;
                if (MeanPressure(sim, full) - MeanPressure(sim, empty) < 5f) settled = seconds;
            }

            Debug.Log($"[edge-flow-ref] two rooms: settled={Show(settled)} farOnset={Show(onset)} doorWind={peakWind:F2} m/s");
            Assert.That(settled, Is.InRange(0f, 15f), "an opened door gusts rather than seeps");
            Assert.That(onset, Is.InRange(0f, 2f), "far-field onset");
            Assert.That(peakWind, Is.GreaterThanOrEqualTo(AtmosWind.KnockdownSpeed), "the gust at the door knocks down");
        }

        private Metrics RunBreach(int width, int depth, string label)
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, width, depth, out int map);

            int holeZ = depth / 2;
            var hole = new TileCoord(map, width + 1, holeZ);

            // Destruction clears only the wall's own layer, so a real hole is a floored air tile
            // between the room and space. That neck about doubles drain time over a bare vacuum edge.
            context.Construction.TryClearTile(new Vector3(width + 1, 0, holeZ), TileLayer.Turf, Direction.North);
            sim.QueueCellRefresh(hole);
            sim.Tick(AtmosConstants.TickInterval);
            Assert.That(sim.TryGetCellIndex(hole, out int holeIndex), Is.True);
            Assert.That(sim.CellMeta[holeIndex].State, Is.Not.EqualTo(AtmosCellState.Blocked), "hole stayed sealed");
            Assert.That(sim.TryGetCellIndex(new TileCoord(map, width + 2, holeZ), out int spaceIndex), Is.True);
            Assert.That(sim.CellMeta[spaceIndex].State, Is.EqualTo(AtmosCellState.Vacuum), "hole does not open onto space");

            List<TileCoord> room = Interior(map, 1, width, depth);
            float start = MeanPressure(sim, room);
            float startMoles = TotalMoles(sim, room);
            var far = new TileCoord(map, 1, holeZ);
            float farStart = sim.GetCellPressure(far);

            var metrics = new Metrics { SecondsToHalf = -1f, SecondsToTenth = -1f, SecondsToHalfMoles = -1f, FarOnset = -1f };
            metrics.PeakByDistance = new float[6];
            int ticks = Mathf.CeilToInt(MaxSeconds / AtmosConstants.TickInterval);
            for (int tick = 1; tick <= ticks && metrics.SecondsToTenth < 0f; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                float seconds = tick * AtmosConstants.TickInterval;
                float pressure = MeanPressure(sim, room);
                if (metrics.SecondsToHalf < 0f && pressure <= start * 0.5f) metrics.SecondsToHalf = seconds;
                if (metrics.SecondsToHalfMoles < 0f && TotalMoles(sim, room) <= startMoles * 0.5f) metrics.SecondsToHalfMoles = seconds;
                if (metrics.SecondsToTenth < 0f && pressure <= start * 0.1f) metrics.SecondsToTenth = seconds;
                if (metrics.FarOnset < 0f && farStart - sim.GetCellPressure(far) >= OnsetDropKilopascals) metrics.FarOnset = seconds;
                for (int distance = 1; distance <= 5; distance++)
                {
                    var cell = new TileCoord(map, width + 1 - distance, holeZ);
                    metrics.PeakByDistance[distance] = Mathf.Max(metrics.PeakByDistance[distance], WindSpeed(sim, cell));
                }
            }


            Debug.Log(
                $"[edge-flow-ref] {label}: half={Show(metrics.SecondsToHalf)} halfMoles={Show(metrics.SecondsToHalfMoles)} tenth={Show(metrics.SecondsToTenth)} " +
                $"farOnset={Show(metrics.FarOnset)} wind by distance {string.Join(" ", metrics.PeakByDistance.Skip(1).Select(v => v.ToString("F2")))} m/s");
            return metrics;
        }

        private static float WindSpeed(AtmosSimulation sim, TileCoord coord) =>
            AtmosWind.Sample(sim, coord).magnitude;

        private static List<TileCoord> Interior(int map, int minX, int width, int depth)
        {
            var cells = new List<TileCoord>(width * depth);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                    cells.Add(new TileCoord(map, minX + x, 1 + z));
            }

            return cells;
        }

        private static float MeanPressure(AtmosSimulation sim, List<TileCoord> cells) =>
            cells.Sum(sim.GetCellPressure) / cells.Count;

        private static float TotalMoles(AtmosSimulation sim, List<TileCoord> cells)
        {
            float total = 0f;
            foreach (TileCoord coord in cells)
            {
                for (ushort gas = 0; gas < AtmosConstants.DefaultGasCount; gas++)
                    total += sim.DebugGetMoles(coord, new GasId(gas));
            }

            return total;
        }

        private static string Show(float seconds) => seconds < 0f ? $">{MaxSeconds:F0}s" : $"{seconds:F1}s";

        private struct Metrics
        {
            public float SecondsToHalf;
            public float SecondsToHalfMoles;
            public float SecondsToTenth;
            public float FarOnset;

            // Index is tiles into the room from the wall, along the line through the hole.
            public float[] PeakByDistance;
        }
    }
}

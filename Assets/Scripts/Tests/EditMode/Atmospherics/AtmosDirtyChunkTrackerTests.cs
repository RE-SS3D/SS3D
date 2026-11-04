using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosDirtyChunkTrackerTests
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
        public void DirtyChunkTracker_NotDirtyWhenNothingChangedBetweenUpdates()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 2, out _);

            var tracker = new AtmosDirtyChunkTracker();
            var buffer = new List<Vector2Int>();

            // First update establishes the baseline; everything reads as "changed" from the
            // zeroed-out previous state.
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            Assert.IsEmpty(buffer);
        }

        [Test]
        public void DirtyChunkTracker_AfterTick_ScansActiveCellsOnly_StaysCleanWhenSettled()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 2, out _);

            // Populate ActiveCells the same way production does before NetworkSync.
            simulation.Tick(AtmosConstants.TickInterval);
            Assert.Greater(simulation.ActiveCellCount, 0);

            var tracker = new AtmosDirtyChunkTracker();
            var buffer = new List<Vector2Int>();
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            Assert.IsEmpty(buffer);
        }

        [Test]
        public void DirtyChunkTracker_MarksChunkDirtyWhenTemperatureChangesMeaningfully()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 2, out int mapId);
            TileCoord coord = AtmosTestFixtures.InteriorCoord(mapId, 2);

            var tracker = new AtmosDirtyChunkTracker();
            var buffer = new List<Vector2Int>();
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            simulation.DebugSetTemperature(coord, 850f);
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            Assert.Contains(Vector2Int.zero, buffer);
        }

        [Test]
        public void DirtyChunkTracker_MarksChunkDirtyWhenFireIgnites()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);
            TileCoord coord = AtmosTestFixtures.InteriorCoord(mapId, 1);

            var tracker = new AtmosDirtyChunkTracker();
            var buffer = new List<Vector2Int>();
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            AtmosTestFixtures.IgnitePlasmaFire(simulation, coord, plasmaMoles: 10f, oxygenMoles: 40f);
            simulation.Tick(AtmosConstants.TickInterval);

            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            Assert.Contains(Vector2Int.zero, buffer);
        }

        [Test]
        public void DirtyChunkTracker_ReportsSlowDrift_AddedUpSinceTheLastReport()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 2, out int mapId);
            TileCoord coord = AtmosTestFixtures.InteriorCoord(mapId, 2);

            var tracker = new AtmosDirtyChunkTracker();
            var buffer = new List<Vector2Int>();
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);

            // Each step is far below any threshold; together they are a real change in the room.
            bool reported = false;
            for (int step = 0; step < 200 && !reported; step++)
            {
                simulation.DebugAddMoles(coord, AtmosConstants.Nitrogen, 0.02f);
                tracker.Update(simulation);
                tracker.ConsumeDirtyChunks(buffer);
                reported = buffer.Count > 0;
            }

            Assert.IsTrue(reported, "a slow drift reaches clients eventually");
        }

        [Test]
        public void DirtyChunkTracker_MarksChunkDirtyWhenWindStops()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 8, out int mapId);
            TileCoord source = AtmosTestFixtures.InteriorCoord(mapId, 4);
            simulation.DebugAddMoles(source, AtmosConstants.Nitrogen, 200f);

            var tracker = new AtmosDirtyChunkTracker();
            var buffer = new List<Vector2Int>();
            bool sawWind = false;
            for (int tick = 0; tick < 200; tick++)
            {
                simulation.Tick(AtmosConstants.TickInterval);
                tracker.Update(simulation);
                sawWind |= AtmosWind.Sample(simulation, new TileCoord(mapId, source.Grid.x + 1, source.Grid.y)).sqrMagnitude > 0.01f;
                tracker.ConsumeDirtyChunks(buffer);
            }

            Assert.IsTrue(sawWind, "the pulse raised a wind");
            Assert.That(AtmosWind.Sample(simulation, new TileCoord(mapId, source.Grid.x + 1, source.Grid.y)).magnitude, Is.LessThan(AtmosWind.MinSpeed));

            // Everything the wind dying changed has been reported by now.
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer);
            Assert.IsEmpty(buffer);
        }

        [Test]
        public void DirtyChunkTracker_Cap_SendsOldestFirst_AndKeepsTheRest()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 40, 8, out int mapId);

            var tracker = new AtmosDirtyChunkTracker();
            var buffer = new List<Vector2Int>();
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer, 1);
            Assert.That(buffer, Has.Count.EqualTo(1));
            Vector2Int first = buffer[0];

            // The waiting chunk goes out next, and the sent one can queue again behind it.
            simulation.DebugSetTemperature(new TileCoord(mapId, first.x * AtmosConstants.ChunkSize + 1, 1), 850f);
            tracker.Update(simulation);
            tracker.ConsumeDirtyChunks(buffer, 1);
            Assert.That(buffer, Has.Count.EqualTo(1));
            Assert.That(buffer[0], Is.Not.EqualTo(first));
            tracker.ConsumeDirtyChunks(buffer);
            Assert.That(buffer, Does.Contain(first));
        }
    }
}

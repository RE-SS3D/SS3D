using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosChunkPatchBuilderTests
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
        public void ChunkPatchBuilder_PressureAndTemperatureMatchSimulation()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 2, out int mapId);

            var coord = AtmosTestFixtures.InteriorCoord(mapId, 2);
            simulation.DebugSetTemperature(coord, 850f);

            var builder = new AtmosChunkPatchBuilder();
            Assert.IsTrue(simulation.TryGetChunkIndex(Vector2Int.zero, out int chunkIndex));
            AtmosChunkPatch patch = builder.Build(simulation, chunkIndex, Vector2Int.zero);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));
            int local = LocalIndex(coord);

            Assert.That(patch.Pressure[local], Is.EqualTo(info.Pressure).Within(0.01f));
            Assert.That(patch.Temperature[local], Is.EqualTo(info.Temperature).Within(0.01f));
            Assert.AreEqual(AtmosCellVisualEncoding.EncodeMask(info.State), patch.Mask[local]);
        }

        [Test]
        public void ChunkPatchBuilder_FireMatchesSimulationBurnIntensity()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);

            var coord = AtmosTestFixtures.InteriorCoord(mapId, 1);
            AtmosTestFixtures.IgnitePlasmaFire(simulation, coord, plasmaMoles: 10f, oxygenMoles: 40f);
            simulation.Tick(AtmosConstants.TickInterval);

            var builder = new AtmosChunkPatchBuilder();
            Assert.IsTrue(simulation.TryGetChunkIndex(Vector2Int.zero, out int chunkIndex));
            AtmosChunkPatch patch = builder.Build(simulation, chunkIndex, Vector2Int.zero);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));
            int local = LocalIndex(coord);

            Assert.That(patch.FireIntensity[local], Is.EqualTo(info.BurnIntensity).Within(0.01f));
        }

        private static int LocalIndex(TileCoord coord) => coord.Grid.y * AtmosConstants.ChunkSize + coord.Grid.x;
    }
}

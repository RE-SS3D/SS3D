using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosNeighbourTests
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
        public void AdjacentPlenumCells_LinkEastNeighbour()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(0, 0, 0));
            TileMapTestUtilities.PlacePlenum(context, new Vector3(1, 0, 0));

            using var simulation = CreateSimulation(context);
            simulation.CreateChunk(CreateChunkRef(context.Map, Vector2Int.zero));
            simulation.UpdateCell(new TileCoord(context.Map.MapId, 0, 0));
            simulation.UpdateCell(new TileCoord(context.Map.MapId, 1, 0));

            Assert.IsTrue(simulation.TryGetCellDebugInfo(new TileCoord(context.Map.MapId, 0, 0), out AtmosCellDebugInfo west));
            Assert.Greater(west.Neighbours.East, -1);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(new TileCoord(context.Map.MapId, 1, 0), out AtmosCellDebugInfo east));
            Assert.AreEqual(AtmosCellState.Active, east.State);
        }

        [Test]
        public void SealedRoomCells_DoNotLinkThroughVacuum()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(0, 0, 0));

            using var simulation = CreateSimulation(context);
            simulation.CreateChunk(CreateChunkRef(context.Map, Vector2Int.zero));
            simulation.UpdateCell(new TileCoord(context.Map.MapId, 0, 0));

            Assert.IsTrue(simulation.TryGetCellDebugInfo(new TileCoord(context.Map.MapId, 0, 0), out AtmosCellDebugInfo center));
            Assert.Greater(center.Neighbours.East, -1);
            Assert.IsTrue(simulation.TryGetCellDebugInfo(new TileCoord(context.Map.MapId, 1, 0), out AtmosCellDebugInfo east));
            Assert.AreEqual(AtmosCellState.Vacuum, east.State);
            Assert.AreEqual(AtmosCellState.Active, center.State);
        }

        private static AtmosSimulation CreateSimulation(TileMapTestUtilities.MapContext context)
        {
            return new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
        }

        private static TileChunkRef CreateChunkRef(TileMap map, Vector2Int chunkKey)
        {
            return new TileChunkRef
            {
                MapId = map.MapId,
                ChunkKey = chunkKey,
                Origin = new Vector3(chunkKey.x * AtmosConstants.ChunkSize, 0, chunkKey.y * AtmosConstants.ChunkSize),
            };
        }
    }
}

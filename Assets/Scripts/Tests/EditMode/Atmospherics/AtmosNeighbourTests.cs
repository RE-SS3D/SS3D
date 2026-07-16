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

        [Test]
        public void WindowWalls_BlockGasFlowLikeSolidWalls()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            const int interiorSize = 3;
            const int origin = 0;
            int outerSize = interiorSize + 2;

            for (int x = 0; x < outerSize; x++)
            {
                for (int z = 0; z < outerSize; z++)
                    TileMapTestUtilities.PlacePlenum(context, new Vector3(origin + x, 0, origin + z));
            }

            for (int x = 0; x < outerSize; x++)
            {
                for (int z = 0; z < outerSize; z++)
                {
                    bool isPerimeter = x == 0 || z == 0 || x == outerSize - 1 || z == outerSize - 1;
                    if (!isPerimeter)
                        continue;

                    Vector3 position = new Vector3(origin + x, 0, origin + z);
                    if (x == outerSize - 1 && z == outerSize / 2)
                        TileMapTestUtilities.PlaceWindow(context, position);
                    else
                        TileMapTestUtilities.PlaceAirtightWall(context, position);
                }
            }

            Assert.IsTrue(
                context.Query.TryGetOccupancy(new TileCoord(context.Map.MapId, origin + outerSize - 1, origin + outerSize / 2), out TileOccupancy windowOccupancy));
            Assert.IsTrue(windowOccupancy.IsWindow);
            Assert.IsTrue(windowOccupancy.IsAirtight);
            Assert.IsFalse(windowOccupancy.BlocksVision);
            Assert.AreNotEqual(0, windowOccupancy.BlockedEdges);

            using var simulation = CreateSimulation(context);
            simulation.CreateChunk(CreateChunkRef(context.Map, Vector2Int.zero));
            for (int x = 0; x < outerSize; x++)
            {
                for (int z = 0; z < outerSize; z++)
                    simulation.UpdateCell(new TileCoord(context.Map.MapId, origin + x, origin + z));
            }

            var interiorCoord = new TileCoord(context.Map.MapId, origin + outerSize - 2, origin + outerSize / 2);
            var windowCoord = new TileCoord(context.Map.MapId, origin + outerSize - 1, origin + outerSize / 2);
            var exteriorCoord = new TileCoord(context.Map.MapId, origin + outerSize, origin + outerSize / 2);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(interiorCoord, out AtmosCellDebugInfo interior));
            Assert.IsTrue(simulation.TryGetCellDebugInfo(windowCoord, out AtmosCellDebugInfo window));
            Assert.IsTrue(simulation.TryGetCellDebugInfo(exteriorCoord, out AtmosCellDebugInfo exterior));
            Assert.AreEqual(AtmosCellState.Active, interior.State);
            Assert.AreEqual(AtmosCellState.Blocked, window.State);
            Assert.AreEqual(AtmosCellState.Vacuum, exterior.State);
            Assert.AreEqual(-1, interior.Neighbours.East);
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

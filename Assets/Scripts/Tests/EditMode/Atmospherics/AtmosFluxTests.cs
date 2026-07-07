using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosFluxTests
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
        public void SealedRoom_ConservesMolesOverTicks()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            BuildSealedRoom(context, 2);

            using var simulation = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            simulation.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });

            for (int x = 0; x < 2; x++)
            {
                for (int z = 0; z < 2; z++)
                    simulation.UpdateCell(new TileCoord(context.Map.MapId, x, z));
            }

            float initialMoles = simulation.GetTotalMoles();
            Assert.Greater(initialMoles, 0f);

            for (int tick = 0; tick < 10; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            Assert.AreEqual(initialMoles, simulation.GetTotalMoles(), 0.01f);
        }

        [Test]
        public void RoomAdjacentToVacuum_LosesMoles()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(0, 0, 0));

            using var simulation = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            simulation.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });
            simulation.UpdateCell(new TileCoord(context.Map.MapId, 0, 0));

            float initialMoles = simulation.GetTotalMoles();
            for (int tick = 0; tick < 20; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            Assert.Less(simulation.GetTotalMoles(), initialMoles);
        }

        private static void BuildSealedRoom(TileMapTestUtilities.MapContext context, int size)
        {
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                    TileMapTestUtilities.PlacePlenum(context, new Vector3(x, 0, z));
            }
        }
    }
}

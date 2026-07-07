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

        [Test]
        public void SealedRoom_ConservesThermalEnergyWithGradient()
        {
            const int size = 3;
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            BuildSealedRoom(context, size);

            using var simulation = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            simulation.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                    simulation.UpdateCell(new TileCoord(context.Map.MapId, x, z));
            }

            // Heat one corner so a pressure gradient forms and advected moles carry enthalpy.
            simulation.DebugSetTemperature(new TileCoord(context.Map.MapId, 0, 0), 1000f);

            float initialEnergy = simulation.GetTotalThermalEnergy();
            float initialMoles = simulation.GetTotalMoles();
            Assert.Greater(initialEnergy, 0f);

            for (int tick = 0; tick < 30; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            // Advection must conserve both moles and total thermal energy in a sealed room.
            Assert.AreEqual(initialMoles, simulation.GetTotalMoles(), initialMoles * 0.005f);
            Assert.AreEqual(initialEnergy, simulation.GetTotalThermalEnergy(), initialEnergy * 0.005f);
        }

        [Test]
        public void SealedRoom_ConductionEqualizesTemperature()
        {
            const int size = 3;
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            BuildSealedRoom(context, size);

            using var simulation = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            simulation.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                    simulation.UpdateCell(new TileCoord(context.Map.MapId, x, z));
            }

            simulation.DebugSetTemperature(new TileCoord(context.Map.MapId, 0, 0), 1000f);

            float initialEnergy = simulation.GetTotalThermalEnergy();
            GetTemperatureSpread(simulation, context.Map.MapId, size, out float initialMin, out float initialMax);
            float initialSpread = initialMax - initialMin;

            for (int tick = 0; tick < 120; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            GetTemperatureSpread(simulation, context.Map.MapId, size, out float finalMin, out float finalMax);
            float finalSpread = finalMax - finalMin;

            // Conduction should pull temperatures together while energy stays conserved.
            Assert.Less(finalSpread, initialSpread * 0.25f);
            Assert.AreEqual(initialEnergy, simulation.GetTotalThermalEnergy(), initialEnergy * 0.005f);
        }

        [Test]
        public void PlasmaFire_ConsumesPlasmaAndProducesCarbonDioxide()
        {
            const int size = 3;
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            BuildSealedRoom(context, size);

            using var simulation = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            simulation.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                    simulation.UpdateCell(new TileCoord(context.Map.MapId, x, z));
            }

            var center = new TileCoord(context.Map.MapId, 1, 1);
            simulation.DebugAddMoles(center, AtmosConstants.Plasma, 10f);
            simulation.DebugAddMoles(center, AtmosConstants.Oxygen, 40f);
            simulation.DebugSetTemperature(center, 1000f);

            float initialPlasma = SumGas(simulation, context.Map.MapId, size, AtmosConstants.Plasma);
            Assert.Greater(initialPlasma, 0f);

            for (int tick = 0; tick < 60; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            float finalPlasma = SumGas(simulation, context.Map.MapId, size, AtmosConstants.Plasma);
            float finalCarbonDioxide = SumGas(simulation, context.Map.MapId, size, AtmosConstants.CarbonDioxide);

            // Combustion should consume plasma and produce carbon dioxide.
            Assert.Less(finalPlasma, initialPlasma * 0.8f);
            Assert.Greater(finalCarbonDioxide, 0f);
        }

        private static float SumGas(AtmosSimulation simulation, int mapId, int size, GasId gasId)
        {
            float total = 0f;
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                    total += simulation.DebugGetMoles(new TileCoord(mapId, x, z), gasId);
            }

            return total;
        }

        private static void GetTemperatureSpread(
            AtmosSimulation simulation, int mapId, int size, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    Assert.IsTrue(simulation.TryGetCellDebugInfo(new TileCoord(mapId, x, z), out AtmosCellDebugInfo info));
                    min = Mathf.Min(min, info.Temperature);
                    max = Mathf.Max(max, info.Temperature);
                }
            }
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

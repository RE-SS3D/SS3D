using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosCombustionTests
    {
        private const int RoomSize = 3;
        private const int Interior = 1;

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
        public void PlasmaFire_AboveIgnition_ProducesBurnIntensity()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int mapId);

            TileCoord center = InteriorCoord(mapId);
            AtmosTestFixtures.IgnitePlasmaFire(simulation, center, plasmaMoles: 10f, oxygenMoles: 40f);

            simulation.Tick(AtmosConstants.TickInterval);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(center, out AtmosCellDebugInfo info));
            Assert.Greater(info.BurnIntensity, 0f);
            Assert.LessOrEqual(info.BurnIntensity, 1f);
        }

        [Test]
        public void PlasmaFire_BelowIgnition_DoesNotBurn()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int mapId);

            TileCoord center = InteriorCoord(mapId);
            simulation.DebugAddMoles(center, AtmosConstants.Plasma, 10f);
            simulation.DebugAddMoles(center, AtmosConstants.Oxygen, 40f);
            simulation.DebugSetTemperature(center, 350f);

            float initialPlasma = AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.Plasma);
            simulation.Tick(AtmosConstants.TickInterval);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(center, out AtmosCellDebugInfo info));
            Assert.AreEqual(0f, info.BurnIntensity);
            Assert.AreEqual(
                initialPlasma,
                AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.Plasma),
                0.05f);
        }

        [Test]
        public void PlasmaFire_OxygenStarvation_StopsBurningWithPlasmaRemaining()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int mapId);

            TileCoord center = InteriorCoord(mapId);
            simulation.DebugAddMoles(center, AtmosConstants.Plasma, 200f);
            simulation.DebugSetTemperature(center, 1000f);

            float initialPlasma = AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.Plasma);

            for (int tick = 0; tick < 80; tick++)
            {
                simulation.Tick(AtmosConstants.TickInterval);
                if (!simulation.TryGetCellDebugInfo(center, out AtmosCellDebugInfo info))
                    continue;

                if (info.BurnIntensity <= 0.001f)
                    break;
            }

            Assert.IsTrue(simulation.TryGetCellDebugInfo(center, out AtmosCellDebugInfo finalInfo));
            Assert.AreEqual(0f, finalInfo.BurnIntensity, 0.001f);

            float remainingPlasma = AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.Plasma);
            Assert.Greater(remainingPlasma, initialPlasma * 0.5f);
        }

        [Test]
        public void PlasmaFire_Stoichiometry_ConsumesTwoOxygenPerPlasma()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int mapId);

            TileCoord center = InteriorCoord(mapId);
            AtmosTestFixtures.IgnitePlasmaFire(simulation, center, plasmaMoles: 8f, oxygenMoles: 40f);

            float initialPlasma = AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.Plasma);
            float initialOxygen = AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.Oxygen);
            float initialCarbonDioxide = AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.CarbonDioxide);

            for (int tick = 0; tick < 15; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            float plasmaBurned = initialPlasma - AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.Plasma);
            float oxygenConsumed = initialOxygen - AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.Oxygen);
            float carbonDioxideProduced = AtmosTestFixtures.SumGas(simulation, mapId, RoomSize, AtmosConstants.CarbonDioxide)
                - initialCarbonDioxide;

            Assert.Greater(plasmaBurned, 0.5f);
            Assert.That(oxygenConsumed, Is.EqualTo(plasmaBurned * AtmosFluxConstants.OxygenPerPlasma).Within(0.5f));
            Assert.That(carbonDioxideProduced, Is.EqualTo(plasmaBurned).Within(0.5f));
        }

        [Test]
        public void PlasmaFire_HeatSpreadsToNeighbour()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int mapId);

            TileCoord center = InteriorCoord(mapId);
            TileCoord west = new TileCoord(mapId, Interior - 1, Interior);
            AtmosTestFixtures.IgnitePlasmaFire(simulation, center, plasmaMoles: 10f, oxygenMoles: 40f);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(west, out AtmosCellDebugInfo initialWest));
            float initialWestTemp = initialWest.Temperature;

            for (int tick = 0; tick < 20; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(west, out AtmosCellDebugInfo finalWest));
            Assert.Greater(finalWest.Temperature, initialWestTemp + 10f);
        }

        [Test]
        public void PlasmaFire_ConservesMolesInSealedRoom()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int mapId);

            TileCoord center = InteriorCoord(mapId);
            AtmosTestFixtures.IgnitePlasmaFire(simulation, center, plasmaMoles: 10f, oxygenMoles: 40f);

            float initialMoles = simulation.GetTotalMoles();

            // Edge cells in plenum-only maps leak to vacuum over long runs; keep this short so
            // combustion stoichiometry is what we are measuring, not breach venting.
            for (int tick = 0; tick < 5; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            Assert.AreEqual(initialMoles, simulation.GetTotalMoles(), initialMoles * 0.01f);
        }

        static TileCoord InteriorCoord(int mapId) => new TileCoord(mapId, Interior, Interior);
    }
}

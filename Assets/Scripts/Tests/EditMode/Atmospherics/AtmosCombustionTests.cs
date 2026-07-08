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

            TileCoord center = AtmosTestFixtures.InteriorCoord(mapId, RoomSize);
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

            TileCoord center = AtmosTestFixtures.InteriorCoord(mapId, RoomSize);
            simulation.DebugAddMoles(center, AtmosConstants.Plasma, 10f);
            simulation.DebugAddMoles(center, AtmosConstants.Oxygen, 40f);
            simulation.DebugSetTemperature(center, 350f);

            float carbonDioxideBefore = simulation.DebugGetMoles(center, AtmosConstants.CarbonDioxide);
            simulation.Tick(AtmosConstants.TickInterval);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(center, out AtmosCellDebugInfo info));
            Assert.AreEqual(0f, info.BurnIntensity);
            // Diffusion redistributes plasma quickly; CO₂ is the combustion fingerprint.
            Assert.AreEqual(
                carbonDioxideBefore,
                simulation.DebugGetMoles(center, AtmosConstants.CarbonDioxide),
                0.01f);
        }

        [Test]
        public void PlasmaFire_OxygenStarvation_StopsBurningWithPlasmaRemaining()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int mapId);

            TileCoord center = AtmosTestFixtures.InteriorCoord(mapId, RoomSize);
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
        public void ReactAtmosJob_Stoichiometry_ConsumesTwoOxygenPerPlasma()
        {
            ReactAtmosJobResult result = AtmosTestFixtures.RunReactAtmosJobOnCell(
                plasmaMoles: 8f,
                oxygenMoles: 40f,
                carbonDioxideMoles: 0f,
                nitrogenMoles: 83f,
                temperature: 1000f,
                deltaTime: AtmosConstants.TickInterval);

            Assert.Greater(result.PlasmaBurned, 0.5f);
            Assert.That(result.OxygenConsumed, Is.EqualTo(result.PlasmaBurned * AtmosFluxConstants.OxygenPerPlasma).Within(0.01f));
            Assert.That(result.CarbonDioxideProduced, Is.EqualTo(result.PlasmaBurned).Within(0.01f));
        }

        [Test]
        public void PlasmaFire_HeatSpreadsToNeighbour()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int mapId);

            TileCoord center = AtmosTestFixtures.InteriorCoord(mapId, RoomSize);
            TileCoord west = new TileCoord(mapId, center.Grid.x - 1, center.Grid.y);
            AtmosTestFixtures.IgnitePlasmaFire(simulation, center, plasmaMoles: 10f, oxygenMoles: 40f);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(west, out AtmosCellDebugInfo initialWest));
            float initialWestTemp = initialWest.Temperature;

            for (int tick = 0; tick < 20; tick++)
                simulation.Tick(AtmosConstants.TickInterval);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(west, out AtmosCellDebugInfo finalWest));
            Assert.Greater(finalWest.Temperature, initialWestTemp + 10f);
        }

        [Test]
        public void ReactAtmosJob_CombustionNetMoleLoss()
        {
            ReactAtmosJobResult result = AtmosTestFixtures.RunReactAtmosJobOnCell(
                plasmaMoles: 10f,
                oxygenMoles: 40f,
                carbonDioxideMoles: 0f,
                nitrogenMoles: 83f,
                temperature: 1000f,
                deltaTime: AtmosConstants.TickInterval);

            Assert.Greater(result.PlasmaBurned, 0.5f);
            // 1 plasma + 2 O2 -> 1 CO2 removes two net moles per plasma burned.
            Assert.AreEqual(result.TotalMolesBefore - result.PlasmaBurned * 2f, result.TotalMolesAfter, 0.01f);
        }

        [Test]
        public void ReactAtmosJob_BelowIgnition_DoesNotBurn()
        {
            ReactAtmosJobResult result = AtmosTestFixtures.RunReactAtmosJobOnCell(
                plasmaMoles: 10f,
                oxygenMoles: 40f,
                carbonDioxideMoles: 0f,
                nitrogenMoles: 83f,
                temperature: 350f,
                deltaTime: AtmosConstants.TickInterval);

            Assert.AreEqual(0f, result.PlasmaBurned, 0.001f);
            Assert.AreEqual(0f, result.BurnIntensity, 0.001f);
            Assert.AreEqual(0f, result.CarbonDioxideProduced, 0.001f);
        }

    }
}

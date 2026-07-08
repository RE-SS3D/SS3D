using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosVisualMetricsTests
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
        public void HotCellAtConstantMoles_HasLowGasFogDensity()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);

            var coord = AtmosTestFixtures.InteriorCoord(mapId, 1);
            simulation.DebugSetTemperature(coord, 2200f);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));
            AtmosCellVisualMetrics metrics = AtmosVisualMetrics.Compute(info, simulation);

            Assert.Less(metrics.GasFogDensity, 0.05f);
        }

        [Test]
        public void OverpressureAtRoomTemperature_IncreasesGasFogDensity()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);

            var coord = AtmosTestFixtures.InteriorCoord(mapId, 1);
            simulation.DebugAddMoles(coord, AtmosConstants.Nitrogen, 80f);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));
            AtmosCellVisualMetrics metrics = AtmosVisualMetrics.Compute(info, simulation);

            Assert.Greater(metrics.GasFogDensity, 0.1f);
        }

        [Test]
        public void ActiveFire_SuppressesCo2MidPlumeSmoke()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);

            var coord = AtmosTestFixtures.InteriorCoord(mapId, 1);
            simulation.DebugAddMoles(coord, AtmosConstants.CarbonDioxide, 12f);
            simulation.DebugSetTemperature(coord, 1500f);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo coldFireInfo));
            coldFireInfo.BurnIntensity = 0f;
            AtmosCellVisualMetrics withoutFire = AtmosVisualMetrics.Compute(coldFireInfo, simulation);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo hotFireInfo));
            hotFireInfo.BurnIntensity = 0.8f;
            AtmosCellVisualMetrics withFire = AtmosVisualMetrics.Compute(hotFireInfo, simulation);

            Assert.Greater(withoutFire.Co2SmokeDriveMid, withFire.Co2SmokeDriveMid);
        }

        [Test]
        public void DepletedPlasmaAfterBurn_SuppressesGlowEstimate()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);

            var coord = AtmosTestFixtures.InteriorCoord(mapId, 1);
            simulation.DebugAddMoles(coord, AtmosConstants.Plasma, 0.01f);
            simulation.DebugSetTemperature(coord, 2000f);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));
            info.BurnIntensity = 0f;
            AtmosCellVisualMetrics metrics = AtmosVisualMetrics.Compute(info, simulation);

            Assert.AreEqual(0f, metrics.PlasmaGlowEstimate, 0.0001f);
        }

        [Test]
        public void HotCell_HasNonZeroDistortionWeightWithoutDenseFog()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);

            var coord = AtmosTestFixtures.InteriorCoord(mapId, 1);
            simulation.DebugSetTemperature(coord, 1500f);

            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));
            AtmosCellVisualMetrics metrics = AtmosVisualMetrics.Compute(info, simulation);

            Assert.Less(metrics.GasFogDensity, 0.05f);
            Assert.Greater(metrics.DistortionWeightEstimate, 0.2f);
        }
    }
}

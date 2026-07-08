using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosGpuUploaderTests
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
        public void GpuUploader_PressureAtlasMatchesSimulation()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 2, out int mapId);

            var coord = new TileCoord(mapId, 1, 1);
            using var uploader = new AtmosGpuUploader();
            uploader.Refresh(simulation);

            AssertAtlasScalarMatches(simulation, uploader, coord, snapshot => snapshot.Pressure, info => info.Pressure);
        }

        [Test]
        public void GpuUploader_TemperatureAtlasMatchesSimulation()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 2, out int mapId);

            var coord = new TileCoord(mapId, 1, 1);
            simulation.DebugSetTemperature(coord, 850f);

            using var uploader = new AtmosGpuUploader();
            uploader.Refresh(simulation);

            AssertAtlasScalarMatches(simulation, uploader, coord, snapshot => snapshot.Temperature, info => info.Temperature);
        }

        [Test]
        public void GpuUploader_FireAtlasMatchesSimulationBurnIntensity()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);

            var coord = new TileCoord(mapId, 0, 0);
            AtmosTestFixtures.IgnitePlasmaFire(simulation, coord, plasmaMoles: 10f, oxygenMoles: 40f);
            simulation.Tick(AtmosConstants.TickInterval);

            using var uploader = new AtmosGpuUploader();
            uploader.Refresh(simulation);

            AssertAtlasScalarMatches(simulation, uploader, coord, snapshot => snapshot.FireIntensity, info => info.BurnIntensity);
        }

        [Test]
        public void GpuUploader_VisualFireDecaysBetweenUploadsWhenSimBurnStops()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            using var simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 1, out int mapId);

            var coord = new TileCoord(mapId, 0, 0);
            AtmosTestFixtures.IgnitePlasmaFire(simulation, coord, plasmaMoles: 10f, oxygenMoles: 40f);
            simulation.Tick(AtmosConstants.TickInterval);

            using var uploader = new AtmosGpuUploader();
            uploader.Refresh(simulation);
            var snapshot = uploader.BuildSnapshot(GasVisualProfileBuilder.CoreDefaults);
            float firstFire = AtmosTestFixtures.SampleSnapshotScalar(snapshot, coord, snapshot.FireIntensity);
            Assert.Greater(firstFire, 0.01f);

            simulation.DebugSetTemperature(coord, 300f);
            simulation.Tick(AtmosConstants.TickInterval);
            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));
            Assert.AreEqual(0f, info.BurnIntensity, 0.001f);

            uploader.Refresh(simulation);
            snapshot = uploader.BuildSnapshot(GasVisualProfileBuilder.CoreDefaults);
            float decayedFire = AtmosTestFixtures.SampleSnapshotScalar(snapshot, coord, snapshot.FireIntensity);

            Assert.That(decayedFire, Is.EqualTo(firstFire * AtmosVisualMetrics.VisualFireDecayPerTick).Within(0.01f));
        }

        private static void AssertAtlasScalarMatches(
            AtmosSimulation simulation,
            AtmosGpuUploader uploader,
            TileCoord coord,
            System.Func<SS3D.Rendering.URP.AtmosRenderContext.Snapshot, Texture2D> textureSelector,
            System.Func<AtmosCellDebugInfo, float> expectedValue)
        {
            Assert.IsTrue(uploader.IsValid);
            var snapshot = uploader.BuildSnapshot(GasVisualProfileBuilder.CoreDefaults);
            Assert.IsTrue(snapshot.Valid);
            Assert.IsTrue(simulation.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info));

            float atlasValue = AtmosTestFixtures.SampleSnapshotScalar(snapshot, coord, textureSelector(snapshot));
            Assert.That(atlasValue, Is.EqualTo(expectedValue(info)).Within(0.01f));
        }
    }
}

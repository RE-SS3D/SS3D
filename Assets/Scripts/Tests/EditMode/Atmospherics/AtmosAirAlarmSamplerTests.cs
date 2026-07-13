using EditorTests.Atmospherics;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;
using SS3D.Tests;

namespace EditorTests.Atmospherics
{
    public class AtmosAirAlarmSamplerTests : EditModeTest
    {
        [Test]
        public void TrySampleTile_ReturnsMoleFractionsForSeededCell()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            using AtmosSimulation simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 3, out int mapId);
            TileCoord coord = AtmosTestFixtures.InteriorCoord(mapId, 3);

            simulation.DebugAddMoles(coord, AtmosConstants.Oxygen, 21f);
            simulation.DebugAddMoles(coord, AtmosConstants.Nitrogen, 79f);

            Assert.IsTrue(AtmosAreaSampler.TrySampleTile(coord, simulation, out AtmosAreaSample sample));
            Assert.AreEqual(1, sample.CellCount);
            Assert.AreEqual(0.21f, sample.OxygenMoleFraction, 0.01f);
            Assert.AreEqual(0.79f, sample.NitrogenMoleFraction, 0.01f);
            Assert.Greater(sample.AveragePressureKpa, 0f);
            Assert.Greater(sample.TemperatureKelvin, 0f);
        }

        [Test]
        public void TrySampleTile_ReturnsPlasmaFraction()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);
            using AtmosSimulation simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 3, out int mapId);
            TileCoord coord = AtmosTestFixtures.InteriorCoord(mapId, 3);

            simulation.DebugAddMoles(coord, AtmosConstants.Oxygen, 50f);
            simulation.DebugAddMoles(coord, AtmosConstants.Nitrogen, 40f);
            simulation.DebugAddMoles(coord, AtmosConstants.Plasma, 10f);

            Assert.IsTrue(AtmosAreaSampler.TrySampleTile(coord, simulation, out AtmosAreaSample sample));
            Assert.AreEqual(0.1f, sample.PlasmaMoleFraction, 0.01f);
        }
    }
}

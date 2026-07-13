using EditorTests.Atmospherics;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;

namespace EditorTests.Atmospherics
{
    public class AtmosAirAlarmSamplerTests : EditModeTest
    {
        [Test]
        public void TrySampleTile_ReturnsMoleFractionsForSeededCell()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateMapContext();
            AtmosSimulation simulation = AtmosTestFixtures.CreateSealedRoomSimulation(context, 3, out int mapId);
            TileCoord coord = AtmosTestFixtures.InteriorCoord(mapId, 3);

            simulation.DebugAddMoles(coord, AtmosConstants.Oxygen, 21f);
            simulation.DebugAddMoles(coord, AtmosConstants.Nitrogen, 79f);

            Assert.IsTrue(AtmosAreaSampler.TrySampleTile(coord, simulation, out AtmosAreaSample sample));
            Assert.AreEqual(1, sample.CellCount);
            Assert.AreEqual(0.21f, sample.OxygenMoleFraction, 0.01f);
            Assert.Greater(sample.AveragePressureKpa, 0f);
        }
    }
}

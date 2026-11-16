using NUnit.Framework;
using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    public class AtmosClientAtlasTests
    {
        [Test]
        public void ClientAtlas_AppliedPatchMatchesSampledSnapshot()
        {
            using var atlas = new AtmosClientAtlas();
            AtmosChunkPatch patch = CreatePatch(mapId: 5, chunkKey: Vector2Int.zero, pressureSeed: 100f);

            atlas.ApplyChunkPatch(patch);

            Assert.IsTrue(atlas.IsValid);
            AtmosRenderContext.Snapshot snapshot = atlas.BuildSnapshot(GasVisualProfileBuilder.CoreDefaults);
            Assert.IsTrue(snapshot.Valid);

            var coord = new TileCoord(5, 3, 4);
            int local = 4 * AtmosConstants.ChunkSize + 3;
            float sampled = AtmosTestFixtures.SampleSnapshotScalar(snapshot, coord, snapshot.Pressure);

            Assert.That(sampled, Is.EqualTo(patch.Pressure[local]).Within(0.01f));
        }

        [Test]
        public void ClientAtlas_GrowsToCoverMultipleChunksWithoutLosingEarlierData()
        {
            using var atlas = new AtmosClientAtlas();
            AtmosChunkPatch first = CreatePatch(mapId: 5, chunkKey: Vector2Int.zero, pressureSeed: 100f);
            atlas.ApplyChunkPatch(first);

            AtmosChunkPatch second = CreatePatch(mapId: 5, chunkKey: new Vector2Int(1, 0), pressureSeed: 200f);
            atlas.ApplyChunkPatch(second);

            AtmosRenderContext.Snapshot snapshot = atlas.BuildSnapshot(GasVisualProfileBuilder.CoreDefaults);
            Assert.IsTrue(snapshot.Valid);

            var firstCoord = new TileCoord(5, 3, 4);
            int firstLocal = 4 * AtmosConstants.ChunkSize + 3;
            float firstSampled = AtmosTestFixtures.SampleSnapshotScalar(snapshot, firstCoord, snapshot.Pressure);
            Assert.That(firstSampled, Is.EqualTo(first.Pressure[firstLocal]).Within(0.01f));

            var secondCoord = new TileCoord(5, AtmosConstants.ChunkSize + 3, 4);
            float secondSampled = AtmosTestFixtures.SampleSnapshotScalar(snapshot, secondCoord, snapshot.Pressure);
            Assert.That(secondSampled, Is.EqualTo(second.Pressure[firstLocal]).Within(0.01f));
        }

        private static AtmosChunkPatch CreatePatch(int mapId, Vector2Int chunkKey, float pressureSeed)
        {
            var patch = new AtmosChunkPatch
            {
                MapId = mapId,
                ChunkKey = chunkKey,
                Pressure = new float[AtmosChunkPatch.CellCount],
                Temperature = new float[AtmosChunkPatch.CellCount],
                Composition = new Color32[AtmosChunkPatch.CellCount],
                Flow = new float[AtmosChunkPatch.CellCount * 2],
                FireIntensity = new float[AtmosChunkPatch.CellCount],
                Mask = new byte[AtmosChunkPatch.CellCount],
            };

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
            {
                patch.Pressure[i] = pressureSeed + i;
                patch.Temperature[i] = 293.15f;
                patch.Mask[i] = AtmosCellVisualEncoding.MaskSimulated;
                patch.Flow[i * 2] = 0.5f;
                patch.Flow[i * 2 + 1] = 0.5f;
            }

            return patch;
        }
    }
}

using FishNet.Serializing;
using NUnit.Framework;
using SS3D.Systems.Atmospherics.Visualization;
using System;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    public class AtmosChunkPatchSerializerTests
    {
        [Test]
        public void AtmosChunkPatchSerializer_RoundTrips()
        {
            AtmosChunkPatch original = CreatePatch();

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteAtmosChunkPatch(original);

            ArraySegment<byte> segment = writer.GetArraySegment();
            Assert.That(segment.Count, Is.LessThanOrEqualTo(AtmosChunkPatch.WireBytes), "the traffic budget is counted in WireBytes");
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            AtmosChunkPatch roundTripped = reader.ReadAtmosChunkPatch();

            Assert.AreEqual(original.MapId, roundTripped.MapId);
            Assert.AreEqual(original.ChunkKey, roundTripped.ChunkKey);
            Assert.AreEqual(AtmosChunkPatch.CellCount, roundTripped.Pressure.Length);
            Assert.AreEqual(AtmosChunkPatch.CellCount, roundTripped.Temperature.Length);
            Assert.AreEqual(AtmosChunkPatch.CellCount, roundTripped.Composition.Length);
            Assert.AreEqual(AtmosChunkPatch.CellCount * 2, roundTripped.Flow.Length);
            Assert.AreEqual(AtmosChunkPatch.CellCount, roundTripped.FireIntensity.Length);
            Assert.AreEqual(AtmosChunkPatch.CellCount, roundTripped.Mask.Length);

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
            {
                Assert.AreEqual(original.Pressure[i], roundTripped.Pressure[i], original.Pressure[i] / 1000f);
                Assert.AreEqual(original.Temperature[i], roundTripped.Temperature[i], original.Temperature[i] / 1000f);
                Assert.AreEqual(original.Composition[i], roundTripped.Composition[i]);
                Assert.AreEqual(original.FireIntensity[i], roundTripped.FireIntensity[i], 0.5f / 254f);
                Assert.AreEqual(original.Mask[i], roundTripped.Mask[i]);
            }

            for (int i = 0; i < AtmosChunkPatch.CellCount * 2; i++)
                Assert.AreEqual(original.Flow[i], roundTripped.Flow[i], 0.5f / 254f);
        }

        private static AtmosChunkPatch CreatePatch()
        {
            var patch = new AtmosChunkPatch
            {
                MapId = 3,
                ChunkKey = new Vector2Int(2, -1),
                Pressure = new float[AtmosChunkPatch.CellCount],
                Temperature = new float[AtmosChunkPatch.CellCount],
                Composition = new Color32[AtmosChunkPatch.CellCount],
                Flow = new float[AtmosChunkPatch.CellCount * 2],
                FireIntensity = new float[AtmosChunkPatch.CellCount],
                Mask = new byte[AtmosChunkPatch.CellCount],
            };

            for (int i = 0; i < AtmosChunkPatch.CellCount; i++)
            {
                patch.Pressure[i] = 101.325f + i * 0.1f;
                patch.Temperature[i] = 293.15f + i * 0.2f;
                patch.Composition[i] = new Color32((byte)(i % 256), (byte)((i * 3) % 256), (byte)((i * 7) % 256), (byte)((i * 11) % 256));
                patch.FireIntensity[i] = (i % 10) * 0.1f;
                patch.Mask[i] = (byte)(i % 4);
                patch.Flow[i * 2] = 0.5f + (i % 5) * 0.01f;
                patch.Flow[i * 2 + 1] = 0.5f - (i % 5) * 0.01f;
            }

            return patch;
        }
    }
}

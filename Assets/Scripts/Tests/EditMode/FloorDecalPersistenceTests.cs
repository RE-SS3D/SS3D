using EditorTests;
using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public sealed class FloorDecalPersistenceTests
    {
        private readonly List<GameObject> _instantiated = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }

            _instantiated.Clear();
        }

        [Test]
        public void FloorDecalIds_RoundTripThroughChunkSave()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 position = new Vector3(3f, 0f, 4f);
            TileMapTestUtilities.PlacePlenum(context, position);

            Assert.IsTrue(context.Map.TrySetFloorDecal(position, 7));
            Assert.IsTrue(context.Map.TryGetFloorDecalId(position, out ushort placed));
            Assert.AreEqual(7, placed);

            SavedTileMap saved = context.Map.Save();
            Assert.IsNotNull(saved.savedChunkList);
            Assert.IsTrue(saved.savedChunkList.Length > 0);

            bool foundDecalPayload = false;
            foreach (SavedTileChunk chunk in saved.savedChunkList)
            {
                if (chunk.floorDecalIds == null)
                    continue;

                foundDecalPayload = true;
                Assert.AreEqual(TileConstants.ChunkSize * TileConstants.ChunkSize, chunk.floorDecalIds.Length);
            }

            Assert.IsTrue(foundDecalPayload, "Expected floorDecalIds on at least one saved chunk.");

            TileMap loaded = TileMap.Create("FloorDecalLoadMap");
            _instantiated.Add(loaded.gameObject);
            loaded.Load(saved, invokeMapLoadedEvent: false);

            Assert.IsTrue(loaded.TryGetFloorDecalId(position, out ushort loadedId));
            Assert.AreEqual(7, loadedId);
        }

        [Test]
        public void TrySetFloorDecal_RequiresPlenum()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 position = new Vector3(1f, 0f, 1f);

            Assert.IsFalse(context.Map.TrySetFloorDecal(position, 3));
            Assert.IsFalse(context.Map.TryGetFloorDecalId(position, out _));
        }

        [Test]
        public void TileLayer_OverlaysRemoved_PipeLayersRenumbered()
        {
            Assert.AreEqual(9, (int)TileLayer.PipeMiddle);
            Assert.AreEqual(10, (int)TileLayer.PipeLeft);
            Assert.AreEqual(11, (int)TileLayer.PipeRight);
            Assert.AreEqual(12, (int)TileLayer.PipeManifold);
            Assert.AreEqual(13, TileHelper.GetTileLayers().Length);
        }
    }
}

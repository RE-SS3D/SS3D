using NUnit.Framework;
using SS3D.Systems.Networking;
using SS3D.Systems.Tile;
using UnityEngine;

namespace EditorTests
{
    public class TileObserverTests
    {
        [Test]
        public void HashGridHalfAccuracy_MatchesChunkSize()
        {
            int halfAccuracy = Mathf.CeilToInt(TileObserverConstants.HashGridAccuracy / 2f);
            Assert.AreEqual(TileChunk.ChunkSize, halfAccuracy);
        }
    }
}

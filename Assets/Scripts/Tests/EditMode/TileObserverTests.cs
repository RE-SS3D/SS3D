using NUnit.Framework;
using SS3D.Systems.Networking;
using SS3D.Systems.Tile;

namespace EditorTests
{
    public class TileObserverTests
    {
        [Test]
        public void HashGridAccuracy_MatchesChunkSize()
        {
            Assert.AreEqual(TileChunk.ChunkSize, TileObserverConstants.HashGridAccuracy);
        }
    }
}

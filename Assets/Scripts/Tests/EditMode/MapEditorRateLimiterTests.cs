using NUnit.Framework;
using SS3D.Systems.Tile.MapEditor.Commands;

namespace SS3D.Tests.EditMode
{
    public sealed class MapEditorRateLimiterTests
    {
        [Test]
        public void BurstUpToCapacity_ThenThrottled_ThenRefills()
        {
            MapEditorRateLimiter limiter = new(capacity: 100, refillPerSecond: 10);

            Assert.IsTrue(limiter.TryConsume(1, 100, 0));
            Assert.IsFalse(limiter.TryConsume(1, 1, 0));
            Assert.IsFalse(limiter.TryConsume(1, 20, 1)); // only 10 refilled
            Assert.IsTrue(limiter.TryConsume(1, 20, 2));  // 20 after two seconds
        }

        [Test]
        public void ClientsHaveSeparateBuckets()
        {
            MapEditorRateLimiter limiter = new(capacity: 10, refillPerSecond: 1);
            Assert.IsTrue(limiter.TryConsume(1, 10, 0));
            Assert.IsTrue(limiter.TryConsume(2, 10, 0));
        }

        [Test]
        public void OversizedRequest_IsRejected_WithoutDrainingBucket()
        {
            MapEditorRateLimiter limiter = new(capacity: 10, refillPerSecond: 1);
            Assert.IsFalse(limiter.TryConsume(1, 11, 0));
            Assert.IsTrue(limiter.TryConsume(1, 10, 0));
        }
    }
}

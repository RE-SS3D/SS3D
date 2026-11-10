using System.Net;
using FishNet.Transporting.Tugboat.Server;
using NUnit.Framework;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P1.8: the server status query can't be used to flood or reflect traffic.</summary>
    public sealed class UnconnectedRequestLimiterTests
    {
        private static readonly IPAddress A = IPAddress.Parse("10.0.0.1");
        private static readonly IPAddress B = IPAddress.Parse("10.0.0.2");

        [Test]
        public void OneAddress_GetsItsBurst_ThenRefillsOverTime()
        {
            UnconnectedRequestLimiter limiter = new(addressBurst: 3, addressPerSecond: 1, globalBurst: 100, globalPerSecond: 100);
            Assert.IsTrue(limiter.TryAcquire(A, 0));
            Assert.IsTrue(limiter.TryAcquire(A, 0));
            Assert.IsTrue(limiter.TryAcquire(A, 0));
            Assert.IsFalse(limiter.TryAcquire(A, 0));
            Assert.IsTrue(limiter.TryAcquire(A, 1.01));
            Assert.IsFalse(limiter.TryAcquire(A, 1.02));
        }

        [Test]
        public void Addresses_HaveSeparateBuckets()
        {
            UnconnectedRequestLimiter limiter = new(addressBurst: 1, addressPerSecond: 0.1, globalBurst: 100, globalPerSecond: 100);
            Assert.IsTrue(limiter.TryAcquire(A, 0));
            Assert.IsFalse(limiter.TryAcquire(A, 0));
            Assert.IsTrue(limiter.TryAcquire(B, 0));
        }

        [Test]
        public void GlobalCap_LimitsManyAddresses()
        {
            UnconnectedRequestLimiter limiter = new(addressBurst: 5, addressPerSecond: 5, globalBurst: 2, globalPerSecond: 1);
            Assert.IsTrue(limiter.TryAcquire(A, 0));
            Assert.IsTrue(limiter.TryAcquire(B, 0));
            Assert.IsFalse(limiter.TryAcquire(IPAddress.Parse("10.0.0.3"), 0));
            Assert.IsTrue(limiter.TryAcquire(IPAddress.Parse("10.0.0.3"), 1.01));
        }

        [Test]
        public void TrackedTable_StaysBounded_AndNullIsRefused()
        {
            UnconnectedRequestLimiter limiter = new(addressBurst: 1, addressPerSecond: 0, globalBurst: 1000, globalPerSecond: 0, maxTrackedAddresses: 2);
            Assert.IsTrue(limiter.TryAcquire(A, 0));
            Assert.IsTrue(limiter.TryAcquire(B, 0));
            Assert.IsTrue(limiter.TryAcquire(IPAddress.Parse("10.0.0.3"), 0)); // clears the table
            Assert.IsTrue(limiter.TryAcquire(A, 0)); // A was forgotten
            Assert.IsFalse(limiter.TryAcquire(null, 0));
        }
    }
}

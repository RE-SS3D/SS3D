using NUnit.Framework;
using SS3D.Core;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P2.2: world-sim suspension nests, so an inner load can't resume sims an outer one paused.</summary>
    public sealed class WorldSimSuspensionTests
    {
        [SetUp]
        [TearDown]
        public void Reset() =>
            typeof(WorldSimSuspension).GetMethod("ResetForNewSession",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, null);

        [Test]
        public void NestedScopes_StaySuspendedUntilBothClose()
        {
            WorldSimSuspension.Scope import = WorldSimSuspension.Begin("map import");
            WorldSimSuspension.Scope load = WorldSimSuspension.Begin("map load");
            Assert.IsTrue(WorldSimSuspension.IsSuspended);

            import.Dispose();
            Assert.IsTrue(WorldSimSuspension.IsSuspended, "the inner load still holds the sims");

            load.Dispose();
            Assert.IsFalse(WorldSimSuspension.IsSuspended);
        }

        [Test]
        public void DisposingTwice_ReleasesOnce()
        {
            WorldSimSuspension.Scope outer = WorldSimSuspension.Begin("outer");
            WorldSimSuspension.Scope inner = WorldSimSuspension.Begin("inner");
            inner.Dispose();
            inner.Dispose();
            Assert.IsTrue(WorldSimSuspension.IsSuspended);
            outer.Dispose();
            Assert.IsFalse(WorldSimSuspension.IsSuspended);
        }

        [Test]
        public void Scope_RemembersItsReason()
        {
            using WorldSimSuspension.Scope scope = WorldSimSuspension.Begin("map clear");
            Assert.AreEqual("map clear", scope.Reason);
        }
    }
}

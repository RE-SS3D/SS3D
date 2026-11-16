using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Remote clients never read <c>Unsupported</c> from AOI, so the server's Floating SyncVar can
    /// arrive before the local capture branch runs (#124). The last grounded velocity must then
    /// become the coast velocity instead of stopping dead.
    /// </summary>
    public class HumanoidSpaceCoastTests
    {
        [Test]
        public void LocallyCapturedVelocityWins()
        {
            Vector3 captured = new(3f, 0f, 0f);
            Vector3 lastGrounded = new(0f, 0f, 5f);

            Assert.AreEqual(captured, HumanoidLivingController.ResolveCoastVelocity(captured, lastGrounded));
        }

        [Test]
        public void MissingLocalCaptureFallsBackToLastGroundedVelocity()
        {
            Vector3 lastGrounded = new(0f, 0f, 5f);

            Assert.AreEqual(lastGrounded,
                HumanoidLivingController.ResolveCoastVelocity(Vector3.zero, lastGrounded));
        }

        [Test]
        public void StandingStillCoastsAtRest()
        {
            Assert.AreEqual(Vector3.zero,
                HumanoidLivingController.ResolveCoastVelocity(Vector3.zero, Vector3.zero));
        }
    }
}

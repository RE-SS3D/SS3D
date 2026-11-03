using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid.Body;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Replicated locomotion axes: remotes must see strafes and backpedals, not forward gait (M1m).
    /// </summary>
    public class BodyAnimationSnapshotTests
    {
        [Test]
        public void Unpack_CarriesStrafeAxes_AndDerivesSpeedMagnitude()
        {
            BodyAnimationSnapshot snapshot = BodyAnimationSnapshot.Unpack(0u, 0f, 0f, -0.6f, -0.8f, 0f, 0f, 0f);

            Assert.AreEqual(-0.6f, snapshot.MovementVelX, 1e-5f);
            Assert.AreEqual(-0.8f, snapshot.MovementVelZ, 1e-5f);
            Assert.AreEqual(1f, snapshot.MovementSpeed, 1e-5f);
        }

        [Test]
        public void Equals_DetectsDirectionChangeAtSameSpeed()
        {
            BodyAnimationSnapshot forward = BodyAnimationSnapshot.Unpack(0u, 0f, 0f, 0f, 0.3f, 0f, 0f, 0f);
            BodyAnimationSnapshot strafe = BodyAnimationSnapshot.Unpack(0u, 0f, 0f, 0.3f, 0f, 0f, 0f, 0f);

            Assert.AreEqual(forward.MovementSpeed, strafe.MovementSpeed, 1e-5f);
            Assert.IsFalse(forward.Equals(strafe));
        }

        [Test]
        public void Pack_RoundTripsAttackWindup_WithoutDisturbingVariantOrMirror()
        {
            BodyAnimationSnapshot snapshot = BodyAnimationSnapshot.Default;
            snapshot.ActiveTrigger = AnimationTriggerId.AttackSwing;
            snapshot.AttackVariant = 2;
            snapshot.AttackWindup = 31;
            snapshot.MirrorUpperBody = true;

            BodyAnimationSnapshot unpacked = BodyAnimationSnapshot.Unpack(snapshot.Pack(), 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            Assert.AreEqual(31, unpacked.AttackWindup);
            Assert.AreEqual(2, unpacked.AttackVariant);
            Assert.IsTrue(unpacked.MirrorUpperBody);
            Assert.AreEqual(AnimationTriggerId.AttackSwing, unpacked.ActiveTrigger);
        }
    }
}

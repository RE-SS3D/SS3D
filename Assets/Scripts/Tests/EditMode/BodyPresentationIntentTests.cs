using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;

namespace EditorTests
{
    public class BodyPresentationIntentTests
    {
        [Test]
        public void HealthyConsciousMapsToLocomotion()
        {
            Assert.AreEqual(
                BodyPresentationState.Locomotion,
                BodyPresentationIntent.FromSnapshot(HealthSnapshot.Default));
        }

        [Test]
        public void UnconsciousMapsToCollapsed()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.IsConscious = false;

            Assert.AreEqual(
                BodyPresentationState.Collapsed,
                BodyPresentationIntent.FromSnapshot(snapshot));
        }

        [Test]
        public void CardiacArrestWhileConsciousMapsToCollapsed()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.IsConscious = true;
            snapshot.IsCardiacArrest = true;
            snapshot.State = HealthState.CardiacArrest;

            Assert.AreEqual(
                BodyPresentationState.Collapsed,
                BodyPresentationIntent.FromSnapshot(snapshot));
        }

        [Test]
        public void CriticalWhileConsciousMapsToCollapsed()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.IsConscious = true;
            snapshot.IsCardiacArrest = false;
            snapshot.State = HealthState.Critical;

            Assert.AreEqual(
                BodyPresentationState.Collapsed,
                BodyPresentationIntent.FromSnapshot(snapshot));
        }

        [Test]
        public void DeadMapsToDeadEvenIfFlagsLinger()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Dead;
            snapshot.IsConscious = false;
            snapshot.IsCardiacArrest = true;

            Assert.AreEqual(
                BodyPresentationState.Dead,
                BodyPresentationIntent.FromSnapshot(snapshot));
        }
    }
}

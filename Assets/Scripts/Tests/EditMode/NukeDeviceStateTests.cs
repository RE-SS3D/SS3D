using NUnit.Framework;
using SS3D.Systems.Furniture;
using SS3D.Systems.Rounds;

namespace EditorTests
{
    /// <summary>
    /// The nuclear device's steps (<c>antagonist-content.md</c> §6): load the disk, arm, and only the
    /// screwdriver-then-wirecutters defuse stops the clock. Pulling the disk out after arming does not.
    /// </summary>
    public class NukeDeviceStateTests
    {
        private static readonly NukeDeviceState Empty = default;

        [Test]
        public void ArmingNeedsALoadedDisk()
        {
            Assert.IsFalse(Empty.CanArm);
            Assert.IsTrue(Empty.CanLoadDisk);
            Assert.IsFalse(Empty.CanEjectDisk);

            NukeDeviceState loaded = Empty.WithDisk(true);
            Assert.IsTrue(loaded.CanArm);
            Assert.IsFalse(loaded.CanLoadDisk, "One disk slot");
            Assert.IsTrue(loaded.CanEjectDisk);
        }

        [Test]
        public void Arming_StartsTheCountdown_AndDetonatesAtTheDeadline()
        {
            NukeDeviceState armed = Empty.WithDisk(true).ArmedAt(100d, 180f);

            Assert.IsTrue(armed.Armed);
            Assert.IsFalse(armed.CanArm);
            Assert.AreEqual(180f, armed.Remaining(100d), 0.001f);
            Assert.AreEqual(30f, armed.Remaining(250d), 0.001f);
            Assert.IsFalse(armed.ShouldDetonate(279.9d));
            Assert.IsTrue(armed.ShouldDetonate(280d));
        }

        [Test]
        public void PullingTheDiskAfterArming_DoesNotStopTheClock()
        {
            NukeDeviceState pulled = Empty.WithDisk(true).ArmedAt(0d, 180f).WithDisk(false);

            Assert.IsTrue(pulled.Armed);
            Assert.IsTrue(pulled.ShouldDetonate(180d));
        }

        [Test]
        public void Defuse_IsScrewdriverThenWirecutters()
        {
            NukeDeviceState armed = Empty.WithDisk(true).ArmedAt(0d, 180f);

            Assert.IsFalse(armed.CanCutWires(10d), "The wires are behind the panel");
            Assert.IsTrue(armed.CanOpenPanel(10d));

            NukeDeviceState open = armed.WithPanelOpen();
            Assert.IsFalse(open.CanOpenPanel(10d));
            Assert.IsTrue(open.CanCutWires(10d));

            NukeDeviceState defused = open.Defused();
            Assert.IsFalse(defused.Armed);
            Assert.IsFalse(defused.PanelOpen);
            Assert.IsFalse(defused.ShouldDetonate(1000d));
            Assert.AreEqual(0f, defused.Remaining(10d));
        }

        [Test]
        public void ADefusedDevice_CanBeReArmed_OnlyWithTheDiskLoaded()
        {
            NukeDeviceState defused = Empty.WithDisk(true).ArmedAt(0d, 180f).WithPanelOpen().Defused();
            Assert.IsTrue(defused.CanArm, "The ops can re-arm while their disk is still in it");

            NukeDeviceState rearmed = defused.ArmedAt(50d, 180f);
            Assert.IsFalse(rearmed.PanelOpen, "Arming closes the panel; a new defuse starts over");
            Assert.IsFalse(defused.WithDisk(false).CanArm);
        }

        [Test]
        public void NoDefuseOrPanelWithoutAnArmedClock_OrAfterTheDeadline()
        {
            NukeDeviceState loaded = Empty.WithDisk(true);
            Assert.IsFalse(loaded.CanOpenPanel(0d));
            Assert.IsFalse(loaded.CanCutWires(0d));

            NukeDeviceState open = loaded.ArmedAt(0d, 180f).WithPanelOpen();
            Assert.IsFalse(open.CanCutWires(180d), "Too late at zero");
        }

        [Test]
        public void ASpentDevice_DoesNothingMore()
        {
            NukeDeviceState spent = Empty.WithDisk(true).ArmedAt(0d, 10f).Detonated();

            Assert.IsTrue(spent.Spent);
            Assert.IsFalse(spent.ShouldDetonate(100d), "Detonates once");
            Assert.IsFalse(spent.CanArm);
            Assert.IsFalse(spent.CanLoadDisk);
            Assert.IsFalse(spent.CanEjectDisk);
        }

        [TestCase(180f, "3:00")]
        [TestCase(179.2f, "3:00")]
        [TestCase(61f, "1:01")]
        [TestCase(9.5f, "0:10")]
        [TestCase(0.1f, "0:01")]
        [TestCase(0f, "0:00")]
        public void FormatTime_RoundsUp(float seconds, string expected)
        {
            Assert.AreEqual(expected, NukeDeviceState.FormatTime(seconds));
        }

        [Test]
        public void ArmAnnouncement_SaysWhereAndHowLong()
        {
            Assert.AreEqual("A nuclear device has been armed in Vault. Detonation in 3:00.", Nuke.ArmAnnouncement("Vault", 180f));
        }

        [Test]
        public void Countdown_DefaultsToThreeMinutes_AndTheFlagOverridesIt()
        {
            Assert.AreEqual(180, NukeOpsProfile.NukeCountdownSeconds);
            Assert.AreEqual(180, NukeOpsProfile.NukeSecondsFrom(new[] { "game" }));
            Assert.AreEqual(90, NukeOpsProfile.NukeSecondsFrom(new[] { "game", "-nukeops-nuke-seconds=90" }));
            Assert.AreEqual(180, NukeOpsProfile.NukeSecondsFrom(new[] { "-nukeops-nuke-seconds=5" }), "Below 10 s is ignored");

            Nuke.SetCountdownSecondsForTesting(20);
            try
            {
                Assert.AreEqual(20f, Nuke.CountdownSeconds);
            }
            finally
            {
                Nuke.SetCountdownSecondsForTesting(null);
            }
        }
    }
}

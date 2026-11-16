using NUnit.Framework;
using SS3D.Systems.Rounds;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// The round start/stop gate. The async races behind it (cancellation, despawn, restart) stay in
    /// the nightly PlayMode <c>RoundLifecycle_*</c> fixtures.
    /// </summary>
    public sealed class RoundRequestRulesTests
    {
        [TestCase(RoundState.Stopped, RoundRequestOutcome.Run)]
        [TestCase(RoundState.Ended, RoundRequestOutcome.Run)]
        [TestCase(RoundState.Preparing, RoundRequestOutcome.Ignore)]
        [TestCase(RoundState.WarmingUp, RoundRequestOutcome.Ignore)]
        [TestCase(RoundState.Ongoing, RoundRequestOutcome.Ignore)]
        [TestCase(RoundState.Ending, RoundRequestOutcome.Ignore)]
        public void Start_RunsOnlyFromStoppedOrEnded(RoundState current, RoundRequestOutcome expected) =>
            Assert.AreEqual(expected, RoundRequestRules.Evaluate(current, start: true));

        [TestCase(RoundState.Preparing, RoundRequestOutcome.CancelCountdown)]
        [TestCase(RoundState.WarmingUp, RoundRequestOutcome.CancelCountdown)]
        [TestCase(RoundState.Ongoing, RoundRequestOutcome.Run)]
        [TestCase(RoundState.Stopped, RoundRequestOutcome.Ignore)]
        [TestCase(RoundState.Ending, RoundRequestOutcome.Ignore)]
        [TestCase(RoundState.Ended, RoundRequestOutcome.Ignore)]
        public void Stop_CancelsCountdownOrEndsOngoingRound(RoundState current, RoundRequestOutcome expected) =>
            Assert.AreEqual(expected, RoundRequestRules.Evaluate(current, start: false));

        [Test]
        public void DoubleStart_SecondRequestIsIgnored()
        {
            Assert.AreEqual(RoundRequestOutcome.Run, RoundRequestRules.Evaluate(RoundState.Stopped, start: true));
            Assert.AreEqual(RoundRequestOutcome.Ignore, RoundRequestRules.Evaluate(RoundState.Preparing, start: true));
        }
    }
}

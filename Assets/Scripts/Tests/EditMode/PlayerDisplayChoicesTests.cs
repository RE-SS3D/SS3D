using NUnit.Framework;
using SS3D.Systems.Settings;
using UnityEngine;

namespace EditorTests
{
    public class PlayerDisplayChoicesTests
    {
        private static readonly Vector2Int Native = new(2560, 1440);

        [Test]
        public void ResolutionListDropsRefreshRateDuplicatesLargestFirst()
        {
            Vector2Int[] modes = { new(1280, 720), new(1920, 1080), new(1920, 1080), new(2560, 1440), new(1280, 720) };

            Assert.That(PlayerResolutions.Choices(modes),
                Is.EqualTo(new[] { new Vector2Int(2560, 1440), new Vector2Int(1920, 1080), new Vector2Int(1280, 720) }));
        }

        [Test]
        public void SavedResolutionIsUsedWhenTheDisplayOffersIt()
        {
            Vector2Int[] choices = { Native, new(1920, 1080) };

            Assert.That(PlayerResolutions.Resolve(1920, 1080, choices, Native), Is.EqualTo(new Vector2Int(1920, 1080)));
        }

        [TestCase(0, 0)]
        [TestCase(3840, 2160)]
        public void UnsetOrUnavailableResolutionFallsBackToNative(int width, int height)
        {
            Vector2Int[] choices = { Native, new(1920, 1080) };

            Assert.That(PlayerResolutions.Resolve(width, height, choices, Native), Is.EqualTo(Native));
        }

        [Test]
        public void UnconfirmedDisplayChangeRevertsAfterFifteenSeconds()
        {
            DisplayChangeConfirmation<string> confirmation = new();
            confirmation.Begin("1080p borderless", now: 100f);

            Assert.That(confirmation.ShouldRevert(114.9f), Is.False);
            Assert.That(confirmation.SecondsLeft(110f), Is.EqualTo(5f).Within(0.001f));
            Assert.That(confirmation.ShouldRevert(115f), Is.True);
            Assert.That(confirmation.Previous, Is.EqualTo("1080p borderless"));
        }

        [Test]
        public void ConfirmedDisplayChangeNeverReverts()
        {
            DisplayChangeConfirmation<string> confirmation = new();
            confirmation.Begin("old", now: 0f);
            confirmation.Confirm();

            Assert.That(confirmation.IsPending, Is.False);
            Assert.That(confirmation.ShouldRevert(60f), Is.False);
        }

        [Test]
        public void SecondChangeWhilePendingStillRevertsToTheLastConfirmedState()
        {
            DisplayChangeConfirmation<string> confirmation = new();
            confirmation.Begin("confirmed", now: 0f);
            confirmation.Begin("unconfirmed", now: 10f);

            Assert.That(confirmation.Previous, Is.EqualTo("confirmed"));
            Assert.That(confirmation.ShouldRevert(20f), Is.False, "the timer restarts");
            Assert.That(confirmation.ShouldRevert(25f), Is.True);
        }
    }
}

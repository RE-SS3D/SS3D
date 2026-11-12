using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.ActionMessages;
using SS3D.Systems.Comms;
using UnityEngine;

namespace EditorTests
{
    /// <summary>Action message pipeline rules (action-messages.md §4–§5, examine feedback Decisions 9, 12, 13).</summary>
    public class ActionMessageTests
    {
        private static readonly List<(string Listener, Vector3 Position)> Listeners = new()
        {
            ("actor", Vector3.zero),
            ("target", new Vector3(1f, 0f, 0f)),
            ("beside", new Vector3(2f, 0f, 0f)),
            ("across", new Vector3(6f, 0f, 0f)),
            ("far", new Vector3(30f, 0f, 0f)),
        };

        private static List<string> Recipients(ActionVisibility level)
        {
            List<string> recipients = new();
            ActionMessageRecipients.Select(level, Vector3.zero, "actor", "target", Listeners, recipients);
            return recipients;
        }

        [Test]
        public void PublicReachesEveryoneInHearingRangeButTheActor()
        {
            CollectionAssert.AreEquivalent(new[] { "target", "beside", "across" }, Recipients(ActionVisibility.Public));
        }

        [Test]
        public void CloseReachesOnlyATileOrTwo()
        {
            CollectionAssert.AreEquivalent(new[] { "target", "beside" }, Recipients(ActionVisibility.Close));
        }

        [Test]
        public void TargetOnlyReachesOnlyTheTarget()
        {
            CollectionAssert.AreEqual(new[] { "target" }, Recipients(ActionVisibility.TargetOnly));

            List<string> selfTargeted = new();
            ActionMessageRecipients.Select(ActionVisibility.TargetOnly, Vector3.zero, "actor", "actor", Listeners, selfTargeted);
            Assert.IsEmpty(selfTargeted, "nobody is told about their own action");
        }

        [Test]
        public void CueIsRateLimitedPerExaminerAndTarget()
        {
            ExamineCueLimiter limiter = new(5f);
            Assert.IsTrue(limiter.TryAllow(1, 10, 0f));
            Assert.IsFalse(limiter.TryAllow(1, 10, 4.9f), "same pair inside the cooldown");
            Assert.IsTrue(limiter.TryAllow(1, 11, 1f), "another target");
            Assert.IsTrue(limiter.TryAllow(2, 10, 1f), "another examiner");
            Assert.IsTrue(limiter.TryAllow(1, 10, 5f), "after the cooldown");
        }

        [Test]
        public void LinesFormatWithViewerSideNames()
        {
            Assert.AreEqual("Marcus Voss looks you over.",
                ActionMessageSubSystem.Format(ActionMessageKeys.ExamineCue, "Marcus Voss", "Ana Ruiz", null));
            Assert.AreEqual("Unknown crew member in red hardsuit is trying to take your wrench!",
                ActionMessageSubSystem.Format(ActionMessageKeys.TakeOpen, "Unknown crew member in red hardsuit", null, "wrench"));
        }

        [Test]
        public void ToggleNarratesOnlyWhenItHasAKey()
        {
            SS3D.Interactions.InteractionEvent interactionEvent = new(null, null);
            List<string> raised = new();
            void Handle(SS3D.Interactions.Interfaces.InteractionNarration narration) => raised.Add(narration.Key);
            SS3D.Interactions.Interfaces.InteractionNarration.ServerNarrated += Handle;
            try
            {
                SS3D.Interactions.ToggleInteraction silent = new();
                Assert.IsFalse(SS3D.Interactions.Interfaces.InteractionNarration.TryRaise(silent, interactionEvent));

                SS3D.Interactions.ToggleInteraction narrated = new() { NarrationKey = ActionMessageKeys.LightSwitch };
                Assert.IsTrue(SS3D.Interactions.Interfaces.InteractionNarration.TryRaise(narrated, interactionEvent));
                CollectionAssert.AreEqual(new[] { ActionMessageKeys.LightSwitch }, raised);
            }
            finally
            {
                SS3D.Interactions.Interfaces.InteractionNarration.ServerNarrated -= Handle;
            }
        }

        [Test]
        public void SpeechOutranksHostileOutranksAimedOutranksRoutineThenProximity()
        {
            var items = new[]
            {
                (Name: "routine-near", Tier: CrowdCapRanker.RoutineActionTier, Distance: 1f),
                (Name: "aimed", Tier: CrowdCapRanker.ActionOnViewerTier, Distance: 9f),
                (Name: "speech-far", Tier: CrowdCapRanker.SpeechTier, Distance: 50f),
                (Name: "hostile", Tier: CrowdCapRanker.HostileActionTier, Distance: 20f),
                (Name: "speech-near", Tier: CrowdCapRanker.SpeechTier, Distance: 4f),
            };

            List<(string Name, int Tier, float Distance)> shown =
                CrowdCapRanker.RankByTier(items, i => i.Tier, i => i.Distance, 4, out int overflow);

            CollectionAssert.AreEqual(new[] { "speech-near", "speech-far", "hostile", "aimed" }, shown.Select(i => i.Name).ToArray());
            Assert.AreEqual(1, overflow, "the routine line drops into the overflow chip");
        }
    }
}

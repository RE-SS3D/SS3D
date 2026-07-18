using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Tests;
using UnityEngine;

namespace EditorTests
{
    public class InteractionPipelineTests : EditModeTest
    {
        [Test]
        public void FilterAndSort_OrdersByPriorityDescending()
        {
            StubInteractionSource source = new();
            List<InteractionEntry> entries = new()
            {
                CreateEntry("Low", 5),
                CreateEntry("High", 100),
                CreateEntry("Mid", 50),
            };

            List<InteractionEntry> viable = InteractionPipeline.FilterAndSort(source, entries, Vector3.zero, Vector3.up, IntentType.Help);

            Assert.AreEqual(3, viable.Count);
            Assert.AreEqual("High", viable[0].Interaction.GetGenericName());
            Assert.AreEqual("Mid", viable[1].Interaction.GetGenericName());
            Assert.AreEqual("Low", viable[2].Interaction.GetGenericName());
        }

        [Test]
        public void FilterAndSort_ExcludesInteractionsThatFailIntent()
        {
            StubInteractionSource source = new();
            List<InteractionEntry> entries = new()
            {
                CreateEntry(new HarmOnlyInteraction(), "Hit"),
                CreateEntry("Pickup", 10),
            };

            List<InteractionEntry> viable = InteractionPipeline.FilterAndSort(source, entries, Vector3.zero, Vector3.up, IntentType.Help);

            Assert.AreEqual(1, viable.Count);
            Assert.AreEqual("Pickup", viable[0].Interaction.GetGenericName());
        }

        [Test]
        public void TryResolve_MatchesTargetComponentIndex()
        {
            CreateGameObject(out GameObject targetObject, out TargetComponent firstTarget);
            TargetComponent secondTarget = targetObject.AddComponent<TargetComponent>();
            instantiated.Add(targetObject);

            secondTarget.GenericName = "Second";
            firstTarget.GenericName = "First";

            List<InteractionEntry> entries = new()
            {
                InteractionEntry.Create(firstTarget, new NamedInteraction(firstTarget.GenericName, 0), targetObject),
                InteractionEntry.Create(secondTarget, new NamedInteraction(secondTarget.GenericName, 0), targetObject),
            };

            InteractionIdentifier secondId = new("Second", 1);

            Assert.IsTrue(InteractionEntry.TryResolve(entries, secondId, out InteractionEntry resolved));
            Assert.AreEqual("Second", resolved.Interaction.GetGenericName());
            Assert.AreEqual(1, resolved.Id.TargetComponentIndex);
        }

        [Test]
        public void TryResolve_MatchesGenericNameNotDisplayName()
        {
            StubInteractionTarget target = new();
            NamedInteraction interaction = new("Pickup", 0)
            {
                DisplayName = "Pick up the shiny thing",
            };

            List<InteractionEntry> entries = new()
            {
                new InteractionEntry(target, interaction, InteractionIdentifier.SyntheticTargetIndex),
            };

            InteractionIdentifier id = new("Pickup", InteractionIdentifier.SyntheticTargetIndex);

            Assert.IsTrue(InteractionEntry.TryResolve(entries, id, out InteractionEntry resolved));
            Assert.AreEqual("Pick up the shiny thing", resolved.Interaction.GetName(new InteractionEvent(new StubInteractionSource(), target)));
        }

        [Test]
        public void InteractionIdentifier_DistinguishesSameGenericNameOnDifferentTargets()
        {
            InteractionIdentifier first = new("Open", 0);
            InteractionIdentifier second = new("Open", 1);

            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void FilterForOutline_ExcludesSourceOnlyEntries()
        {
            StubInteractionTarget target = new();
            List<InteractionEntry> entries = new()
            {
                new InteractionEntry(null, new NamedInteraction("Drop", 5), InteractionIdentifier.SourceOnlyTargetIndex),
                new InteractionEntry(target, new NamedInteraction("Pickup", 10), InteractionIdentifier.SyntheticTargetIndex),
            };

            List<InteractionEntry> outlineEntries = InteractionPipeline.FilterForOutline(entries);

            Assert.AreEqual(1, outlineEntries.Count);
            Assert.AreEqual("Pickup", outlineEntries[0].Interaction.GetGenericName());
        }

        [Test]
        public void TryResolve_ReturnsFalseWhenIdentifierNotFound()
        {
            List<InteractionEntry> entries = new()
            {
                CreateEntry("Pickup", 10),
            };

            InteractionIdentifier missing = new("Drop", InteractionIdentifier.SyntheticTargetIndex);

            Assert.IsFalse(InteractionEntry.TryResolve(entries, missing, out _));
        }

        private static InteractionEntry CreateEntry(string genericName, int priority)
        {
            return CreateEntry(new NamedInteraction(genericName, priority), genericName);
        }

        private static InteractionEntry CreateEntry(IInteraction interaction, string genericName)
        {
            StubInteractionTarget target = new();
            return new InteractionEntry(target, interaction, InteractionIdentifier.SyntheticTargetIndex);
        }

        private class NamedInteraction : IInteraction
        {
            public NamedInteraction(string genericName, int priority)
            {
                GenericName = genericName;
                Priority = priority;
            }

            public string DisplayName { get; set; }

            public int Priority { get; }

            private string GenericName { get; }

            public string GetGenericName() => GenericName;

            public string GetName(InteractionEvent interactionEvent) => DisplayName ?? GenericName;

            public Sprite GetIcon(InteractionEvent interactionEvent) => null;

            public bool CanInteract(InteractionEvent interactionEvent) => true;

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference) => false;
        }

        private sealed class HarmOnlyInteraction : NamedInteraction, IIntentRestrictedInteraction
        {
            public HarmOnlyInteraction() : base("Hit", 100)
            {
            }

            public IntentType AllowedIntent => IntentType.Harm;
        }

        private sealed class StubInteractionTarget : IInteractionTarget
        {
            public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent) => System.Array.Empty<IInteraction>();
        }

        private sealed class TargetComponent : MonoBehaviour, IInteractionTarget
        {
            public string GenericName = "First";

            public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
            {
                return new IInteraction[] { new NamedInteraction(GenericName, 0) };
            }
        }

        private sealed class StubInteractionSource : IInteractionSource
        {
            public GameObject GameObject { get; } = new("StubSource");

            public FishNet.Object.NetworkObject NetworkObject => null;

            public IInteractionSource Source { get; set; }

            public bool CanExecuteInteraction(IInteraction interaction) => true;

            public bool CanContinueInteraction() => true;

            public bool CanInteractWithTarget(IInteractionTarget target) => true;

            public void CancelInteraction(InteractionReference reference)
            {
            }

            public void ClientInteract(InteractionEvent interactionEvent, IInteraction interaction, InteractionReference reference)
            {
            }

            public void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries)
            {
            }

            public InteractionInstance GetInstanceFromReference(InteractionReference reference) => null;

            public bool HasInteraction(InteractionReference reference) => false;

            public InteractionReference Interact(InteractionEvent interactionEvent, IInteraction interaction) => new(1);
        }
    }
}

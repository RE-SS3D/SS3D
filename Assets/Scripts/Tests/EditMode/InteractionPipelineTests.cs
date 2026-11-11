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
        public void ExplainNotViable_NamesTheFailingCheck()
        {
            StubInteractionSource source = new();
            InteractionEvent e = new(source, null, Vector3.zero, Vector3.up);
            List<InteractionEntry> entries = new()
            {
                CreateEntry(new HarmOnlyInteraction(), "Hit"),
                CreateEntry("Pickup", 10),
                CreateEntry(new RefusingInteraction(), "Drop"),
            };
            InteractionIdentifier drop = new("Drop", InteractionIdentifier.SyntheticTargetIndex);
            InteractionIdentifier hit = new("Hit", InteractionIdentifier.SyntheticTargetIndex);
            InteractionIdentifier pickup = new("Pickup", InteractionIdentifier.SyntheticTargetIndex);
            InteractionIdentifier missing = new("Climb", InteractionIdentifier.SyntheticTargetIndex);

            Assert.AreEqual(InteractionPipeline.ViabilityFailure.WrongIntent,
                InteractionPipeline.ExplainNotViable(source, entries, e, IntentType.Help, hit));
            Assert.AreEqual(InteractionPipeline.ViabilityFailure.None,
                InteractionPipeline.ExplainNotViable(source, entries, e, IntentType.Help, pickup));
            Assert.AreEqual(InteractionPipeline.ViabilityFailure.CannotInteract,
                InteractionPipeline.ExplainNotViable(source, entries, e, IntentType.Help, drop));
            Assert.AreEqual(InteractionPipeline.ViabilityFailure.NoSuchInteraction,
                InteractionPipeline.ExplainNotViable(source, entries, e, IntentType.Help, missing));
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
        public void FilterAndSort_HarmExcludesUnrestrictedWorldVerbs()
        {
            StubInteractionSource source = new();
            List<InteractionEntry> entries = new()
            {
                CreateEntry(new HarmOnlyInteraction(), "Hit"),
                CreateEntry("Pickup", 10),
                CreateEntry("Drop", 5),
            };

            List<InteractionEntry> viable = InteractionPipeline.FilterAndSort(source, entries, Vector3.zero, Vector3.up, IntentType.Harm);

            Assert.AreEqual(1, viable.Count);
            Assert.AreEqual("Hit", viable[0].Interaction.GetGenericName());
        }

        [Test]
        public void FilterAndSort_HelpExcludesHarmOnlyInteractions()
        {
            StubInteractionSource source = new();
            List<InteractionEntry> entries = new()
            {
                CreateEntry(new HarmOnlyInteraction(), "Hit"),
                CreateEntry("Open", 20),
            };

            List<InteractionEntry> help = InteractionPipeline.FilterAndSort(source, entries, Vector3.zero, Vector3.up, IntentType.Help);
            List<InteractionEntry> harm = InteractionPipeline.FilterAndSort(source, entries, Vector3.zero, Vector3.up, IntentType.Harm);

            Assert.AreEqual(1, help.Count);
            Assert.AreEqual("Open", help[0].Interaction.GetGenericName());
            Assert.AreEqual(1, harm.Count);
            Assert.AreEqual("Hit", harm[0].Interaction.GetGenericName());
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
                InteractionEntry.SourceOnly(new NamedInteraction("Drop", 5)),
                new InteractionEntry(target, new NamedInteraction("Pickup", 10), InteractionIdentifier.SyntheticTargetIndex),
            };

            Assert.IsTrue(entries[0].IsSourceOnly);
            Assert.IsFalse(entries[1].IsSourceOnly);

            List<InteractionEntry> outlineEntries = InteractionPipeline.FilterForOutline(entries);

            Assert.AreEqual(1, outlineEntries.Count);
            Assert.AreEqual("Pickup", outlineEntries[0].Interaction.GetGenericName());
        }

        [Test]
        public void TryEvaluateOutlineInteractability_IgnoresSourceOnlyAndReportsViable()
        {
            StubInteractionSource source = new();
            OfferingTarget target = new(new NamedInteraction("Pickup", 10));
            List<IInteractionTarget> targets = new() { target };
            InteractionEvent interactionEvent = new(source, target, Vector3.zero, Vector3.up);

            bool found = InteractionPipeline.TryEvaluateOutlineInteractability(
                source,
                targets,
                interactionEvent,
                IntentType.Help,
                out bool hasViable);

            Assert.IsTrue(found);
            Assert.IsTrue(hasViable);
        }

        [Test]
        public void TryEvaluateOutlineInteractability_HarmHidesUnrestrictedWorldVerbs()
        {
            StubInteractionSource source = new();
            OfferingTarget target = new(new NamedInteraction("Pickup", 10));
            List<IInteractionTarget> targets = new() { target };
            InteractionEvent interactionEvent = new(source, target, Vector3.zero, Vector3.up);

            bool found = InteractionPipeline.TryEvaluateOutlineInteractability(
                source,
                targets,
                interactionEvent,
                IntentType.Harm,
                out bool hasViable);

            Assert.IsTrue(found);
            Assert.IsFalse(hasViable);
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

        [Test]
        public void Discover_MergesTargetBoundAndSourceOnly()
        {
            SourceOnlyAwareSource source = new();
            OfferingTarget target = new(new NamedInteraction("Pickup", 10));
            List<IInteractionTarget> targets = new() { target };
            InteractionEvent interactionEvent = new(source, target, new Vector3(1f, 0f, 0f), Vector3.up);

            List<InteractionEntry> discovered = InteractionPipeline.Discover(source, targets, interactionEvent);

            Assert.AreEqual(2, discovered.Count);
            Assert.IsFalse(discovered[0].IsSourceOnly);
            Assert.AreEqual("Pickup", discovered[0].Interaction.GetGenericName());
            Assert.IsTrue(discovered[1].IsSourceOnly);
            Assert.AreEqual("Drop", discovered[1].Interaction.GetGenericName());
            Assert.AreEqual(InteractionIdentifier.SourceOnlyTargetIndex, discovered[1].Id.TargetComponentIndex);
            Assert.IsTrue(source.LastContext.HasPoint);
            Assert.AreEqual(new Vector3(1f, 0f, 0f), source.LastContext.Point);
        }

        [Test]
        public void Discover_PassesUnsetPointContextToSource()
        {
            SourceOnlyAwareSource source = new();
            OfferingTarget target = new(new NamedInteraction("Pickup", 10));
            List<IInteractionTarget> targets = new() { target };
            InteractionEvent interactionEvent = new(source, target);

            InteractionPipeline.Discover(source, targets, interactionEvent);

            Assert.IsNotNull(source.LastContext);
            Assert.IsFalse(source.LastContext.HasPoint);
        }

        [Test]
        public void InteractionEvent_WithTarget_PreservesHasPoint()
        {
            StubInteractionSource source = new();
            StubInteractionTarget first = new();
            StubInteractionTarget second = new();

            InteractionEvent withPoint = new(source, first, Vector3.zero, Vector3.up);
            InteractionEvent copied = withPoint.WithTarget(second);

            Assert.IsTrue(copied.HasPoint);
            Assert.AreEqual(Vector3.zero, copied.Point);
            Assert.AreSame(second, copied.Target);

            InteractionEvent withoutPoint = new(source, first);
            InteractionEvent copiedUnset = withoutPoint.WithTarget(second);

            Assert.IsFalse(copiedUnset.HasPoint);
            Assert.AreSame(second, copiedUnset.Target);
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

        private sealed class RefusingInteraction : NamedInteraction, IInteraction
        {
            public RefusingInteraction() : base("Drop", 5)
            {
            }

            bool IInteraction.CanInteract(InteractionEvent interactionEvent) => false;
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

        private sealed class OfferingTarget : IInteractionTarget
        {
            private readonly IInteraction[] _interactions;

            public OfferingTarget(params IInteraction[] interactions)
            {
                _interactions = interactions;
            }

            public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent) => _interactions;
        }

        private sealed class TargetComponent : MonoBehaviour, IInteractionTarget
        {
            public string GenericName = "First";

            public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
            {
                return new IInteraction[] { new NamedInteraction(GenericName, 0) };
            }
        }

        private class StubInteractionSource : IInteractionSource
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


            public void CancelInteraction(InteractionReference reference, DelayedCancelReason reason)

            {

            }

            public void ClientInteract(InteractionEvent interactionEvent, IInteraction interaction, InteractionReference reference)
            {
            }

            public virtual void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries, InteractionEvent context)
            {
            }

            public InteractionInstance GetInstanceFromReference(InteractionReference reference) => null;

            public bool HasInteraction(InteractionReference reference) => false;

            public InteractionReference Interact(InteractionEvent interactionEvent, IInteraction interaction) => new(1);
        }

        private sealed class SourceOnlyAwareSource : StubInteractionSource
        {
            public InteractionEvent LastContext { get; private set; }

            public override void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries, InteractionEvent context)
            {
                LastContext = context;
                entries.Add(InteractionEntry.SourceOnly(new NamedInteraction("Drop", 5)));
            }
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Tests;
using UnityEngine;

namespace EditorTests
{
    public class InteractionDispatchTests : EditModeTest
    {
        [Test]
        public void TryResolveDispatched_FallsBackToGenericNameWhenIndexMismatches()
        {
            StubTarget target = new();
            NamedInteraction interaction = new("Search", 10);
            List<InteractionEntry> entries = new()
            {
                new InteractionEntry(target, interaction, targetComponentIndex: 0),
            };

            // Client hovered a child Selectable (index 3); server rediscovers on root (index 0).
            InteractionIdentifier mismatched = new("Search", 3);

            Assert.IsFalse(InteractionEntry.TryResolve(entries, mismatched, out _));
            Assert.IsTrue(InteractionDispatch.TryResolveDispatchedInteraction(entries, mismatched, out InteractionEntry resolved));
            Assert.AreEqual("Search", resolved.Interaction.GetGenericName());
            Assert.AreEqual(0, resolved.Id.TargetComponentIndex);
        }

        [Test]
        public void FilterRadialInteractions_ExcludesExamine()
        {
            StubTarget target = new();
            List<InteractionEntry> entries = new()
            {
                new InteractionEntry(target, new NamedInteraction("Pickup", 10), 0),
                new InteractionEntry(target, new NamedInteraction(InteractionDispatch.ExamineInteractionName, 5), 0),
            };

            List<InteractionEntry> filtered = InteractionDispatch.FilterRadialInteractions(entries);

            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual("Pickup", filtered[0].Interaction.GetGenericName());
        }

        private sealed class StubTarget : IInteractionTarget
        {
            public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent) => System.Array.Empty<IInteraction>();
        }

        private sealed class NamedInteraction : IInteraction
        {
            public NamedInteraction(string genericName, int priority)
            {
                GenericName = genericName;
                Priority = priority;
            }

            public int Priority { get; }

            private string GenericName { get; }

            public string GetGenericName() => GenericName;

            public string GetName(InteractionEvent interactionEvent) => GenericName;

            public Sprite GetIcon(InteractionEvent interactionEvent) => null;

            public bool CanInteract(InteractionEvent interactionEvent) => true;

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference) => true;
        }
    }
}

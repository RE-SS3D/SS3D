using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Interactions;
using UnityEngine;

namespace EditorTests
{
    public class SearchInteractionTests
    {
        [Test]
        public void GenericNameIsSearch()
        {
            Assert.AreEqual("Search", SearchInteraction.GenericName);
            Assert.AreEqual("Search", SearchInteraction.Instance.GetGenericName());
            Assert.AreEqual("Search", SearchInteraction.Instance.GetName(default));
        }

        [Test]
        public void TierIsInstant()
        {
            Assert.AreEqual(
                InteractionTier.Instant,
                SearchInteraction.Instance.GetTier(default));
        }

        [Test]
        public void CanInteractRejectsNonHandSource()
        {
            InteractionEvent interactionEvent = new(new StubInteractionSource(), null);
            Assert.IsFalse(SearchInteraction.Instance.CanInteract(interactionEvent));
        }

        [Test]
        public void TryResolveVictimFailsWithoutTarget()
        {
            InteractionEvent interactionEvent = new(new StubInteractionSource(), null);
            Assert.IsFalse(SearchInteraction.TryResolveVictim(interactionEvent, out _, out _));
        }

        [Test]
        public void CreateClientReturnsSearchClientInteraction()
        {
            IClientInteraction client = SearchInteraction.Instance.CreateClient(default);
            Assert.IsInstanceOf<SearchClientInteraction>(client);
        }

        private sealed class StubInteractionSource : IInteractionSource
        {
            public GameObject GameObject { get; } = new("StubSearchSource");

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

            public void CreateSourceInteractions(IInteractionTarget[] targets, System.Collections.Generic.List<InteractionEntry> entries, InteractionEvent context)
            {
            }

            public InteractionInstance GetInstanceFromReference(InteractionReference reference) => null;

            public bool HasInteraction(InteractionReference reference) => false;

            public InteractionReference Interact(InteractionEvent interactionEvent, IInteraction interaction) => new(1);
        }
    }
}

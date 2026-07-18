using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Tests;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTests
{
    public class InteractionRangeCheckTests : EditModeTest
    {
        [Test]
        public void RangeCheck_WithMissingPoint_UsesTargetTransformNotUnlimitedRange()
        {
            CreateGameObject(out GameObject sourceObject, out RangeLimitBehaviour rangeLimit);
            CreateGameObject(out GameObject targetObject, out TargetComponent target);

            sourceObject.transform.position = Vector3.zero;
            rangeLimit.Origin = Vector3.zero;
            rangeLimit.Range = new RangeLimit(1.5f, 2f);
            targetObject.transform.position = new Vector3(10f, 0f, 0f);

            StubInteractionSource source = new(sourceObject);
            InteractionEvent farEvent = new(source, target, Vector3.zero);

            Assert.IsFalse(InteractionExtensions.RangeCheck(farEvent));

            targetObject.transform.position = new Vector3(1f, 0f, 0f);
            InteractionEvent nearEvent = new(source, target, Vector3.zero);

            Assert.IsTrue(InteractionExtensions.RangeCheck(nearEvent));
        }

        [Test]
        public void RangeCheck_WithResolvedPoint_UsesPointDistance()
        {
            CreateGameObject(out GameObject sourceObject, out RangeLimitBehaviour rangeLimit);
            CreateGameObject(out GameObject targetObject, out TargetComponent target);

            sourceObject.transform.position = Vector3.zero;
            rangeLimit.Origin = Vector3.zero;
            rangeLimit.Range = new RangeLimit(1.5f, 2f);
            targetObject.transform.position = new Vector3(10f, 0f, 0f);

            StubInteractionSource source = new(sourceObject);

            Assert.IsFalse(InteractionExtensions.RangeCheck(new InteractionEvent(source, target, new Vector3(10f, 0f, 0f))));
            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(source, target, new Vector3(1f, 0f, 0f))));
        }

        private sealed class RangeLimitBehaviour : MonoBehaviour, IInteractionRangeLimit, IInteractionOriginProvider
        {
            public RangeLimit Range = new(1.5f, 2f);
            public Vector3 Origin;

            public RangeLimit GetInteractionRange() => Range;

            public Vector3 InteractionOrigin => Origin;
        }

        private sealed class TargetComponent : MonoBehaviour, IInteractionTarget, IGameObjectProvider
        {
            public GameObject GameObject => gameObject;

            public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent) => System.Array.Empty<IInteraction>();
        }

        private sealed class StubInteractionSource : IInteractionSource, IGameObjectProvider
        {
            public StubInteractionSource(GameObject gameObject)
            {
                GameObject = gameObject;
            }

            public GameObject GameObject { get; }

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

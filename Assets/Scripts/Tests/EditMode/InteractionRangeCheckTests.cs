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
            InteractionEvent farEvent = new(source, target);

            Assert.IsFalse(farEvent.HasPoint);
            Assert.IsFalse(InteractionExtensions.RangeCheck(farEvent));

            targetObject.transform.position = new Vector3(1f, 0f, 0f);
            InteractionEvent nearEvent = new(source, target);

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

            Assert.IsFalse(InteractionExtensions.RangeCheck(new InteractionEvent(source, target, new Vector3(10f, 0f, 0f), Vector3.up)));
            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(source, target, new Vector3(1f, 0f, 0f), Vector3.up)));
        }

        [Test]
        public void RangeCheck_WorldOriginHit_IsResolvedPointNotUnset()
        {
            CreateGameObject(out GameObject sourceObject, out RangeLimitBehaviour rangeLimit);
            CreateGameObject(out GameObject targetObject, out TargetComponent target);

            sourceObject.transform.position = new Vector3(0.5f, 0f, 0f);
            rangeLimit.Origin = sourceObject.transform.position;
            rangeLimit.Range = new RangeLimit(1.5f, 2f);
            // Target far away — only a resolved point at origin should pass.
            targetObject.transform.position = new Vector3(50f, 0f, 0f);

            StubInteractionSource source = new(sourceObject);
            InteractionEvent originHit = new(source, target, Vector3.zero, Vector3.up);

            Assert.IsTrue(originHit.HasPoint);
            Assert.IsTrue(InteractionExtensions.RangeCheck(originHit));

            InteractionEvent unset = new(source, target);
            Assert.IsFalse(unset.HasPoint);
            Assert.IsFalse(InteractionExtensions.RangeCheck(unset));
        }

        [Test]
        public void RangeCheck_WallBetweenSourceAndPoint_Blocks()
        {
            StubInteractionSource source = CreateSourceAtOrigin();
            CreateWall(new Vector3(0.6f, 0f, 0f), blocksReach: true);
            CreateGameObject(out GameObject targetObject, out TargetComponent target);
            targetObject.transform.position = new Vector3(1.2f, 0f, 0f);
            Physics.SyncTransforms();

            Assert.IsFalse(InteractionExtensions.RangeCheck(new InteractionEvent(source, target, new Vector3(1.2f, 0f, 0f), Vector3.left)));
            Assert.IsFalse(InteractionExtensions.RangeCheck(new InteractionEvent(source, target)));
        }

        [Test]
        public void RangeCheck_NonBlockingColliderBetween_DoesNotBlock()
        {
            StubInteractionSource source = CreateSourceAtOrigin();
            CreateWall(new Vector3(0.6f, 0f, 0f), blocksReach: false);
            CreateGameObject(out GameObject targetObject, out TargetComponent target);
            targetObject.transform.position = new Vector3(1.2f, 0f, 0f);
            Physics.SyncTransforms();

            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(source, target, new Vector3(1.2f, 0f, 0f), Vector3.left)));
        }

        [Test]
        public void RangeCheck_TargetIsTheBlocker_IsReachable()
        {
            StubInteractionSource source = CreateSourceAtOrigin();
            GameObject wall = CreateWall(new Vector3(1f, 0f, 0f), blocksReach: true);
            TargetComponent target = wall.AddComponent<TargetComponent>();
            Physics.SyncTransforms();

            // Point on the wall's near face, and the unresolved-point fallback.
            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(source, target, new Vector3(0.9f, 0f, 0f), Vector3.left)));
            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(source, target)));
        }

        [Test]
        public void RangeCheck_WallMount_ReachableOnNearFaceOnly()
        {
            StubInteractionSource source = CreateSourceAtOrigin();
            // Wall spans x 0.9..1.1.
            CreateWall(new Vector3(1f, 0f, 0f), blocksReach: true);

            CreateGameObject(out GameObject nearMount, out TargetComponent nearTarget);
            nearMount.transform.position = new Vector3(0.89f, 0f, 0f);
            CreateGameObject(out GameObject farMount, out TargetComponent farTarget);
            farMount.transform.position = new Vector3(1.11f, 0f, 0f);
            Physics.SyncTransforms();

            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(source, nearTarget, new Vector3(0.89f, 0f, 0f), Vector3.left)));
            Assert.IsFalse(InteractionExtensions.RangeCheck(new InteractionEvent(source, farTarget, new Vector3(1.11f, 0f, 0f), Vector3.left)));
        }

        [Test]
        public void RangeCheck_BlockerIsATargetWithoutGameObjectProvider_IsReachable()
        {
            StubInteractionSource source = CreateSourceAtOrigin();
            GameObject door = CreateWall(new Vector3(1f, 0f, 0f), blocksReach: true);
            BareTargetComponent target = door.AddComponent<BareTargetComponent>();
            Physics.SyncTransforms();

            // A point inside the door, past its near face, as an airlock click gives.
            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(source, target, new Vector3(1f, 0f, 0f), Vector3.left)));
        }

        [Test]
        public void RangeCheck_MountInsideWallSolid_ReachableOnlyFromItsFace()
        {
            // Mount at the wall's centre, facing the source; wall spans x 0.9..1.1.
            StubInteractionSource source = CreateSourceAtOrigin();
            CreateWall(new Vector3(1f, 0f, 0f), blocksReach: true);
            CreateGameObject(out GameObject facingUs, out SurfaceTargetComponent facingTarget);
            facingUs.transform.position = new Vector3(1f, 0f, 0f);
            facingTarget.Face = new Vector3(0.9f, 0f, 0f);
            CreateGameObject(out GameObject facingAway, out SurfaceTargetComponent awayTarget);
            facingAway.transform.position = new Vector3(1f, 0f, 0f);
            awayTarget.Face = new Vector3(1.1f, 0f, 0f);
            Physics.SyncTransforms();

            Assert.IsTrue(InteractionExtensions.RangeCheck(new InteractionEvent(source, facingTarget, new Vector3(1f, 0f, 0f), Vector3.left)));
            Assert.IsFalse(InteractionExtensions.RangeCheck(new InteractionEvent(source, awayTarget, new Vector3(1f, 0f, 0f), Vector3.left)));
        }

        private StubInteractionSource CreateSourceAtOrigin()
        {
            CreateGameObject(out GameObject sourceObject, out RangeLimitBehaviour rangeLimit);
            sourceObject.transform.position = Vector3.zero;
            rangeLimit.Origin = Vector3.zero;
            rangeLimit.Range = new RangeLimit(1.5f, 2f);
            return new StubInteractionSource(sourceObject);
        }

        /// <summary>A 0.2 m thick, 2 m tall slab across the X axis, on the Default layer like a real wall.</summary>
        private GameObject CreateWall(Vector3 centre, bool blocksReach)
        {
            CreateGameObject(out GameObject wall, out ReachBlockerBehaviour blocker);
            blocker.Blocks = blocksReach;
            wall.transform.position = centre;
            BoxCollider box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(0.2f, 2f, 2f);
            return wall;
        }

        /// <summary>Like an airlock: a target that isn't an <see cref="IGameObjectProvider"/>.</summary>
        private sealed class BareTargetComponent : MonoBehaviour, IInteractionTarget
        {
            public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent) => System.Array.Empty<IInteraction>();
        }

        private sealed class SurfaceTargetComponent : MonoBehaviour, IInteractionTarget, IReachSurface
        {
            public Vector3 Face;

            public Vector3 GetReachPoint(Vector3 point) => Face;

            public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent) => System.Array.Empty<IInteraction>();
        }

        private sealed class ReachBlockerBehaviour : MonoBehaviour, IReachBlocker
        {
            public bool Blocks = true;

            public bool BlocksReach => Blocks;
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

            public void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries, InteractionEvent context)
            {
            }

            public InteractionInstance GetInstanceFromReference(InteractionReference reference) => null;

            public bool HasInteraction(InteractionReference reference) => false;

            public InteractionReference Interact(InteractionEvent interactionEvent, IInteraction interaction) => new(1);
        }
    }
}

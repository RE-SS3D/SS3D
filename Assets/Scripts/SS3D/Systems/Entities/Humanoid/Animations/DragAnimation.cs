using DG.Tweening;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Dragging;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Systems.Animations
{
    public class DragAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private const float JointBreakForce = 5000f;

        private readonly float _itemReachDuration;

        private readonly Draggable _grabbedObject;

        private readonly Hand _mainHand;

        private readonly Hand _secondaryHand;

        private FixedJoint _fixedJoint;

        private LayerMask _grabbableLayer;

        public DragAnimation(ProceduralAnimationController proceduralAnimationController, float time, Hand mainHand, Hand secondaryHand, NetworkObject target)
            : base(time, proceduralAnimationController)
        {
            _grabbedObject = target.GetComponent<Draggable>();
            _mainHand = mainHand;
            _itemReachDuration = time / 2;
            _secondaryHand = secondaryHand;
            SetUpGrab(false);
        }

        public override void ClientPlay()
        {
            GrabReach();
        }

        public override void Cancel()
        {
            ReleaseGrab();
        }

        private void ReleaseGrab()
        {
            if (_grabbedObject && _grabbedObject.TryGetComponent(out Rigidbody grabbedRb))
            {
                grabbedRb.detectCollisions = true;
            }

            if (_fixedJoint)
            {
                Object.Destroy(_fixedJoint);
            }

            Sequence stopSequence = DOTween.Sequence();

            // Stop looking
            stopSequence.Append(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _itemReachDuration));

            // Stop picking
            stopSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachDuration));

            Controller.PositionController.ChangeGrab(false);
        }

        private void SetUpGrab(bool withTwoHands)
        {
            _mainHand.Hold.HandTargetFollowTransform(_grabbedObject.transform);

            // Needed if this has been changed elsewhere
            _mainHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;

            // Reproduce changes on secondary hand if necessary.
            if (withTwoHands)
            {
                _secondaryHand.Hold.HandTargetFollowTransform(_grabbedObject.transform);
                _secondaryHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;
            }

            // Set up the look at target locker on the item to pick up.
            Controller.LookAtTargetLocker.Follow(_grabbedObject.transform, true);
            OrientTargetForHandRotation(_mainHand, _grabbedObject.transform.position);
        }

        private void GrabReach()
        {
            TryRotateTowardTargetPosition(Controller.transform, _itemReachDuration, _grabbedObject.transform.position);

            Controller.PositionController.ChangeGrab(true);

            // Start looking at grabbed part
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 1f, _itemReachDuration));

            // At the same time change pickup constraint weight of the main hand from 0 to 1
            InteractionSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration)
                .OnComplete(HandleGrabbing));

            // Stop looking
            InteractionSequence.Append(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _itemReachDuration));

            // Stop picking
            InteractionSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachDuration));
        }

        private void HandleGrabbing()
        {
            _mainHand.HandBone.GetComponent<Rigidbody>().isKinematic = true;
            _mainHand.HandBone.GetComponent<Collider>().enabled = false;

            // Only the owner handle physics since transform are client authoritative for now
            if (!_mainHand.IsOwner)
            {
                return;
            }

            Rigidbody grabbedRb = _grabbedObject.GetComponent<Rigidbody>();

            if (_grabbedObject.MoveToGrabber)
            {
                _grabbedObject.transform.position = _mainHand.Hold.HoldTransform.position;
                grabbedRb.velocity = Vector3.zero;
                grabbedRb.position = _mainHand.Hold.HoldTransform.position;
                grabbedRb.detectCollisions = false;
            }

            _fixedJoint = Controller.GameObject.AddComponent<FixedJoint>();
            _fixedJoint.connectedBody = grabbedRb;
            _fixedJoint.breakForce = JointBreakForce;
        }
    }
}

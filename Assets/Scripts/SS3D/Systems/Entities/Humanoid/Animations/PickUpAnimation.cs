using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public sealed class PickUpAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private readonly Hand _mainHand;

        private readonly Hand _secondaryHand;

        private readonly float _itemReachDuration;

        private readonly AbstractHoldable _holdable;

        public PickUpAnimation(ProceduralAnimationController proceduralAnimationController, float time, Hand mainHand, Hand secondaryHand, AbstractHoldable item)
            : base(time, proceduralAnimationController)
        {
            _mainHand = mainHand;
            _secondaryHand = secondaryHand;
            _itemReachDuration = time / 2;
            _holdable = item;
        }

        public override void ClientPlay()
        {
            bool withTwoHands = _secondaryHand && _secondaryHand.Empty && _holdable.CanHoldTwoHand;

            SetUpPickup(withTwoHands);

            PickupReach(withTwoHands);
        }

        public override void Cancel()
        {
            Debug.Log("cancel pick up animation");

            InteractionSequence?.Kill();

            Sequence sequence = DOTween.Sequence();

            // Those times are to keep the speed of movements pretty much the same as when it was reaching
            float timeToCancelHold = (1 - _mainHand.Hold.HoldIkConstraint.weight) * _itemReachDuration;
            float timeToCancelLookAt = (1 - Controller.LookAtConstraint.weight) * _itemReachDuration;
            float timeToCancelPickup = (1 - _mainHand.Hold.PickupIkConstraint.weight) * _itemReachDuration;

            sequence.Append(DOTween.To(() => _mainHand.Hold.HoldIkConstraint.weight, x => _mainHand.Hold.HoldIkConstraint.weight = x, 0f, timeToCancelHold));
            sequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, timeToCancelLookAt));
            sequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 0f, timeToCancelPickup));
            Controller.PositionController.TryToStandUp();
        }

        [Client]
        private void SetUpPickup(bool withTwoHands)
        {
            // Orient hand in a natural position to reach for item.
            OrientTargetForHandRotation(_mainHand, _holdable.transform.position);

            // Place hand ik target on the item, at their respective position and rotation.
            _mainHand.Hold.HandTargetFollowHold(false, _holdable, false, 0f, false);

            // Needed if this has been changed elsewhere
            _mainHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;

            // Needed as the hand need to reach when picking up in an extended position, it looks unnatural
            // if it takes directly the rotation of the hold.
            _mainHand.Hold.HoldIkConstraint.data.targetRotationWeight = 0f;

            // Reproduce changes on secondary hand if necessary.
            if (withTwoHands)
            {
                OrientTargetForHandRotation(_secondaryHand, _holdable.transform.position);
                _secondaryHand.Hold.HandTargetFollowHold(true, _holdable);
                _secondaryHand.Hold.PickupIkConstraint.data.tipRotationWeight = 1f;
                _secondaryHand.Hold.HoldIkConstraint.data.targetRotationWeight = 0f;
            }

            // Set up the look at target locker on the item to pick up.
            Controller.LookAtTargetLocker.Follow(_holdable.transform, true);
        }

        [Client]
        private void PickupReach(bool withTwoHands)
        {
            // Rotate player toward item
            TryRotateTowardTargetPosition(Controller.transform, _itemReachDuration, _holdable.transform.position);

            AdaptPosition(Controller.PositionController, _mainHand, _holdable.transform.position);

            // Start looking at item
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 1f, _itemReachDuration));

            // At the same time change pickup constraint weight of the main hand from 0 to 1
            InteractionSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration));

            // Reproduce changes on second hand if picking up with two hands
            if (withTwoHands)
            {
                InteractionSequence.Join(DOTween.To(() => _secondaryHand.Hold.PickupIkConstraint.weight, x => _secondaryHand.Hold.PickupIkConstraint.weight = x, 1f, _itemReachDuration));
            }

            // Once reached, start moving and rotating item toward its constrained position.
            InteractionSequence.AppendInterval(_itemReachDuration);

            // At the same time stop looking at the item and uncrouch
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _itemReachDuration).
                OnStart(() => RestorePosition(Controller.PositionController)));

            // At the same time start getting the right rotation for the hand
            InteractionSequence.Join(DOTween.To(() => _mainHand.Hold.HoldIkConstraint.data.targetRotationWeight, x => _mainHand.Hold.HoldIkConstraint.data.targetRotationWeight = x, 1f, _itemReachDuration).OnStart(() =>
            {
                _mainHand.Hold.HandTargetFollowHold(false, _holdable, true, _itemReachDuration);

                // Controller.HoldController.BringToHand(_mainHand, _holdable, _itemReachDuration);
            }));

            // At the same time, remove the pickup constraint
            InteractionSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachDuration));

            // Reproduce changes on second hand if picking up with two hands
            if (withTwoHands)
            {
                InteractionSequence.Join(DOTween.To(() => _secondaryHand.Hold.HoldIkConstraint.weight, x => _secondaryHand.Hold.HoldIkConstraint.weight = x, 1f, _itemReachDuration));

                InteractionSequence.Join(DOTween.To(() => _secondaryHand.Hold.HoldIkConstraint.data.targetRotationWeight, x => _secondaryHand.Hold.HoldIkConstraint.data.targetRotationWeight = x, 1f, _itemReachDuration)).OnStart(() =>
                {
                    _secondaryHand.Hold.HandTargetFollowHold(true, _holdable, true, _itemReachDuration);
                });

                InteractionSequence.Join(DOTween.To(() => _secondaryHand.Hold.PickupIkConstraint.weight, x => _secondaryHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachDuration));
            }

            InteractionSequence.OnComplete(() =>
            {
                OnCompletion?.Invoke(this);
            });
        }
    }
}

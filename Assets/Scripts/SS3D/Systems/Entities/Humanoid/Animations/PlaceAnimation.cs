using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    public sealed class PlaceAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private readonly float _itemReachPlaceDuration;

        private readonly Hand _mainHand;

        private readonly Hand _secondaryHand;

        private readonly AbstractHoldable _placedObject;

        private readonly Vector3 _placePosition;

        public PlaceAnimation(ProceduralAnimationController proceduralAnimationController, float time, Hand mainHand, Hand secondaryHand, AbstractHoldable placedObject, Vector3 placePosition)
            : base(time, proceduralAnimationController)
        {
            _itemReachPlaceDuration = time / 2;
            _mainHand = mainHand;
            _secondaryHand = secondaryHand;
            _placedObject = placedObject;
            _secondaryHand = secondaryHand;
            _placePosition = placePosition;
        }

        public override void ClientPlay()
        {
            Debug.Log("Start animate place item");

            bool withTwoHands = _secondaryHand.Empty && _placedObject.CanHoldTwoHand;

            SetupPlace(withTwoHands);

            Place(withTwoHands);
        }

        public override void Cancel()
        {
            Debug.Log("Cancel place animation");
            InteractionSequence.Kill();

            Sequence cancelSequence = DOTween.Sequence();

            float timeToCancelPlace = Controller.LookAtConstraint.weight * _itemReachPlaceDuration;

            // Needed to constrain item to position, in case the weight has been changed elsewhere
            _mainHand.Hold.ItemPositionConstraint.weight = 1f;

            // Place pickup and hold target lockers on the item, at their respective position and rotation.
            _mainHand.Hold.HandTargetFollowHold(false, _placedObject);

            // Move and rotate item toward its constrained position.
            _placedObject.transform.parent = _mainHand.Hold.ItemPositionTargetLocker;
            cancelSequence.Append(_placedObject.transform.DOLocalMove(Vector3.zero, timeToCancelPlace));
            cancelSequence.Join(_placedObject.transform.DOLocalRotate(Quaternion.identity.eulerAngles, timeToCancelPlace));

            // Stop looking at item
            cancelSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, timeToCancelPlace));

            // At the same time, remove the pickup constraint
            cancelSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 0f, timeToCancelPlace));

            // put back hold constraint weight of the main hand to 1
            cancelSequence.Join(DOTween.To(() => _mainHand.Hold.HoldIkConstraint.weight, x => _mainHand.Hold.HoldIkConstraint.weight = x, 1f, timeToCancelPlace));

            cancelSequence.OnStart(() =>
            {
                Controller.PositionController.TryToStandUp();
            });
        }

        [Client]
        private void SetupPlace(bool withTwoHands)
        {
            // set pickup constraint to 1 so that the player can bend to reach at its feet or further in front.
            _mainHand.Hold.PickupIkConstraint.weight = 1f;

            // Remove hold constraint from second hand if item held with two hands.
            if (withTwoHands)
            {
                _secondaryHand.Hold.PickupIkConstraint.weight = 1f;
            }

            // Place look at target at place item position
            Controller.LookAtTargetLocker.transform.parent = null;
            Controller.LookAtTargetLocker.transform.position = _placePosition;
        }

        [Client]
        private void Place(bool withTwoHands)
        {
            // Turn character toward the position to place the item.
            TryRotateTowardTargetPosition(Controller.transform, _itemReachPlaceDuration, _placePosition);

            AdaptPosition(Controller.PositionController, _mainHand, _placePosition);

            // Slowly increase looking at place item position
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 1f, _itemReachPlaceDuration));

            // At the same time, Slowly move item toward the position it should be placed.
            InteractionSequence.Join(_placedObject.transform.DOMove(_placePosition, _itemReachPlaceDuration).OnComplete(() =>
            {
                RestorePosition(Controller.PositionController);
                _mainHand.Hold.HandIkTarget.parent = null;
            }));

            // Then, Slowly stop looking at item place position
            InteractionSequence.Append(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _itemReachPlaceDuration));

            // Slowly decrease main hand pick up constraint so player stop reaching for pickup target
            InteractionSequence.Join(DOTween.To(() => _mainHand.Hold.PickupIkConstraint.weight, x => _mainHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
            InteractionSequence.Join(DOTween.To(() => _mainHand.Hold.HoldIkConstraint.weight, x => _mainHand.Hold.HoldIkConstraint.weight = x, 0f, _itemReachPlaceDuration));

            // reproduce changes on second hand
            if (withTwoHands)
            {
                InteractionSequence.Join(DOTween.To(() => _secondaryHand.Hold.PickupIkConstraint.weight, x => _secondaryHand.Hold.PickupIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
                InteractionSequence.Join(DOTween.To(() => _secondaryHand.Hold.HoldIkConstraint.weight, x => _secondaryHand.Hold.HoldIkConstraint.weight = x, 0f, _itemReachPlaceDuration));
            }

            InteractionSequence.OnComplete(() => OnCompletion?.Invoke(this));
        }
    }
}

using DG.Tweening;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Procedural animation for interaction done with hand holding nothing.
    /// </summary>
    public class InteractWithHandAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private readonly float _moveHandTime;
        private readonly Hand _hand;
        private readonly Vector3 _targetPosition;
        private readonly InteractionType _interactionType;

        public InteractWithHandAnimation(ProceduralAnimationController proceduralAnimationController, float time, Hand mainHand, Vector3 targetPosition, InteractionType interactionType)
            : base(time, proceduralAnimationController)
        {
            _hand = mainHand;
            _moveHandTime = time;
            _targetPosition = targetPosition;
            _interactionType = interactionType;
        }

        public override void ClientPlay()
        {
            SetupInteract();
            InteractWithHand();
        }

        public override void Cancel()
        {
            InteractionSequence.Kill();

            // Stop looking at item
            DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _moveHandTime);

            // Stop reaching for the position of interaction
            DOTween.To(() => _hand.Hold.PickupIkConstraint.weight, x => _hand.Hold.PickupIkConstraint.weight = x, 0f, _moveHandTime);

            _hand.Hold.StopAnimation();
        }

        private void SetupInteract()
        {
            // disable position constraint the time of the interaction
            _hand.Hold.PickupIkConstraint.weight = 1f;
            _hand.Hold.HandIkTarget.position = _hand.HandBone.position;
            Controller.LookAtTargetLocker.transform.position = _targetPosition;
        }

        private void InteractWithHand()
        {
            AlignHandWithShoulder(_targetPosition, _hand);

            AdaptPosition(Controller.PositionController, _hand, _targetPosition);

            // Rotate player toward item
            TryRotateTowardTargetPosition(Controller.transform, _moveHandTime, _targetPosition);

            // Start looking at item
            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 1f, _moveHandTime));

            // Move hand toward target
            InteractionSequence.Join(_hand.Hold.HandIkTarget.DOMove(_targetPosition, _moveHandTime).OnComplete(() => _hand.Hold.PlayAnimation(_interactionType)));

            InteractionSequence.AppendInterval(InteractionTime - _moveHandTime);

            // Stop looking at item
            InteractionSequence.Append(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, _moveHandTime).OnStart(() =>
            {
                _hand.Hold.StopAnimation();
                RestorePosition(Controller.PositionController);
            }));

            // Stop reaching for the position of interaction
            InteractionSequence.Join(DOTween.To(() => _hand.Hold.PickupIkConstraint.weight, x => _hand.Hold.PickupIkConstraint.weight = x, 0f, _moveHandTime));

            InteractionSequence.OnComplete(() =>
            {
                OnCompletion?.Invoke(this);
            });
        }

        private void AlignHandWithShoulder(Vector3 interactionPoint, Hand mainHand)
        {
            Vector3 fromShoulderToTarget = (interactionPoint - mainHand.Hold.UpperArm.transform.position).normalized;
            mainHand.Hold.HandIkTarget.rotation = Quaternion.LookRotation(fromShoulderToTarget);
            mainHand.Hold.HandIkTarget.rotation *= Quaternion.Euler(90f, 0f, 0);
            mainHand.Hold.HandIkTarget.rotation *= Quaternion.Euler(0f, 180f, 0);
        }
    }
}

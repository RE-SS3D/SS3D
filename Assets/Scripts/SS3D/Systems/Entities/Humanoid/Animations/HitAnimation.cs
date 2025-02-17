// using DebugDrawingExtension;
using DG.Tweening;
using FishNet.Object;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Procedural animation for punching stuff
    /// </summary>
    public class HitAnimation : AbstractProceduralAnimation
    {
        public override event Action<IProceduralAnimation> OnCompletion;

        private readonly Vector3 _targetHitPosition;
        private readonly Hand _mainHand;
        private readonly Transform _rootTransform;

        public HitAnimation(ProceduralAnimationController controller, float interactionTime, Vector3 targetHit, Hand mainHand)
            : base(interactionTime, controller)
        {
            _targetHitPosition = targetHit;
            _mainHand = mainHand;
            _rootTransform = controller.transform;
        }

        public override void ClientPlay()
        {
            HitAnimate();
        }

        public override void Cancel()
        {
        }

        [Client]
        private void HitAnimate()
        {
            Vector3 directionFromTransformToTarget = _targetHitPosition - _rootTransform.position;
            directionFromTransformToTarget.y = 0f;
            Quaternion finalRotationPlayer = Quaternion.LookRotation(directionFromTransformToTarget);
            float timeToRotate = (Quaternion.Angle(_rootTransform.rotation, finalRotationPlayer) / 180f) * InteractionTime;

            // In sequence, we first rotate toward the target
            TryRotateTowardTargetPosition(Controller.transform, timeToRotate, _targetHitPosition);

            InteractionSequence.Join(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 1f, timeToRotate));

            InteractionSequence.InsertCallback(timeToRotate * 0.4f, () => Controller.AnimatorController.Punch(_mainHand, _targetHitPosition));

            InteractionSequence.AppendInterval(0.5f);

            InteractionSequence.Append(DOTween.To(() => Controller.LookAtConstraint.weight, x => Controller.LookAtConstraint.weight = x, 0f, timeToRotate));

            InteractionSequence.OnStart(() =>
            {
                AdaptPosition(Controller.PositionController, _mainHand, _targetHitPosition);
            });
            InteractionSequence.OnComplete(() =>
            {
                RestorePosition(Controller.PositionController);
            });

            InteractionSequence.OnUpdate(() =>
            {
                // Set up the look at target locker on the item to pick up.
                Controller.LookAtTargetLocker.transform.position = _targetHitPosition;
            });
        }
    }
}

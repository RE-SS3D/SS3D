using DG.Tweening;
using JetBrains.Annotations;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using System;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Provide an abstract class for the IProceduralAnimation interface, giving access to  IProceduralAnimations to some common methods often used in procedural animations
    /// </summary>
    public abstract class AbstractProceduralAnimation : IProceduralAnimation
    {
        public abstract event Action<IProceduralAnimation> OnCompletion;

        private bool _positionHasChanged;

        protected AbstractProceduralAnimation(float interactionTime, ProceduralAnimationController controller)
        {
            Controller = controller;
            InteractionTime = interactionTime;
            InteractionSequence = DOTween.Sequence();
        }

        protected float InteractionTime { get; private set; }

        protected Sequence InteractionSequence { get; private set; }

        protected ProceduralAnimationController Controller { get; private set; }

        public abstract void ClientPlay();

        public abstract void Cancel();

        /// <summary>
        /// Create a rotation of the IK target to make sure the hand reach in a natural way the item.
        /// The rotation is such that it's Y axis is aligned with the line crossing through the character shoulder and IK target.
        /// </summary>
        protected static void OrientTargetForHandRotation([NotNull] Hand hand, Vector3 targetPosition)
        {
            Vector3 armTargetDirection = targetPosition - hand.Hold.UpperArm.position;

            Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);

            targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

            hand.Hold.HandIkTarget.rotation = targetRotation;
        }

        /// <summary>
        ///  Try to rotate on the flat plane the player toward a given position, often useful for procedural animations.
        /// </summary>
        protected void TryRotateTowardTargetPosition(Transform rootTransform, float rotateTime, Vector3 position)
        {
            if (Controller.PositionController.PositionType != PositionType.Sitting)
            {
                InteractionSequence.Join(Controller.transform.DORotate(
                    QuaternionExtension.SameHeightPlaneLookRotation(position - rootTransform.position, Vector3.up).eulerAngles, rotateTime));
            }
        }

        /// <summary>
        /// Adapt the position of the player interacting to make the animation look more natural, such as crouching when the interaction is too low.
        /// </summary>
        protected void AdaptPosition(PositionController positionController, [NotNull] Hand mainHand, Vector3 targetPosition)
        {
            if (mainHand.HandBone.transform.position.y - targetPosition.y > 0.3)
            {
                _positionHasChanged = positionController.PositionType != PositionType.Crouching;
                positionController.TryCrouch();
            }
        }

        protected void RestorePosition(PositionController positionController)
        {
            if (_positionHasChanged)
            {
                positionController.TryToGetToPreviousPosition();
            }
        }
    }
}

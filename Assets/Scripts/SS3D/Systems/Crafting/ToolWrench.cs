using DG.Tweening;
using SS3D.Core.Behaviours;
using SS3D.Systems.Animations;
using SS3D.Systems.Interactions;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public class ToolWrench : NetworkActor, IInteractiveTool
    {
        private Tween _animation;

        private Vector3 _positionAtAnimationStart;

        private Quaternion _rotationAtAnimationStart;

        [SerializeField]
        private Transform _interactionPoint;

        public GameObject GameObject => gameObject;

        public Transform InteractionPoint => _interactionPoint;

        public void PlayAnimation(InteractionType interactionType)
        {
            RotateAroundPivot();
        }

        public void StopAnimation()
        {
            if (_animation != null && _animation.IsPlaying())
            {
                _animation.Kill(); // Stop and kill the rotation animation
            }
        }

        // Method to rotate the screwdriver around the pivot point
        private void RotateAroundPivot()
        {
            // Calculate the direction from the pivot point to the screwdriver
            Vector3 pivotToScrewdriver = transform.position - InteractionPoint.position;

            _positionAtAnimationStart = transform.position;
            _rotationAtAnimationStart = transform.rotation;

            // Use DoTween to rotate around the pivot by modifying the screwdriver's position
            _animation = DOTween.To(
                    () => 0f,
                    RotateScrewdriver,
                    25f,
                    0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        // This method updates both position and rotation based on the current rotation angle
        private void RotateScrewdriver(float currentRotation)
        {
            transform.SetPositionAndRotation(_positionAtAnimationStart, _rotationAtAnimationStart);
            transform.RotateAround(InteractionPoint.position, InteractionPoint.right, currentRotation);
        }
    }
}

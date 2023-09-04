using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Data;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    public class HumanoidAnimatorController : Actor
    {
        [SerializeField] private HumanoidController _movementController;

        [SerializeField] private Animator _animator;
        [SerializeField] private float _lerpMultiplier;

        protected override void OnStart()
        {
            base.OnStart();
            SubscribeToEvents();    
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            _movementController.OnSpeedChangeEvent += UpdateMovement;
        }

        private void UnsubscribeFromEvents()
        {
            _movementController.OnSpeedChangeEvent -= UpdateMovement;
        }

        private void UpdateMovement(float speed)
        {
            bool isMoving = speed != 0;
            float currentSpeed = _animator.GetFloat(Animations.Humanoid.MovementSpeed);
            float newLerpModifier = isMoving ? _lerpMultiplier : (_lerpMultiplier * 3);
            speed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * newLerpModifier);
            
            _animator.SetFloat(Animations.Humanoid.MovementSpeed, speed);
        }
    }
}
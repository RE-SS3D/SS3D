using Content.Systems.Player.Movement;
using UnityEngine;

namespace SS3D.Content.Systems.Player.Movement
{
    public class BipedAnimatorController : MonoBehaviour
    {
        [SerializeField] private BipedMovementController movementController;

        [SerializeField] private Animator animator;
        [SerializeField] private float lerpMultiplier;

        private void Start()
        {
            SubscribeToEvents();    
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            movementController.AnimationSpeedChanged += UpdateMovementAnimation;
        }

        private void UnsubscribeFromEvents()
        {
            movementController.AnimationSpeedChanged -= UpdateMovementAnimation;
        }

        private void UpdateMovementAnimation(float speed)
        {
            bool isMoving = speed != 0;
            float currentSpeed = animator.GetFloat(BipedAnimations.Human.MovementSpeed);
            float newLerpModifier = isMoving ? lerpMultiplier : (lerpMultiplier * 3);
            speed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * newLerpModifier);
            
            animator.SetFloat(BipedAnimations.Human.MovementSpeed, speed);
        }
        
    }
}
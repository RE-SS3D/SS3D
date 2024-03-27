using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class DummyAnimatorController : Actor
{
    [SerializeField] private DummyMovement _movementController;

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

    public void TriggerPickUp()
    {
        _animator.SetTrigger("PickUpRight");
    }
    
    public void Throw(HandType handtype)
    {
        if(handtype == HandType.RightHand)
            _animator.SetTrigger("ThrowRight");
        else
            _animator.SetTrigger("ThrowLeft");
    }

    public void Sit(bool sitState)
    {
        _animator.SetBool("Sit", sitState);
    }
    
    public void Crouch(bool crouchState)
    {
        _animator.SetBool("Crouch", crouchState);
    }
    
}

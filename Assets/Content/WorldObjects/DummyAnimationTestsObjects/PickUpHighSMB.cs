using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpHighSMB : StateMachineBehaviour
{
    private DummyIkController _dummyIkController;

    
    private float _pickUpTimeFactor = 0.45f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _dummyIkController = animator.GetComponent<DummyIkController>();

        // Start coroutine to smoothly transition weight
        animator.GetComponent<DummyIkController>().StartCoroutine(ModifyPickUpIkRigWeight(stateInfo));
        animator.GetComponent<DummyIkController>().StartCoroutine(ModifyHoldIkRigWeight(stateInfo));
    }

    public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime < 0.5f)
        {
            OrientPlayerTowardTarget(animator.transform, stateInfo);
        
            OrientTargetForHandRotation();
        }
         
    }

    private IEnumerator ModifyPickUpIkRigWeight(AnimatorStateInfo stateInfo)
    {
        
        float elapsedTime = 0f;

        while (elapsedTime < _pickUpTimeFactor*stateInfo.length)
        {
            float currentLoopNormalizedTime = elapsedTime/(_pickUpTimeFactor*stateInfo.length);

            if (currentLoopNormalizedTime < 0.5f) {  
                _dummyIkController.pickUpRig.weight = currentLoopNormalizedTime *2;
            }
            else
            {
                _dummyIkController.pickUpRig.weight = 2f - 2f * currentLoopNormalizedTime;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        _dummyIkController.pickUpRig.weight = 0f;
    }
    
    private IEnumerator ModifyHoldIkRigWeight(AnimatorStateInfo stateInfo)
    {
        
        float elapsedTime = 0f;

        while (elapsedTime < _pickUpTimeFactor*stateInfo.length)
        {
            float currentLoopNormalizedTime = elapsedTime/(_pickUpTimeFactor*stateInfo.length);

            if (currentLoopNormalizedTime < 0.5f)
            {
                yield return null;
            }
            else
            {
                _dummyIkController.holdRig.weight = currentLoopNormalizedTime;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        _dummyIkController.holdRig.weight = 1f;
    }

    /// <summary>
    /// Slowly turn the character to make sure it's facing the aimed target.
    /// </summary>
    private void OrientPlayerTowardTarget(Transform playerTransform, AnimatorStateInfo stateInfo)
    {
        
        // Calculate the direction from this object to the target object
        Vector3 directionFromPlayerToTarget = _dummyIkController.RightHandIkTarget.transform.position - playerTransform.position;
        
        // The y component should be 0 so the human rotate only on the XZ plane.
        directionFromPlayerToTarget.y = 0f;
        
        // Create a rotation to look in that direction
        Quaternion rotation = Quaternion.LookRotation(directionFromPlayerToTarget);

        // Interpolate the rotation based on the normalized time of the animation
        playerTransform.rotation = Quaternion.Lerp(playerTransform.rotation, rotation, 2*stateInfo.normalizedTime);
    }

    /// <summary>
    /// Create a rotation of the IK target to make sure the hand reach in a natural way the item.
    /// The rotation is such that it's Y axis is aligned with the line crossing through the character shoulder and IK target.
    /// </summary>
    private void OrientTargetForHandRotation()
    {
        Vector3 armTargetDirection = _dummyIkController.RightHandIkTarget.transform.position - _dummyIkController.RightUpperArm.transform.position;
        
        Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);
        
        targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

        _dummyIkController.RightHandIkTarget.transform.rotation = targetRotation;
    }
}

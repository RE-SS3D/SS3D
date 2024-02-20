using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Animations;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class DummyPunchIk : StateMachineBehaviour
{
    
    private float targetWeight = 1.0f;       // The target weight for the constraint
    private float transitionDuration = 0.6f; // The duration of the weight transition

    private DummyIkController _dummyIkController;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _dummyIkController = animator.GetComponent<DummyIkController>();

        // Start coroutine to smoothly transition weight
        animator.GetComponent<DummyIkController>().StartCoroutine(ModifyWeightOverTime(stateInfo));


    }

    public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Calculate the direction from this object to the target object
        Vector3 direction = _dummyIkController.RightPunchTarget.transform.position - animator.transform.position;

        // Create a rotation to look in that direction
        Quaternion rotation = Quaternion.LookRotation(direction);

        // Set rotation directly when normalized time reaches one
        if (stateInfo.normalizedTime >= 1f)
        {
            animator.transform.LookAt(_dummyIkController.RightPunchTarget.transform);
        }
        else
        {
            // Interpolate the rotation based on the normalized time of the animation
            animator.transform.rotation = Quaternion.Lerp(animator.transform.rotation, rotation, stateInfo.normalizedTime);
        }
        
        Vector3 armTargetDirection = _dummyIkController.RightArmTwoBoneIKConstraint.data.root.position
            - _dummyIkController.RightPunchTarget.transform.position;
        
        Quaternion targetRotation = Quaternion.LookRotation(-armTargetDirection.normalized);

        _dummyIkController.RightPunchTarget.transform.rotation = targetRotation;

        Vector3 upDirection = Vector3.Cross(direction, _dummyIkController.RightPunchTarget.transform.right).normalized;
        
        targetRotation = Quaternion.LookRotation(upDirection);
        
        _dummyIkController.RightPunchTarget.transform.rotation = targetRotation;
    }

    private IEnumerator ModifyWeightOverTime(AnimatorStateInfo stateInfo)
    {
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float currentLoopNormalizedTime = elapsedTime/transitionDuration;

            if (currentLoopNormalizedTime < 0.5f) {  
                //_dummyIkController.RightArmTwoBoneIKConstraint.weight = currentLoopNormalizedTime * 2;
                _dummyIkController.RightArmChainIKConstraint.weight = currentLoopNormalizedTime *2;
            }
            else
            {
                //dummyIkController.RightArmTwoBoneIKConstraint.weight = -2 * currentLoopNormalizedTime + 2;
                _dummyIkController.RightArmChainIKConstraint.weight = 2f - 2f * currentLoopNormalizedTime;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        _dummyIkController.RightArmChainIKConstraint.weight = 0f;
    }
}

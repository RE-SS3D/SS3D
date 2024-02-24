using System.Collections;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class DummyPunchIk : StateMachineBehaviour
{

    private DummyIkController _dummyIkController;

    
    private float _pickUpTimeFactor = 0.45f;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _dummyIkController = animator.GetComponent<DummyIkController>();

        // Start coroutine to smoothly transition weight
        animator.GetComponent<DummyIkController>().StartCoroutine(ModifyWeightOverTime(stateInfo));


    }

    public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OrientPlayerTowardTarget(animator.transform, stateInfo);
        
        OrientTargetForHandRotation();
    }

    private IEnumerator ModifyWeightOverTime(AnimatorStateInfo stateInfo)
    {
        
        float elapsedTime = 0f;

        while (elapsedTime < _pickUpTimeFactor*stateInfo.length)
        {
            float currentLoopNormalizedTime = elapsedTime/(_pickUpTimeFactor*stateInfo.length);

            if (currentLoopNormalizedTime < 0.5f) {  
                _dummyIkController.RightArmChainIKConstraint.weight = currentLoopNormalizedTime *2;
                _dummyIkController.HeadIKConstraint.weight = currentLoopNormalizedTime *2;
            }
            else
            {
                _dummyIkController.RightArmChainIKConstraint.weight = 2f - 2f * currentLoopNormalizedTime;
                _dummyIkController.HeadIKConstraint.weight = 2f - 2f * currentLoopNormalizedTime;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        _dummyIkController.RightArmChainIKConstraint.weight = 0f;
    }

    private void OrientPlayerTowardTarget(Transform playerTransform, AnimatorStateInfo stateInfo)
    {
        // Calculate the direction from this object to the target object
        Vector3 directionFromPlayerToTarget = _dummyIkController.RightPunchTarget.transform.position - playerTransform.position;
        
        // The y component should be 0 so the human rotate only on the XZ plane.
        directionFromPlayerToTarget.y = 0f;
        
        // Create a rotation to look in that direction
        Quaternion rotation = Quaternion.LookRotation(directionFromPlayerToTarget);

        // Interpolate the rotation based on the normalized time of the animation
        playerTransform.rotation = Quaternion.Lerp(playerTransform.rotation, rotation, stateInfo.normalizedTime);
    }

    private void OrientTargetForHandRotation()
    {
        Vector3 armTargetDirection = _dummyIkController.RightPunchTarget.transform.position 
            - _dummyIkController.RightArmTwoBoneIKConstraint.data.root.position;
        
        Quaternion targetRotation = Quaternion.LookRotation(armTargetDirection.normalized, Vector3.down);
        
        targetRotation *= Quaternion.AngleAxis(90f, Vector3.right);

        _dummyIkController.RightPunchTarget.transform.rotation = targetRotation;
    }
}

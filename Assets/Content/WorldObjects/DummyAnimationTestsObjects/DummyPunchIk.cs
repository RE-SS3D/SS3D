using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class DummyPunchIk : StateMachineBehaviour
{
    
    private float targetWeight = 1.0f;       // The target weight for the constraint
    private float transitionDuration = 1.1f; // The duration of the weight transition

    private DummyIkController _dummyIkController;
    
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _dummyIkController = animator.GetComponent<DummyIkController>();

        // Start coroutine to smoothly transition weight
        animator.GetComponent<DummyIkController>().StartCoroutine(ModifyWeightOverTime(stateInfo));
    }
    
    private IEnumerator ModifyWeightOverTime(AnimatorStateInfo stateInfo)
    {
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float currentLoopNormalizedTime = elapsedTime/transitionDuration;

            if (currentLoopNormalizedTime < 0.5f) {  
                _dummyIkController.RightArmChainIKConstraint.weight = currentLoopNormalizedTime * 2;}
            else
            {
                _dummyIkController.RightArmChainIKConstraint.weight = -2 * currentLoopNormalizedTime + 2;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        _dummyIkController.RightArmChainIKConstraint.weight = 0f;
    }
}

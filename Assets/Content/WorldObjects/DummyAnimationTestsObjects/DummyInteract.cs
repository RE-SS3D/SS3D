using InspectorGadgets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyInteract : MonoBehaviour
{
    
    public DummyHands hands;
    public Transform lookAtTargetLocker;
    public Transform hips;
    
    public MultiAimConstraint lookAtConstraint;

    public float interactionMoveDuration;
    
    public bool UnderMaxDistanceFromHips(Vector3 position) => Vector3.Distance(hips.position, position) < 1.1f;
    

    // Update is called once per frame
    void Update()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        TryInteract();
    }
    
    private void TryInteract()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit) && hands.SelectedHand.Full &&
            UnderMaxDistanceFromHips(hit.point) && hands.SelectedHand.item.TryGetComponent(out DummyTool tool))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;
            StartCoroutine(Interact(obj.transform, hands.SelectedHand));
        }
        
        
    }

    private IEnumerator Interact(Transform interactionTarget, DummyHand mainHand)
    {
        DummyItem tool = hands.SelectedHand.item;

        // disable position constraint the time of the interaction
        mainHand.itemPositionConstraint.weight = 0f;
        
        mainHand.pickupIkConstraint.weight = 1f;
        
        // Start looking at item
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            0f, 1f, interactionMoveDuration));
        
        // turn the player toward its target so all subsequent computations
        // are correctly done with player oriented toward target. Then, in the same frame,
        // put player at its initial rotation.
        Vector3 directionFromTransformToTarget = interactionTarget.position - transform.position;
        directionFromTransformToTarget.y = 0f;
        Quaternion initialPlayerRotation = transform.rotation;
        transform.rotation = Quaternion.LookRotation(directionFromTransformToTarget);
        
        Vector3 startPosition = tool.transform.position;

        Transform initialParent = tool.transform.parent;

        Vector3 fromShoulderToTarget = (interactionTarget.transform.position - mainHand.upperArm.transform.position).normalized;
        
        // rotate the tool such that its interaction transform Z axis align with the fromShoulderToTarget vector.
        Quaternion rotation = Quaternion.FromToRotation(tool.interactionPoint.TransformDirection(Vector3.forward), fromShoulderToTarget.normalized);
        
        // Apply the rotation to transform A
        tool.transform.rotation = rotation * tool.transform.rotation;
        
        // Calculate the difference between the tool position and its interaction point.
        // Warning : do it only after applying the rotation.
        Vector3 difference = tool.interactionPoint.position - tool.transform.position;

        // Compute the desired position for the tool
        Vector3 endPosition = interactionTarget.position - difference;

        // take back initial rotation after insuring all computations above are done
        // with the right orientation.
        transform.rotation = initialPlayerRotation;
        
        // Rotate player toward item
        StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
            transform, interactionTarget, interactionMoveDuration, false, true));

        yield return CoroutineHelper.ModifyVector3OverTime(x => 
            tool.transform.position = x,  startPosition, endPosition, interactionMoveDuration);

        yield return new WaitForSeconds(0.6f);
        
        tool.transform.parent = initialParent;
        
        // Stop looking at item         
        StartCoroutine(CoroutineHelper.ModifyValueOverTime(x => lookAtConstraint.weight= x,
            1f, 0f, interactionMoveDuration));
        
        StartCoroutine(CoroutineHelper.ModifyQuaternionOverTime(x => 
                tool.transform.localRotation = x,tool.transform.localRotation,
            Quaternion.identity, 2*interactionMoveDuration));
        
        
        yield return CoroutineHelper.ModifyVector3OverTime(x => 
            tool.transform.localPosition = x, tool.transform.localPosition,
            Vector3.zero, 2*interactionMoveDuration);

        tool.transform.localRotation = Quaternion.identity;
        mainHand.itemPositionConstraint.weight = 1f;
        mainHand.pickupIkConstraint.weight = 0f;
        

    }
}

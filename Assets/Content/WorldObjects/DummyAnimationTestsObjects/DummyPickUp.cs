using SS3D.Systems.Inventory.Containers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPickUp : MonoBehaviour
{

    public float itemMoveDuration;
    public float itemReachDuration;

    private float _pickUpDuration;

    public DummyIkController dummyIkController;

    private void Start()
    {
        _pickUpDuration = itemMoveDuration + itemReachDuration;
    }
    
    private void Update()
    {

        if (!Input.GetMouseButtonDown(0))
            return;

        if (GetComponent<DummyHands>().IsSelectedHandEmpty)
        {
            TryPickUp();
        }
        else
        {
            TryThrow();
        }

    }

    private void PickUp(DummyItem item)
    {
        GetComponent<DummyAnimatorController>().TriggerPickUp();

        StartCoroutine(ModifyPickUpIkRigWeight());
        
        GetComponent<DummyIkController>().rightHandPickUpIkTarget.transform.parent = item.rightHandHold.transform;
        GetComponent<DummyIkController>().rightHandPickUpIkTarget.transform.localPosition = Vector3.zero;
        
        GetComponent<DummyIkController>().leftHandPickUpIkTarget.transform.parent = item.leftHandHold.transform;
        GetComponent<DummyIkController>().leftHandPickUpIkTarget.transform.localPosition = Vector3.zero;
        
        GetComponent<DummyIkController>().rightHandHoldIkTarget.transform.parent = item.rightHandHold.transform;
        GetComponent<DummyIkController>().rightHandHoldIkTarget.transform.localPosition = Vector3.zero;
        GetComponent<DummyIkController>().rightHandHoldIkTarget.transform.localRotation = Quaternion.identity;
        
        GetComponent<DummyIkController>().leftHandHoldIkTarget.transform.parent = item.leftHandHold.transform;
        GetComponent<DummyIkController>().leftHandHoldIkTarget.transform.localPosition = Vector3.zero;
        GetComponent<DummyIkController>().leftHandHoldIkTarget.transform.localRotation = Quaternion.identity;


        StartCoroutine(PutItemInHand(item));
    }

    private IEnumerator PutItemInHand(DummyItem item)
    {
        yield return new WaitForSeconds(itemReachDuration);
        
        StartCoroutine(MoveItemToHold(item.gameObject,  GetComponent<DummyIkController>().gunHold));
    }

    private void TryPickUp()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;

            if (obj.TryGetComponent(out DummyItem item))
            {
                PickUp(item);
            }
            
        }
    }

    private void TryThrow()
    {
        GetComponent<DummyAnimatorController>().TriggerThrow();
        GetComponent<DummyHands>().RemoveItemFromSelectedHand();
        GetComponent<DummyIkController>().holdRig.weight = 0f;
    }
    
    private IEnumerator MoveItemToHold(GameObject item, GameObject targetHold)
    {
        Vector3 initialPosition = item.transform.position;
        Quaternion initialRotation = item.transform.rotation;
        float timer = 0.0f;

        while (timer < itemMoveDuration)
        {
            float t = timer / itemMoveDuration;
            item.transform.position = Vector3.Lerp(initialPosition, targetHold.transform.position, t);
            item.transform.rotation = Quaternion.Lerp(initialRotation, targetHold.transform.rotation, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final transform matches the target values exactly
        item.transform.position = targetHold.transform.position;
        item.transform.rotation = targetHold.transform.rotation;
        
        GetComponent<DummyHands>().AddItemToSelectedHand(item.gameObject);
    }
    
    private IEnumerator ModifyPickUpIkRigWeight()
    {
        
        float elapsedTime = 0f;
        bool startHolding = false;

        while (elapsedTime < _pickUpDuration)
        {
            float currentLoopNormalizedTime = elapsedTime/_pickUpDuration;

            if (currentLoopNormalizedTime < 0.5f) {  
                dummyIkController.pickUpRig.weight = currentLoopNormalizedTime *2;
            }
            else
            {
                if (!startHolding)
                {
                    StartCoroutine(ModifyHoldIkRigWeight());
                    startHolding = true;
                }
                dummyIkController.pickUpRig.weight = 2f - 2f * currentLoopNormalizedTime;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        dummyIkController.pickUpRig.weight = 0f;
    }
    
    private IEnumerator ModifyHoldIkRigWeight()
    {
        
        float elapsedTime = 0f;

        while (elapsedTime < itemMoveDuration)
        {
            float weight = elapsedTime/itemMoveDuration;

            dummyIkController.holdRig.weight = weight;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weight reaches the target value exactly
        dummyIkController.holdRig.weight = 1f;
    }
}

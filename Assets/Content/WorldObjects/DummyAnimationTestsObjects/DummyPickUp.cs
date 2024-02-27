using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPickUp : MonoBehaviour
{
    private RaycastHit hit; // Store information about the object hit by the raycast

    public DummyItem SelectedItem;

    public Transform RightHandHoldTransform;

    public Transform LeftHandHoldTransform;

    private void Update()
    {

        if (!Input.GetMouseButtonDown(0))
            return;

        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the collider belongs to a GameObject
            GameObject obj = hit.collider.gameObject;

            if (obj.TryGetComponent(out DummyItem item))
            {
                PickUp(item);
            }
            
        }

    }

    private void PickUp(DummyItem item)
    {
        GetComponent<DummyAnimatorController>().TriggerPickUp();
        GetComponent<DummyIkController>().RightHandIkTarget.transform.position = item.rightHandHold.transform.position;
        GetComponent<DummyIkController>().RightHandIkTarget.transform.rotation = item.rightHandHold.transform.rotation;
    }
}

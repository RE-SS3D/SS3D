using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPickUp : MonoBehaviour
{

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
        GetComponent<DummyIkController>().RightHandIkTarget.transform.position = item.rightHandHold.transform.position;
        GetComponent<DummyIkController>().RightHandIkTarget.transform.rotation = item.rightHandHold.transform.rotation;
        StartCoroutine(PutItemInHand(item));
    }

    private IEnumerator PutItemInHand(DummyItem item)
    {
        yield return new WaitForSeconds(0.4f);
        GetComponent<DummyHands>().AddItemToSelectedHand(item.gameObject);
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
    }
}

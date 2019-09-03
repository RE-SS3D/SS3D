using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetButtonDown("Click"))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100))
            {
                InteractableClass interactable = hit.collider.GetComponent<InteractableClass>();
                if (interactable) interactable.Click();
            }
        }
    }
}

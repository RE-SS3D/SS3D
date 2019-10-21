using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Allows the player to interact with things
 */
public class Interaction : MonoBehaviour
{
    public Tool selectedTool;

    public void Update() {
        // TODO: Use proper input types
        if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            // Find the object being pointed to.
            // If there is one, interact with it using the selected tool.
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100))
                selectedTool.Interact(hit.collider.gameObject, Input.GetMouseButtonDown(2));
        }
    }
}

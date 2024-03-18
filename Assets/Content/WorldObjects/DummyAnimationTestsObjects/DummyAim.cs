using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyAim : MonoBehaviour
{

    public Transform aimTarget;

    public DummyHands hands;

    public IntentController intents;

    public float rotationSpeed = 5f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            aimTarget.position = hit.point;
        }

        if (intents.intent == Intent.Harm && hands.SelectedHand.Full)
        {
            DummyItem item = hands.SelectedHand.item;
            item.transform.parent = hands.SelectedHand.shoulderWeaponPivot;
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            
            // Get the direction to the target
            Vector3 direction = aimTarget.position - transform.position;
            direction.y = 0f; // Ignore Y-axis rotation

            // Rotate towards the target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}

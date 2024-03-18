using InspectorGadgets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyAim : MonoBehaviour
{

    public Transform aimTarget;

    public DummyHands hands;

    public IntentController intents;

    public Rig bodyAimRig;

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

        if (intents.intent == Intent.Harm && hands.SelectedHand.Full 
            && hands.SelectedHand.item.TryGetComponent(out DummyGun gun))
        {
            bodyAimRig.weight = 0.3f;
            DummyItem item = hands.SelectedHand.item;
            item.transform.parent = hands.SelectedHand.shoulderWeaponPivot;
            
            // position correctly the gun on the shoulder, assuming the rifle butt transform is defined correctly
            item.transform.localPosition = -gun.rifleButt.localPosition ;
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
        else
        {
            bodyAimRig.weight = 0f;
        }
    }
}

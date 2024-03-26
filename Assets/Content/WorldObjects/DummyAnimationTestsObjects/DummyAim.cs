using InspectorGadgets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UIElements;

public class DummyAim : MonoBehaviour
{

    public Transform aimTarget;

    public DummyHands hands;

    public IntentController intents;

    public HoldController holdController;

    public Rig bodyAimRig;

    public float rotationSpeed = 5f;

    public bool canAim;

    public bool isAiming;
  

    private void Update()
    {
        UpdateAimAbility(hands.SelectedHand);

        if (canAim && Input.GetMouseButton(1))
        {
            UpdateAimTargetPosition();
            
            if (!isAiming)
            {
                Aim(hands.SelectedHand, hands.SelectedHand.item.GetComponent<DummyGun>());
                isAiming = true;
            }

            if (GetComponent<DummyPositionController>().Position != PositionType.Sitting)
            {
                RotatePlayerTowardTarget();
            }

            
        }
        else if(isAiming && (!canAim || !Input.GetMouseButton(1)))
        {
            StopAiming(hands.SelectedHand);
        }
        
        Debug.Log( "Has gun : "+ hands.SelectedHand.item.TryGetComponent(out DummyGun gun));
        Debug.Log("is aiming");
        Debug.Log("hands full");

        if (Input.GetKey(KeyCode.E) && hands.SelectedHand.Full 
            && gun != null && isAiming)
        {
            gun.GetComponent<DummyFire>().Fire();
        }
    }

    private void Aim(DummyHand hand, DummyGun gun)
    {
        bodyAimRig.weight = 0.3f;
        gun.transform.parent = hands.SelectedHand.shoulderWeaponPivot;
            
        // position correctly the gun on the shoulder, assuming the rifle butt transform is defined correctly
        gun.transform.localPosition = -gun.rifleButt.localPosition ;
        gun.transform.localRotation = Quaternion.identity;
    }

    private void StopAiming(DummyHand hand)
    {
        isAiming = false;
        bodyAimRig.weight = 0f;
        if(!hand.Full) return;

        hand.item.transform.parent = hand.itemPositionTargetLocker;
        holdController.UpdateItemPositionConstraintAndRotation(hand, true, 0.5f);
    }
    
    private void UpdateAimAbility(DummyHand selectedHand)
    {
        if (intents.intent == Intent.Harm && selectedHand.Full 
            && selectedHand.item.TryGetComponent(out DummyGun gun))
        {
            canAim = true;
        }
        else
        {
            canAim = false;
        }
    }

    private void UpdateAimTargetPosition()
    {
        // Cast a ray from the mouse position into the scene
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // Check if the ray hits any collider
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            aimTarget.position = hit.point;
        }
    }

    private void RotatePlayerTowardTarget()
    {
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

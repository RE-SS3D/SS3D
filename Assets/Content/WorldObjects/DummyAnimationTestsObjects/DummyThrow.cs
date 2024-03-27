using InspectorGadgets;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class DummyThrow : MonoBehaviour
{
    public DummyHands hands;
    public IntentController intents;
    public DummyAnimatorController animatorController;

    public float timeToTarget = 1f;

    public Transform aimTarget;

    private float _maxForce = 10;
    
    public float rotationSpeed = 5f;

    private bool canAim;

    private bool isAiming;

    public HoldController holdController;
    
    public Rig bodyAimRig;
    
    
    // Update is called once per frame
    void Update()
    {
        UpdateAimAbility(hands.SelectedHand);

        if (canAim && Input.GetMouseButton(1))
        {
            UpdateAimTargetPosition();
            
            if (!isAiming)
            {
                Aim();
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

        if (Input.GetKeyDown(KeyCode.Y) && hands.SelectedHand.Full && isAiming)
        {
            StartCoroutine(Throw());
        }

    }

    private IEnumerator Throw()
    {
        DummyItem item = hands.SelectedHand.item;
        hands.SelectedHand.itemPositionConstraint.weight = 0f;
        hands.SelectedHand.holdIkConstraint.weight = 0f;
        hands.SelectedHand.pickupIkConstraint.weight = 0f;
        
        item.transform.parent = hands.SelectedHand.handBone.transform;
        
        animatorController.Throw(hands.SelectedHand.handType);
        
        StartCoroutine(DummyTransformHelper.OrientTransformTowardTarget(
            transform, aimTarget.transform, 0.18f, false, true));

        yield return new WaitForSeconds(0.18f);

        Vector2 targetCoordinates = ComputeTargetCoordinates(aimTarget.position, transform);

        Vector2 initialItemCoordinates = ComputeItemInitialCoordinates(item.transform.position, transform);

        Vector2 initialVelocity = ComputeInitialVelocity(timeToTarget, targetCoordinates, 
            initialItemCoordinates.y, initialItemCoordinates.x);

        Vector3 initialVelocityInRootCoordinate = new Vector3(0, initialVelocity.y, initialVelocity.x);

        Vector3 initialVelocityInWorldCoordinate = transform.TransformDirection(initialVelocityInRootCoordinate);

        hands.SelectedHand.RemoveItem();
        
        item.GetComponent<Rigidbody>().AddForce(initialVelocityInWorldCoordinate, ForceMode.VelocityChange);
    }

    private Vector2 ComputeInitialVelocity(float timeToReachTarget, Vector2 targetCoordinates, float initialHeight,
        float initialHorizontalPosition)
    {
        // Those computations assume gravity is pulling in the same plane as the throw.
        // it works with any vertical gravity but not if there's a horizontal component to it.
        // be careful as g = -9.81 and not 9.81
        float g = Physics.gravity.y;
        float initialHorizontalVelocity = (targetCoordinates.x-initialHorizontalPosition) / timeToReachTarget;
        
        float initialVerticalVelocity = (targetCoordinates.y - initialHeight
            - 0.5f * g * (math.pow(targetCoordinates.x - initialHorizontalPosition,2) / math.pow(initialHorizontalVelocity,2)))
            * initialHorizontalVelocity/(targetCoordinates.x-initialHorizontalPosition);

        return new Vector2(initialHorizontalVelocity, initialVerticalVelocity);

    }

    /// <summary>
    /// Compute coordinates in the local coordinate system of the throwing hand
    /// This method assumes that the target position is in the same plane as the plane defined by the
    /// player y and z local axis.
    /// return vector2 with components in order z and y, as z is forward and y upward.
    /// </summary>
    private Vector2 ComputeTargetCoordinates(Vector3 targetPosition, Transform playerRoot)
    {
        Vector3 rootRelativeTargetPosition = playerRoot.InverseTransformPoint(targetPosition);

        if (rootRelativeTargetPosition.x > 0.1f)
        {
            Debug.LogError("target not in the same plane as the player root : " + rootRelativeTargetPosition.x);
        }

        return new Vector2(rootRelativeTargetPosition.z, rootRelativeTargetPosition.y);
    }

    private Vector2 ComputeItemInitialCoordinates(Vector3 itemPosition, Transform playerRoot)
    {
        Vector3 rootRelativeItemPosition = playerRoot.InverseTransformPoint(itemPosition);

        return new Vector2(rootRelativeItemPosition.z, rootRelativeItemPosition.y);
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
    
    private void Aim()
    {
        bodyAimRig.weight = 0.3f;
        holdController.UpdateItemPositionConstraintAndRotation(hands.SelectedHand, 
            false, 0.2f, true);
    }

    private void StopAiming(DummyHand hand)
    {
        isAiming = false;
        bodyAimRig.weight = 0f;
        holdController.UpdateItemPositionConstraintAndRotation(hands.SelectedHand, 
            false, 0.2f, false);
    }
    
    private void UpdateAimAbility(DummyHand selectedHand)
    {
        if (selectedHand.Full)
        {
            canAim = true;
        }
        else
        {
            canAim = false;
        }
    }
}

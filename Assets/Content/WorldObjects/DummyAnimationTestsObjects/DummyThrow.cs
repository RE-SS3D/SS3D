using InspectorGadgets;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class DummyThrow : MonoBehaviour
{
    public DummyHands hands;
    public IntentController intents;
    public DummyAnimatorController animatorController;

    public Transform aimTarget;

    private float _maxForce = 10;
    
    public float rotationSpeed = 5f;
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (intents.intent != Intent.Throw || !Input.GetKeyDown(KeyCode.Y))
            return;
        
        UpdateAimTargetPosition();

        StartCoroutine(Throw());
    }

    private IEnumerator Throw()
    {
        animatorController.Throw(hands.SelectedHand.handType);

        yield return new WaitForSeconds(0.25f);

        Vector2 targetCoordinates = ComputeTargetCoordinates(aimTarget.position, transform);

        Vector2 initialVelocity = ComputeInitialVelocity(3f, targetCoordinates);
    }

    private Vector2 ComputeInitialVelocity(float timeToReachTarget, Vector2 targetCoordinates)
    {
        // Those computations assume gravity is pulling in the same plane as the throw.
        // it works with any vertical gravity but not if there's a horizontal component to it.
        float g = Physics.gravity.y;
        float initialHorizontalVelocity = targetCoordinates.x / timeToReachTarget;
        
        float initialVerticalVelocity = (targetCoordinates.y 
            + 0.5f * g * (math.pow(targetCoordinates.x,2) / math.pow(initialHorizontalVelocity,2)))
            * initialHorizontalVelocity/targetCoordinates.x;

        return new Vector2(initialHorizontalVelocity, initialVerticalVelocity);

    }

    /// <summary>
    /// Compute coordinates in the local coordinate system of the throwing hand
    /// This method assumes that the target position is in the same plane as the plane defined by the
    /// player y and z local axis.
    /// </summary>
    private Vector2 ComputeTargetCoordinates(Vector3 targetPosition, Transform playerRoot)
    {
        Vector3 rootRelativeTargetPosition = playerRoot.InverseTransformPoint(targetPosition);

        if (rootRelativeTargetPosition.x > 0.1f)
        {
            Debug.LogError("target not in the same plane as the player root");
        }

        return new Vector2(rootRelativeTargetPosition.y, rootRelativeTargetPosition.z);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyCrouch : MonoBehaviour
{
    public Transform hipBone;
    public Transform kneeBone;
    public Transform root;

    public float crouchHeight = 0.5f; // Adjust as needed

    void Update()
    {
        // Calculate desired hip position based on crouchHeight
        Vector3 desiredHipPosition = hipBone.position - Vector3.up * crouchHeight;

        // Calculate direction from foot to hip
        Vector3 footToHipDirection = (desiredHipPosition - root.position).normalized;

        // Calculate rotation angle for the knee
        float kneeRotationAngle = Vector3.Angle(Vector3.up, footToHipDirection);

        // Apply rotation to knee bone
        kneeBone.rotation = Quaternion.FromToRotation(Vector3.up, footToHipDirection) * kneeBone.rotation;
    }
}

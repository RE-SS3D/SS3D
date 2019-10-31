/*
    Attach this script onto a slippery object like soap with a collider
    set to activate with a trigger and it will attempt to ragdoll anything
    that gets in the collider's bounds
*/

using UnityEngine;
using Mirror;

[RequireComponent(typeof(Collider))]
public class Soap : NetworkBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Set the object that touched the soap, and check to see if it has a ragdoll script
        var ragdollManager = other.gameObject.GetComponent<RagdollManager>();
        var moveController = other.gameObject.GetComponent<MovementController>();
        
        // This will check to see if we can ragdoll stuff, most likely the player
        if (ragdollManager && moveController && moveController.currentMovement.magnitude > 3f)
        {
            // Enable the object's ragdoll so that it starts to fall
            ragdollManager.CmdSetRagdolled(true);

            // Magnitude of the force is 167 * magnitude of player movement which gets up to 6 so 1000 maximum force
            var force = other.gameObject.transform.forward * moveController.currentMovement.magnitude * 167f;
            
            // Check to see if fall foward or backward
            if (Mathf.Round(Random.value) == 1f)
            {
                // Push the feet back
                ragdollManager.CmdAddForce("lower_leg.r", force);
                ragdollManager.CmdAddForce("lower_leg.l", force);

                // Push the soap relative to player
                GetComponent<Rigidbody>().AddForce(other.gameObject.transform.forward * 200f);
            }
            else
            {
                // Push the feet forward
                ragdollManager.CmdAddForce("lower_leg.r", -force);
                ragdollManager.CmdAddForce("lower_leg.l", -force);

                // Push the soap relative to player
                GetComponent<Rigidbody>().AddForce(other.gameObject.transform.forward * -200f);
            }
        }
    }
}

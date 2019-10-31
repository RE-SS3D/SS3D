/*
    This manager will ragdoll the whatever given different inputs.
    To use it, simply add this script to a valid model with a ragdoll
    setup, then you can use CmdSetRagdolled(true) to switch the ragdoller on
    
    Functions:
    GetRagdolled() - Returns the state of the ragdoll
    CmdSetRagdolled(bool newValue) - Enables the ragdoll on the player
    CmdAddForce(string targetName, Vector3 amount) - Adds a force to a
        target bone's name given a magnitude and direction
*/

using UnityEngine;
using Mirror;

public class RagdollManager : NetworkBehaviour
{
    private bool ragdolled = false;
    private Rigidbody[] bodies;
    private Animator animator;
    private CharacterController charController;


    // Getter for ragdolled
    public bool GetRagdolled()
    {
        return ragdolled;
    }


    // Enables the ragdoll on the player
    [Command]
    public void CmdSetRagdolled(bool newValue)
    {
        // Tell server to update Animator, CharacterController, and ragdolled, if available
        if (animator)
            animator.enabled = !newValue;
        if (charController)
            charController.enabled = !newValue;
        ragdolled = newValue;

        RpcSetRagdolled(newValue);
    }


    [ClientRpc]
    public void RpcSetRagdolled(bool newValue)
    {
        // Tell clients to update Animator, CharacterController, and ragdolled, if available
        if (animator)
            animator.enabled = !newValue;
        if (charController)
            charController.enabled = !newValue;
        ragdolled = newValue;

        // For each of the components in the array, treat the component as a Rigidbody and set its isKinematic property
        foreach (Rigidbody rb in bodies)
        {
            rb.isKinematic = !newValue;
        }
    }


    // Applies a force on the rigid body given the bone's name
    [Command]
    public void CmdAddForce(string targetString, Vector3 amount)
    {
        RpcAddForce(targetString, amount);
    }


    [ClientRpc]
    public void RpcAddForce(string targetString, Vector3 amount)
    {
        foreach (Rigidbody rb in bodies)
        {
            if(rb.name == targetString)
                rb.AddForce(amount);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        bodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();
        charController = GetComponent<CharacterController>();
        // Make sure the player is not ragdolled on start
        CmdSetRagdolled(false);
    }


    // Check if player can get up after being ragdolled
    void FixedUpdate()
    {
        // The ragdoll will "sleep" effectively stopping it from updating, that's when you get up
        if (ragdolled && bodies[0].IsSleeping())
        {
            CmdSetRagdolled(false);
            transform.position = bodies[0].position;
        }
    }
}

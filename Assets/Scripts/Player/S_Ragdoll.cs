/*
Ragdoll Script
Created by Singulo
10/26/2019
The purpose of this script is to ragdoll the player given different inputs
*/

using UnityEngine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(MovementController))]
public class S_Ragdoll : NetworkBehaviour
{
    public bool ragdolled = false;
    private Rigidbody[] bodies;


    [Command]
    void CmdGetUp(bool newValue)
    {
        GetComponent<Animator>().enabled = newValue;
        GetComponent<CharacterController>().enabled = newValue;
        RpcSlip(0f , newValue);
    }


    [ClientRpc]
    public void RpcSlip(float force, bool newValue)
    {
        GetComponent<Animator>().enabled = newValue;
        GetComponent<CharacterController>().enabled = newValue;

        // For each of the components in the array, treat the component as a Rigidbody and set its isKinematic property
        foreach (Rigidbody rb in bodies)
        {
            rb.isKinematic = newValue;
            if((rb.name == "lower_leg.l" || rb.name == "lower_leg.r") && newValue == false)
                    // Magnitude of force is 167 * max value of 6 ~= 1000. This way ragdoll won't slip if not moving
                    rb.AddForce(transform.forward * force * GetComponent<MovementController>().currentMovement.magnitude);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        bodies = GetComponentsInChildren<Rigidbody>();
        CmdGetUp(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer) return;
        // Debug button press to test the ragdoll
        if(Input.GetKeyDown(KeyCode.R))
        {
            ragdolled = !ragdolled;
            if(!ragdolled)
            {
                CmdGetUp(true);
                transform.position = bodies[0].position;
            }
        }
    }
}

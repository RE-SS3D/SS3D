/*
The purpose of this script is to ragdoll anything that touches the soap
*/

using UnityEngine;
using Mirror;

public class Soap : NetworkBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Set the object that touched the soap, and check to see if it has a ragdoll script
        var ragdollManager = other.gameObject.GetComponent<RagdollManager>();
        if (ragdollManager)
        {
            ragdollManager.SetRagdolled(true);
            // Check to see if fall foward or backward
            if (Mathf.Round(Random.value) == 1f)
            {
                ragdollManager.CmdSlip(167f, false);

                // Push the soap relative to player
                GetComponent<Rigidbody>().AddForce(other.gameObject.transform.forward * 200f);
            }
            else
            {
                ragdollManager.CmdSlip(-167f, false);

                // Push the soap relative to player
                GetComponent<Rigidbody>().AddForce(other.gameObject.transform.forward * -200f);
            }
        }
    }
}

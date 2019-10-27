/*
Soap Script
Created by Singulo
10/26/2019
The purpose of this script is to ragdoll anything that touches the soap
*/

using UnityEngine;

public class S_Soap : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Set the object that touched the soap, and check to see if it has a ragdoll script
        var ragdollScript = other.gameObject.GetComponent<S_Ragdoll>();
        if (ragdollScript)
        {
            ragdollScript.ragdolled = true;
            // Check to see if fall foward or backward
            if (Mathf.Round(Random.value) == 1f)
            {
                ragdollScript.RpcSlip(167f ,false);
                GetComponent<Rigidbody>().AddForce(other.gameObject.transform.forward * 200f);
            }
            else
            {
                ragdollScript.RpcSlip(-167f ,false);
                GetComponent<Rigidbody>().AddForce(other.gameObject.transform.forward * -200f);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

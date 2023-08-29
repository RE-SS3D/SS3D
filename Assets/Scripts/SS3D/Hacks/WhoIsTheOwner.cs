using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple script to check the owner of a game object this script is on.
/// Simply press F to show the owner in console.
/// </summary>
public class WhoIsTheOwner : NetworkBehaviour
{

    // Update is called once per frame
    void Update()
    {

		if (!Input.GetKeyDown(KeyCode.F))
		{
			return;
		}

		Debug.Log("Owner of gameobject" + gameObject.name + " is " + Owner);
	}
}

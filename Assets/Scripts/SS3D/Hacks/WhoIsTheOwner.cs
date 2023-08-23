using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhoIsTheOwner : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

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

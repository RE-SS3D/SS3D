using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraRegister : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
            Camera.main.GetComponent<CameraFollow>().target = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

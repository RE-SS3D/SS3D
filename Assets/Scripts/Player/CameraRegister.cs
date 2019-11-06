using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraRegister : NetworkBehaviour
{
    // Start is called before the first frame update
    public override void OnStartLocalPlayer()
    {
        Camera.main.GetComponent<CameraFollow>().SetTarget(gameObject);
    }
}

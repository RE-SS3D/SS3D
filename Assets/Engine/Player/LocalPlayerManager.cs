using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LocalPlayerManager : MonoBehaviour
{
    public static LocalPlayerManager singleton { get; private set; }

    public NetworkConnection networkConnection;
    
    private void Awake()
    {
        if (singleton != null && singleton != this)
        {
            Destroy(gameObject);
        }
        else
        {
            singleton = this;
        }
    }
}

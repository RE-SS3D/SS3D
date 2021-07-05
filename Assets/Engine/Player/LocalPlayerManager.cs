using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


/// <summary>
/// <b>This is handy to find the local user</b>
///
/// <para>
/// This has to be a singleton so we always get the same guy,
/// avoid this at all costs for networked operations
/// </para>
///
/// <para>
/// The local player has the <i>DontDestroyOnLoad(this)</i> so it is persistent throughout
/// loading and unloading of scenes
/// </para>
/// <param name="ckey">unique client key</param>
/// <param name="networkConnection">connection the local player is</param>
/// </summary>
public class LocalPlayerManager : MonoBehaviour
{
    public static LocalPlayerManager singleton { get; private set; }

    // Unique client username
    public string ckey;
    
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
        
        DontDestroyOnLoad(this);
    }
}

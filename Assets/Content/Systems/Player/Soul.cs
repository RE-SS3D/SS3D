using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Soul : NetworkBehaviour
{
    // Handy stuff to get the player's connection
    public NetworkConnectionToClient connection;

    private void Start()
    {
        if (isServer) ServerLobbyUIHelper.singleton.UnlockServerSettings();
        
        if(!isLocalPlayer) return;

        LocalPlayerManager.singleton.networkConnection = netIdentity.connectionToClient;
        connection = netIdentity.connectionToClient;
    }
}

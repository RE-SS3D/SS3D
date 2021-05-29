using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Soul : NetworkBehaviour
{
    private void Start()
    {
        if (isServer) ServerLobbyUIHelper.singleton.UnlockServerSettings();
        
        if(!isLocalPlayer) return;

        LocalPlayerManager.singleton.networkConnection = netIdentity.connectionToClient;

    }
}

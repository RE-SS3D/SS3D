using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using SS3D.Engine.Database;

public class Soul : NetworkBehaviour
{
    public CharacterData[] characterData;
    private void Start()
    {
        if (isServer) ServerLobbyUIHelper.singleton.UnlockServerSettings();
        
        if(!isLocalPlayer) return;

        LocalPlayerManager.singleton.networkConnection = netIdentity.connectionToClient;

    }
}

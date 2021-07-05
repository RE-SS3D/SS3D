using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using SS3D.Engine.Database;
using UnityEngine.SocialPlatforms;

/// <summary>
/// <b>
/// Unique object that each player has, it stores his current controlled GameObject
/// </b>
///
/// <para>
/// The user needs to be able to control anything we want them to, so we have a Soul
/// for each player, that persists if the player dies
/// </para>
///
/// <param name="characterData">All CharacterData this player has</param>
/// <param name="ckey">Unique key the client has</param>
/// 
/// </summary>
public class Soul : NetworkBehaviour
{   
    public CharacterData[] characterData;
    [SyncVar] public string ckey;
    private void Start()
    {
        // if the Soul is the host we unlock the server settings
        if (isServer) ServerLobbyUIHelper.singleton.UnlockServerSettings();

        // if the Soul is not the local player we skip the rest
        if(!isLocalPlayer) return;

        // sets up the connection so we have easy access for local stuff
        LocalPlayerManager.singleton.networkConnection = netIdentity.connectionToClient;
        
        // updates the CKEY from the local player and sends it to the server
        CmdUpdateCkey(LocalPlayerManager.singleton.ckey);
    }

    /// <summary>
    /// Updates the ckey on the network connection
    /// </summary>
    [Command(ignoreAuthority = true)]
    private void CmdUpdateCkey(string newCkey, NetworkConnectionToClient sender = null)
    {
        if (sender != null)
            // updates the connection ckey (might be removed)
            sender.ckey = newCkey;
        
        // updates the Soul ckey
        ckey = newCkey; 
    }
}

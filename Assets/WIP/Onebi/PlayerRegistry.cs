using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[Serializable]
public class SyncListPlayers : SyncList<PlayerData> { }

[Serializable]
public class PlayerRegistry : NetworkBehaviour
{
    public SyncListPlayers playersList;
    
    internal void RegisterPlayer(PlayerData component)
    {
        playersList.Add(component);
        Debug.Log("new player added, now have " + playersList.Count + " players");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

/* 
 * This class should be used to stock meta-game data.
 * it should contain but is not limited to :
 * the Player's OOC pseudonym
 * the Player's character templates (the ones he has customized and will play)
 * the player's status, is he playing? observing?
 * the player's playtime
 * the player's history of bans and the likes
 * the player's current moderation status
 * the likes
 * */

public enum PlayerStatus { PLAYING, OBSERVING, LOBBY, READY }

public class SyncListCharArch : SyncList<CharArch> { }

[Serializable]
public class PlayerData : NetworkBehaviour
{
    [SyncVar]
    public string pseudonym;
    [SyncVar]
    public PlayerStatus status;
    [SyncVar]
    public float playTime; // in minutes
    public SyncListCharArch charactersArchetypes;
    //public ModerationData
}

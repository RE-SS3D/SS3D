using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

[Serializable]
public class CharArch
{
    /* ChararacterArchetypeData
     * This class exists to stock the character's specific data, as jobs preferences and their physical appearence.
     * the Characters will be spawned using this data structure */

    [SyncVar]
    public CharId characterId;
    [SyncVar]
    public JobPreferences jobPreferences;
}

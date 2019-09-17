using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public enum Species { HUMAN/*, MOTH, MONKEY, ROCKMAN, etc...*/ }

[Serializable]
public class SpeciesDetails
{
    [SyncVar]
    public Species specie;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

[Serializable]
public class HairStyle
{

}

[Serializable]
public class FacialHairStyle
{

}

[Serializable]
public class HumanDetails : SpeciesDetails
{
    [SyncVar]
    public Color eyeColor;
    [SyncVar]
    public Color hairColor;
    [SyncVar]
    public float melaninLevels;
    [SyncVar]
    public float size;
    [SyncVar]
    HairStyle hairStyle;
    [SyncVar]
    FacialHairStyle facialHairStyle;

    //Shape keys

    [SyncVar]
    public float femininity;
    [SyncVar]
    public float breastsSize;
    [SyncVar]
    public float muscles;
    [SyncVar]
    public float fat;
}

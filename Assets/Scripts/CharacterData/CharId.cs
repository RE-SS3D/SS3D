using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public enum Gender { MALE, FEMALE, UNDEFINED }

/* CharId (Character Identity) is the class supposed to keep track of the character physical appearence */
[Serializable]
public class CharId
{
    [SyncVar]
    public string name;
    [SyncVar]
    public string surname;
    [SyncVar]
    public string nickname;
    [SyncVar]
    public Gender gender;
    [SyncVar]
    public int age;
    [SyncVar]
    public SpeciesDetails speciesDetails;
}

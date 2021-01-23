using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileBase : ScriptableObject
{
    // Should be unique
    public string id;

    // Refers to the general type of object this is, e.g. wall, table, etc.
    public string genericType;

    public GameObject prefab;
}

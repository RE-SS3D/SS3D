using SS3D.Engine.Tiles;
using SS3D.Engine.Tiles.State;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct FixtureState
{
    public Rotation rotation;
}


public class TileBase : ScriptableObject
{
    // Should be unique
    public string id;

    // Refers to the general type of object this is, e.g. wall, table, etc.
    public string genericType;
    public GameObject prefab;

    //[SerializeField]
    //protected Rotation rotation = Rotation.North;

    //public void SetRotation(Rotation rotation)
    //{
    //    this.rotation = rotation;
    //}

    //public Rotation GetRotation()
    //{
    //    return rotation;
    //}
}

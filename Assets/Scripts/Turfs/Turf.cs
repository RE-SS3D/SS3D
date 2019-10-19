using System;
using Mirror;
using UnityEngine;

// Turfs. Turfs relate to the combination of underplating (or open space), and the wall or floor above them.

public class Turf : ScriptableObject
{
    TileManager tileManager;

    public virtual void Initialize(TileManager parent)
    {
        tileManager = parent;
    }

    public virtual Mesh getMesh(Tile tile)
    {
        return new Mesh();
    }
}
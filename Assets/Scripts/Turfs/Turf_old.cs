using System;
using Mirror;
using UnityEngine;

// Turfs relate to the combination of underplating (or open space), and the wall or floor above them.

public class Turf_old : ScriptableObject
{
    public virtual Mesh getMesh(Tile tile)
    {
        return new Mesh();
    }
}
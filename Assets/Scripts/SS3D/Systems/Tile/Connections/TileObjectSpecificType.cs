using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Specific objects can further subdivide adjacency connections by building material used
    /// </summary>
    public enum TileObjectSpecificType
    {
        None = 0,
        Glass = 1,
        Poker = 2,
        Steel = 3,
        Wood = 4,
        Carpet = 5,
    }
}
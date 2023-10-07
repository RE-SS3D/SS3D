using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Generic objects are used by adjacencies to connect to each other
    /// </summary>
    public enum TileObjectGenericType
    {
        None,
        Pipe,
        Plant,
        Wall,
        Pew,
        Booth,
        Sofa,
        Table,
        Plenum,
        Floor,
        Counter,
        Cable,
        Wire,
        Door
    }

    /// <summary>
    /// Specific objects can further subdivide adjacency connections by building material used
    /// </summary>
    public enum TileObjectSpecificType
    {
        None,
        Glass,
        Poker,
        Steel,
        Wood,
        Carpet
    }
}
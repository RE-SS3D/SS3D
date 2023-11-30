using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IElectricDevice
{
    /// <summary>
    /// Global coordinates of the device on the tilemap. (0,0) is the same as (0,0) in the Unity editor.
    /// Coordinates can be negative.
    /// </summary>
    public PlacedTileObject TileObject { get; }
}

using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for saving tile locations.
/// </summary>
public interface ISavedTileLocation
{
    public List<SavedPlacedTileObject> GetPlacedObjects();

    public Vector2Int Location
    {
        get;
        set;
    }

    public TileLayer Layer
    {
        get;
        set;
    }
}

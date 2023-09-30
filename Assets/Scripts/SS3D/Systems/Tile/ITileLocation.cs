using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public interface ITileLocation
    {
        public TileLayer Layer
        {
            get;
        }

        /// <summary>
        /// Try to get a placed object in the given direction.
        /// </summary>
        public bool TryGetPlacedObject(out PlacedTileObject placedObject, Direction direction = Direction.North);

        /// <summary>
        /// Check if the tile is empty for a given direction.
        /// </summary>
        public bool IsEmpty(Direction direction = Direction.North);

        /// <summary>
        /// Check if the tile is fully empty (in all directions).
        /// </summary>
        public bool IsFullyEmpty();

        /// <summary>
        /// Remove a tile object from the tile in a given direction.
        /// </summary>
        public bool TryClearPlacedObject(Direction direction = Direction.North);

        /// <summary>
        /// Remove all tile objects from the tile in all given direction.
        /// </summary>
        public void ClearAllPlacedObject();

        /// <summary>
        /// Add a tile object in the given direction to this location.
        /// </summary>
        public void AddPlacedObject(PlacedTileObject tileObject, Direction direction = Direction.North);

        /// <summary>
        /// Save the tile location, put all relevant information in a format that can be easily serialized.
        /// </summary>
        public ISavedTileLocation Save();
    }
}

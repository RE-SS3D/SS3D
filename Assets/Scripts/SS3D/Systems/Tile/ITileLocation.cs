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

        public bool TryGetPlacedObject(out PlacedTileObject placedObject, Direction direction = Direction.North);

        public bool IsEmpty(Direction direction = Direction.North);

        public bool IsFullyEmpty();

        public bool TryClearPlacedObject(Direction direction = Direction.North);

        public void ClearAllPlacedObject();

        public void AddPlacedObject(PlacedTileObject tileObject, Direction direction = Direction.North);

        public ISavedTileLocation Save();
    }
}

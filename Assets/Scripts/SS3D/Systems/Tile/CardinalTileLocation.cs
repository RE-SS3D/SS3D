using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Represent a Tile location able to contain up to 4 tile objects, in each cardinal directions.
    /// </summary>
    public class CardinalTileLocation : ITileLocation
    {
        private TileLayer _layer;
        private int _x;
        private int _y;

        /// <summary>
        /// the four potential placed tile objects. 0 is for north, 1 is for east, 2 for south, 3 for west.
        /// </summary>
        private PlacedTileObject[] _cardinalPlacedTileObject = new PlacedTileObject[4];

        /// <summary>
        /// The layer this location is on.
        /// </summary>
        public TileLayer Layer => _layer;

        public CardinalTileLocation(TileLayer layer, int x, int y)
        {
            _layer = layer;
            _x = x;
            _y = y;
        }

        public void ClearAllPlacedObject()
        {
            for (int i = 0; i < 4; i++)
            {
                TryClearPlacedObject(IndexToDir(i));
            }
        }

        public bool IsEmpty(Direction direction = Direction.North)
        {
            if (!TileHelper.IsCardinalDirection(direction))
            {
                return false;
            }
            return _cardinalPlacedTileObject[DirToIndex(direction)] == null;
        }

        public bool IsFullyEmpty()
        {
            return _cardinalPlacedTileObject.Where(x => x != null).Count() == 0;
        }

        public ISavedTileLocation Save()
        {
            List<SavedPlacedTileObject> savedTileObjects= new List<SavedPlacedTileObject>();
            foreach(PlacedTileObject tileObject in _cardinalPlacedTileObject.Where(x => x != null))
            {
                savedTileObjects.Add(tileObject.Save());
            }

            return new SavedTileCardinalLocation(savedTileObjects, new Vector2Int(_x, _y), _layer);
        }

        public bool TryClearPlacedObject(Direction direction = Direction.North)
        {
            if (!TileHelper.IsCardinalDirection(direction))
            {
                return false;
            }

            PlacedTileObject placedObject = _cardinalPlacedTileObject[DirToIndex(direction)];
            if (placedObject != null)
            {
                placedObject.DestroySelf();
                placedObject = null;
                return true;
            }
            return false;
        }

        public bool TryGetPlacedObject(out PlacedTileObject placedObject, Direction direction = Direction.North)
        {
            if (!TileHelper.IsCardinalDirection(direction))
            {
                placedObject = null;
                return false;
            }

            PlacedTileObject currentPlacedObject = _cardinalPlacedTileObject[DirToIndex(direction)];
            if (currentPlacedObject != null)
            {
                placedObject = currentPlacedObject;
                return true;
            }
            placedObject = null;
            return false;
        }

        public void AddPlacedObject(PlacedTileObject tileObject, Direction direction = Direction.North)
        {
            if (!TileHelper.CardinalDirections().Contains(direction))
            {
                return;
            }
            else
            {
                _cardinalPlacedTileObject[DirToIndex(direction)] = tileObject;
            }
        }

        /// <summary>
        /// Tie an index array to each cardinal direction.
        /// </summary>
        private int DirToIndex(Direction direction)
        {
            return (int)direction / 2;
        }

        /// <summary>
        /// Tie an index array to each cardinal direction.
        /// </summary>
        private Direction IndexToDir(int i)
        {
            return (Direction) (i*2);
        }
    }
}

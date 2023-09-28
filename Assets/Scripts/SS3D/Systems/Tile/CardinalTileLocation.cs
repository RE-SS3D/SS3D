using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    public class CardinalTileLocation : ITileLocation
    {
        private TileLayer _layer;
        private int _x;
        private int _y;

        private PlacedTileObject[] _cardinalPlacedTileObject = new PlacedTileObject[4];

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
                TryClearPlacedObject((Direction)(2*i));
            }
        }

        public bool IsEmpty(Direction direction = Direction.North)
        {
            if (!TileHelper.IsCardinalDirection(direction))
            {
                return false;
            }
            return _cardinalPlacedTileObject[(int)direction / 2] == null;
        }

        public bool IsFullyEmpty()
        {
            return _cardinalPlacedTileObject.Where(x => x != null).Any();
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

            PlacedTileObject placedObject = _cardinalPlacedTileObject[(int) direction / 2];
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

            PlacedTileObject currentPlacedObject = _cardinalPlacedTileObject[(int) direction / 2];
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
                _cardinalPlacedTileObject[(int)direction / 2] = tileObject;
            }
        }
    }
}

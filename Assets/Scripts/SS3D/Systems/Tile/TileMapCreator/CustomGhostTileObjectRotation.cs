using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// This script help you define what rotations are allowed on tile objects when using the construction menu.
    /// Just define in order the direction you want, and it will pick them up one by one.
    /// e.g. you can define all directions counter clock wise if you want, and the rotation will be the other way around.
    /// </summary>
    public class CustomGhostTileObjectRotation : MonoBehaviour, ICustomGhostRotation
    {
        [SerializeField]
        private Direction _defaultDirection = Direction.North;

        public Direction DefaultDirection => _defaultDirection;

        [FormerlySerializedAs("AllowedDirections")]
        [SerializeField]
        private List<Direction> _allowedDirections;

        public void Awake()
        {
            // Insure us there'at least one allowed direction in the list.
            if (!_allowedDirections.Contains(_defaultDirection))
            {
                _allowedDirections.Add(_defaultDirection);
            }
        }

        public Direction GetNextDirection(Direction dir)
        {
            int index = _allowedDirections.IndexOf(dir);
            if(index == -1) 
            {
                Debug.LogError("Direction not part of any allowed directions for this tile objects," +
                    "returning the first allowed direction.");
                return _allowedDirections[0];
            }
            if(index == _allowedDirections.Count - 1)
            {
                return _allowedDirections[0];
            }
            else
            {
                return _allowedDirections[index + 1];
            }
        }
        
        public Direction[] GetAllowedRotations() => _allowedDirections.ToArray();
    }
}

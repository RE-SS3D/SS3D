using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Logging;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.UI;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

        [SerializeField]
        private List<Direction> AllowedDirections;

        public void Awake()
        {
            // Insure us there'at least one allowed direction in the list.
            if (!AllowedDirections.Contains(_defaultDirection))
            {
                AllowedDirections.Add(_defaultDirection);
            }
        }


        public Direction GetNextDirection(Direction dir)
        {
            int index = AllowedDirections.IndexOf(dir);
            if(index == -1) 
            {
                Debug.LogError("Direction not part of any allowed directions for this tile objects," +
                    "returning the first allowed direction.");
                return AllowedDirections[0];
            }
            if(index == AllowedDirections.Count - 1)
            {
                return AllowedDirections[0];
            }
            else
            {
                return AllowedDirections[index + 1];
            }
        }
    }
}

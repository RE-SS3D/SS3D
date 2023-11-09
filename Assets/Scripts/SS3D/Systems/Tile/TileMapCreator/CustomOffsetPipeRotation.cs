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
    public class CustomOffsetPipeRotation : MonoBehaviour, ICustomGhostRotation
    {
        [SerializeField]
        private Direction _defaultDirection = Direction.North;

        public Direction DefaultDirection => _defaultDirection;

        public Direction GetNextDirection(Direction dir)
        {
            switch (dir)
            {
                case Direction.North:
                    return Direction.East;
                case Direction.East:
                    return Direction.North;
                default:
                    Debug.LogError("Blue pipes should be only oriented North or East, returning North");
                    return Direction.North;
            }
        }
    }
}

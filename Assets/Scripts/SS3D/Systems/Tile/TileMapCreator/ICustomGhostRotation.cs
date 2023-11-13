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
    /// This interface is to be implemented for game objects which need to have special rotations, outside of
    /// the classic North, South,East,West, while using the construction's ghost.
    /// E.g. the blue offset pipe should only ever be rotated north and east, the two other don't make much sense
    /// visually.
    /// </summary>
    public interface ICustomGhostRotation
    {
        public Direction DefaultDirection { get; }

        public Direction GetNextDirection(Direction dir);

    }
}

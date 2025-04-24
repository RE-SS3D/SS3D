using DG.Tweening;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Walls are mostly simply working like advanced connectors, but there is a few exceptions,
    /// in particular with the way they connect with doors, hence why they need their own connector
    /// script.
    /// </summary>
    public class WallAdjacencyConnector : AbstractHorizontalConnector, IAdjacencyConnector
    {
        /// <summary>
        /// Script to help walls to choose their shape.
        /// </summary>
        [SerializeField] private AdvancedConnector _advancedAdjacency;

        protected override IMeshAndDirectionResolver AdjacencyResolver => _advancedAdjacency;

        /// <summary>
        /// Check if this wall is conencted to the neighbour object when it's a door.
        /// Walls only connect to doors when they are on left or on right of doors.
        /// </summary>
        private bool IsConnectedToDoor(PlacedTileObject neighbourObject)
        {
            var doorConnector = neighbourObject.GetComponent<DoorAdjacencyConnector>();
            var door = doorConnector.PlacedObject;
            if (door != null)
            {
                if (_placedObject.IsOnLeft(door) || _placedObject.IsOnRight(door))
                {
                    return true;
                }
                    
            }
            return false;
        }

        /// <summary>
        /// Try to get a door on the left or on the right of this wall.
        /// </summary>
        private bool TryGetOnLeftOrRightDoor(out PlacedTileObject door)
        {
            var tileSystem = SubSystems.Get<TileSubSystem>();
            var map = tileSystem.CurrentMap;
            var neighbours = map.GetNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position);

            foreach( var neighbour in neighbours )
            {
                var doorConnector = neighbour?.GetComponent<DoorAdjacencyConnector>();
                if (doorConnector == null) continue;

                if (_placedObject.IsOnLeft(neighbour) || _placedObject.IsOnRight(neighbour))
                {
                    door = neighbour;
                    return true;
                }
                    
            }
            door = null;
            return false;
        }

        /// <summary>
        /// Walls connect to other walls, and to doors, when doors are placed on their left or on their
        /// right.
        /// </summary>
        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            if(neighbourObject == null) return false;

            bool isConnected = (neighbourObject.HasAdjacencyConnector);

            isConnected &= neighbourObject.GenericType == TileObjectGenericType.Wall ||
                neighbourObject.GenericType == TileObjectGenericType.Door;

            if (neighbourObject.GetComponent<DoorAdjacencyConnector>() != null)
            {
                isConnected &= IsConnectedToDoor(neighbourObject);
            }

            var tileSystem = SubSystems.Get<TileSubSystem>();
            var map = tileSystem.CurrentMap;

            // Needed for a weird edge case when you put walls all around a door. Will avoid connecting
            // to a wall in front or behind the door, if this wall is itself connected to door.
            // This is to avoid the wall considering there's 3 connections, because if it does, it takes
            // a L2 shape which allow to see through the wall.

            if(TryGetOnLeftOrRightDoor(out PlacedTileObject door) 
                && neighbourObject.GetComponent<WallAdjacencyConnector>() != null)
            {
                if (neighbourObject.IsInFront(door) || neighbourObject.IsBehind(door))
                {
                    isConnected &= false;
                }
            }

            return isConnected;
        }
    }
}

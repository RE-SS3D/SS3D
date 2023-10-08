using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using math = SS3D.Utils.MathUtility;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Component that is added to every tile object that is part of the tilemap. Tiles are more restrictive and need to have an origin, fixed grid position and direction to face.
    /// </summary>
    public class PlacedTileObject: NetworkBehaviour
    {
        /// <summary>
        /// Creates a new PlacedTileObject from a TileObjectSO at a given position and direction. Uses NetworkServer.Spawn() if a server is running.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="dir"></param>
        /// <param name="tileObjectSo"></param>
        /// <returns></returns>
        public static PlacedTileObject Create(Vector3 worldPosition, Vector2Int origin, Direction dir, TileObjectSo tileObjectSo)
        {
            GameObject placedGameObject = Instantiate(tileObjectSo.prefab);
            placedGameObject.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));

            PlacedTileObject placedObject = placedGameObject.GetComponent<PlacedTileObject>();
            if (placedObject == null)
            {
                // Ideally an editor script adds this instead of doing it at runtime
                placedObject = placedGameObject.AddComponent<PlacedTileObject>();
            }

            placedObject.Setup(tileObjectSo, origin, dir);

            if (InstanceFinder.ServerManager != null)
            {
                if (placedObject.GetComponent<NetworkObject>() == null)
                    Log.Information(Subsystems.Get<TileSystem>(), "{placedObject} does not have a Network Component and will not be spawned",
                        Logs.Generic, placedObject.NameString);
                else
                    InstanceFinder.ServerManager.Spawn(placedGameObject);
            }

            return placedObject;
        }

        /// <summary>
        /// Returns a new SaveObject for use in saving/loading.
        /// </summary>
        /// <returns></returns>
        

        private TileObjectSo _tileObjectSo;
        private Vector2Int _origin;
        private Direction _dir;
        private IAdjacencyConnector _connector;

        /// <summary>
        /// Returns a list of all grids positions that object occupies.
        /// </summary>
        /// <returns></returns>
        public List<Vector2Int> GridOffsetList => _tileObjectSo.GetGridOffsetList(_dir);

        public Vector2Int Origin => _origin;

        public TileObjectGenericType GenericType => _tileObjectSo.genericType;

        public TileObjectSpecificType SpecificType => _tileObjectSo.specificType;

        public TileLayer Layer => _tileObjectSo.layer;

        public Direction Direction => _dir;

        public string NameString => _tileObjectSo.nameString;

        public bool HasAdjacencyConnector => _connector != null;

        /// <summary>
        /// Set up a new PlacedTileObject.
        /// </summary>
        /// <param name="tileObjectSo"></param>
        /// <param name="dir"></param>
        private void Setup(TileObjectSo tileObjectSo, Vector2Int origin, Direction dir)
        {
            _tileObjectSo = tileObjectSo;
            _origin = origin;
            _dir = dir;
            _connector = GetComponent<IAdjacencyConnector>();
        }

        /// <summary>
        /// Destroys itself.
        /// </summary>
        public void DestroySelf()
        {
            InstanceFinder.ServerManager.Despawn(gameObject);
        }

        public void UpdateAdjacencies(PlacedTileObject[] neighbourObjects)
        {
            if (HasAdjacencyConnector)
                _connector.UpdateAllConnections(neighbourObjects);
        }

        public void UpdateSingleAdjacency(PlacedTileObject neighbourObject, Direction dir)
        {
            if (HasAdjacencyConnector)
                _connector.UpdateSingleConnection(dir, neighbourObject, false);
        }

        public SavedPlacedTileObject Save()
        {
            return new SavedPlacedTileObject
            {
                tileObjectSOName = _tileObjectSo.nameString,
                origin = _origin,
                dir = _dir,
            };
        }

        /// <summary>
        /// Is this in front of the other object ?
        /// </summary>
        public bool IsInFront(PlacedTileObject other)
        {
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(other.Direction);
            Vector2Int OtherMoved = new Vector2Int(math.mod(other.Origin.x + diff.x, TileConstants.ChunkSize),
                math.mod(other.Origin.y + diff.y, TileConstants.ChunkSize));
            if (Origin == OtherMoved)
                return true;

            return false;
        }

        /// <summary>
        /// Is this behind the other object ?
        /// </summary>
        public bool IsBehind(PlacedTileObject other)
        {
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(other.Direction);
            Vector2Int OtherMoved = new Vector2Int(math.mod(other.Origin.x - diff.x, TileConstants.ChunkSize),
                math.mod(other.Origin.y - diff.y, TileConstants.ChunkSize));
            if (Origin == OtherMoved)
                return true;

            return false;
        }

        /// <summary>
        /// Is this on the right of the other object ?
        /// </summary>
        public bool IsOnRight(PlacedTileObject other)
        {
            Direction dirOnRight = TileHelper.GetNextCardinalDir(other.Direction);
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(dirOnRight);
            Vector2Int OtherMoved = new Vector2Int(math.mod(other.Origin.x + diff.x, TileConstants.ChunkSize),
                math.mod(other.Origin.y + diff.y, TileConstants.ChunkSize));
            if (Origin == OtherMoved)
                return true;

            return false;
        }

        /// <summary>
        /// Is this on the left of the other object ?
        /// </summary>
        public bool IsOnLeft(PlacedTileObject other)
        {
            Direction dirOnLeft = TileHelper.GetNextCardinalDir(other.Direction);
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(dirOnLeft);
            Vector2Int OtherMoved = new Vector2Int(math.mod(other.Origin.x - diff.x, TileConstants.ChunkSize),
                math.mod(other.Origin.y - diff.y, TileConstants.ChunkSize));
            if (Origin == OtherMoved)
                return true;

            return false;
        }
    }
}
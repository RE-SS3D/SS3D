using FishNet;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using math = SS3D.Utils.MathUtility;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Component that is added to every tile object that is part of the tilemap. Tiles are more restrictive and need to have an origin, fixed grid position and direction to face.
    /// </summary>
    public class PlacedTileObject : NetworkBehaviour, IWorldObjectAsset
    {
        [SerializeField]
#if UNITY_EDITOR
        [ReadOnly]
        [Header("This field is filled automatically by the AssetData system.")]
#endif
        private WorldObjectAssetReference _asset;

        [SerializeField]
        private TileObjectSo _tileObjectSo;
        private Vector2Int _origin;
        private Direction _dir;

        private IAdjacencyConnector _connector;
        private Vector2Int _worldOrigin;

        /// <summary>
        /// Returns a list of all grids positions that object occupies.
        /// </summary>
        /// <returns></returns>
        public List<Vector2Int> GridOffsetList => _tileObjectSo.GetGridOffsetList(_dir);

        public Vector2Int Origin => _origin;

        public Vector2Int WorldOrigin => _worldOrigin;

        public TileObjectGenericType GenericType => _tileObjectSo.GenericType;

        public TileObjectSpecificType SpecificType => _tileObjectSo.SpecificType;

        public TileLayer Layer => _tileObjectSo.Layer;

        public Direction Direction => _dir;

        public string NameString => _tileObjectSo.NameString;

        public TileObjectSo TileObjectSo => _tileObjectSo;

        public bool HasAdjacencyConnector => _connector != null;

        public WorldObjectAssetReference Asset
        {
            get => _asset;
            set
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Serilog.Log.Warning($"Field {nameof(Asset)} is being modified in runtime. This should not happen in normal conditions.");
                }

                _asset = value;
            }
        }

        public IAdjacencyConnector Connector => _connector;

        /// <summary>
        /// Creates a new PlacedTileObject from a TileObjectSO at a given position and direction.
        /// Uses NetworkServer.Spawn() if a server is running.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="dir"></param>
        /// <param name="tileObjectSo"></param>
        /// <returns></returns>
        public static PlacedTileObject Create(Vector3 worldPosition, Vector2Int origin, Direction dir, TileObjectSo tileObjectSo)
        {
            GameObject placedGameObject = Instantiate(tileObjectSo.Prefab);
            placedGameObject.transform.SetPositionAndRotation(worldPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));

            if (!placedGameObject.TryGetComponent(out PlacedTileObject placedObject))
            {
                // Ideally an editor script adds this instead of doing it at runtime
                placedObject = placedGameObject.AddComponent<PlacedTileObject>();
            }

            placedObject.Setup(tileObjectSo, origin, worldPosition, dir);

            // TODO : Spawning the placed game object does not spawn with it everything. In particular, the values
            // such as tileobjectSO, origin or world position are not spawned. This might (or not) be an issue later on.
            if (InstanceFinder.ServerManager == null)
            {
                return placedObject;
            }

            if (placedObject.GetComponent<NetworkObject>() == null)
            {
                Log.Information(
                    Subsystems.Get<TileSubSystem>(),
                    "{placedObject} does not have a Network Component and will not be spawned",
                    Logs.Generic,
                    placedObject.NameString);
            }
            else
            {
                InstanceFinder.ServerManager.Spawn(placedGameObject);
            }

            return placedObject;
        }

        /// <summary>
        /// Destroys itself.
        /// </summary>
        [Server]
        public void DestroySelf()
        {
            InstanceFinder.ServerManager.Despawn(gameObject);
        }

        public void UpdateAdjacencies()
        {
            if (HasAdjacencyConnector)
            {
                _connector.UpdateAllConnections();
            }
        }

        public void UpdateSingleAdjacency(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            if (HasAdjacencyConnector)
            {
                _connector.UpdateSingleConnection(dir, neighbourObject, updateNeighbour);
            }
        }

        public SavedPlacedTileObject Save()
        {
            return new SavedPlacedTileObject
            {
                TileObjectSoName = _tileObjectSo.NameString,
                Origin = _origin,
                Dir = _dir,
            };
        }

        public void SetDirection(Direction dir)
        {
            _dir = dir;
        }

        /// <summary>
        /// Is this in front of the other object ?
        /// </summary>
        public bool IsInFront(PlacedTileObject other)
        {
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(other.Direction);
            Vector2Int otherMoved = new Vector2Int(
                math.mod(other.Origin.x + diff.x, TileConstants.ChunkSize),
                math.mod(other.Origin.y + diff.y, TileConstants.ChunkSize));
            return Origin == otherMoved;
        }

        /// <summary>
        /// Is this behind the other object ?
        /// </summary>
        public bool IsBehind(PlacedTileObject other)
        {
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(other.Direction);
            Vector2Int otherMoved = new Vector2Int(
                math.mod(other.Origin.x - diff.x, TileConstants.ChunkSize),
                math.mod(other.Origin.y - diff.y, TileConstants.ChunkSize));
            return Origin == otherMoved;
        }

        /// <summary>
        /// Is this on the right of the other object ?
        /// </summary>
        public bool IsOnRight(PlacedTileObject other)
        {
            Direction dirOnRight = TileHelper.GetNextCardinalDir(other.Direction);
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(dirOnRight);
            Vector2Int otherMoved = new Vector2Int(
                math.mod(other.Origin.x + diff.x, TileConstants.ChunkSize),
                math.mod(other.Origin.y + diff.y, TileConstants.ChunkSize));
            return Origin == otherMoved;
        }

        /// <summary>
        /// Is this on the left of the other object ?
        /// </summary>
        public bool IsOnLeft(PlacedTileObject other)
        {
            Direction dirOnLeft = TileHelper.GetNextCardinalDir(other.Direction);
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(dirOnLeft);
            Vector2Int otherMoved = new Vector2Int(
                math.mod(other.Origin.x - diff.x, TileConstants.ChunkSize),
                math.mod(other.Origin.y - diff.y, TileConstants.ChunkSize));
            return Origin == otherMoved;
        }

        /// <summary>
        /// TODO don't use chunk if possible, or chunk coordinate as well.
        /// Is this at the direction of the other object ? (has to be adjacent).
        /// </summary>
        public bool AtDirectionOf(PlacedTileObject other, Direction dir)
        {
            return dir switch
            {
                Direction.North => math.mod(other.Origin.y - 1, TileConstants.ChunkSize) == Origin.y,
                Direction.South => math.mod(other.Origin.y + 1, TileConstants.ChunkSize) == Origin.y,
                Direction.East => math.mod(other.Origin.x - 1, TileConstants.ChunkSize) == Origin.x,
                Direction.West => math.mod(other.Origin.x + 1, TileConstants.ChunkSize) == Origin.x,
                _ => false,
            };
        }

        public bool HasNeighbourFrontBack(List<PlacedTileObject> neighbours, [CanBeNull] out PlacedTileObject inFrontOrBack, bool front)
        {
            foreach (PlacedTileObject neighbour in neighbours)
            {
                switch (front)
                {
                    case true when neighbour.IsInFront(this):
                    {
                        inFrontOrBack = neighbour;
                        return true;
                    }

                    case false when neighbour.IsBehind(this):
                    {
                        inFrontOrBack = neighbour;
                        return true;
                    }
                }
            }

            inFrontOrBack = null;
            return false;
        }

        public bool HasNeighbourOnSide(List<PlacedTileObject> neighbours, out PlacedTileObject onSide, bool left)
        {
            foreach (PlacedTileObject neighbour in neighbours)
            {
                switch (left)
                {
                    case false when neighbour.IsOnRight(this):
                    {
                        onSide = neighbour;
                        return true;
                    }

                    case true when neighbour.IsOnLeft(this):
                    {
                        onSide = neighbour;
                        return true;
                    }
                }
            }

            onSide = null;
            return false;
        }

        public bool HasNeighbourAtDirection(List<PlacedTileObject> neighbours, out PlacedTileObject atDirection, Direction dir)
        {
            foreach (PlacedTileObject neighbour in neighbours.Where(neighbour => AtDirectionOf(neighbour, dir)))
            {
                atDirection = neighbour;
                return true;
            }

            atDirection = null;
            return false;
        }

        /// Other is a neighbour, placed at some direction from this.
        /// <param name="other">another placedTileObject, which should be neighbouring this.</param>
        /// <param name="direction"> the found direction, north by default</param>
        /// <returns>true if other is a neighbour of this in term of coordinates</returns>
        public bool NeighbourAtDirectionOf(PlacedTileObject other, out Direction direction)
        {
            direction = Direction.North;

            if (other == null)
            {
                return false;
            }

            Vector2Int coordinateDifference = other.WorldOrigin - WorldOrigin;

            if (coordinateDifference == Vector2Int.up)
            {
                direction = Direction.North;
            }
            else if (coordinateDifference == Vector2Int.down)
            {
                direction = Direction.South;
            }
            else if (coordinateDifference == Vector2Int.left)
            {
                direction = Direction.West;
            }
            else if (coordinateDifference == Vector2Int.right)
            {
                direction = Direction.East;
            }
            else if (coordinateDifference == Vector2Int.up + Vector2Int.right)
            {
                direction = Direction.NorthEast;
            }
            else if (coordinateDifference == Vector2Int.up + Vector2Int.left)
            {
                direction = Direction.NorthWest;
            }
            else if (coordinateDifference == Vector2Int.down + Vector2Int.left)
            {
                direction = Direction.SouthWest;
            }
            else if (coordinateDifference == Vector2Int.down + Vector2Int.right)
            {
                direction = Direction.SouthEast;
            }
            else
            {
                return false;
            }

            return true;
        }

        public bool IsCardinalNeighbour(PlacedTileObject other)
        {
            bool isCardinalNeighbour = false;

            if (other == null)
            {
                return false;
            }

            Vector2Int coordinateDifference = other.WorldOrigin - WorldOrigin;

            isCardinalNeighbour |= coordinateDifference == Vector2Int.up;
            isCardinalNeighbour |= coordinateDifference == Vector2Int.down;
            isCardinalNeighbour |= coordinateDifference == Vector2Int.left;
            isCardinalNeighbour |= coordinateDifference == Vector2Int.right;

            return isCardinalNeighbour;
        }

        /// <summary>
        /// Set up a new PlacedTileObject.
        /// </summary>
        /// <param name="tileObjectSo"></param>
        /// <param name="dir"></param>
        private void Setup(TileObjectSo tileObjectSo, Vector2Int origin, Vector3 worldPosition, Direction dir)
        {
            _tileObjectSo = tileObjectSo;
            _origin = origin;
            _dir = dir;
            _connector = GetComponent<IAdjacencyConnector>();
            _worldOrigin = new Vector2Int((int)Math.Round(worldPosition.x), (int)Math.Round(worldPosition.z));
        }
    }
}

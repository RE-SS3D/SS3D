using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using SS3D.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using static UnityEngine.ParticleSystem;

namespace SS3D.Systems.Tile.Connections
{

    /// <summary>
    /// ISSUE : I connectors not connecting when one side isn't straight.
    /// ISSUE : can't alternate between LIN LOUT diagonnally.
    /// </summary>
    public class DirectionnalAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {

        protected TileObjectGenericType _genericType;


        protected TileObjectSpecificType _specificType;

        
        public DirectionnalShapeResolver AdjacencyResolver;

        protected AdjacencyMap _adjacencyMap;

        protected MeshFilter _filter;

        protected PlacedTileObject _placedObject;

        public PlacedTileObject PlacedObject => _placedObject;

        public Direction PlacedObjectDirection => _placedObject.Direction;

        private bool _initialized;

        public override void OnStartClient()
        {
            base.OnStartClient();
            Setup();
        }

        /// <summary>
        /// Simply set things up, including creating new references, and fetching generic and specific type
        /// from the associated scriptable object.
        /// </summary>
        private void Setup()
        {
            if (!_initialized)
            {
                _adjacencyMap = new AdjacencyMap();
                _filter = GetComponent<MeshFilter>();

                _placedObject = GetComponent<PlacedTileObject>();
                if (_placedObject == null)
                {
                    _genericType = TileObjectGenericType.None;
                    _specificType = TileObjectSpecificType.None;
                }
                else
                {
                    _genericType = _placedObject.GenericType;
                    _specificType = _placedObject.SpecificType;
                }
                _initialized = true;
            }
        }

        /// <summary>
        /// implement
        /// </summary>
        public bool IsConnected(Direction dir, PlacedTileObject neighbourObject)
        {
            return true;
        }

        // ignore param
        public void UpdateAllConnections(PlacedTileObject[] neighbourObjects)
        {
            UpdateAllAndNeighbours(true);
        }

        

        // ignore param
        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            UpdateAllAndNeighbours(true);
            return true;
        }

        private void UpdateAllAndNeighbours(bool updateNeighbours)
        {
            Setup();
            var neighbours = GetNeighbourDirectionnal();

            Tuple<Mesh, float, Direction> results;

            if (HasSingleConnection(neighbours, out PlacedTileObject neighbour))
            {
                results = UpdateMeshDirectionSingleConnection(neighbours);
            }

            else if (HasDoubleConnection(neighbours, out PlacedTileObject first, out PlacedTileObject second))
            {
                results = UpdateMeshDirectionDoubleConnection(neighbours);
            }

            // no connections then
            else
            {
                results = new Tuple<Mesh, float, Direction>(AdjacencyResolver.o,
                    TileHelper.AngleBetween(Direction.North, _placedObject.Direction), _placedObject.Direction);
            }

            _placedObject.SetDirection(results.Item3);
            _filter.mesh = results.Item1;

            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, results.Item2, eulerRotation.z);
            transform.localRotation = localRotation;

            if (updateNeighbours)
            {
                foreach(var adjacent in neighbours)
                {
                    adjacent.GetComponent<DirectionnalAdjacencyConnector>().UpdateAllAndNeighbours(false);
                }
            }
        }

        private bool HasSingleConnection(List<PlacedTileObject> neighbours ,out PlacedTileObject neighbour)
        {
            neighbour = null;

            // has double connection, therefore not a single one.
            if(HasDoubleConnection(neighbours, out var first, out var second)) return false;

            return IsULeftBisConfiguration(neighbours, out var single)
                || IsURightBisConfiguration(neighbours, out single)
                || IsULeftConfiguration(neighbours, out single)
                || IsURightConfiguration(neighbours, out single);
        }

        private bool HasDoubleConnection(List<PlacedTileObject> neighbours, out PlacedTileObject first, out PlacedTileObject second)
        {
            return IsFirstLInConfiguration(neighbours, out first, out second)
                || IsSecondLInConfiguration(neighbours, out first, out second)
                || IsFirstLOutConfiguration(neighbours, out first, out second)
                || IsSecondLOutConfiguration(neighbours, out first, out second)
                || IsIConfiguration(neighbours, out first, out second);
        }



        private List<PlacedTileObject> GetNeighbourDirectionnal()
        {
            var tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;

            var neighbours = map.GetCardinalNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position);
            return neighbours.Where(x => x != null &&
                x.TryGetComponent<DirectionnalAdjacencyConnector>(out var component)).ToList();
        }

        private Tuple<Mesh,float,Direction> UpdateMeshDirectionSingleConnection(List<PlacedTileObject> neighbours)
        {
            float rotation = TileHelper.AngleBetween(Direction.North, _placedObject.Direction);

            if (IsULeftConfiguration(neighbours, out var single) || IsULeftBisConfiguration(neighbours, out single))
            {
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.uLeft, rotation, _placedObject.Direction);
            }

            if (IsURightConfiguration(neighbours, out single) || IsURightBisConfiguration(neighbours, out single))
            {
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.uRight, rotation, _placedObject.Direction);
            }

            Debug.LogError("should not reach this point");
            return new Tuple<Mesh, float, Direction>(AdjacencyResolver.o, 0, Direction.North);
        }

        /// <summary>
        /// In case a neighbour is in front or behind of this placed object, it should be the first neighbour parameter.
        /// no object should be null.
        /// </summary>
        private Tuple<Mesh, float, Direction> UpdateMeshDirectionDoubleConnection(List<PlacedTileObject> neighbours)
        {
            float rotation;
            Direction direction;

            if (IsFirstLInConfiguration(neighbours,out var first, out var second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, second.Direction);
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.lIn, rotation, direction);
            }

            if (IsSecondLInConfiguration(neighbours, out first, out second))
            {
                // if the second neighbour is on left (the one facing the same direction), then the other 
                // other side of the Lin couch should be linked to it, so we need to do a -90 degree rotation compared to neighbour on right.
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetPreviousCardinalDir(second.Direction));
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.lIn, rotation, direction);
            }

            if (IsFirstLOutConfiguration(neighbours, out first, out second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetNextCardinalDir(second.Direction));
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.lOut, rotation, direction);
            }

            if (IsSecondLOutConfiguration(neighbours, out first, out second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetOpposite(second.Direction));
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.lOut, rotation, direction);
            }

            if(IsIConfiguration(neighbours, out first, out second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, _placedObject.Direction);
                direction = first.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.i, rotation, direction);
            }

            Debug.LogError("should not reach this point");

            return new Tuple<Mesh, float, Direction>(AdjacencyResolver.o, 0f, Direction.North);
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// FirstLIn is when a neighbour is in front, looking toward east, and another neighbour on the right,
        /// looking toward north as well.
        /// </summary>
        private bool IsFirstLInConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasFront = HasNeighbourInFront(neighbours, out firstNeighbour);
            bool hasRight = HasNeighbourOnRight(neighbours, out secondNeighbour);

            if (!(hasFront && hasRight)) return false;

            bool firstCondition = firstNeighbour.Direction == TileHelper.GetNextCardinalDir(_placedObject.Direction);
            bool secondCondition = secondNeighbour.Direction == _placedObject.Direction;
            return firstCondition && secondCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// SecondLIn is when a neighbour is in front, looking toward west, and another neighbour on the left,
        /// looking toward north as well.
        /// </summary>
        private bool IsSecondLInConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasFront = HasNeighbourInFront(neighbours, out firstNeighbour);
            bool hasLeft = HasNeighbourOnLeft(neighbours, out secondNeighbour);

            if (!(hasFront && hasLeft)) return false;

            bool firstCondition = firstNeighbour.Direction == TileHelper.GetPreviousCardinalDir(_placedObject.Direction);
            bool secondCondition = secondNeighbour.Direction == _placedObject.Direction;

            return firstCondition && secondCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// FirstLOut is when a neighbour is behind, looking toward west, and another neighbour on the right,
        /// looking toward north as well.
        /// </summary>
        private bool IsFirstLOutConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasBehind = HasNeighbourBehind(neighbours, out firstNeighbour);
            bool hasRight = HasNeighbourOnLeft(neighbours, out secondNeighbour);

            if (!(hasBehind && hasRight)) return false;

            bool firstCondition = firstNeighbour.Direction == TileHelper.GetPreviousCardinalDir(_placedObject.Direction);
            bool secondCondition = secondNeighbour.Direction == _placedObject.Direction;

            return firstCondition && secondCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// SecondLOut is when a neighbour is behind, looking toward east, and another neighbour on the left,
        /// looking toward north as well.
        /// </summary>
        private bool IsSecondLOutConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasBehind = HasNeighbourBehind(neighbours, out firstNeighbour);
            bool hasLeft = HasNeighbourOnLeft(neighbours, out secondNeighbour);

            if (!(hasBehind && hasLeft)) return false;

            bool firstCondition = firstNeighbour.Direction == TileHelper.GetNextCardinalDir(_placedObject.Direction);
            bool secondCondition = secondNeighbour.Direction == _placedObject.Direction;

            return firstCondition && secondCondition;
        }

        /// <summary>
        /// 
        /// </summary>
        private bool IsIConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasLeft = HasNeighbourOnLeft(neighbours, out firstNeighbour);
            bool hasRight = HasNeighbourOnRight(neighbours, out secondNeighbour);

            if (!(hasLeft && hasRight)) return false;

            var firstConnector = firstNeighbour.GetComponent<DirectionnalAdjacencyConnector>();
            var secondConnector = secondNeighbour.GetComponent<DirectionnalAdjacencyConnector>();

            var neighboursOfFirst = firstConnector.GetNeighbourDirectionnal().Where(x => x != _placedObject).ToList();
            var neighboursOfSecond = secondConnector.GetNeighbourDirectionnal().Where(x => x != _placedObject).ToList();

            bool firstGoodDirection = _placedObject.Direction == firstNeighbour.Direction 
                || (firstNeighbour.Direction != TileHelper.GetOpposite(_placedObject.Direction) 
                    && firstConnector.HasSingleConnection(neighboursOfFirst, out var n1));

            bool secondGoodDirection = _placedObject.Direction == firstNeighbour.Direction
                || (firstNeighbour.Direction != TileHelper.GetOpposite(_placedObject.Direction)
                    && firstConnector.HasSingleConnection(neighboursOfFirst, out var n2));

            return firstGoodDirection && secondGoodDirection;
        }

        /// <summary>
        /// Neighbour is on left and looking in the same direction, it's a connection (Ushape)
        /// </summary>
        private bool IsULeftConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject single)
        {
            bool hasLeft = HasNeighbourOnLeft(neighbours, out single);
            return hasLeft && single.Direction == _placedObject.Direction;
        }

        /// <summary>
        /// Neighbour is on right and looking in the same direction, it's a connection (Ushape)
        /// </summary>
        private bool IsURightConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject single)
        {
            bool hasRight = HasNeighbourOnRight(neighbours, out single);
            return hasRight && single.Direction == _placedObject.Direction;
        }

        /// <summary>
        /// Neighbour is on right and has a double connection with one connection with this object
        /// </summary>
        private bool IsURightBisConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject single)
        {
            if(!HasNeighbourOnRight(neighbours, out single)) return false;

            var neighbourConnector = single.GetComponent<DirectionnalAdjacencyConnector>();
            var neighbourNeighbours = neighbourConnector.GetNeighbourDirectionnal();
            if (neighbourConnector.HasDoubleConnection(neighbourNeighbours, out var first, out var second))
            {
                if (first == _placedObject || second == _placedObject) return true;
            }

            return false;
        }

        /// <summary>
        /// Neighbour is on left and has a double connection with one connection with this object
        /// </summary>
        private bool IsULeftBisConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject single)
        {
            if (!HasNeighbourOnLeft(neighbours, out single)) return false;

            var neighbourConnector = single.GetComponent<DirectionnalAdjacencyConnector>();
            var neighbourNeighbours = neighbourConnector.GetNeighbourDirectionnal();
            if (neighbourConnector.HasDoubleConnection(neighbourNeighbours, out var first, out var second))
            {
                if (first == _placedObject || second == _placedObject) return true;
            }

            return false;
        }

        private bool HasNeighbourBehind(List<PlacedTileObject> neighbours, out PlacedTileObject behind)
        {
            foreach (var neighbour in neighbours)
            {
                if (neighbour.IsBehind(_placedObject))
                {
                    behind = neighbour;
                    return true;
                }
            }

            behind = null;
            return false;
        }

        private bool HasNeighbourInFront(List<PlacedTileObject> neighbours, out PlacedTileObject inFront) 
        {
            foreach (var neighbour in neighbours)
            {
                if (neighbour.IsInFront(_placedObject))
                {
                    inFront = neighbour;
                    return true;
                }
            }

            inFront = null;
            return false;
        }

        private bool HasNeighbourOnRight(List<PlacedTileObject> neighbours, out PlacedTileObject onRight)
        {
            foreach (var neighbour in neighbours)
            {
                if (neighbour.IsOnRight(_placedObject))
                {
                    onRight = neighbour;
                    return true;
                }
            }

            onRight = null;
            return false;
        }

        private bool HasNeighbourOnLeft(List<PlacedTileObject> neighbours, out PlacedTileObject onLeft)
        {
            foreach (var neighbour in neighbours)
            {
                if (neighbour.IsOnLeft(_placedObject))
                {
                    onLeft = neighbour;
                    return true;
                }
            }

            onLeft = null;
            return false;
        }
    }


}


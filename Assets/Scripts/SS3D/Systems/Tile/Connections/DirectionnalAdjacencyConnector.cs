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

namespace SS3D.Systems.Tile.Connections
{
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
                if (neighbour.IsOnRight(_placedObject))
                    results = UpdateMeshDirectionSingleConnection(true, neighbour);
                else
                    results = UpdateMeshDirectionSingleConnection(false, neighbour);
            }

            else if (HasDoubleConnection(neighbours, out PlacedTileObject first, out PlacedTileObject second, true))
            {
                results = UpdateMeshDirectionDoubleConnection(first, second);
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
            if(HasDoubleConnection(neighbours, out var firstNeighbour, out var secondNeighbour, true))
            {
                return false;
            }

            // neighbour is on right or left and in the same direction, it's a connection (Ushape)
            foreach (var placed in neighbours)
            {
                if (placed.IsOnLeft(_placedObject) && placed.Direction == _placedObject.Direction ||
                        placed.IsOnRight(_placedObject) && placed.Direction == _placedObject.Direction)
                {
                    neighbour = placed;
                    return true;
                }
            }

            // neighbour is on right or left and has a double connection with one connection with this object
            // seems like it works ! 
            foreach (var placed in neighbours)
            {
                if (placed.IsOnLeft(_placedObject) || placed.IsOnRight(_placedObject))
                {
                    var neighbourNeighbours = placed.GetComponent<DirectionnalAdjacencyConnector>().GetNeighbourDirectionnal();
                    var neighbourConnector = placed.GetComponent<DirectionnalAdjacencyConnector>();
                    if (neighbourConnector.HasDoubleConnection(neighbourNeighbours, out var first, out var second, true))
                    {
                        if(first == _placedObject || second == _placedObject)
                        {
                            neighbour = placed;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HasDoubleConnection(List<PlacedTileObject> neighbours,
            out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour, bool checkForIShape)
        {
            firstNeighbour = null;
            secondNeighbour = null;

            // check for I shape, if has a neighbour on both left and right.
            foreach (var neighbour in neighbours)
            {
                if (neighbour.IsOnLeft(_placedObject))
                {
                    secondNeighbour = neighbour;
                }
                if (neighbour.IsOnRight(_placedObject))
                {
                    firstNeighbour = neighbour;
                }
            }

            // two connections there, for an I shape.
            // todo I shape should not be only when neighbours are same directions.
            if (firstNeighbour != null && secondNeighbour != null)
            {
                if (firstNeighbour.Direction == _placedObject.Direction && secondNeighbour.Direction == _placedObject.Direction) return true;
            }

            // checks for the UIN and UOut shape.
            firstNeighbour = null;
            secondNeighbour = null;

            // check if neighbour on left facing the same direction.
            foreach (var neighbour in neighbours)
            {
                if (neighbour.IsOnLeft(_placedObject) && neighbour.Direction == _placedObject.Direction)
                {
                    secondNeighbour = neighbour;
                }
            }

            // check if neighbour on right as well facing the same direction, or only on right
            foreach (var neighbour in neighbours)
            {
                if (neighbour.IsOnRight(_placedObject) && neighbour.Direction == _placedObject.Direction)
                {
                    if (secondNeighbour == null)
                    {
                        secondNeighbour = neighbour;
                    }
                    else
                    {
                        firstNeighbour = neighbour;
                    }
                }
            }

            if (secondNeighbour != null && secondNeighbour.IsOnLeft(_placedObject))
            {
                foreach (var neighbour in neighbours)
                {
                    // two connections for a LIN
                    if (neighbour.IsInFront(_placedObject) && 
                            neighbour.Direction == TileHelper.GetPreviousCardinalDir(_placedObject.Direction))
                    {
                        firstNeighbour = neighbour;
                        return true;
                    }

                    // two connections for a LOut
                    if (neighbour.IsBehind(_placedObject) &&
                            neighbour.Direction == TileHelper.GetNextCardinalDir(_placedObject.Direction))
                    {
                        firstNeighbour = neighbour;
                        return true;
                    }
                }
            }

            if (secondNeighbour != null && secondNeighbour.IsOnRight(_placedObject))
            {
                foreach (var neighbour in neighbours)
                {
                    // two connections for a LIN
                    if (neighbour.IsInFront(_placedObject) &&
                            neighbour.Direction == TileHelper.GetNextCardinalDir(_placedObject.Direction))
                    {
                        firstNeighbour = neighbour;
                        return true;
                    }

                    // two connections for a LOut
                    if (neighbour.IsBehind(_placedObject) &&
                            neighbour.Direction == TileHelper.GetPreviousCardinalDir(_placedObject.Direction))
                    {
                        firstNeighbour = neighbour;
                        return true;
                    }
                }
            }

            return false;
        }



        private List<PlacedTileObject> GetNeighbourDirectionnal()
        {
            var tileSystem = Subsystems.Get<TileSystem>();
            var map = tileSystem.CurrentMap;

            // todo only keep those on cardinal.
            var neighbours = map.GetCardinalNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position);
            return neighbours.Where(x => x != null &&
                x.TryGetComponent<DirectionnalAdjacencyConnector>(out var component)).ToList();
        }

        private Tuple<Mesh,float,Direction> UpdateMeshDirectionSingleConnection(bool neighbourOnRight, PlacedTileObject neighbourObject)
        {
            float rotation = TileHelper.AngleBetween(Direction.North, _placedObject.Direction);
            // should it take neighbour direction or its own ? Or that depends ? 
            if (neighbourOnRight)
            {
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.uRight, rotation, _placedObject.Direction);
            }
            else
            {
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.uLeft, rotation, _placedObject.Direction);
            }
        }

        /// <summary>
        /// In case a neighbour is in front or behind of this placed object, it should be the first neighbour parameter.
        /// no object should be null.
        /// </summary>
        private Tuple<Mesh, float, Direction> UpdateMeshDirectionDoubleConnection(PlacedTileObject firstNeighbour, PlacedTileObject secondNeighbour)
        {
            // Cases for Lin
            // this looks toward north, object in front looks toward east and object on the right side looks toward north.
            float rotation;
            Direction direction;

            // first Lin
            if (firstNeighbour.Direction == TileHelper.GetNextCardinalDir(_placedObject.Direction) && firstNeighbour.IsInFront(_placedObject)
                && secondNeighbour.IsOnRight(_placedObject) && secondNeighbour.Direction == _placedObject.Direction)
            {
                rotation = TileHelper.AngleBetween(Direction.North, secondNeighbour.Direction);
                // direction = TileHelper.GetDiagonalBetweenTwoCardinals(firstNeighbour.Direction, secondNeighbour.Direction);
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.lIn, rotation, direction);
            }

            //second Lin
            if (firstNeighbour.Direction == TileHelper.GetPreviousCardinalDir(_placedObject.Direction) && firstNeighbour.IsInFront(_placedObject)
                && secondNeighbour.IsOnLeft(_placedObject) && secondNeighbour.Direction == _placedObject.Direction)
            {
                // if the second neighbour is on left (the one facing the same direction), then the other 
                // other side of the Lin couch should be linked to it, so we need to do a -90 degree rotation compared to neighbour on right.
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetPreviousCardinalDir(secondNeighbour.Direction));
                // direction = TileHelper.GetDiagonalBetweenTwoCardinals(firstNeighbour.Direction, secondNeighbour.Direction);
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.lIn, rotation, direction);
            }

            // first LOut
            // This looks toward north, behind looks toward west, and on the right looks toward north
            if (firstNeighbour.Direction == TileHelper.GetPreviousCardinalDir(_placedObject.Direction) && firstNeighbour.IsBehind(_placedObject)
                && secondNeighbour.IsOnRight(_placedObject) && secondNeighbour.Direction == _placedObject.Direction)
            {
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetNextCardinalDir(secondNeighbour.Direction));
                // direction = TileHelper.GetDiagonalBetweenTwoCardinals(firstNeighbour.Direction, secondNeighbour.Direction);
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.lOut, rotation, direction);
            }

            // second LOut
            // This looks toward north, behind looks toward east, and on the left looks toward north
            if (firstNeighbour.Direction == TileHelper.GetNextCardinalDir(_placedObject.Direction) && firstNeighbour.IsBehind(_placedObject)
                && secondNeighbour.IsOnLeft(_placedObject) && secondNeighbour.Direction == _placedObject.Direction)
            {
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetOpposite(secondNeighbour.Direction));
                // direction = TileHelper.GetDiagonalBetweenTwoCardinals(firstNeighbour.Direction, secondNeighbour.Direction);
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.lOut, rotation, direction);
            }

            // I case
            if(_placedObject.Direction == firstNeighbour.Direction && _placedObject.Direction == secondNeighbour.Direction)
            {
                rotation = TileHelper.AngleBetween(Direction.North, secondNeighbour.Direction);
                direction = firstNeighbour.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.i, rotation, direction);
            }

            var firstConnector = firstNeighbour.GetComponent<DirectionnalAdjacencyConnector>();
            var secondConnector = secondNeighbour.GetComponent<DirectionnalAdjacencyConnector>();

            var neighboursOfFirst = firstConnector.GetNeighbourDirectionnal().Where(x => x != _placedObject).ToList();
            var neighboursOfSecond = secondConnector.GetNeighbourDirectionnal().Where(x => x != _placedObject).ToList() ;

            // second I Case, neighbours are facing cardinal directions from this, but they are connected to another one.
            if (
                (_placedObject.Direction == firstNeighbour.Direction 
                    || (firstNeighbour.Direction != TileHelper.GetOpposite(_placedObject.Direction) &&
                        firstConnector.HasSingleConnection(neighboursOfFirst, out var n1)))
                
                && ((_placedObject.Direction == secondNeighbour.Direction) 
                    || (secondNeighbour.Direction != TileHelper.GetOpposite(_placedObject.Direction) &&
                        secondConnector.HasSingleConnection(neighboursOfSecond, out var n2)))
                )
            {
                rotation = TileHelper.AngleBetween(Direction.North, _placedObject.Direction);
                // rotation = TileHelper.AngleBetween(Direction.North, secondNeighbour.Direction);
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction>(AdjacencyResolver.i, rotation, direction);
            }

            Debug.LogError("should not reach this point");

            return new Tuple<Mesh, float, Direction>(AdjacencyResolver.o, 0f, Direction.North);
        }
    }


}


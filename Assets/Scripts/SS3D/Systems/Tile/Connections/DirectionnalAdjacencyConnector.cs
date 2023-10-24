﻿using FishNet.Object.Synchronizing;
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
using UnityEngine.UIElements;
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

        private float _currentRotation;

        private int _currentConnections;

        private AdjacencyShape _currentShape;



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
            foreach(var neighbourObject in neighbourObjects)
            {
                UpdateAllAndNeighbours(true);
            }
        }

        

        // ignore param
        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            UpdateAllAndNeighbours(true);
            return true;
        }

        private void UpdateAllAndNeighbours(bool updateNeighbour)
        {
            Setup();
            var neighbours = GetNeighbourDirectionnal();
            neighbours = neighbours.Where(x =>
               !DirectionnalHasTwoConnections(x, out var first, out var second, true)).ToList();
            int connections = 0;

            Tuple<Mesh, float, Direction, AdjacencyShape> results;

            if (HasSingleConnection(neighbours, out PlacedTileObject neighbour))
            {
                results = SelectMeshDirectionRotationSingleConnection(neighbours);
                connections = 1;
            }

            else if (HasDoubleConnection(neighbours, out PlacedTileObject first, out PlacedTileObject second))
            {
                results = SelectMeshDirectionRotationDoubleConnection(neighbours);
                connections = 2;
            }

            // no connections then
            else
            {
                results = new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.o,
                    TileHelper.AngleBetween(Direction.North, _placedObject.Direction), _placedObject.Direction, AdjacencyShape.O);
                connections = 0;
            }

            bool updated = UpdateMeshRotationDirection(results.Item1, results.Item2, results.Item3, results.Item4, connections);

           
            // update all neighbours the first time, then update only neighbours if this is updated.
            if (updated || updateNeighbour)
            {
                foreach(var adjacent in neighbours)
                {
                    adjacent.GetComponent<DirectionnalAdjacencyConnector>().UpdateAllAndNeighbours(false);
                }
            }
        }

        private bool UpdateMeshRotationDirection(Mesh mesh, float rotation, Direction direction, AdjacencyShape shape, int connectionNumbers)
        {
            bool updated = direction != _placedObject.Direction;
            updated |= rotation != _currentRotation;
            updated |= shape != _currentShape;
            updated |= connectionNumbers != _currentConnections;

            _currentShape = shape;
            _currentRotation = rotation;
            _currentConnections = connectionNumbers;
            

            _placedObject.SetDirection(direction);
            _filter.mesh = mesh;

            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, rotation, eulerRotation.z);
            transform.localRotation = localRotation;

            return updated;
        }

        private bool HasSingleConnection(List<PlacedTileObject> neighbours ,out PlacedTileObject neighbour)
        {
            neighbour = null;

            // has double connection, therefore not a single one.
            if(HasDoubleConnection(neighbours, out var first, out var second)) return false;

            return IsULeftConfiguration(neighbours, out var single)
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

        private Tuple<Mesh,float,Direction, AdjacencyShape> SelectMeshDirectionRotationSingleConnection(List<PlacedTileObject> neighbours)
        {
            float rotation = TileHelper.AngleBetween(Direction.North, _placedObject.Direction);

            if (IsULeftConfiguration(neighbours, out var single))
            {
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.uLeft, rotation, _placedObject.Direction, AdjacencyShape.ULeft);
            }

            if (IsURightConfiguration(neighbours, out single))
            {
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.uRight, rotation, _placedObject.Direction, AdjacencyShape.URight);
            }

            Debug.LogError("should not reach this point");
            return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.o, 0, Direction.North, AdjacencyShape.O);
        }

        /// <summary>
        /// In case a neighbour is in front or behind of this placed object, it should be the first neighbour parameter.
        /// no object should be null.
        /// </summary>
        private Tuple<Mesh, float, Direction, AdjacencyShape> SelectMeshDirectionRotationDoubleConnection(List<PlacedTileObject> neighbours)
        {
            float rotation;
            Direction direction;

            if (IsIConfiguration(neighbours, out var first, out var second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, _placedObject.Direction);
                direction = _placedObject.Direction;
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.i, rotation, direction, AdjacencyShape.I);
            }

            if (IsFirstLInConfiguration(neighbours,out first, out second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, second.Direction);
                var firstAdjacentDirections = TileHelper.GetAdjacentAndMiddleDirection(first.Direction);
                var secondAdjacentDirections = TileHelper.GetAdjacentAndMiddleDirection(second.Direction);
                direction = firstAdjacentDirections.Intersect(secondAdjacentDirections).First();
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.lIn, rotation, direction, AdjacencyShape.LIn);
            }

            if (IsSecondLInConfiguration(neighbours, out first, out second))
            {
                // if the second neighbour is on left (the one facing the same direction), then the other 
                // other side of the Lin couch should be linked to it, so we need to do a -90 degree rotation compared to neighbour on right.
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetPreviousCardinalDir(second.Direction));
                var firstAdjacentDirections = TileHelper.GetAdjacentAndMiddleDirection(first.Direction);
                var secondAdjacentDirections = TileHelper.GetAdjacentAndMiddleDirection(second.Direction);
                direction = firstAdjacentDirections.Intersect(secondAdjacentDirections).First();
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.lIn, rotation, direction, AdjacencyShape.LIn);
            }

            if (IsFirstLOutConfiguration(neighbours, out first, out second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetNextCardinalDir(first.Direction));
                var firstAdjacentDirections = TileHelper.GetAdjacentAndMiddleDirection(first.Direction);
                var secondAdjacentDirections = TileHelper.GetAdjacentAndMiddleDirection(second.Direction);
                direction = firstAdjacentDirections.Intersect(secondAdjacentDirections).First();
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.lOut, rotation, direction, AdjacencyShape.LOut);
            }

            if (IsSecondLOutConfiguration(neighbours, out first, out second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetOpposite(second.Direction));
                var firstAdjacentDirections = TileHelper.GetAdjacentAndMiddleDirection(first.Direction);
                var secondAdjacentDirections = TileHelper.GetAdjacentAndMiddleDirection(second.Direction);
                direction = firstAdjacentDirections.Intersect(secondAdjacentDirections).First();
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.lOut, rotation, direction, AdjacencyShape.LOut);
            }

            Debug.LogError("should not reach this point");

            return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.o, 0f, Direction.North, AdjacencyShape.O);
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// FirstLIn is when a neighbour is in front, looking toward east, and another neighbour on the right,
        /// looking toward north as well.
        /// </summary>
        private bool IsFirstLInConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            // Seems to work ! Need to do the same for all L config and I config.
            bool hasFirstCorrect;
            bool hasSecondCorrect;
            List<Direction> firstAllowedDirections;
            List<Direction> secondAllowedDirections;

            // check front and right when this placed object is facing a cardinal direction,
            // otherwise check if directionnables are at adjacent directions.
            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                hasFirstCorrect = HasNeighbourInFront(neighbours, out firstNeighbour);
                hasSecondCorrect = HasNeighbourOnRight(neighbours, out secondNeighbour);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                hasFirstCorrect = HasNeighbourAtDirection(neighbours, out firstNeighbour, directions[1]);
                hasSecondCorrect = HasNeighbourAtDirection(neighbours, out secondNeighbour, directions[0]);
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetPreviousCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_placedObject.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_placedObject.Direction);
            return firstCondition && secondCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// SecondLIn is when a neighbour is in front, looking toward west, and another neighbour on the left,
        /// looking toward north as well.
        /// </summary>
        private bool IsSecondLInConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            // Seems to work ! Need to do the same for all L config and I config.
            bool hasFirstCorrect;
            bool hasSecondCorrect;
            List<Direction> firstAllowedDirections;
            List<Direction> secondAllowedDirections;

            // check front and right when this placed object is facing a cardinal direction,
            // otherwise check if directionnables are at adjacent directions.
            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                hasFirstCorrect = HasNeighbourInFront(neighbours, out firstNeighbour);
                hasSecondCorrect = HasNeighbourOnLeft(neighbours, out secondNeighbour);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                hasFirstCorrect = HasNeighbourAtDirection(neighbours, out firstNeighbour, directions[1]);
                hasSecondCorrect = HasNeighbourAtDirection(neighbours, out secondNeighbour, directions[0]);
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetNextCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_placedObject.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_placedObject.Direction);
            return firstCondition && secondCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// FirstLOut is when a neighbour is behind, looking toward west, and another neighbour on the right,
        /// looking toward north as well.
        /// </summary>
        private bool IsFirstLOutConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            // Seems to work ! Need to do the same for all L config and I config.
            bool hasFirstCorrect;
            bool hasSecondCorrect;
            List<Direction> firstAllowedDirections;
            List<Direction> secondAllowedDirections;

            // check front and right when this placed object is facing a cardinal direction,
            // otherwise check if directionnables are at adjacent directions.
            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                hasFirstCorrect = HasNeighbourBehind(neighbours, out firstNeighbour);
                hasSecondCorrect = HasNeighbourOnRight(neighbours, out secondNeighbour);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                hasFirstCorrect = HasNeighbourAtDirection(neighbours, out firstNeighbour, TileHelper.GetOpposite(directions[1]));
                hasSecondCorrect = HasNeighbourAtDirection(neighbours, out secondNeighbour, TileHelper.GetOpposite(directions[0]));
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetNextCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_placedObject.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_placedObject.Direction);
            return firstCondition && secondCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// SecondLOut is when a neighbour is behind, looking toward east, and another neighbour on the left,
        /// looking toward north as well.
        /// </summary>
        private bool IsSecondLOutConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            // Seems to work ! Need to do the same for all L config and I config.
            bool hasFirstCorrect;
            bool hasSecondCorrect;
            List<Direction> firstAllowedDirections;
            List<Direction> secondAllowedDirections;

            // check front and right when this placed object is facing a cardinal direction,
            // otherwise check if directionnables are at adjacent directions.
            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                hasFirstCorrect = HasNeighbourBehind(neighbours, out firstNeighbour);
                hasSecondCorrect = HasNeighbourOnLeft(neighbours, out secondNeighbour);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                hasFirstCorrect = HasNeighbourAtDirection(neighbours, out firstNeighbour, TileHelper.GetOpposite(directions[1]));
                hasSecondCorrect = HasNeighbourAtDirection(neighbours, out secondNeighbour, TileHelper.GetOpposite(directions[0]));
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetPreviousCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_placedObject.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_placedObject.Direction);
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

            var AllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(_placedObject.Direction);

            bool firstGoodDirection = AllowedDirections.Contains(firstNeighbour.Direction);

            bool secondGoodDirection = AllowedDirections.Contains(secondNeighbour.Direction);

            return firstGoodDirection && secondGoodDirection;
        }

        /// <summary>
        /// Neighbour is on left and looking in the same direction, it's a connection (Ushape)
        /// </summary>
        private bool IsULeftConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject single)
        {
            if(!HasNeighbourOnLeft(neighbours, out single)) return false;

            var AllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(_placedObject.Direction);      
            bool firstCondition = AllowedDirections.Contains(single.Direction);

            return firstCondition;
        }

        /// <summary>
        /// Neighbour is on right and looking in the same direction, it's a connection (Ushape)
        /// </summary>
        private bool IsURightConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject single)
        {
            if (!HasNeighbourOnRight(neighbours, out single)) return false;

            var AllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(_placedObject.Direction);
            bool firstCondition = AllowedDirections.Contains(single.Direction);

            return firstCondition;    
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

        private bool HasNeighbourAtDirection(List<PlacedTileObject> neighbours, out PlacedTileObject atDirection, Direction dir)
        {
            foreach (var neighbour in neighbours)
            {
                if (_placedObject.AtDirectionOf(neighbour, dir))
                {
                    atDirection = neighbour;
                    return true;
                }
            }

            atDirection = null;
            return false;
        }

        private bool DirectionnalHasTwoConnections(PlacedTileObject directionnal,
            out PlacedTileObject first, out PlacedTileObject second, bool excludingThis)
        {
            var directionnalConnector = directionnal.GetComponent<DirectionnalAdjacencyConnector>();
            var directionnalNeighbours = directionnalConnector.GetNeighbourDirectionnal();
            if (excludingThis) directionnalNeighbours.Remove(_placedObject);
            return directionnalConnector.HasDoubleConnection(directionnalNeighbours, out first, out second);
        }

        private bool DirectionnalHasSingleConnection(PlacedTileObject directionnal, out PlacedTileObject first,
            bool excludingThis)
        {
            var directionnalConnector = directionnal.GetComponent<DirectionnalAdjacencyConnector>();
            var directionnalNeighbours = directionnalConnector.GetNeighbourDirectionnal();
            if(excludingThis) directionnalNeighbours.Remove(_placedObject);
            return directionnalConnector.HasSingleConnection(directionnalNeighbours, out first);
        }
    }


}


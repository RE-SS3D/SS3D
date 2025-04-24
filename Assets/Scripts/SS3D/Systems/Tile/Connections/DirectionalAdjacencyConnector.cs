using FishNet.Object;
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
using UnityEngine.UIElements;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using static UnityEngine.ParticleSystem;

namespace SS3D.Systems.Tile.Connections
{

    /// <summary>
    /// Use this script for things such as booths or other models for which direction matters for connections. 
    /// It should mostly work with models such as booth, bench, and other connectable seats.
    /// DO NOT MODIFY WITHOUT A LOT OF CARE. There's a lot of edge cases and it's easy to break it. You've been warned.
    /// </summary>
    public class DirectionalAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {

        /// <summary>
        /// Struct to help find the right shape, direction and rotation for a given directional.
        /// </summary>
        public DirectionnalShapeResolver AdjacencyResolver;

       /// <summary>
       /// The mesh filter of the current directional.
       /// </summary>
        protected MeshFilter _filter;

        /// <summary>
        /// The current placed object associated with this connector.
        /// </summary>
        protected PlacedTileObject _placedObject;

        /// <summary>
        /// True if the script has been initialized.
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// The current rotation of this directionnal.
        /// </summary>
        private float _currentRotation;

        /// <summary>
        /// The current number of connected neighbours to this directionnal.
        /// </summary>
        private int _currentConnections;

        /// <summary>
        /// The current shape the directionnal is taking.
        /// </summary>
        private AdjacencyShape _currentShape;

        /// <summary>
        /// First connected neighbour of the directionnal, server only.
        /// </summary>
        private PlacedTileObject _firstNeighbour;

        /// <summary>
        /// Second connected neighbour of the directionnal, server only.
        /// </summary>
        private PlacedTileObject _secondNeighbour;


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
                _filter = GetComponent<MeshFilter>();

                _placedObject = GetComponent<PlacedTileObject>();
                _initialized = true;
            }
        }

        /// <summary>
        /// Get all neighbour directionals, in cardinal directions from this. 
        /// </summary>
        /// <returns> a list of found neighbours.</returns>
        public List<PlacedTileObject> GetNeighbours()
        {
            Setup();
            var tileSystem = SubSystems.Get<TileSubSystem>();
            var map = tileSystem.CurrentMap;

            var neighbours = map.GetCardinalNeighbourPlacedObjects(_placedObject.Layer, _placedObject.transform.position);
            return neighbours.Where(x => x != null &&
                x.TryGetComponent<DirectionalAdjacencyConnector>(out var component)).ToList();
        }

        /// <summary>
        /// implement
        /// </summary>
        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            return neighbourObject == _firstNeighbour || neighbourObject == _secondNeighbour;
        }

        // ignore param
        public void UpdateAllConnections()
        {
            foreach(var neighbourObject in GetNeighbours())
            {
                UpdateAllAndNeighbours(true);
            }
        }

        /// <summary>
        /// Directionnals should update only their connected neighbours, and should do each time a connection
        /// is updated. They can't update a single connection like other connectables.
        /// </summary>
        public bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            UpdateAllAndNeighbours(true);
            return true;
        }

        private void UpdateAllAndNeighbours(bool updateNeighbour)
        {
            Setup();
            var neighbours = GetNeighbours();
            // We don't want to update neighbours which are already fully connected, they should stay as they are.
            neighbours = neighbours.Where(x => !DirectionnalAlreadyHasTwoConnections(x, true)).ToList();
            int connections = 0;

            Tuple<Mesh, float, Direction, AdjacencyShape> results;

            if (HasSingleConnection(neighbours, out PlacedTileObject neighbour))
            {
                results = SelectMeshDirectionRotationSingleConnection(neighbours);
                connections = 1;
                _firstNeighbour = neighbour;
                _secondNeighbour = null;
            }

            else if (HasDoubleConnection(neighbours, out PlacedTileObject first, out PlacedTileObject second))
            {
                results = SelectMeshDirectionRotationDoubleConnection(neighbours);
                connections = 2;
                _firstNeighbour = first;
                _secondNeighbour = second;
            }
 
            else
            {
                results = new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.o,
                    TileHelper.AngleBetween(Direction.North, _placedObject.Direction), _placedObject.Direction, AdjacencyShape.O);
                connections = 0;
                _firstNeighbour = null;
                _secondNeighbour = null;
            }

            bool updated = UpdateMeshRotationDirection(results.Item1, results.Item2, results.Item3, results.Item4, connections);

            if (updated)
            {
                RpcUpdateOnClient(results.Item2, results.Item4);
            }

            if (updated || updateNeighbour)
            {
                foreach(var adjacent in neighbours)
                {
                    adjacent.GetComponent<DirectionalAdjacencyConnector>().UpdateAllAndNeighbours(false);
                }
            }


        }

        /// <summary>
        /// Update everything regarding this directionnal.
        /// </summary>
        /// <param name="mesh">The new mesh to apply to this directionnal</param>
        /// <param name="rotation">The new rotation to apply to this directionnal</param>
        /// <param name="direction">The new direction to apply to this directionnal</param>
        /// <param name="shape">The new shape to apply to this directionnal</param>
        /// <param name="connectionNumbers">The new number of connections to apply to this directionnal</param>
        /// <returns>true if anything changed compared to before.</returns>
        private bool UpdateMeshRotationDirection(Mesh mesh, float rotation,
            Direction direction, AdjacencyShape shape, int connectionNumbers)
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

        [ObserversRpc(ExcludeOwner = false, BufferLast = true)]
        private void RpcUpdateOnClient(float rotation, AdjacencyShape shape)
        {
            Quaternion localRotation = transform.localRotation;
            Vector3 eulerRotation = localRotation.eulerAngles;
            localRotation = Quaternion.Euler(eulerRotation.x, rotation, eulerRotation.z);
            transform.localRotation = localRotation;
            Mesh mesh = AdjacencyResolver.ShapeToMesh(shape);
            _filter.mesh = mesh;

        }

        /// <summary>
        /// check if the directionnal has two connections. Single connections for 
        /// directionnals means they are in a particular configuration relative
        /// to their neighbours. They are in a U configuration. It's also important to note that
        /// directionnals with two connections are not considered to have one connection, they
        /// are mutually exclusive.
        /// </summary>
        /// <param name="neighbours">a list of other directionnals, adjacent to this one,
        /// which are not fully connected yet.</param>
        /// <param name="neighbour">The single connection if it exists</param>
        /// <returns>true if it has one and only one connection with a neighbour.</returns>
        private bool HasSingleConnection(List<PlacedTileObject> neighbours, 
            out PlacedTileObject neighbour)
        {
            neighbour = null;

            // has double connection, therefore not a single one.
            if (HasDoubleConnection(neighbours, out PlacedTileObject first,
                    out PlacedTileObject second))
            {
                return false;
            }

            return IsUConfiguration(neighbours, out neighbour, true)
                || IsUConfiguration(neighbours, out neighbour, false);
        }

        /// <summary>
        /// check if the directionnal has two connections. Double connections for 
        /// directionnals means they are in a particular configuration relative
        /// to their neighbours.
        /// </summary>
        /// <param name="neighbours"> a list of other directionnals, adjacent to this one,
        /// which are not fully connected yet.</param>
        /// <param name="first"> The first connection if it exists</param>
        /// <param name="second">The second connection if it exists</param>
        /// <returns> true if this has two connections with adjacent neighbours.</returns>
        private bool HasDoubleConnection(List<PlacedTileObject> neighbours,
            out PlacedTileObject first, out PlacedTileObject second)
        {
            return IsIConfiguration(neighbours, out first, out second)
                || IsFirstLInConfiguration(neighbours, out first, out second)
                || IsSecondLInConfiguration(neighbours, out first, out second)
                || IsFirstLOutConfiguration(neighbours, out first, out second)
                || IsSecondLOutConfiguration(neighbours, out first, out second);
        }

        /// <summary>
        /// Given a particular configuration of neighbours with a single connection, 
        /// give all necessary informations on how this directionnal should be.
        /// </summary>
        /// <param name="neighbours">A list of other directionnals, adjacent to this one,
        /// which are not fully connected yet.</param>
        /// <returns>All necessary data to update this</returns>
        private Tuple<Mesh,float,Direction, AdjacencyShape> SelectMeshDirectionRotationSingleConnection(List<PlacedTileObject> neighbours)
        {
            float rotation;
            Direction direction;
            AdjacencyShape shape;
            Mesh mesh;

            if (IsUConfiguration(neighbours, out var single, true))
            {
                shape = AdjacencyShape.ULeft;
                mesh = AdjacencyResolver.uLeft;
            }
            else if (IsUConfiguration(neighbours, out single, false))
            {
                shape = AdjacencyShape.URight;
                mesh = AdjacencyResolver.uRight;
            }
            else
            {
                Debug.LogError("should not reach this point");
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.o, 0, Direction.North, AdjacencyShape.O);
            }

            direction = _placedObject.Direction;

            // if placedObject is currently facing diagonal directions
            // change its direction with an adjacent one such
            // that it keeps connection with the neighbour.
            if (!TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                var adjacents = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                Direction original = _placedObject.Direction;
                if(shape == AdjacencyShape.ULeft)
                {
                    _placedObject.SetDirection(adjacents[0]);
                    if (IsUConfiguration(neighbours, out var temp, true) && temp == single)
                        direction = adjacents[0];
                    _placedObject.SetDirection(adjacents[1]);
                    if (IsUConfiguration(neighbours, out temp, true) && temp == single)
                        direction = adjacents[1];
                }
                else if (shape == AdjacencyShape.URight)
                {
                    _placedObject.SetDirection(adjacents[0]);
                    if (IsUConfiguration(neighbours, out var temp, false) && temp == single)
                        direction = adjacents[0];
                    _placedObject.SetDirection(adjacents[1]);
                    if (IsUConfiguration(neighbours, out temp, false) && temp == single)
                        direction = adjacents[1];
                }
            }
            else
            {
                direction = _placedObject.Direction;
            }
            
            rotation = TileHelper.AngleBetween(Direction.North, direction);

            return new Tuple<Mesh, float, Direction, AdjacencyShape>(mesh, rotation, direction, shape);
        }

        /// <summary>
        /// Given a particular configuration of neighbours with a double connection, 
        /// give all necessary informations on how this directionnal should be.
        /// </summary>
        /// <param name="neighbours">A list of other directionnals, adjacent to this one,
        /// which are not fully connected yet.</param>
        /// <returns>All necessary data to update this</returns>
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
                direction = DirectionInLConfiguration(first, second, true);
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetPreviousDir(direction));
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.lIn, rotation, direction, AdjacencyShape.LIn);
            }

            if (IsSecondLInConfiguration(neighbours, out first, out second))
            {
                direction = DirectionInLConfiguration(first, second, true);
                rotation = TileHelper.AngleBetween(Direction.North, TileHelper.GetPreviousDir(direction));
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.lIn, rotation, direction, AdjacencyShape.LIn);
            }

            if (IsFirstLOutConfiguration(neighbours, out first, out second))
            {
                direction = DirectionInLConfiguration(first, second, false);
                var toRotate = TileHelper.GetOpposite(direction);
                toRotate = TileHelper.GetPreviousDir(toRotate);
                rotation = TileHelper.AngleBetween(Direction.North, toRotate);

                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.lOut, rotation, direction, AdjacencyShape.LOut);
            }

            if (IsSecondLOutConfiguration(neighbours, out first, out second))
            {
                direction = DirectionInLConfiguration(first, second, false);
                var toRotate = TileHelper.GetOpposite(direction);
                toRotate = TileHelper.GetPreviousDir(toRotate);
                rotation = TileHelper.AngleBetween(Direction.North, toRotate);
                return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.lOut, rotation, direction, AdjacencyShape.LOut);
            }

            Debug.LogError("should not reach this point");

            return new Tuple<Mesh, float, Direction, AdjacencyShape>(AdjacencyResolver.o, 0f, Direction.North, AdjacencyShape.O);
        }

        /// <summary>
        /// (in case of this placed object turned toward north or north-east) 
        /// FirstLIn is when a neighbour is in front, 
        /// looking toward east, south-east or north-east and another neighbour on the right,
        /// looking toward north, north-east or north-west.
        /// This method checks that this placed object and its neighbours are respecting this configuration.
        /// </summary>
        private bool IsFirstLInConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasFirstCorrect;
            bool hasSecondCorrect;
            List<Direction> firstAllowedDirections;
            List<Direction> secondAllowedDirections;

            // check front and right when this placed object is facing a cardinal direction,
            // otherwise check if directionnables are at adjacent directions from the direction
            // this is looking at. 
            // e.g : if this is looking at north east, check if neighbours are at north and east.
            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                hasFirstCorrect = _placedObject.HasNeighbourFrontBack(neighbours, out firstNeighbour, true);
                hasSecondCorrect = _placedObject.HasNeighbourOnSide(neighbours, out secondNeighbour, false);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                hasFirstCorrect = _placedObject.HasNeighbourAtDirection(neighbours, out firstNeighbour, directions[1]);
                hasSecondCorrect = _placedObject.HasNeighbourAtDirection(neighbours, out secondNeighbour, directions[0]);
            }

            // If the neighbours are not at the right places return false.
            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            // This check for neighbours right directions. A range of directions 
            if (TileHelper.IsCardinal(_placedObject.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetPreviousCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetFiveAdjacents(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetFiveAdjacents(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_placedObject.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_placedObject.Direction);
            bool thirdCondition = !(TileHelper.IsCardinal(firstNeighbour.Direction) 
                && TileHelper.IsCardinal(secondNeighbour.Direction)
                && firstNeighbour.Direction == secondNeighbour.Direction);
            return firstCondition && secondCondition && thirdCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// SecondLIn is when a neighbour is in front, looking toward west, south-west or north-west, and another neighbour on the left,
        /// looking toward north, north-west or north-east as well.
        /// If the placed object is looking in a direction different from north. Just apply the description above with rotating everything.
        /// This method checks that this placed object and its neighbours are respecting this configuration.
        /// </summary>
        private bool IsSecondLInConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasFirstCorrect;
            bool hasSecondCorrect;
            List<Direction> firstAllowedDirections;
            List<Direction> secondAllowedDirections;

            // check front and left when this placed object is facing a cardinal direction,
            // otherwise check if directionnables are at adjacent directions.
            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                hasFirstCorrect = _placedObject.HasNeighbourFrontBack(neighbours, out firstNeighbour, true);
                hasSecondCorrect = _placedObject.HasNeighbourOnSide(neighbours, out secondNeighbour, true);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                hasFirstCorrect = _placedObject.HasNeighbourAtDirection(neighbours, out firstNeighbour, directions[1]);
                hasSecondCorrect = _placedObject.HasNeighbourAtDirection(neighbours, out secondNeighbour, directions[0]);
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetNextCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetFiveAdjacents(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetFiveAdjacents(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_placedObject.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_placedObject.Direction);
            bool thirdCondition = !(TileHelper.IsCardinal(firstNeighbour.Direction)
                && TileHelper.IsCardinal(secondNeighbour.Direction)
                && firstNeighbour.Direction == secondNeighbour.Direction);
            return firstCondition && secondCondition && thirdCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// FirstLOut is when a neighbour is behind, looking toward west, south-west or north-west, and another neighbour on the right,
        /// looking toward north, north-west or north-east as well.
        /// If the placed object is looking in a direction different from north. Just apply the description above with rotating everything.
        /// This method checks that this placed object and its neighbours are respecting this configuration.
        /// </summary>
        private bool IsFirstLOutConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            // Seems to work ! Need to do the same for all L config and I config.
            bool hasFirstCorrect;
            bool hasSecondCorrect;
            List<Direction> firstAllowedDirections;
            List<Direction> secondAllowedDirections;

            // check front and right when this placed object is facing a cardinal direction,
            // otherwise check if directionnables are at adjacent directions (the two cardinal adjacent ones).
            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                hasFirstCorrect = _placedObject.HasNeighbourFrontBack(neighbours, out firstNeighbour, false);
                hasSecondCorrect = _placedObject.HasNeighbourOnSide(neighbours, out secondNeighbour, false);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                hasFirstCorrect = _placedObject.HasNeighbourAtDirection(neighbours, out firstNeighbour, TileHelper.GetOpposite(directions[1]));
                hasSecondCorrect = _placedObject.HasNeighbourAtDirection(neighbours, out secondNeighbour, TileHelper.GetOpposite(directions[0]));
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetNextCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetFiveAdjacents(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetFiveAdjacents(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_placedObject.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_placedObject.Direction);
            bool thirdCondition = !(TileHelper.IsCardinal(firstNeighbour.Direction)
    && TileHelper.IsCardinal(secondNeighbour.Direction)
    && firstNeighbour.Direction == secondNeighbour.Direction);
            return firstCondition && secondCondition && thirdCondition;
        }

        /// <summary>
        /// (in case of this placed object turned toward north) 
        /// SecondLOut is when a neighbour is behind, looking toward east, south-east or north-east, and another neighbour on the left,
        /// looking toward north, north-west or north-east as well.
        /// If the placed object is looking in a direction different from north. Just apply the description above with rotating everything.
        /// This method checks that this placed object and its neighbours are respecting this configuration.
        /// </summary>
        private bool IsSecondLOutConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasFirstCorrect;
            bool hasSecondCorrect;
            List<Direction> firstAllowedDirections;
            List<Direction> secondAllowedDirections;

            // check front and right when this placed object is facing a cardinal direction,
            // otherwise check if directionnables are at adjacent directions.
            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                hasFirstCorrect = _placedObject.HasNeighbourFrontBack(neighbours, out firstNeighbour, false);
                hasSecondCorrect = _placedObject.HasNeighbourOnSide(neighbours, out secondNeighbour, true);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_placedObject.Direction);
                hasFirstCorrect = _placedObject.HasNeighbourAtDirection(neighbours, out firstNeighbour, TileHelper.GetOpposite(directions[1]));
                hasSecondCorrect = _placedObject.HasNeighbourAtDirection(neighbours, out secondNeighbour, TileHelper.GetOpposite(directions[0]));
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_placedObject.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetPreviousCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetFiveAdjacents(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetFiveAdjacents(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_placedObject.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_placedObject.Direction);
            bool thirdCondition = !(TileHelper.IsCardinal(firstNeighbour.Direction)
     && TileHelper.IsCardinal(secondNeighbour.Direction)
     && firstNeighbour.Direction == secondNeighbour.Direction);
            return firstCondition && secondCondition && thirdCondition;
        }

        /// <summary>
        /// An I Configuration is when a directional has two neighbour next to it, on its left and on its right, such that
        /// each neighbour is either facing the same direction, or an adjacent one (45 degree direction difference).
        /// This method checks that this placed object and its neighbours are respecting this configuration.
        /// </summary>
        private bool IsIConfiguration(List<PlacedTileObject> neighbours, out PlacedTileObject firstNeighbour, out PlacedTileObject secondNeighbour)
        {
            bool hasLeft = _placedObject.HasNeighbourOnSide(neighbours, out firstNeighbour, true);
            bool hasRight = _placedObject.HasNeighbourOnSide(neighbours, out secondNeighbour, false);

            if (!(hasLeft && hasRight)) return false;

            var AllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(_placedObject.Direction);

            bool firstGoodDirection = AllowedDirections.Contains(firstNeighbour.Direction);

            bool secondGoodDirection = AllowedDirections.Contains(secondNeighbour.Direction);

            return firstGoodDirection && secondGoodDirection;
        }

        /// <summary>
        /// An U Configuration is when a directional has a single neighbour next to it, on its left or right, such that
        /// the neighbour is either facing the same direction, or an adjacent one (45 degree direction difference).
        /// This method checks that this placed object and its neighbours are respecting this configuration.
        /// </summary>
        private bool IsUConfiguration(List<PlacedTileObject> neighbours,
            out PlacedTileObject single, bool left)
        {
            Direction original = _placedObject.Direction;
            bool diagonal = false;
            List<Direction> adjacents = TileHelper.GetAdjacentDirections(_placedObject.Direction);
            List<Direction> AllowedDirections;

            if (!TileHelper.CardinalDirections().Contains(original)) diagonal = true;

            if (diagonal)
            {
                _placedObject.SetDirection(adjacents[0]);
                bool hasNeighbourFirstCardinal =
                    IsUConfiguration(neighbours, out var firstCardinal, left);
                _placedObject.SetDirection(adjacents[1]);
                bool hasNeighbourSecondCardinal =
                    IsUConfiguration(neighbours, out var secondCardinal, left);

                if (hasNeighbourFirstCardinal) single = firstCardinal;
                else if (hasNeighbourSecondCardinal) single = secondCardinal;
                else single = null;
                _placedObject.SetDirection(original);
                AllowedDirections = TileHelper.GetFiveAdjacents(_placedObject.Direction);

            }
            else
            {
                AllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(_placedObject.Direction);
                _placedObject.HasNeighbourOnSide(neighbours, out single, left);
            }

            if (single == null) return false;
            return AllowedDirections.Contains(single.Direction);
        }

        /// <summary>
        /// Check if the placed object, which must have a directional adjacency connector component, has already two connections.
        /// </summary>
        /// <param name="directionnal">The placed object we want to test.</param>
        /// <param name="excludingThis"> check if the direction has already two connection without taking into consideration
        /// the placed object of this instance.</param>
        /// <returns></returns>
        private bool DirectionnalAlreadyHasTwoConnections(PlacedTileObject directionnal, bool excludingThis)
        {
            var neighbourConnector = directionnal.GetComponent<DirectionalAdjacencyConnector>();
            if (neighbourConnector._currentConnections == 2)
            {
                if (excludingThis && 
                    (neighbourConnector._firstNeighbour.GetComponent<DirectionalAdjacencyConnector>() == this
                    || neighbourConnector._secondNeighbour.GetComponent<DirectionalAdjacencyConnector>() == this))
                {
                    return false;
                }
                return true;

            }
            return false;
        }

        /// <summary>
        /// In L configuration, the direction of the middle directionnal depends on the direction
        /// of its two neighbours. There is nine possible L configuration, for both first
        /// and second LIn and LOut. It's better to draw them to understand properly. On those nine 
        /// configurations, 5 of them contain a neighbour with a single diagonal direction.
        /// The direction of this directional is then simply the closest diagonal from both directions
        /// of neighbours.
        /// e.g : neighbour 1 looks toward west, neighbour 2 looks toward north, this looks toward
        /// northwest.
        /// eg : neighbour 1 looks toward northwest, neighbour 2 looks toward north, this looks
        /// toward northwest.
        /// When the two neighbours looks into diagonal directions, it's a bit different.
        /// When the two diagonals are opposite, it's also the closest diagonal.
        /// When the two diagonals are the same, it's also the closest diagonal.
        /// Two cases are special, when neighbours diagonal directions are adjacent. 
        /// In this case, the middle directionnal should take the direction of the neighbour
        /// in a LOut shape, if the middle is LIn, and the neighbour in a LIn shape, if the middle
        /// is LOut.
        /// </summary>
        /// <param name="first">first neighbour part of the LIn configuration.</param>
        /// <param name="second">second neighbour part of the LIn configuration.</param>
        /// <param name="LIn">if the middle (this placed object) is in LIn configuration.</param>
        /// <returns>the good direction for the middle directionnal.</returns>
        private Direction DirectionInLConfiguration(PlacedTileObject first, PlacedTileObject second, bool LIn)
        {
            Direction direction;
            AdjacencyShape shapeToCheck = LIn ? AdjacencyShape.LOut : AdjacencyShape.LIn;
            if (TileHelper.IsDiagonal(first.Direction) && TileHelper.IsDiagonal(second.Direction)
                   && TileHelper.GetOpposite(first.Direction) != second.Direction
                   && first.Direction != second.Direction)
            {
                if (first.GetComponent<DirectionalAdjacencyConnector>()._currentShape == shapeToCheck)
                {
                    direction = first.Direction;
                }
                else
                {
                    direction = second.Direction;
                }
            }
            else direction = TileHelper.ClosestDiagonalFromTwo(first.Direction, second.Direction);

            return direction;
        }
    }


}


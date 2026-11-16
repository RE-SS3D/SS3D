using SS3D.Logging;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Pure geometry evaluation for directional connectors (booths, benches, etc.).
    /// </summary>
    public static class DirectionalConfigurationEvaluator
    {
        public static DirectionalAdjacencyResult Evaluate(
            PlacedTileObject self,
            IReadOnlyList<PlacedTileObject> neighbours,
            Func<PlacedTileObject, DirectionalNeighbourState> getNeighbourState,
            DirectionnalShapeResolver resolver)
        {
            List<PlacedTileObject> eligible = neighbours
                .Where(n => !NeighbourAlreadyFullyConnected(self, getNeighbourState(n)))
                .ToList();

            var evaluator = new Evaluator(self, getNeighbourState, resolver);
            (Mesh Mesh, float Rotation, Direction Facing, AdjacencyShape Shape, int ConnectionCount,
                PlacedTileObject First, PlacedTileObject Second) resolved = evaluator.Resolve(eligible);

            return new DirectionalAdjacencyResult(
                resolved.Mesh,
                resolved.Rotation,
                resolved.Facing,
                resolved.Shape,
                resolved.ConnectionCount,
                resolved.First,
                resolved.Second);
        }

        /// <summary>
        /// Neighbours with two connections are frozen unless this tile is one of their partners.
        /// </summary>
        public static bool NeighbourAlreadyFullyConnected(
            PlacedTileObject self,
            DirectionalNeighbourState neighbourState)
        {
            if (neighbourState.ConnectionCount != 2)
                return false;

            return neighbourState.FirstNeighbour != self && neighbourState.SecondNeighbour != self;
        }

        private sealed class Evaluator
        {
            private readonly PlacedTileObject _self;
            private readonly Func<PlacedTileObject, DirectionalNeighbourState> _getNeighbourState;
            private readonly DirectionnalShapeResolver _resolver;

            internal Evaluator(
                PlacedTileObject self,
                Func<PlacedTileObject, DirectionalNeighbourState> getNeighbourState,
                DirectionnalShapeResolver resolver)
            {
                _self = self;
                _getNeighbourState = getNeighbourState;
                _resolver = resolver;
            }

            internal (Mesh Mesh, float Rotation, Direction Facing, AdjacencyShape Shape, int ConnectionCount,
                PlacedTileObject First, PlacedTileObject Second) Resolve(List<PlacedTileObject> neighbours)
            {
                if (HasSingleConnection(neighbours, out PlacedTileObject neighbour))
                {
                    (Mesh Mesh, float Rotation, Direction Direction, AdjacencyShape Shape) single =
                        SelectMeshDirectionRotationSingleConnection(neighbours);
                    return (single.Mesh, single.Rotation, single.Direction, single.Shape, 1, neighbour, null);
                }

                if (HasDoubleConnection(neighbours, out PlacedTileObject first, out PlacedTileObject second))
                {
                    (Mesh Mesh, float Rotation, Direction Direction, AdjacencyShape Shape) dbl =
                        SelectMeshDirectionRotationDoubleConnection(neighbours);
                    return (dbl.Mesh, dbl.Rotation, dbl.Direction, dbl.Shape, 2, first, second);
                }

                float oRotation = TileHelper.AngleBetween(Direction.North, _self.Direction);
                return (_resolver.o, oRotation, _self.Direction, AdjacencyShape.O, 0, null, null);
            }

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
        private (Mesh Mesh, float Rotation, Direction Direction, AdjacencyShape Shape) SelectMeshDirectionRotationSingleConnection(List<PlacedTileObject> neighbours)
        {
            float rotation;
            Direction direction;
            AdjacencyShape shape;
            Mesh mesh;

            if (IsUConfiguration(neighbours, out var single, true))
            {
                shape = AdjacencyShape.ULeft;
                mesh = _resolver.uLeft;
            }
            else if (IsUConfiguration(neighbours, out single, false))
            {
                shape = AdjacencyShape.URight;
                mesh = _resolver.uRight;
            }
            else
            {
                Log.Error(typeof(DirectionalConfigurationEvaluator), "should not reach this point");
                return (_resolver.o, 0, Direction.North, AdjacencyShape.O);
            }

            direction = _self.Direction;

            // if placedObject is currently facing diagonal directions
            // change its direction with an adjacent one such
            // that it keeps connection with the neighbour.
            if (!TileHelper.CardinalDirections().Contains(_self.Direction))
            {
                var adjacents = TileHelper.GetAdjacentDirections(_self.Direction);
                Direction original = _self.Direction;
                if(shape == AdjacencyShape.ULeft)
                {
                    _self.SetDirection(adjacents[0]);
                    if (IsUConfiguration(neighbours, out var temp, true) && temp == single)
                        direction = adjacents[0];
                    _self.SetDirection(adjacents[1]);
                    if (IsUConfiguration(neighbours, out temp, true) && temp == single)
                        direction = adjacents[1];
                }
                else if (shape == AdjacencyShape.URight)
                {
                    _self.SetDirection(adjacents[0]);
                    if (IsUConfiguration(neighbours, out var temp, false) && temp == single)
                        direction = adjacents[0];
                    _self.SetDirection(adjacents[1]);
                    if (IsUConfiguration(neighbours, out temp, false) && temp == single)
                        direction = adjacents[1];
                }
            }
            else
            {
                direction = _self.Direction;
            }
            
            rotation = TileHelper.AngleBetween(Direction.North, direction);

            return (mesh, rotation, direction, shape);
        }

        /// <summary>
        /// Given a particular configuration of neighbours with a double connection, 
        /// give all necessary informations on how this directionnal should be.
        /// </summary>
        /// <param name="neighbours">A list of other directionnals, adjacent to this one,
        /// which are not fully connected yet.</param>
        /// <returns>All necessary data to update this</returns>
        private (Mesh Mesh, float Rotation, Direction Direction, AdjacencyShape Shape) SelectMeshDirectionRotationDoubleConnection(List<PlacedTileObject> neighbours)
        {
            float rotation;
            Direction direction;

            if (IsIConfiguration(neighbours, out var first, out var second))
            {
                rotation = TileHelper.AngleBetween(Direction.North, _self.Direction);
                direction = _self.Direction;
                return (_resolver.i, rotation, direction, AdjacencyShape.I);
            }

            if (IsFirstLInConfiguration(neighbours,out first, out second))
            {
                direction = DirectionInLConfiguration(first, second, true);
                rotation = LCornerRotation(AdjacencyShape.LIn, direction);
                return (_resolver.ShapeToMesh(AdjacencyShape.LIn), rotation, direction, AdjacencyShape.LIn);
            }

            if (IsSecondLInConfiguration(neighbours, out first, out second))
            {
                direction = DirectionInLConfiguration(first, second, true);
                rotation = LCornerRotation(AdjacencyShape.LIn, direction);
                return (_resolver.ShapeToMesh(AdjacencyShape.LIn), rotation, direction, AdjacencyShape.LIn);
            }

            if (IsFirstLOutConfiguration(neighbours, out first, out second))
            {
                direction = DirectionInLConfiguration(first, second, false);
                rotation = LCornerRotation(AdjacencyShape.LOut, direction);
                return (_resolver.ShapeToMesh(AdjacencyShape.LOut), rotation, direction, AdjacencyShape.LOut);
            }

            if (IsSecondLOutConfiguration(neighbours, out first, out second))
            {
                direction = DirectionInLConfiguration(first, second, false);
                rotation = LCornerRotation(AdjacencyShape.LOut, direction);
                return (_resolver.ShapeToMesh(AdjacencyShape.LOut), rotation, direction, AdjacencyShape.LOut);
            }

            Log.Error(typeof(DirectionalConfigurationEvaluator), "should not reach this point");

            return (_resolver.o, 0f, Direction.North, AdjacencyShape.O);
        }

        /// <summary>
        /// Mesh asset names are inverted relative to LIn/LOut configuration names, so each shape
        /// uses the rotation formula that matches its displayed mesh.
        /// </summary>
        private static float LCornerRotation(AdjacencyShape shape, Direction direction)
        {
            if (shape == AdjacencyShape.LIn)
            {
                Direction toRotate = TileHelper.GetPreviousDir(TileHelper.GetOpposite(direction));
                return TileHelper.AngleBetween(Direction.North, toRotate);
            }

            return TileHelper.AngleBetween(Direction.North, TileHelper.GetPreviousDir(direction));
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
            if (TileHelper.CardinalDirections().Contains(_self.Direction))
            {
                hasFirstCorrect = _self.HasNeighbourFrontBack(neighbours, out firstNeighbour, true);
                hasSecondCorrect = _self.HasNeighbourOnSide(neighbours, out secondNeighbour, false);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_self.Direction);
                hasFirstCorrect = _self.HasNeighbourAtDirection(neighbours, out firstNeighbour, directions[1]);
                hasSecondCorrect = _self.HasNeighbourAtDirection(neighbours, out secondNeighbour, directions[0]);
            }

            // If the neighbours are not at the right places return false.
            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            // This check for neighbours right directions. A range of directions 
            if (TileHelper.IsCardinal(_self.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetPreviousCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetFiveAdjacents(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetFiveAdjacents(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_self.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_self.Direction);
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
            if (TileHelper.CardinalDirections().Contains(_self.Direction))
            {
                hasFirstCorrect = _self.HasNeighbourFrontBack(neighbours, out firstNeighbour, true);
                hasSecondCorrect = _self.HasNeighbourOnSide(neighbours, out secondNeighbour, true);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_self.Direction);
                hasFirstCorrect = _self.HasNeighbourAtDirection(neighbours, out firstNeighbour, directions[1]);
                hasSecondCorrect = _self.HasNeighbourAtDirection(neighbours, out secondNeighbour, directions[0]);
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_self.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetNextCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetFiveAdjacents(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetFiveAdjacents(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_self.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_self.Direction);
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
            if (TileHelper.CardinalDirections().Contains(_self.Direction))
            {
                hasFirstCorrect = _self.HasNeighbourFrontBack(neighbours, out firstNeighbour, false);
                hasSecondCorrect = _self.HasNeighbourOnSide(neighbours, out secondNeighbour, false);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_self.Direction);
                hasFirstCorrect = _self.HasNeighbourAtDirection(neighbours, out firstNeighbour, TileHelper.GetOpposite(directions[1]));
                hasSecondCorrect = _self.HasNeighbourAtDirection(neighbours, out secondNeighbour, TileHelper.GetOpposite(directions[0]));
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_self.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetNextCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetFiveAdjacents(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetFiveAdjacents(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_self.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_self.Direction);
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
            if (TileHelper.CardinalDirections().Contains(_self.Direction))
            {
                hasFirstCorrect = _self.HasNeighbourFrontBack(neighbours, out firstNeighbour, false);
                hasSecondCorrect = _self.HasNeighbourOnSide(neighbours, out secondNeighbour, true);
            }
            else
            {
                var directions = TileHelper.GetAdjacentDirections(_self.Direction);
                hasFirstCorrect = _self.HasNeighbourAtDirection(neighbours, out firstNeighbour, TileHelper.GetOpposite(directions[1]));
                hasSecondCorrect = _self.HasNeighbourAtDirection(neighbours, out secondNeighbour, TileHelper.GetOpposite(directions[0]));
            }

            if (!(hasFirstCorrect && hasSecondCorrect)) return false;

            if (TileHelper.CardinalDirections().Contains(_self.Direction))
            {
                firstAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(TileHelper.GetPreviousCardinalDir(firstNeighbour.Direction));
                secondAllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(secondNeighbour.Direction);
            }
            else
            {
                firstAllowedDirections = TileHelper.GetFiveAdjacents(firstNeighbour.Direction);
                secondAllowedDirections = TileHelper.GetFiveAdjacents(secondNeighbour.Direction);
            }

            bool firstCondition = firstAllowedDirections.Contains(_self.Direction);
            bool secondCondition = secondAllowedDirections.Contains(_self.Direction);
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
            bool hasLeft = _self.HasNeighbourOnSide(neighbours, out firstNeighbour, true);
            bool hasRight = _self.HasNeighbourOnSide(neighbours, out secondNeighbour, false);

            if (!(hasLeft && hasRight)) return false;

            var AllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(_self.Direction);

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
            Direction original = _self.Direction;
            bool diagonal = false;
            List<Direction> adjacents = TileHelper.GetAdjacentDirections(_self.Direction);
            List<Direction> AllowedDirections;

            if (!TileHelper.CardinalDirections().Contains(original)) diagonal = true;

            if (diagonal)
            {
                _self.SetDirection(adjacents[0]);
                bool hasNeighbourFirstCardinal =
                    IsUConfiguration(neighbours, out var firstCardinal, left);
                _self.SetDirection(adjacents[1]);
                bool hasNeighbourSecondCardinal =
                    IsUConfiguration(neighbours, out var secondCardinal, left);

                if (hasNeighbourFirstCardinal) single = firstCardinal;
                else if (hasNeighbourSecondCardinal) single = secondCardinal;
                else single = null;
                _self.SetDirection(original);
                AllowedDirections = TileHelper.GetFiveAdjacents(_self.Direction);

            }
            else
            {
                AllowedDirections = TileHelper.GetAdjacentAndMiddleDirection(_self.Direction);
                _self.HasNeighbourOnSide(neighbours, out single, left);
            }

            if (single == null) return false;
            return AllowedDirections.Contains(single.Direction);
        }

        private Direction DirectionInLConfiguration(PlacedTileObject first, PlacedTileObject second, bool LIn)
        {
            Direction direction;
            AdjacencyShape shapeToCheck = LIn ? AdjacencyShape.LOut : AdjacencyShape.LIn;
            if (TileHelper.IsDiagonal(first.Direction) && TileHelper.IsDiagonal(second.Direction)
                   && TileHelper.GetOpposite(first.Direction) != second.Direction
                   && first.Direction != second.Direction)
            {
                if (_getNeighbourState(first).CurrentShape == shapeToCheck)
                    direction = first.Direction;
                else
                    direction = second.Direction;
            }
            else
            {
                direction = TileHelper.ClosestDiagonalFromTwo(first.Direction, second.Direction);
            }

            return direction;
        }
        }
    }
}

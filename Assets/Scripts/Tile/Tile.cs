using UnityEngine;
using System;
using System.Collections;

namespace TileMap {
    public enum Direction
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest,
    }
    public enum Orientation
    {
        Vertical, // North-South
        Horizontal // East-West
    }

    public static class DirectionHelper {
        public static float ToAngle(Direction direction)
        {
            return ((int)direction) * 45.0f;
        }
        public static Tuple<int, int> ToCardinalVector(Direction direction)
        {
            return new Tuple<int, int>(
                (direction > Direction.North && direction < Direction.South) ? 1 : (direction > Direction.South) ? -1 : 0,
                (direction > Direction.East && direction < Direction.West) ? -1 : (direction == Direction.East || direction == Direction.West) ? 0 : 1
            );
        }
        public static Direction GetOpposite(Direction direction)
        {
            return (Direction)(((int)direction + 4) % 8);
        }
    }

    public enum TileLayer
    {
        Turf,
        Fixture,
    }

    /**
     * Describes a tile in networkable information
     */
    public struct NetworkableTile
    {
        // The base of the tile, could be a wall or floor. Is id of Turf scriptable object
        public string turf;

        public string fixture; // Id of a Fixture scriptable object
        public Direction fixtureDirection;
    }

    /**
     * Same as above but with IDs converted to actual scriptable objects
     */
    [Serializable]
    public struct ConstructibleTile
    {
        public Turf turf;
        public Fixture fixture;

        public byte[][] attributes;

        public static ConstructibleTile NullObject = new ConstructibleTile { turf = null, fixture = null, fixtureDirection = Direction.North }; 
    }
}
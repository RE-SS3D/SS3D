using UnityEngine;
using System;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

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

    public static class DirectionHelper
    {
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

        // Same as AngleBetween(North, direction)
        public static float ToAngle(Direction direction)
        {
            return ((int)direction) * 45.0f;
        }
        public static float AngleBetween(Direction from, Direction to)
        {
            return ((int)to - (int)from) * 45.0f;
        }
    }
    public static class OrientationHelper
    {
        public static float ToAngle(Orientation orientation)
        {
            return (int)orientation * 90.0f;
        }
        public static float AngleBetween(Orientation from, Orientation to)
        {
            return ((int)to - (int)from) * 90.0f;
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
    public struct NetworkableTileDefinition
    {
        // The base of the tile, could be a wall or floor. Is id of Turf scriptable object
        public string turf;
        public string fixture; // Id of a Fixture scriptable object
        
        public byte[][] attributes;
    }

    /**
     * Same as above but with IDs converted to actual scriptable objects
     */
    [Serializable]
    public struct TileDefinition
    {
        public Turf turf;
        public Fixture fixture;

        // An array of serializable objects containing the state of each subtile object.
        // If a subtile object has state, it will be stored at the corresponding index (turf=0, fixture=1).
        // If no subtile object has state, the array may be null or have no length.
        // The array may only be as long as it needs to be to store all non-null objects.
        public object[] subData;

        public static TileDefinition NullObject = new TileDefinition { turf = null, fixture = null, subData = null };

        public static bool operator ==(TileDefinition a, TileDefinition b)
        {
            return a.turf == b.turf && a.fixture == b.fixture && a.subData.Equals(b.subData);
        }
        public static bool operator !=(TileDefinition a, TileDefinition b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return obj is TileDefinition definition &&
                   EqualityComparer<Turf>.Default.Equals(turf, definition.turf) &&
                   EqualityComparer<Fixture>.Default.Equals(fixture, definition.fixture) &&
                   EqualityComparer<object[]>.Default.Equals(subData, definition.subData);
        }
        public override int GetHashCode()
        {
            var hashCode = 1153620473;
            hashCode = hashCode * -1521134295 + EqualityComparer<Turf>.Default.GetHashCode(turf);
            hashCode = hashCode * -1521134295 + EqualityComparer<Fixture>.Default.GetHashCode(fixture);
            hashCode = hashCode * -1521134295 + EqualityComparer<object[]>.Default.GetHashCode(subData);
            return hashCode;
        }
        public bool IsEmpty()
        {
            return turf == null;
        }
    }
}
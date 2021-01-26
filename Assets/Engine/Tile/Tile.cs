using UnityEngine;
using System;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using SS3D.Engine.Atmospherics;

namespace SS3D.Engine.Tiles {
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

    public enum Rotation
    {
        North,
        East,
        South,
        West
    }

    public static class DirectionHelper
    {
        /**
         * Applies the second direction on top of the first
         */
        public static Direction Apply(Direction first, Direction second)
        {
            return (Direction)(((int)first + (int)second + 8) % 8);
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
        /**
         * North for vertical, east for horizontal
         */
        public static Direction ToPrincipalDirection(Orientation orientation)
        {
            return (Direction)((int)orientation * 2);
        }
    }

    public enum TileLayers
    {
        Plenum,
        Turf,
        Wire,
        Disposal,
        Pipe1,
        Pipe2,
        Pipe3,
        HighWallNorth,
        HighWallEast,
        HighWallSouth,
        HighWallWest,
        LowWallNorth,
        LowWallEast,
        LowWallSouth,
        LowWallWest,
        AtmosMachinery,
        Overlay1,
        Overlay2,
        Overlay3,
        FurnitureMain,
        Furniture2,
        Furniture3,
        Furniture4,
        Furniture5,
        AtmosObject,
    }

    public enum TileVisibilityLayers
    {
        Plenum,
        Turf,
        Wire,
        Disposal,
        Pipe,
        HighWall,
        LowWall,
        AtmosMachinery,
        Overlay,
        Furniture,
        AtmosObject,
    }

    public enum PipeLayers
    {
        Pipe1,
        Pipe2,
        Pipe3,
    }

    public enum OverlayLayers
    {
        Overlay1,
        Overlay2,
        Overlay3,
    }

    public enum FurnitureLayers
    {
        FurnitureMain,
        Furniture2,
        Furniture3,
        Furniture4,
        Furniture5,
    }



    /**
     * Same as above but with IDs converted to actual scriptable objects
     */
    [Serializable]
    public struct TileDefinition
    {
        public Plenum plenum;
        public Turf turf;
        public FixturesContainer fixtures;
        public AtmosObject atmos;

        // An array of serializable objects containing the state of each subtile object.
        // If a subtile object has state, it will be stored at the corresponding index (plenum=0, turf=1, fixtures=2..x).
        // If no subtile object has state, the array may be null or have no length.
        // The array may only be as long as it needs to be to store all non-null objects.
        public object[] subStates;

        public static TileDefinition NullObject = new TileDefinition { plenum = null, turf = null, fixtures = null, subStates = null };

        public static int GetTileFixtureLayerSize()
        {
            return Enum.GetValues(typeof(TileFixtureLayers)).Length;
        }

        public static int GetWallFixtureLayerSize()
        {
            return Enum.GetValues(typeof(WallFixtureLayers)).Length;
        }

        public static int GetFloorFixtureLayerSize()
        {
            return Enum.GetValues(typeof(FloorFixtureLayers)).Length;
        }

        public static int GetAllFixtureLayerSize()
        {
            return GetTileFixtureLayerSize() + GetWallFixtureLayerSize() + GetFloorFixtureLayerSize();
        }

        public static TileFixtureLayers[] GetTileFixtureLayerNames()
        {
            return (TileFixtureLayers[])Enum.GetValues(typeof(TileFixtureLayers));
        }

        public static WallFixtureLayers[] GetWallFixtureLayerNames()
        {
            return (WallFixtureLayers[])Enum.GetValues(typeof(WallFixtureLayers));
        }

        public static FloorFixtureLayers[] GetFloorFixtureLayerNames()
        {
            return (FloorFixtureLayers[])Enum.GetValues(typeof(FloorFixtureLayers));
        }

        public static bool operator ==(TileDefinition a, TileDefinition b)
        {
            return a.plenum == b.plenum && a.turf == b.turf && a.fixtures.Equals(b.fixtures) && a.subStates.Equals(b.subStates);
        }
        public static bool operator !=(TileDefinition a, TileDefinition b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            return obj is TileDefinition definition &&
                   EqualityComparer<Plenum>.Default.Equals(plenum, definition.plenum) &&
                   EqualityComparer<Turf>.Default.Equals(turf, definition.turf) &&
                   EqualityComparer<FixturesContainer>.Default.Equals(fixtures, definition.fixtures) &&
                   EqualityComparer<object[]>.Default.Equals(subStates, definition.subStates);
        }
        public override int GetHashCode()
        {
            var hashCode = 1153620473;
            hashCode = hashCode * -1521134295 + EqualityComparer<Plenum>.Default.GetHashCode(plenum);
            hashCode = hashCode * -1521134295 + EqualityComparer<Turf>.Default.GetHashCode(turf);
            hashCode = hashCode * -1521134295 + EqualityComparer<FixturesContainer>.Default.GetHashCode(fixtures);
            hashCode = hashCode * -1521134295 + EqualityComparer<object[]>.Default.GetHashCode(subStates);
            return hashCode;
        }
        public bool IsEmpty()
        {
            return plenum == null;
        }
    }
}
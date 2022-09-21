using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SS3D.Engine.Tile.TileRework.Connections.AdjacencyTypes;
using SS3D.Engine.Tiles;

namespace SS3D.Engine.Tile.TileRework.Connections
{
    /// <summary>
    /// Used for storing adjacencies.
    /// </summary>
    public class AdjacencyMap
    {
        //Stores an array of which of the 8 surrounding tiles have a connection. Order assumed to match Direction enum values.
        private AdjacencyData[] connections;

        public AdjacencyMap()
        {
            connections = new [] {
                new AdjacencyData (TileObjectGenericType.None, TileObjectSpecificType.None, false),
                new AdjacencyData (TileObjectGenericType.None, TileObjectSpecificType.None, false),
                new AdjacencyData (TileObjectGenericType.None, TileObjectSpecificType.None, false),
                new AdjacencyData (TileObjectGenericType.None, TileObjectSpecificType.None, false),
                new AdjacencyData (TileObjectGenericType.None, TileObjectSpecificType.None, false),
                new AdjacencyData (TileObjectGenericType.None, TileObjectSpecificType.None, false),
                new AdjacencyData (TileObjectGenericType.None, TileObjectSpecificType.None, false),
                new AdjacencyData (TileObjectGenericType.None, TileObjectSpecificType.None, false),
            };
        }

        public AdjacencyData[] Connections => connections;

        public bool HasConnection(Direction direction)
        {
            return connections[(int)direction].Exists;
        }

        public int GetCardinalConnectionCount()
        {
            return GetAdjacencies(true).Count;
        }

        public int GetDiagonalConnectionCount()
        {
            return GetAdjacencies(false).Count;
        }

        /**
         * Gets the direction of the only cardinal/diagonal connection.
         * Assumes there is exactly one cardinal/diagonal connection.
         */
        public Direction GetSingleConnection(bool cardinal = true)
        {
            List<Direction> foundConnections = GetAdjacencies(cardinal);
            return foundConnections[0];
        }
        
        /**
         * Get the direction of the only cardinal/diagonal non connection.
         * Assumes there is exactly one cardinal/diagonal non connection
         */
        public Direction GetSingleNonConnection(bool cardinal = true)
        {
            List<Direction> foundConnections = GetAdjacencies(cardinal);
            List<Direction> directions = cardinal ? TileHelper.CardinalDirections() : TileHelper.DiagonalDirections();
            List<Direction> missingConnections =
                directions.Where(direction => !foundConnections.Contains(direction)).ToList();

            return missingConnections[0];
        }

        /**
         * Assuming the cardinals/diagonal have exactly two adjacent connections,
         * gets the direction between them.
         */
        public Direction GetDirectionBetweenTwoConnections(bool cardinal = true)
        {
            List<Direction> foundConnections = GetAdjacencies(cardinal);

            return cardinal
                ? TileHelper.GetDiagonalBetweenTwoCardinals(foundConnections[0], foundConnections[1])
                : TileHelper.GetCardinalBetweenTwoDiagonals(foundConnections[0], foundConnections[1]);
        }

        /**
         * Updates the map, and returns whether there was a change
         */
        public bool SetConnection(Direction direction, AdjacencyData data)
        {
            bool changed = !data.Equals(connections[(int) direction]);
            if (changed)
            {
                connections[(int) direction] = data;
            }

            return changed;
        }

        private List<Direction> GetAdjacencies(bool cardinal)
        {
            //Are we getting adjacencies for cardinal or diagonal directions?
            List<int> directionIndexes = cardinal ?
                TileHelper.CardinalDirections().Select(direction => (int)direction).ToList() : 
                TileHelper.DiagonalDirections().Select(direction => (int)direction).ToList();
            //Loop through each index in direction indexes, pick those that exist and cast them to the Direction enum.
            return (from index in directionIndexes where connections[index].Exists select (Direction) index).ToList();
        }
        
        public void DeserializeFromByte(byte bytemap)
        {
            BitArray bits = new BitArray(new byte[] { bytemap });
            AdjacencyData[] adjacencyData = new AdjacencyData[8];
            for (int i = 0; i < bits.Length; i++)
            {
                adjacencyData[i] = new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, bits[i]);
            }

            connections = adjacencyData;
        }

        public byte SerializeToByte()
        {
            int sum = 0;
            for (int i = 1, direction = 0; i < 256; i *= 2, direction++)
            {
                if (connections[direction].Exists)
                {
                    sum += i;
                }
            }

            return (byte) sum;
        }

        public override string ToString()
        {
            return $"{connections}";
        }
    }
}
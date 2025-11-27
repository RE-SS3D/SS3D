using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Used for storing which neighbouring objects the primary object is connected to. Used for connecting walls, tables, carpets etc together by changing their mesh.
    /// </summary>
    public class AdjacencyMap
    {
        // Stores an array of which of the 8 surrounding tiles have a connection. Order assumed to match Direction enum values.
        private bool[] _connections;

        public AdjacencyMap()
        {
            _connections = new bool[8];
        }

        /// <summary>
        /// Get the number of cardinal connections
        /// </summary>
        public int CardinalConnectionCount => GetAdjacencies(true).Count;

        /// <summary>
        /// Get the number of diagional connections
        /// </summary>
        public int DiagonalConnectionCount => GetAdjacencies(false).Count;

        /// <summary>
        /// Get the total number of connections.
        /// </summary>
        public int ConnectionCount => GetAdjacencies(true).Count + GetAdjacencies(false).Count;

        public bool HasConnection(Direction direction) => _connections[(int)direction];

        /// <summary>
        /// Gets the direction of the only cardinal/diagonal connection.
        /// Assumes there is exactly one cardinal/diagonal connection.
        /// </summary>
        /// <param name="cardinal"></param>
        /// <returns></returns>
        public Direction GetSingleConnection(bool cardinal = true)
        {
            List<Direction> foundConnections = GetAdjacencies(cardinal);
            return foundConnections[0];
        }

        /// <summary>
        /// Get the direction of the only cardinal/diagonal non connection.
        /// Assumes there is exactly one cardinal/diagonal non connection
        /// </summary>
        /// <param name="cardinal"></param>
        /// <returns></returns>
        public Direction GetSingleNonConnection(bool cardinal = true)
        {
            List<Direction> foundConnections = GetAdjacencies(cardinal);
            List<Direction> directions = cardinal ? TileHelper.CardinalDirections() : TileHelper.DiagonalDirections();
            List<Direction> missingConnections =
                directions.Where(direction => !foundConnections.Contains(direction)).ToList();

            return missingConnections[0];
        }

        /// <summary>
        /// Assuming the cardinals/diagonal have exactly two adjacent connections, gets the direction between them.
        /// </summary>
        /// <param name="cardinal"></param>
        /// <returns></returns>
        public Direction GetDirectionBetweenTwoConnections(bool cardinal = true)
        {
            List<Direction> foundConnections = GetAdjacencies(cardinal);

            return cardinal
                ? TileHelper.GetDiagonalBetweenTwoCardinals(foundConnections[0], foundConnections[1])
                : TileHelper.GetCardinalBetweenTwoDiagonals(foundConnections[0], foundConnections[1]);
        }

        /// <summary>
        /// Updates the map, and returns whether there was a change
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SetConnection(Direction direction, bool isConnected)
        {
            bool changed = _connections[(int)direction] != isConnected;
            _connections[(int)direction] = isConnected;
            return changed;
        }

        public List<Direction> GetAdjacencies(bool cardinal)
        {
            // Are we getting adjacencies for cardinal or diagonal directions?
            List<int> directionIndexes = cardinal ?
                TileHelper.CardinalDirections().ConvertAll(direction => (int)direction) :
                TileHelper.DiagonalDirections().ConvertAll(direction => (int)direction);

            // Loop through each index in direction indexes, pick those that exist and cast them to the Direction enum.
            return (from index in directionIndexes where _connections[index] select (Direction)index).ToList();
        }

        public void DeserializeFromByte(byte bytemap)
        {
            BitArray bits = new(new byte[] { bytemap });
            bool[] adjacencyData = new bool[8];

            for (int i = 0; i < bits.Length; i++)
            {
                adjacencyData[i] = bits[i];
            }

            _connections = adjacencyData;
        }

        public byte SerializeToByte()
        {
            int sum = 0;
            for (int i = 1, direction = 0; i < 256; i *= 2, direction++)
            {
                if (_connections[direction])
                {
                    sum += i;
                }
            }

            return (byte)sum;
        }

        public override string ToString()
        {
            return $"{_connections}";
        }
    }
}

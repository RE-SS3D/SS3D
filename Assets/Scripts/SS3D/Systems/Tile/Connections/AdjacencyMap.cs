using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Used for storing which neighbouring objects the primary object is connected to. Used for connecting walls, tables, carpets etc together by changing their mesh.
    /// </summary>
    public class AdjacencyMap
    {
        //Stores an array of which of the 8 surrounding tiles have a connection. Order assumed to match Direction enum values.
        private AdjacencyData[] _connections;

        /// <summary>
        /// Get the number of cardinal connections
        /// </summary>
        public int CardinalConnectionCount => GetAdjacencies(true).Count;

        /// <summary>
        /// Get the number of diagional connections
        /// </summary>
        public int DiagonalConnectionCount => GetAdjacencies(false).Count;

        public AdjacencyMap()
        {
            _connections = new [] {
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

        public bool HasConnection(Direction direction)
        {
            return _connections[(int)direction].Exists;
        }


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
        public bool SetConnection(Direction direction, AdjacencyData data)
        {
            bool changed = !data.Equals(_connections[(int) direction]);
            if (changed)
            {
                _connections[(int) direction] = data;
            }

            return changed;
        }

        public List<Direction> GetAdjacencies(bool cardinal)
        {
            //Are we getting adjacencies for cardinal or diagonal directions?
            List<int> directionIndexes = cardinal ?
                TileHelper.CardinalDirections().Select(direction => (int)direction).ToList() : 
                TileHelper.DiagonalDirections().Select(direction => (int)direction).ToList();
            //Loop through each index in direction indexes, pick those that exist and cast them to the Direction enum.
            return (from index in directionIndexes where _connections[index].Exists select (Direction) index).ToList();
        }
        
        public void DeserializeFromByte(byte bytemap)
        {
            BitArray bits = new(new[] { bytemap });
            AdjacencyData[] adjacencyData = new AdjacencyData[8];

            for (int i = 0; i < bits.Length; i++)
            {
                adjacencyData[i] = new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, bits[i]);
            }

            _connections = adjacencyData;
        }

        public byte SerializeToByte()
        {
            int sum = 0;
            for (int i = 1, direction = 0; i < 256; i *= 2, direction++)
            {
                if (_connections[direction].Exists)
                {
                    sum += i;
                }
            }

            return (byte) sum;
        }

        public override string ToString()
        {
            return $"{_connections}";
        }
    }
}
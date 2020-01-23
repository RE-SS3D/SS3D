using UnityEngine;
using System.Collections;

namespace TileMap {
    /**
     * These classes are getting very specific...
     * This one is the same as SimpleAdjacencyConnector, but it has specific unique I connections
     * to a more generic type.
     * This is currently used for glass walls, where when connecting to any other wall we need to
     * 'close' the glass window.
     */
    [RequireComponent(typeof(MeshFilter))]
    public class TieredIAdjacencyConnector : MonoBehaviour, AdjacencyConnector
    {
        public enum TileLayer
        {
            Turf,
            Fixture,
        }

        // Id that adjacent objects must be to count. If null, any id is accepted
        public string id;
        public string genericType;

        [Header("Meshes")]
        // A mesh where no edges are connected
        public Mesh o;
        // A mesh where the east edge is connected
        public Mesh c;
        // A mesh where east and west edges are connected
        public Mesh i;
        // A mesh where west connects to same type, and east connects to the generic type
        public Mesh iBorder;
        // A mesh for a single I tile between two generic ones
        public Mesh iAlone;
        // A mesh where the south and west edges are connected
        public Mesh l;
        // A mesh where the north, south, and east edge is connected
        public Mesh t;
        // A mesh where all edges are connected
        public Mesh x;

        public void OnEnable()
        {
            SetMeshAndDirection();
        }

        /**
         * When a single adjacent turf is updated
         */
        public void UpdateSingle(Direction direction, TileDefinition tile)
        {
            UpdateSingleConnection(direction, tile);
            SetMeshAndDirection();
        }

        /**
         * When all (or a significant number) of adjacent turfs update.
         * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
         */
        public void UpdateAll(TileDefinition[] tiles)
        {
            for (int i = 0; i < tiles.Length; i++) {
                UpdateSingleConnection((Direction)i, tiles[i]);
            }
            SetMeshAndDirection();
        }

        public void Awake()
        {
            filter = GetComponent<MeshFilter>();
        }

        /**
         * Adjusts the connections value based on the given new tile
         */
        private void UpdateSingleConnection(Direction direction, TileDefinition tile)
        {
            bool isGeneric = (tile.turf && (tile.turf.genericType == genericType || genericType == null)) || (tile.fixture && (tile.fixture.genericType == genericType || genericType == null));
            bool isSpecific = (tile.turf && (tile.turf.id == id || id == null)) || (tile.fixture && (tile.fixture.id == id || id == null));

            // Set the direction bit to isConnected (1 or 0)
            connections &= (byte)~(1 << (int)direction);
            connections |= (byte)((isGeneric ? 1 : 0) << (int)direction);

            specificConnections &= (byte)~(1 << (int)direction);
            specificConnections |= (byte)((isSpecific ? 1 : 0) << (int)direction);
        }

        private void SetMeshAndDirection()
        {
            // Count number of connections along cardinal (which is all that we use atm)
            int north = Adjacent(Direction.North);
            int east = Adjacent(Direction.East);
            int south = Adjacent(Direction.South);
            int west = Adjacent(Direction.West);

            int numConnections = north + east + south + west;

            float rotation = 0.0f;
            Mesh mesh;
            if (numConnections == 0)
                mesh = o;
            else if (numConnections == 1) {
                mesh = c;
                rotation = south * 90 + west * 180 + north * 270;
            }
            else if (numConnections == 2) {
                // If north and south are both 1 or 0, must be a line.
                if (north == south) {
                    // Check for specific connections
                    int numSpecific = SpecificAdjacent(Direction.North)
                        + SpecificAdjacent(Direction.East)
                        + SpecificAdjacent(Direction.South)
                        + SpecificAdjacent(Direction.West);

                    if (numSpecific == 1) {
                        mesh = iBorder;
                        rotation = SpecificAdjacent(Direction.North) * 90 + SpecificAdjacent(Direction.East) * 180 + SpecificAdjacent(Direction.South) * 270;
                    }
                    else {
                        mesh = numSpecific == 2 ? i : iAlone;
                        rotation = east > 0 ? 0 : 90;
                    }
                }
                else {
                    mesh = l;
                    rotation = south > 0 ? east > 0 ? 0 : 90 : east > 0 ? 270 : 180;
                }
            }
            else if (numConnections == 3) {
                mesh = t;
                rotation = (1 - north) * 90 + (1 - east) * 180 + (1 - south) * 270;
            }
            else
                mesh = x;

            if (filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = mesh;
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, rotation, transform.localRotation.eulerAngles.z);
        }

        /**
         * Returns 0 if no adjacency, or 1 if there is.
         */
        private int Adjacent(Direction direction)
        {
            return (connections >> (int)direction) & 0x1;
        }

        /**
         * Returns 0 if no adjacency, or 1 if there is.
         */
        private int SpecificAdjacent(Direction direction)
        {
            return (specificConnections >> (int)direction) & 0x1;
        }


        // A bitfield of connections. Total of 8 connections -> 8 bits, ascending order with direction.
        private byte connections = 0;
        private byte specificConnections = 0;

        private MeshFilter filter;
    }
}
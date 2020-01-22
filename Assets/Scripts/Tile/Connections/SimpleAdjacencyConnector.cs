using UnityEngine;
using System.Collections;

namespace TileMap {
    /**
     * A simple adjacency connector uses 6 meshes and checks for all possible scenarios
     * which stem from only the 4 cardinal directions.
     */
    [RequireComponent(typeof(MeshFilter))]
    public class SimpleAdjacencyConnector : MonoBehaviour, AdjacencyConnector
    {
        public enum TileLayer
        {
            Turf,
            Fixture,
        }

        // Id that adjacent objects must be to count. If null, any id is accepted
        public string type;

        [Header("Meshes")]
        // A mesh where no edges are connected
        public Mesh o;
        // A mesh where the east edge is connected
        public Mesh c;
        // A mesh where east and west edges are connected
        public Mesh i;
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
        public void UpdateSingle(Direction direction, ConstructibleTile tile)
        {
            UpdateSingleConnection(direction, tile);
            SetMeshAndDirection();
        }

        /**
         * When all (or a significant number) of adjacent turfs update.
         * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
         */
        public void UpdateAll(ConstructibleTile[] tiles)
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
        private void UpdateSingleConnection(Direction direction, ConstructibleTile tile)
        {
            bool isConnected = (tile.turf && (tile.turf.genericType == type || type == null)) || (tile.fixture && (tile.fixture.genericType == type || type == null));

            // Set the direction bit to isConnected (1 or 0)
            connections &= (byte)~(1 << (int)direction);
            connections |= (byte)((isConnected ? 1 : 0) << (int)direction);
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
                    mesh = i;
                    rotation = east > 0 ? 0 : 90;
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

        // A bitfield of connections. Total of 8 connections -> 8 bits, ascending order with direction.
        private byte connections = 0;
        private MeshFilter filter;
    }
}
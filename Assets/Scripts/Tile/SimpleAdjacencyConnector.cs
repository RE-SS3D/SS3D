using UnityEngine;
using System.Collections;

namespace TileMap {
    [RequireComponent(typeof(MeshFilter))]
    public class SimpleAdjacencyConnector : AdjacencyConnector
    {
        public enum TileLayer
        {
            Turf,
            Fixture,
        }

        // TODO: Editor Category
        // A mesh where the south and west edges are connected
        public Mesh corner;
        // A mesh where east and west edges are connected
        public Mesh i;
        // A mesh where the east edge is connected
        public Mesh c;
        // A mesh where the north, south, and east edge is connected
        public Mesh t;
        // A mesh where all edges are connected
        public Mesh x;
        // A mesh where no edges are connected
        public Mesh none;

        // The layer on which to count connections
        public TileLayer layer;

        // Id that adjacent objects must be to count. If null, any id is accepted
        public string id;

        public void OnEnable()
        {
            SetMeshAndDirection();
        }

        /**
         * When a single adjacent turf is updated
         */
        public override void UpdateSingle(Direction direction, ConstructibleTile tile)
        {
            UpdateSingleConnection(direction, tile);
            SetMeshAndDirection();
        }

        /**
         * When all (or a significant number) of adjacent turfs update.
         * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
         */
        public override void UpdateAll(ConstructibleTile[] tiles)
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
            string adjacentId;
            if (layer == TileLayer.Turf)
                adjacentId = tile.turf?.id;
            else // if (layer == TileLayer.Fixture)
                adjacentId = tile.fixture?.id;

            bool isConnected = adjacentId != null && (adjacentId == id || id == null);

            // Set the direction bit to isConnected (1 or 0)
            connections &= (byte)~(1 << (int)direction);
            connections |= (byte)((isConnected ? 1 : 0) << (int)direction);
        }

        private void SetMeshAndDirection()
        {
            // Count number of connections along cardinal (which is all that we use atm)
            int north = (connections >> (int)Direction.North) & 0x1;
            int east = (connections >> (int)Direction.East) & 0x1;
            int south = (connections >> (int)Direction.South) & 0x1;
            int west = (connections >> (int)Direction.West) & 0x1;

            int numConnections = north + east + south + west;

            float rotation = 0.0f;
            Mesh mesh;
            if (numConnections == 0)
                mesh = none;
            else if (numConnections == 1) {
                mesh = c;
                rotation = north * 90 + west * 180 + south * 270;
            }
            else if (numConnections == 2) {
                // If north and south are both 1 or 0, must be a line.
                if (north == south) {
                    mesh = i;
                    rotation = east > 0 ? 0 : 90;
                }
                else {
                    mesh = corner;
                    rotation = south > 0 ? east > 0 ? 0 : 270 : east > 0 ? 90 : 180;
                }
            }
            else if (numConnections == 3) {
                mesh = t;
                rotation = (1 - north) * 90 + (1 - west) * 180 + (1 - south) * 270;
            }
            else
                mesh = x;

            if (filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = mesh;
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, rotation, transform.localRotation.eulerAngles.z);
        }

        // A bitfield of connections. Total of 8 connections -> 8 bits, ascending order with direction.
        private byte connections = 0;
        private MeshFilter filter;
    }
}
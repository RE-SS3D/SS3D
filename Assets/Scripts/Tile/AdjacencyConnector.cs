using UnityEngine;
using System.Collections;

namespace TileMap {
    /**
     * This component gets notified whenever the same layer on an adjacent tile is modified
     * Warning: Lots of bit logic
     */
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class AdjacencyConnector : MonoBehaviour
    {
        public enum TileLayer
        {
            Turf,
            Fixture,
        }

        // TODO: Editor Category
        // A mesh where the northwest edges aren't connected
        public Mesh corner;
        // A mesh where north and south edges aren't connected
        public Mesh line;
        // A mesh where the north edge is not connected
        public Mesh single;
        // A mesh where the west, north, and east edge is not connected
        public Mesh tri;
        // A mesh where no edge is connected (all bare faces)
        public Mesh all;
        // A mesh where all edges are connected (no bare faces)
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
            for(int i = 0; i < tiles.Length; i++) {
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
            int north = (connections >> (int)Direction.North)& 0x1;
            int east = (connections >> (int)Direction.East) & 0x1;
            int south = (connections >> (int)Direction.South) & 0x1;
            int west = (connections >> (int)Direction.West) & 0x1;

            int numConnections = north + east + south + west;

            float rotation = 0.0f;
            Mesh mesh;
            if (numConnections == 0)
                mesh = all;
            else if (numConnections == 1) { 
                mesh = single;
                rotation = east * 90 + south * 180 + west * 270;
            }
            else if (numConnections == 2) { 
                // If north and south are both 1 or 0, must be a line.
                if(north == south) {
                    mesh = line;
                    rotation = east > 0 ? 90 : 0;
                }
                else {
                    mesh = corner;
                    rotation = north > 0 ? west > 0 ? 0 : 90 : east > 0 ? 180 : 270;
                }
            }
            else if (numConnections == 3) {
                mesh = tri;
                rotation = (1 - west) * 90 + (1 - north) * 180 + (1 - east) * 270;
            }
            else
                mesh = none;

            if(filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = mesh;
            transform.Rotate(new Vector3(0, rotation - prevRotation, 0), Space.World);
            prevRotation = rotation;
        }

        // A bitfield of connections. Total of 8 connections -> 8 bits, ascending order with direction.
        private byte connections = 0;
        private float prevRotation = 0;
        private MeshFilter filter;
    }
}

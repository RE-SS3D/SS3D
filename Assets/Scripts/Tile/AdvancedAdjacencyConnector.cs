using UnityEngine;
using System.Collections;

namespace TileMap
{
    /**
     * The advanced adjacency connector considers
     * all the cases of the SimpleAdjacencyConnector, combined with
     * considering diagonal cardinals that turn inside corners into outside corners, but
     * not outside corners to inside corners, e.g.:
     *   An X mesh will be fully solid, unless there is an unconnected diagonal, in which case a corner
     *   is made.
     *   An O mesh, however, will not change corners regardless of connected or unconnected diagonals.
     * This increases the number of required meshes to 14.
     */
    public class AdvancedAdjacencyConnector : AdjacencyConnector
    {
        // Id to match against
        public string type;

        [Header("Meshes")]
        // A mesh where no edges are connected
        public Mesh o;
        // A mesh where the east edge is connected
        public Mesh c;
        // A mesh where east and west edges are connected
        public Mesh i;

        // A mesh where the south and west edges are connected, no corners
        public Mesh lNone;
        // A mesh where the south and west edges are connected, and ne is a corner
        public Mesh lSingle;

        // A mesh where the north, south, and east edge is connected, no corners
        public Mesh tNone;
        // A mesh where the north, south, and east edge is connected, southeast is a corner
        public Mesh tSingleRight;
        // A mesh where the north, south, and east edge is connected, northeast is a corner
        public Mesh tSingleLeft;
        // A mesh where north, south, and east is connected, northeast and southeast are corners
        public Mesh tDouble;

        // A mesh where all edges are connected, no corners
        public Mesh xNone;
        // A mesh where all edges are connected, southeast is a corner
        public Mesh xSingle;
        // A mesh where all edges connected, southeast and northeast corners
        public Mesh xSide;
        // A mesh where all edges connected, southeast and northwest corners
        public Mesh xOpposite;
        // A mesh where all edges connected, all but northwest are corners
        public Mesh xTriple;
        // A mesh where all edges connected, all corners
        public Mesh xQuad;

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
            bool isConnected = (tile.turf && (tile.turf.genericType == type || type == null)) || (tile.fixture && (tile.fixture.genericType == type || type == null));

            // Set the direction bit to isConnected (1 or 0)
            connections &= (byte)~(1 << (int)direction);
            connections |= (byte)((isConnected ? 1 : 0) << (int)direction);
        }

        /**
         * Sorry about the length, but each part is pretty well seperated, so it shouldn't be too difficult to
         * follow hopefully (-nonanon).
         * 
         * This code takes the information about what tiles are adjacent (each of the 8 directions either has an adjacent connectable or not),
         * and converts this into the mesh to use, along with a direction to rotate the mesh in.
         */
        private void SetMeshAndDirection()
        {
            // Count number of connections along cardinal, to determine which 'outer' mesh we use of O, C, I/L, T, X
            int north = Adjacent(Direction.North);
            int east = Adjacent(Direction.East);
            int south = Adjacent(Direction.South);
            int west = Adjacent(Direction.West);

            int numConnections = north + east + south + west;

            float rotation = 0.0f;
            Mesh mesh;
            if(numConnections == 0)
            {
                mesh = o;
            }
            else if(numConnections == 1)
            {
                mesh = c;
                rotation = south * 90 + west * 180 + north * 270;
            }
            else if(numConnections == 2)
            {
                // If north and south are both 1 or 0, must be a line.
                if (north == south) {
                    mesh = i;
                    rotation = east > 0 ? 0 : 90;
                }
                else {
                    // Determine lSolid or lCorner by finding whether the area between the two connections is filled
                    // We check for if any of the following bitfields matches the connection bitfield
                    // N+NE+E = 0/1/2, E+SE+S = 2/3/4, S+SW+W = 4/5/6, W+NW+N = 6/7/0
                    bool isFilled = (connections & 0b00000111) == 0b00000111 || (connections & 0b00011100) == 0b00011100 || (connections & 0b01110000) == 0b01110000 || (connections & 0b11000001) == 0b11000001;
                    mesh = isFilled ? lNone : lSingle;
                    rotation = south > 0 ? east > 0 ? 0 : 90 : east > 0 ? 270 : 180;
                }
            }
            else if(numConnections == 3)
            {
                // We make another bitfield (noticing a pattern?). 0x0 means no fills, 0x1 means right corner filled, 0x2 means left corner filled,
                // therefore both corners filled = 0x3.
                int corners = ((1 - north) * 2 | (1 - east)) * Adjacent(Direction.SouthWest)
                            + ((1 - east) * 2 | (1 - south)) * Adjacent(Direction.NorthWest)
                            + ((1 - south) * 2 | (1 - west)) * Adjacent(Direction.NorthEast)
                            + ((1 - west) * 2 | (1 - north)) * Adjacent(Direction.SouthEast);
                mesh = corners == 0 ? tDouble
                    : corners == 1 ? tSingleLeft
                    : corners == 2 ? tSingleRight
                    : tNone;

                rotation = (1 - north) * 90 + (1 - east) * 180 + (1 - south) * 270;
            }
            else
            {
                // Ok, the really stupid part is that this is effectively a duplicate of the above code, where we now use corners instead of sides
                // INCLUDING ROTATION.
                int northEast = Adjacent(Direction.NorthEast);
                int northWest = Adjacent(Direction.NorthWest);
                int southEast = Adjacent(Direction.SouthEast);
                int southWest = Adjacent(Direction.SouthWest);

                int numCorners = northEast + northWest + southEast + southWest;
                switch (numCorners) {
                    case 0:
                        mesh = xNone;
                        break;
                    case 1:
                        mesh = xSingle;
                        rotation = southWest * 90 + northEast * 180 + northWest * 270;
                        break;
                    case 2:
                        if(northEast == southWest) {
                            mesh = xOpposite;
                            rotation = northEast > 0 ? 0 : 90;
                        }
                        else {
                            mesh = xSide;
                            rotation = southEast > 0 ? southWest > 0 ? 0 : 90 : northEast > 0 ? 180 : 270;
                        }
                        break;
                    case 3:
                        mesh = xTriple;
                        rotation = (1 - southWest) * 90 + (1 - southEast) * 180 + (1 - northEast) * 270;
                        break;
                    default:
                        mesh = xQuad;
                        break;
                }
            }

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
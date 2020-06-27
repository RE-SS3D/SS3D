using UnityEngine;
using System.Collections;
using SS3D.Engine.Tiles.Connections;
using System;

namespace SS3D.Engine.Tiles.Connections
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
     * Only used by wall, reinforced walls, and tables currently.
     */
    [RequireComponent(typeof(MeshFilter))]
    public class AdvancedAdjacencyConnector : MonoBehaviour, AdjacencyConnector
    {
        // Id to match against
        public string type;
        public int LayerIndex { get; set; }

        [Header("Meshes")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;

        [Tooltip("A mesh where north connects to same type")]
        public Mesh c;

        [Tooltip("A mesh where north and south edges are connected")]
        public Mesh i;

        [Tooltip("A mesh where the north and east edges are connected, no corners")]
        public Mesh lNone;
        [Tooltip("A mesh where the north and east edges are connected, and NE is a corner")]
        public Mesh lSingle;

        [Tooltip("A mesh where the north, east, and west edges are connected, no corners")]
        public Mesh tNone;
        [Tooltip("A mesh where the north, east, and west edges are connected, NW is a corner")]
        public Mesh tSingleRight;
        [Tooltip("A mesh where the north, east, and west edges are connected, NE is a corner")]
        public Mesh tSingleLeft;
        [Tooltip("A mesh where north, east, and west edges are connected, NW & NE are corners")]
        public Mesh tDouble;

        [Tooltip("A mesh where all edges are connected, no corners")]
        public Mesh xNone;
        [Tooltip("A mesh where all edges are connected, SW is a corner")]
        public Mesh xSingle;
        [Tooltip("A mesh where all edges are connected, SW & SW are corners")]
        public Mesh xSide;
        [Tooltip("A mesh where all edges are connected, NW & SE are corners")]
        public Mesh xOpposite;
        [Tooltip("A mesh where all edges are connected, all but NE are corners")]
        public Mesh xTriple;
        [Tooltip("A mesh where all edges are connected, all corners")]
        public Mesh xQuad;

        public GameObject[] viewObstacles;
        public bool opaque;
        /**
         * When a single adjacent turf is updated
         */
        public void UpdateSingle(Direction direction, TileDefinition tile)
        {
            if (UpdateSingleConnection(direction, tile))
                UpdateMeshAndDirection();
        }

        /**
         * When all (or a significant number) of adjacent turfs update.
         * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
         */
        public void UpdateAll(TileDefinition[] tiles)
        {
            bool changed = false;
            for (int i = 0; i < tiles.Length; i++)
            {
                changed |= UpdateSingleConnection((Direction)i, tiles[i]);
            }

            if (changed)
                UpdateMeshAndDirection();
        }

        public void Awake() => filter = GetComponent<MeshFilter>();
        public void OnEnable() => UpdateMeshAndDirection();

        /**
         * Adjusts the connections value based on the given new tile.
         * Returns whether value changed.
         */
        private bool UpdateSingleConnection(Direction direction, TileDefinition tile)
        {
            int index = LayerIndex;
            if (index == 0)
                index = 17; // Hardcoded to the FixtureMain layer until I got a better solution for this. Is needed to make Airlocks connect to walls

            bool isConnected = (tile.plenum && (tile.plenum.genericType == type || type == null));
            isConnected |= (tile.turf && (tile.turf.genericType == type || type == null));
            if (tile.fixtures != null)
                isConnected = isConnected || (tile.fixtures.GetFixtureAtLayerIndex(index) && (tile.fixtures.GetFixtureAtLayerIndex(index).genericType == type || type == null));
            return adjacents.UpdateDirection(direction, isConnected);
        }

        /**
         * Sorry about the length, but each part is pretty well seperated, so it shouldn't be too difficult to
         * follow hopefully (-nonanon).
         * 
         * This code takes the information about what tiles are adjacent (each of the 8 directions either has an adjacent connectable or not),
         * and converts this into the mesh to use, along with a direction to rotate the mesh in.
         */
        private void UpdateMeshAndDirection()
        {
            // Count number of connections along cardinal, to determine which 'outer' mesh we use of O, C, I/L, T, X
            var cardinalInfo = adjacents.GetCardinalInfo();

            float rotation = 0.0f;
            Mesh mesh;
            if (cardinalInfo.IsO())
            {
                mesh = o;
            }
            else if (cardinalInfo.IsC())
            {
                mesh = c;
                rotation = DirectionHelper.AngleBetween(Direction.North, cardinalInfo.GetOnlyPositive());

                if(opaque)
                {
                    viewObstacles[0].SetActive(false);
                    viewObstacles[1].SetActive(false);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(false);
                }
            }
            else if (cardinalInfo.IsI())
            {
                mesh = i;
                rotation = OrientationHelper.AngleBetween(Orientation.Vertical, cardinalInfo.GetFirstOrientation());

                if(opaque)
                {
                    viewObstacles[0].SetActive(false);
                    viewObstacles[1].SetActive(false);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(true);
                }
            }
            else if (cardinalInfo.IsL())
            {
                // Determine lSolid or lCorner by finding whether the area between the two connections is filled
                // We check for if any of the following bitfields matches the connection bitfield
                // N+NE+E = 0/1/2, E+SE+S = 2/3/4, S+SW+W = 4/5/6, W+NW+N = 6/7/0
                bool isFilled = (adjacents.Connections & 0b00000111) == 0b00000111 || (adjacents.Connections & 0b00011100) == 0b00011100 || (adjacents.Connections & 0b01110000) == 0b01110000 || (adjacents.Connections & 0b11000001) == 0b11000001;
                mesh = isFilled ? lSingle : lNone;
                rotation = DirectionHelper.AngleBetween(Direction.NorthEast, cardinalInfo.GetCornerDirection());

                if(opaque)
                {
                    viewObstacles[0].SetActive(false);
                    viewObstacles[1].SetActive(true);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(false);
                }
            }
            else if (cardinalInfo.IsT())
            {
                // We make another bitfield (noticing a pattern?). 0x0 means no fills, 0x1 means right corner filled, 0x2 means left corner filled,
                // therefore both corners filled = 0x3.
                int corners = ((1 - cardinalInfo.north) * 2 | (1 - cardinalInfo.east)) * adjacents.Adjacent(Direction.SouthWest)
                            + ((1 - cardinalInfo.east) * 2 | (1 - cardinalInfo.south)) * adjacents.Adjacent(Direction.NorthWest)
                            + ((1 - cardinalInfo.south) * 2 | (1 - cardinalInfo.west)) * adjacents.Adjacent(Direction.NorthEast)
                            + ((1 - cardinalInfo.west) * 2 | (1 - cardinalInfo.north)) * adjacents.Adjacent(Direction.SouthEast);
                mesh = corners == 0 ? tNone
                    : corners == 1 ? tSingleLeft
                    : corners == 2 ? tSingleRight
                    : tDouble;

                rotation = DirectionHelper.AngleBetween(Direction.South, cardinalInfo.GetOnlyNegative());

                if(opaque)
                {
                    viewObstacles[0].SetActive(true);
                    viewObstacles[1].SetActive(true);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(corners>=3);
                }
            }
            else
            {
                // This sneaky piece of code uses the cardinal info to store diagonals by rotating everything -45 degrees
                // NE -> N, SW -> S, etc.
                var diagonals = new AdjacencyBitmap.CardinalInfo((byte)(adjacents.Connections >> 1));

                switch (diagonals.numConnections)
                {
                    case 0:
                        mesh = xNone;
                        break;
                    case 1:
                        mesh = xSingle;
                        rotation = DirectionHelper.AngleBetween(Direction.South, diagonals.GetOnlyPositive());
                        break;
                    case 2:
                        if (diagonals.north == diagonals.south)
                        {
                            mesh = xOpposite;
                            rotation = OrientationHelper.AngleBetween(Orientation.Horizontal, diagonals.GetFirstOrientation());
                        }
                        else
                        {
                            mesh = xSide;
                            rotation = DirectionHelper.AngleBetween(Direction.SouthEast, diagonals.GetCornerDirection());
                        }
                        break;
                    case 3:
                        mesh = xTriple;
                        rotation = DirectionHelper.AngleBetween(Direction.North, diagonals.GetOnlyNegative());
                        break;
                    default:
                        mesh = xQuad;
                        break;
                        
                }
                if(opaque)
                {
                    viewObstacles[0].SetActive(true);
                    viewObstacles[1].SetActive(true);
                    viewObstacles[2].SetActive(true);
                    viewObstacles[3].SetActive(true);
                }
            }

            if (filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = mesh;
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, rotation, transform.localRotation.eulerAngles.z);
        }

        // A bitfield of connections. Total of 8 connections -> 8 bits, ascending order with direction.
        private AdjacencyBitmap adjacents = new AdjacencyBitmap();
        private MeshFilter filter;
    }
}
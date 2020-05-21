using UnityEngine;
using System.Collections;
using SS3D.Engine.Tiles.Connections;
using System;

namespace SS3D.Engine.Tiles.Connections
{
    /**
     * These classes are getting very specific...
     * This one is the same as AdvanceseAdjacencyConnector, but it has specific unique I connections
     * to a more generic type.
     * This is currently used for glass walls, where when connecting to any other wall we need to
     * 'close' the glass window.
     */
    [RequireComponent(typeof(MeshFilter))]
    public class TieredAdvancedAdjacencyConnector : MonoBehaviour, AdjacencyConnector
    {
        // Id to match against
        public string id;
        public string genericType;
        public FixtureLayers Layer { get; set; }

        [Header("Meshes")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;

        [Tooltip("A mesh where east connects to same type")]
        public Mesh c;

        [Tooltip("A mesh where east and west edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where west connects to same type, and east connects to the generic type")]
        public Mesh iBorder;
        [Tooltip("A mesh for a single I tile between two generic ones")]
        public Mesh iAlone;

        [Tooltip("A mesh where the south and west edges are connected, no corners")]
        public Mesh lNone;
        [Tooltip("A mesh where the south and west edges are connected, and ne is a corner")]
        public Mesh lSingle;

        [Tooltip("A mesh where the north, south, and east edge is connected, no corners")]
        public Mesh tNone;
        [Tooltip("A mesh where the north, south, and east edge is connected, southeast is a corner")]
        public Mesh tSingleRight;
        [Tooltip("A mesh where the north, south, and east edge is connected, northeast is a corner")]
        public Mesh tSingleLeft;
        [Tooltip("A mesh where north, south, and east is connected, northeast and southeast are corners")]
        public Mesh tDouble;

        [Tooltip("A mesh where all edges are connected, no corners")]
        public Mesh xNone;
        [Tooltip("A mesh where all edges are connected, southeast is a corner")]
        public Mesh xSingle;
        [Tooltip("A mesh where all edges connected, southeast and northeast corners")]
        public Mesh xSide;
        [Tooltip("A mesh where all edges connected, southeast and northwest corners")]
        public Mesh xOpposite;
        [Tooltip("A mesh where all edges connected, all but northwest are corners")]
        public Mesh xTriple;
        [Tooltip("A mesh where all edges connected, all corners")]
        public Mesh xQuad;

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
            for (int i = 0; i < tiles.Length; i++) {
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
            int index = (int)Layer;

            bool isGeneric = (tile.turf && (tile.turf.genericType == genericType || genericType == null));
            if (tile.fixtures != null)
                isGeneric = isGeneric || (tile.fixtures[index] && (tile.fixtures[index].genericType == genericType || genericType == null));

            bool isSpecific = (tile.turf && (tile.turf.id == id || id == null));
            if (tile.fixtures != null)
                isSpecific = isSpecific || (tile.fixtures[index] && (tile.fixtures[index].id == id || id == null));

            bool changed = generalAdjacents.UpdateDirection(direction, isGeneric, true);
            changed |= specificAdjacents.UpdateDirection(direction, isSpecific, true);

            return changed;
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
            var generalCardinals = generalAdjacents.GetCardinalInfo();

            float rotation = 0.0f;
            Mesh mesh;
            if(generalCardinals.IsO())
            {
                mesh = o;
            }
            else if(generalCardinals.IsC())
            {
                mesh = c;
                rotation = DirectionHelper.AngleBetween(Direction.East, generalCardinals.GetOnlyPositive());
            }
            else if(generalCardinals.IsI())
            {
                var specificCardinals = specificAdjacents.GetCardinalInfo();

                if (specificCardinals.numConnections == 1)
                {
                    mesh = iBorder;
                    rotation = DirectionHelper.AngleBetween(Direction.West, specificCardinals.GetOnlyPositive());
                }
                else
                {
                    mesh = specificCardinals.numConnections == 2 ? i : iAlone;
                    rotation = OrientationHelper.AngleBetween(Orientation.Horizontal, generalCardinals.GetFirstOrientation());
                }
            }
            else if(generalCardinals.IsL())
            {
                // Determine lSolid or lCorner by finding whether the area between the two connections is filled
                // We check for if any of the following bitfields matches the connection bitfield
                // N+NE+E = 0/1/2, E+SE+S = 2/3/4, S+SW+W = 4/5/6, W+NW+N = 6/7/0
                bool isFilled = (generalAdjacents.Connections & 0b00000111) == 0b00000111 || (generalAdjacents.Connections & 0b00011100) == 0b00011100 || (generalAdjacents.Connections & 0b01110000) == 0b01110000 || (generalAdjacents.Connections & 0b11000001) == 0b11000001;
                mesh = isFilled ? lSingle : lNone;
                rotation = DirectionHelper.AngleBetween(Direction.SouthEast, generalCardinals.GetCornerDirection());
            }
            else if(generalCardinals.IsT())
            {
                // We make another bitfield (noticing a pattern?). 0x0 means no fills, 0x1 means right corner filled, 0x2 means left corner filled,
                // therefore both corners filled = 0x3.
                int corners = ((1 - generalCardinals.north) * 2 | (1 - generalCardinals.east)) * generalAdjacents.Adjacent(Direction.SouthWest)
                            + ((1 - generalCardinals.east) * 2 | (1 - generalCardinals.south)) * generalAdjacents.Adjacent(Direction.NorthWest)
                            + ((1 - generalCardinals.south) * 2 | (1 - generalCardinals.west)) * generalAdjacents.Adjacent(Direction.NorthEast)
                            + ((1 - generalCardinals.west) * 2 | (1 - generalCardinals.north)) * generalAdjacents.Adjacent(Direction.SouthEast);
                mesh = corners == 0 ? tNone
                    : corners == 1 ? tSingleLeft
                    : corners == 2 ? tSingleRight
                    : tDouble;

                rotation = DirectionHelper.AngleBetween(Direction.West, generalCardinals.GetOnlyNegative());
            }
            else
            {
                // This sneaky piece of code uses the cardinal info to store diagonals by rotating everything -45 degrees
                // NE -> N, SW -> S, etc.
                var diagonals = new AdjacencyBitmap.CardinalInfo((byte)(generalAdjacents.Connections >> 1));

                switch (diagonals.numConnections) {
                    case 0:
                        mesh = xNone;
                        break;
                    case 1:
                        mesh = xSingle;
                        rotation = DirectionHelper.AngleBetween(Direction.West, diagonals.GetOnlyPositive());
                        break;
                    case 2:
                        if(diagonals.north == diagonals.south) {
                            mesh = xOpposite;
                            rotation = OrientationHelper.AngleBetween(Orientation.Vertical, diagonals.GetFirstOrientation());
                        }
                        else {
                            mesh = xSide;
                            rotation = DirectionHelper.AngleBetween(Direction.SouthWest, diagonals.GetCornerDirection());
                        }
                        break;
                    case 3:
                        mesh = xTriple;
                        rotation = DirectionHelper.AngleBetween(Direction.East, diagonals.GetOnlyNegative());
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

        // A bitfield of connections. Total of 8 connections -> 8 bits, ascending order with direction.
        private AdjacencyBitmap generalAdjacents = new AdjacencyBitmap();
        private AdjacencyBitmap specificAdjacents = new AdjacencyBitmap();
        private MeshFilter filter;
    }
}
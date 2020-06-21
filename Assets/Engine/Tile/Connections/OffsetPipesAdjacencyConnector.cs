using UnityEngine;
using System.Collections;
using SS3D.Engine.Tiles.Connections;
using System;

namespace SS3D.Engine.Tiles.Connections
{
    /**
     * The offset pipes adjacency connector is for small pipes layers 1 and 3...
     */
    [RequireComponent(typeof(MeshFilter))]
    public class OffsetPipesAdjacencyConnector : MonoBehaviour, AdjacencyConnector
    {
        public enum TileLayer
        {
            Turf,
            Fixture,
        }

        public int LayerIndex { get; set; }

        // Id that adjacent objects must be to count. If null, any id is accepted
        public string type;


        [Header("Meshes")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the north edge is connected, can be rotated to the east")]
        public Mesh cNorth;
        [Tooltip("A mesh where the south edge is connected, can be rotated to the west")]
        public Mesh cSouth;
        [Tooltip("A mesh where north & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the north & east edges are connected")]
        public Mesh lNE;
        [Tooltip("A mesh where the north & west edges are connected")]
        public Mesh lNW;
        [Tooltip("A mesh where the south & east edges are connected")]
        public Mesh lSE;
        [Tooltip("A mesh where the south & west edges are connected")]
        public Mesh lSW;
        [Tooltip("A mesh where the north, east, & west edges are connected")]
        public Mesh tNEW;
        [Tooltip("A mesh where the north, south, & west edges are connected")]
        public Mesh tNSW;
        [Tooltip("A mesh where the north, south, & east edges are connected")]
        public Mesh tNSE;
        [Tooltip("A mesh where the South, west, & east edges are connected")]
        public Mesh tSWE;
        [Tooltip("A mesh where all edges are connected")]
        public Mesh x;

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
            bool isConnected = (tile.turf && (tile.turf.genericType == type || type == null));
            if (tile.fixtures != null)
                isConnected = isConnected || (tile.fixtures.GetFixtureAtLayerIndex(LayerIndex) && (tile.fixtures.GetFixtureAtLayerIndex(LayerIndex).genericType == type || type == null));
            return adjacents.UpdateDirection(direction, isConnected, true);
        }

        private void UpdateMeshAndDirection()
        {
            // Count number of connections along cardinal (which is all that we use atm)
            var cardinalInfo = adjacents.GetCardinalInfo();

            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            if (cardinalInfo.IsO())
                mesh = o;
            else if (cardinalInfo.IsC())
            {
                if(cardinalInfo.north > 0||cardinalInfo.east > 0)
                    mesh = cNorth;
                else
                    mesh = cSouth;
                rotation = DirectionHelper.AngleBetween(Direction.North, cardinalInfo.GetOnlyPositive());
            }
            else if (cardinalInfo.IsI())
            {
                mesh = i;
                rotation = OrientationHelper.AngleBetween(Orientation.Vertical, cardinalInfo.GetFirstOrientation());
            }
            else if (cardinalInfo.IsL())
            {
                Direction sides = cardinalInfo.GetCornerDirection();
                mesh = sides == Direction.NorthEast ? lNE
                    : sides == Direction.SouthEast ? lSE
                    : sides == Direction.SouthWest ? lSW
                    : lNW;
                rotation = 90;
            }
            else if (cardinalInfo.IsT())
            {
                Direction notside = cardinalInfo.GetOnlyNegative();
                mesh = notside == Direction.North ? tSWE
                    : notside == Direction.East ? tNSW
                    : notside == Direction.South ? tNEW
                    : tNSE;
                rotation = 90;
            }
            else // Must be X
            {
                mesh = x;
                rotation = 90;
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
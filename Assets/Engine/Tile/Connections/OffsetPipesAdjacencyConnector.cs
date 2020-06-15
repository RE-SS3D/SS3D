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

        public FixtureLayers Layer { get; set; }

        // Id that adjacent objects must be to count. If null, any id is accepted
        public string type;


        [Header("Meshes")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the east edge is connected, can be rotated to the north")]
        public Mesh cEast;
        [Tooltip("A mesh where the west edge is connected, can be rotated to the south")]
        public Mesh cWest;
        [Tooltip("A mesh where east and west edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the south and west edges are connected")]
        public Mesh lSW;
        [Tooltip("A mesh where the south and east edges are connected")]
        public Mesh lSE;
        [Tooltip("A mesh where the north and east edges are connected")]
        public Mesh lNE;
        [Tooltip("A mesh where the north and west edges are connected")]
        public Mesh lNW;
        [Tooltip("A mesh where the north, south, and east edge is connected")]
        public Mesh tNSE;
        [Tooltip("A mesh where the north, west, and east edge is connected")]
        public Mesh tNEW;
        [Tooltip("A mesh where the north, south, and west edge is connected")]
        public Mesh tNSW;
        [Tooltip("A mesh where the South, west, and east edge is connected")]
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
            int index = (int)Layer;

            bool isConnected = (tile.turf && (tile.turf.genericType == type || type == null));
            if (tile.fixtures != null)
                isConnected = isConnected || (tile.fixtures[index] && (tile.fixtures[index].genericType == type || type == null));
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
                mesh = cEast;
            }
            else if (cardinalInfo.IsI())
            {
                mesh = i;
                rotation = OrientationHelper.AngleBetween(Orientation.Horizontal, cardinalInfo.GetFirstOrientation());
            }
            else if (cardinalInfo.IsL())
            {
                mesh = lSW;
                rotation = DirectionHelper.AngleBetween(Direction.SouthEast, cardinalInfo.GetCornerDirection());
            }
            else if (cardinalInfo.IsT())
            {
                mesh = tNSE;
                rotation = DirectionHelper.AngleBetween(Direction.West, cardinalInfo.GetOnlyNegative());
            }
            else // Must be X
                mesh = x;

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
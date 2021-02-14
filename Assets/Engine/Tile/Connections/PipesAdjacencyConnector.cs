using UnityEngine;
using System.Collections;
using SS3D.Engine.Tiles.Connections;
using System;
using SS3D.Engine.Tiles.State;

namespace SS3D.Engine.Tiles.Connections
{
    /**
     * The pipes adjacency connector is for small pipes layers 2 and 4...
     */
    [RequireComponent(typeof(MeshFilter))]
    public class PipesAdjacencyConnector : AdjacencyStateMaintainer, AdjacencyConnector
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
        [Tooltip("A mesh where the north edge is connected")]
        public Mesh c;
        [Tooltip("A mesh where the north & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the north & east edges are connected")]
        public Mesh l;
        [Tooltip("A mesh where the north, east, and west edges are connected")]
        public Mesh t;
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
            {
                isConnected = isConnected || (tile.fixtures.GetFixtureAtLayerIndex(LayerIndex) && (tile.fixtures.GetFixtureAtLayerIndex(LayerIndex).genericType == type || type == null));
            }

            isConnected &= (AdjacencyBitmap.Adjacent(TileState.blockedDirection, direction) == 0);

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
                mesh = c;
                rotation = DirectionHelper.AngleBetween(Direction.North, cardinalInfo.GetOnlyPositive());
            }
            else if (cardinalInfo.IsI())
            {
                mesh = i;
                rotation = OrientationHelper.AngleBetween(Orientation.Vertical, cardinalInfo.GetFirstOrientation());
            }
            else if (cardinalInfo.IsL())
            {
                mesh = l;
                rotation = DirectionHelper.AngleBetween(Direction.NorthEast, cardinalInfo.GetCornerDirection());
            }
            else if (cardinalInfo.IsT())
            {
                mesh = t;
                rotation = DirectionHelper.AngleBetween(Direction.South, cardinalInfo.GetOnlyNegative());
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
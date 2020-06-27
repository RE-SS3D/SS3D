using UnityEngine;
using System.Collections;
using SS3D.Engine.Tiles.Connections;

namespace SS3D.Engine.Tiles.Connections
{
    /**
     * These classes are getting very specific...
     * This one is the same as SimpleAdjacencyConnector, but it has specific unique I connections
     * to a more generic type.
     * This is not currently used on any connectibles.
     */
    [RequireComponent(typeof(MeshFilter))]
    public class TieredAdjacencyConnector : MonoBehaviour, AdjacencyConnector
    {
        // Id that adjacent objects must be to count. If null, any id is accepted
        public string id;
        public string genericType;
        public int LayerIndex { get; set; }

        [Header("Meshes")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the north edge is connected")]
        public Mesh c;
        [Tooltip("A mesh where north & south edges are connected too the same type")]
        public Mesh i;
        [Tooltip("A mesh where north connects to same type (window), and south connects to the generic type (wall)")]
        public Mesh iBorder;
        [Tooltip("A mesh for a single I tile between two generic ones")]
        public Mesh iAlone;
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
            {
                UpdateMeshAndDirection();
            }
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
         * Adjusts the connections value based on the given new tile
         */
        private bool UpdateSingleConnection(Direction direction, TileDefinition tile)
        {
            bool isGeneric = (tile.turf && (tile.turf.genericType == genericType || genericType == null));
            if (tile.fixtures != null)
                isGeneric = isGeneric || (tile.fixtures.GetFixtureAtLayerIndex(LayerIndex) && (tile.fixtures.GetFixtureAtLayerIndex(LayerIndex).genericType == genericType || genericType == null));

            bool isSpecific = (tile.turf && (tile.turf.id == id || id == null));
            if (tile.fixtures != null)
                isSpecific = isSpecific || (tile.fixtures.GetFixtureAtLayerIndex(LayerIndex) && (tile.fixtures.GetFixtureAtLayerIndex(LayerIndex).id == id || id == null));

            bool changed = generalAdjacents.UpdateDirection(direction, isGeneric, true);
            changed |= specificAdjacents.UpdateDirection(direction, isSpecific, true);

            return changed;
        }

        private void UpdateMeshAndDirection()
        {
            // Count number of connections along cardinal (which is all that we use atm)
            var generalCardinals = generalAdjacents.GetCardinalInfo();

            float rotation = 0.0f;
            Mesh mesh;
            if (generalCardinals.IsO())
                mesh = o;
            else if (generalCardinals.IsC())
            {
                mesh = c;
                rotation = DirectionHelper.AngleBetween(Direction.North, generalCardinals.GetOnlyPositive());
            }
            else if (generalCardinals.IsI())
            {
                // Check for specific connections
                var specificCardinals = specificAdjacents.GetCardinalInfo();

                if (specificCardinals.numConnections == 1)
                {
                    mesh = iBorder;
                    rotation = DirectionHelper.AngleBetween(Direction.South, specificCardinals.GetOnlyPositive());
                }
                else
                {
                    mesh = specificCardinals.numConnections == 2 ? i : iAlone;
                    rotation = OrientationHelper.AngleBetween(Orientation.Vertical, generalCardinals.GetFirstOrientation());
                }
            }
            else if (generalCardinals.IsL())
            {
                mesh = l;
                rotation = DirectionHelper.AngleBetween(Direction.NorthEast, generalCardinals.GetCornerDirection());
            }
            else if (generalCardinals.IsT())
            {
                mesh = t;
                rotation = DirectionHelper.AngleBetween(Direction.South, generalCardinals.GetOnlyNegative());
            }
            else
                mesh = x;

            if (filter == null)
                filter = GetComponent<MeshFilter>();

            filter.mesh = mesh;
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, rotation, transform.localRotation.eulerAngles.z);
        }

        private AdjacencyBitmap generalAdjacents = new AdjacencyBitmap();
        private AdjacencyBitmap specificAdjacents = new AdjacencyBitmap();

        private MeshFilter filter;
    }
}
using UnityEngine;
using System.Collections;
using Engine.Tiles.Connections;

namespace Engine.Tiles.Connections
{
    /**
     * These classes are getting very specific...
     * This one is the same as SimpleAdjacencyConnector, but it has specific unique I connections
     * to a more generic type.
     * This is currently used for glass walls, where when connecting to any other wall we need to
     * 'close' the glass window.
     */
    [RequireComponent(typeof(MeshFilter))]
    public class TieredIAdjacencyConnector : MonoBehaviour, AdjacencyConnector
    {
        // Id that adjacent objects must be to count. If null, any id is accepted
        public string id;
        public string genericType;

        [Header("Meshes")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the east edge is connected")]
        public Mesh c;
        [Tooltip("A mesh where east and west edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where west connects to same type, and east connects to the generic type")]
        public Mesh iBorder;
        [Tooltip("A mesh for a single I tile between two generic ones")]
        public Mesh iAlone;
        [Tooltip("A mesh where the south and west edges are connected")]
        public Mesh l;
        [Tooltip("A mesh where the north, south, and east edge is connected")]
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
            for (int i = 0; i < tiles.Length; i++) {
                changed |= UpdateSingleConnection((Direction)i, tiles[i]);
            }
            if(changed)
                UpdateMeshAndDirection();
        }

        public void Awake() => filter = GetComponent<MeshFilter>();
        public void OnEnable() => UpdateMeshAndDirection();

        /**
         * Adjusts the connections value based on the given new tile
         */
        private bool UpdateSingleConnection(Direction direction, TileDefinition tile)
        {
            bool isGeneric = (tile.turf && (tile.turf.genericType == genericType || genericType == null)) || (tile.fixture && (tile.fixture.genericType == genericType || genericType == null));
            bool isSpecific = (tile.turf && (tile.turf.id == id || id == null)) || (tile.fixture && (tile.fixture.id == id || id == null));

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
                rotation = DirectionHelper.AngleBetween(Direction.East, generalCardinals.GetOnlyPositive());
            }
            else if (generalCardinals.IsI())
            {
                // Check for specific connections
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
            else if (generalCardinals.IsL())
            {
                mesh = l;
                rotation = DirectionHelper.AngleBetween(Direction.SouthEast, generalCardinals.GetCornerDirection());
            }
            else if (generalCardinals.IsT()) {
                mesh = t;
                rotation = DirectionHelper.AngleBetween(Direction.West, generalCardinals.GetOnlyNegative());
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
using UnityEngine;
using System.Collections;

namespace TileMap {
    /**
     * This component gets notified whenever the same layer on an adjacent tile is modified
     * Warning: Lots of bit logic
     */
    [ExecuteInEditMode]
    public abstract class AdjacencyConnector : MonoBehaviour
    {
        /**
         * When a single adjacent turf is updated
         */
        public abstract void UpdateSingle(Direction direction, ConstructibleTile tile);

        /**
         * When all (or a significant number) of adjacent turfs update.
         * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
         */
        public abstract void UpdateAll(ConstructibleTile[] tiles);
    }
}

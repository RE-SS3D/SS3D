using UnityEngine;
using System.Collections;

namespace TileMap {
    /**
     * Should be attached to a component to get notified whenever the same layer on an adjacent tile is modified
     * Warning: Lots of bit logic
     */
    public interface AdjacencyConnector
    {
        /**
         * When a single adjacent turf is updated
         */
        void UpdateSingle(Direction direction, ConstructibleTile tile);

        /**
         * When all (or a significant number) of adjacent turfs update.
         * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
         */
        void UpdateAll(ConstructibleTile[] tiles);
    }
}

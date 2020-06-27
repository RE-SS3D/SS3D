using UnityEngine;
using System.Collections;

namespace SS3D.Engine.Tiles.Connections
{
    /**
     * Should be attached to a component to get notified whenever the same layer on an adjacent tile is modified
     * Warning: Lots of bit logic
     */
    public interface AdjacencyConnector
    {
        int LayerIndex { get; set; }

        /**
         * When a single adjacent turf is updated
         */
        void UpdateSingle(Direction direction, TileDefinition tile);

        /**
         * When all (or a significant number) of adjacent turfs update.
         * Turfs are ordered by direction, i.e. North, NorthEast, East ... NorthWest
         */
        void UpdateAll(TileDefinition[] tiles);
    }
}

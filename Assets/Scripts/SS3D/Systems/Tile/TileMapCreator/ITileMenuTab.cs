using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Interface for the tabs of the tile menu.
    /// </summary>
    public interface ITileMenuTab
    {
        /// <summary>
        /// Clear the content of a tab in the tilemap menu.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Display the content of a tab in the tilemap menu.
        /// </summary>
        public void Display();

        /// <summary>
        /// Refresh the content of a tab in the tilemap menu.
        /// </summary>
        public void Refresh();
    }

}
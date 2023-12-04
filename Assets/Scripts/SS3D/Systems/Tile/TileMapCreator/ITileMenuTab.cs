using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    public interface ITileMenuTab
    {
        public void Clear();

        public void Display();

        public void Refresh();
    }

}
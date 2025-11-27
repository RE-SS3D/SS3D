using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public interface IAtmosPipe
    {
        PlacedTileObject PlacedTileObject { get; }

        public int PipeNetIndex { get; set; }

        public AtmosObject AtmosObject { get; set; }

        public TileLayer TileLayer { get; }

        public Vector2Int WorldOrigin { get; }
    }
}

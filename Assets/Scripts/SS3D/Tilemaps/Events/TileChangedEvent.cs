using Coimbra.Services.Events;

namespace SS3D.Tilemaps.Events
{
    public partial struct TileChangedEvent : IEvent
    {
        public TileData? Tile;

        public TileChangedEvent(TileData? tile)
        {
            Tile = tile;
        }
    }
}
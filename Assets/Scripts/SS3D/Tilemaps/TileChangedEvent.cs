using Coimbra.Services.Events;

namespace SS3D.Tilemaps
{
    public partial struct TileChangedEvent : IEvent
    {
        public Tile Tile;

        public TileChangedEvent(Tile tile)
        {
            Tile = tile;
        }
    }
}
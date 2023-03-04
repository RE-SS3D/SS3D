using Coimbra.Services.Events;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Called when a player's containers and inventory are set up
    /// </summary>
    public partial struct PlayerContainersReady : IEvent
    {
        public Entity Player;

        public PlayerContainersReady(Entity player)
        {
            Player = player;
        }
    }
}
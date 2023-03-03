using Coimbra.Services.Events;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Inventory.Containers
{
    public partial struct PlayerContainersReady : IEvent
    {
        public Entity Player;

        public PlayerContainersReady(Entity player)
        {
            Player = player;
        }
    }
}
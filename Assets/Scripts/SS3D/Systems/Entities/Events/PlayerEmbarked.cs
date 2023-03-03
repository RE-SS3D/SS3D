using System.Collections.Generic;
using Coimbra.Services.Events;

namespace SS3D.Systems.Entities.Events
{
    public partial struct PlayerEmbarked : IEvent
    {
        public readonly Entity Player;

        public PlayerEmbarked(Entity player)
        {
            Player = player;
        }
    }
}
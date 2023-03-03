using System.Collections.Generic;
using Coimbra.Services.Events;

namespace SS3D.Systems.Entities.Events
{
    public partial struct IndividualPlayerEmbarked : IEvent
    {
        public readonly Entity Player;

        public IndividualPlayerEmbarked(Entity player)
        {
            Player = player;
        }
    }
}
using System.Collections.Generic;
using Coimbra.Services.Events;
using SS3D.Systems.Entities;

namespace SS3D.Systems.PlayerControl.Events
{
    public partial struct ServerSoulsChanged : IEvent
    {
        public readonly List<Soul> ServerSouls;

        public ChangeType ChangeType;
        public readonly Soul Changed;

        public ServerSoulsChanged(List<Soul> serverSouls, ChangeType changeType, Soul changed)
        {
            ServerSouls = serverSouls;
            ChangeType = changeType;
            Changed = changed;
        }
    }
}
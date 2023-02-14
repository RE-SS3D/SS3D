using System.Collections.Generic;
using Coimbra.Services.Events;
using SS3D.Systems.Entities;

namespace SS3D.Systems.PlayerControl.Events
{
    public partial struct OnlineSoulsChanged  : IEvent
    {
        public readonly List<Soul> OnlineSouls;

        public ChangeType ChangeType;
        public readonly Soul Changed;
        public readonly string ChangedCkey;

        public OnlineSoulsChanged(List<Soul> onlineSouls, ChangeType changeType, Soul changed, string ckey)
        {
            OnlineSouls = onlineSouls;
            ChangeType = changeType;
            Changed = changed;
            ChangedCkey = ckey;
        }
    }
}
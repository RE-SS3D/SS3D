using Coimbra.Services.Events;
using SS3D.Core.Settings;

namespace SS3D.Core.Events
{
    public partial struct ApplicationStartedEvent : IEvent
    {
        public readonly string Ckey;
        public readonly NetworkType NetworkType;

        public ApplicationStartedEvent(string ckey, NetworkType networkType)
        {
            Ckey = ckey;
            NetworkType = networkType;
        }
    }
}
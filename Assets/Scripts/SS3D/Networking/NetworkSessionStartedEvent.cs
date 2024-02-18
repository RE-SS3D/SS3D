using Coimbra.Services.Events;

namespace SS3D.Networking
{
    public partial struct NetworkSessionStartedEvent : IEvent
    {
        public readonly string Ckey;
        public readonly NetworkType NetworkType;

        public NetworkSessionStartedEvent(string ckey, NetworkType networkType)
        {
            Ckey = ckey;
            NetworkType = networkType;
        }
    }
}
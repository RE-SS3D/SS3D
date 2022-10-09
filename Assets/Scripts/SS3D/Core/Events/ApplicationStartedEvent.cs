using Coimbra.Services.Events;

namespace SS3D.Core.Events
{
    public partial struct ApplicationStartedEvent : IEvent
    {
        public readonly string Ckey;
        public readonly ApplicationMode ApplicationMode;

        public ApplicationStartedEvent(string ckey, ApplicationMode applicationMode)
        {
            Ckey = ckey;
            ApplicationMode = applicationMode;
        }
    }
}
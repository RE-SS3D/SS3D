using Coimbra.Services.Events;
using SS3D.Systems.Furniture;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct NukeDetonateEvent : IEvent
    {
        Nuke Nuke;
        public NukeDetonateEvent(Nuke nuke)
        {
            Nuke = nuke;
        }
    }
}
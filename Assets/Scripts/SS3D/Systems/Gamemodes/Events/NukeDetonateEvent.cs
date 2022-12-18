using Coimbra.Services.Events;
using FishNet.Connection;
using SS3D.Systems.Furniture;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct NukeDetonateEvent : IEvent
    {
        public Nuke Nuke;
        public NetworkConnection Author;

        public NukeDetonateEvent(Nuke nuke, NetworkConnection author)
        {
            Nuke = nuke;
            Author = author;
        }
    }
}
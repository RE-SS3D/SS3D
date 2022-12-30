using Coimbra.Services.Events;
using SS3D.Systems.Furniture;

namespace SS3D.Systems.GameModes.Events
{
    public partial struct NukeDetonateEvent : IEvent
    {
        public Nuke Nuke;
        public string Author;

        public NukeDetonateEvent(Nuke nuke, string author)
        {
            Nuke = nuke;
            Author = author;
        }
    }
}
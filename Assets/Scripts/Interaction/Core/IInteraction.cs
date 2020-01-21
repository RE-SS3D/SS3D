using System;

namespace Interaction.Core
{
    public interface IInteraction
    {
        void Setup(Action<string> listen, Action<string> blocks);
        bool Handle(Event e);
    }
}
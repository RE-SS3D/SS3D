using System;

namespace Interaction.Core
{
    public interface IInteractable
    {
        void Setup(Action<string> listen, Action<string> blocks);
        bool Handle(InteractionEvent e);
    }
}
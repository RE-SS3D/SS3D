using System;

namespace Interaction
{
    public interface IInteractable
    {
        void Setup(Action<string> listen, Action<string> blocks);
        bool Handle(InteractionEvent e);
    }
}
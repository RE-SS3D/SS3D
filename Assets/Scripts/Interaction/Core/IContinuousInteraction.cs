using System.Collections;

namespace Interaction.Core
{
    public interface IContinuousInteraction : IBaseInteraction
    {
        IEnumerator Handle(InteractionEvent e);
    }
}
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public sealed class OpenMachineInterfaceInteraction : IInteraction, IClientInteractionSource
    {
        public string GetGenericName() => "Open interface";

        public string GetName(InteractionEvent interactionEvent) => "Open interface";

        public Sprite GetIcon(InteractionEvent interactionEvent) => null;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent?.Target == null || !interactionEvent.Target.GetGameObject())
            {
                return false;
            }

            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            return interactionEvent.Target is MachineInterfaceBehaviour;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is MachineInterfaceBehaviour machine)
            {
                machine.ServerHandleOpenRequest(interactionEvent);
            }

            return false;
        }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is MachineInterfaceBehaviour machine)
            {
                machine.CmdRequestOpen();
            }

            return null;
        }
    }
}

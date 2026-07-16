using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Opens a machine panel via the validated server interaction path only.
    /// Do not add a client open ServerRpc — that bypasses range checks.
    /// </summary>
    public sealed class OpenMachineInterfaceInteraction : IInteraction
    {
        public int Priority => 60;

        public string GetGenericName() => "OpenMachineInterface";

        public string GetName(InteractionEvent interactionEvent) => "Open interface";

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIconLookup.MachineInterface;

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
    }
}

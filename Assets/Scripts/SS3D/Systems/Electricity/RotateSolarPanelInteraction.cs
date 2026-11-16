using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Cycles a <see cref="SolarPanel"/> aim by one 45° <see cref="Tile.Direction"/> step.
    /// </summary>
    public sealed class RotateSolarPanelInteraction : IInteraction, IClientInteractionSource
    {
        public Sprite Icon;

        public int Priority => 40;

        public string GetName(InteractionEvent interactionEvent) => "Rotate";

        public string GetGenericName() => "RotateSolarPanel";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : InteractionIconLookup.Power;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is not SolarPanel)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is SolarPanel panel)
            {
                panel.RotateAimStep();
            }

            return false;
        }
    }
}

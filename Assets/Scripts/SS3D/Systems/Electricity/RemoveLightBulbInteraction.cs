using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>Takes the bulb or tube, whole or broken, out of a fixture into the empty hand.</summary>
    public sealed class RemoveLightBulbInteraction : IInteraction, IClientInteractionSource
    {
        public int Priority => 30;

        public string GetName(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is not LightFixtureBulb fixture)
            {
                return "Remove light bulb";
            }

            string broken = fixture.State == LightBulbState.Broken ? "broken " : string.Empty;
            return "Remove " + broken + (fixture.Kind == LightBulbKind.Tube ? "light tube" : "light bulb");
        }

        public string GetGenericName() => "RemoveLightBulb";

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIconLookup.Take;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            return interactionEvent.Target is LightFixtureBulb fixture
                && interactionEvent.Source is Hand hand
                && LightBulbRules.CanRemove(fixture.State, hand.IsEmpty())
                && InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is LightFixtureBulb fixture && interactionEvent.Source is Hand hand)
            {
                fixture.ServerRemoveInto(hand);
            }

            return false;
        }
    }
}

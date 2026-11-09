using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Fits the held bulb or tube into an empty fixture of the same kind, or swaps it for a broken one, which drops
    /// to the floor.
    /// </summary>
    public sealed class InsertLightBulbInteraction : IInteraction, IClientInteractionSource
    {
        public int Priority => 40;

        public string GetName(InteractionEvent interactionEvent)
        {
            return interactionEvent.Target is LightFixtureBulb { State: LightBulbState.Broken }
                ? "Replace " + Noun(interactionEvent)
                : "Insert " + Noun(interactionEvent);
        }

        public string GetGenericName() => "InsertLightBulb";

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIconLookup.Power;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Target is not LightFixtureBulb fixture || !TryGetBulb(interactionEvent, out _, out LightBulbItem bulb))
            {
                return false;
            }

            return LightBulbRules.CanInsert(fixture.State, fixture.Kind, bulb.Kind, bulb.IsBroken)
                && InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Target is LightFixtureBulb fixture && TryGetBulb(interactionEvent, out Item item, out _))
            {
                fixture.ServerInsert(item);
            }

            return false;
        }

        private static string Noun(InteractionEvent interactionEvent)
        {
            return interactionEvent.Target is LightFixtureBulb { Kind: LightBulbKind.Tube } ? "light tube" : "light bulb";
        }

        private static bool TryGetBulb(InteractionEvent interactionEvent, out Item item, out LightBulbItem bulb)
        {
            item = null;
            bulb = null;
            if (interactionEvent.Source is not IGameObjectProvider provider)
            {
                return false;
            }

            item = provider.GameObject.GetComponent<Item>();
            bulb = provider.GameObject.GetComponent<LightBulbItem>();
            return item != null && bulb != null;
        }
    }
}

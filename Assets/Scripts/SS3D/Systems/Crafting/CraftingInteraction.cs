using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Logging;
using SS3D.Substances;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public abstract class CraftingInteraction : DelayedInteraction, ICraftingInteraction
    {
        private List<Item> ItemsToConsume;

        private CraftingRecipe recipe;

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!CanCraft(interactionEvent)) return false;

            return true;
        }

        public bool CanCraft(InteractionEvent interactionEvent)
        {
            if (!Subsystems.TryGet(out CraftingSystem craftingSystem)) return false;

            if (interactionEvent.Target is not Item) return false;

            Item target = interactionEvent.Target as Item;

            if(!craftingSystem.TryGetRecipe(this, target, out recipe)) return false;

            List<Item> closeItemsFromTarget = craftingSystem.GetCloseItemsFromTarget(target);

            Dictionary<ItemId, int> potentialRecipeElements = craftingSystem.
                ItemListToDictionnaryOfRecipeElements(closeItemsFromTarget);

            if (!craftingSystem.CheckEnoughCloseItemsForRecipe(potentialRecipeElements, recipe)) return false;

            ItemsToConsume = craftingSystem.BuildListOfItemToConsume(closeItemsFromTarget, recipe);

            return true;
        }

        [Server]
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            base.Start(interactionEvent, reference);
            return true;
        }

        [Server]
        public void Craft(InteractionEvent interactionEvent)
        {
            if (ItemsToConsume == null)
            {
                Punpun.Error(this, "List of items to consume is null, call CanInteract first.");
                return;
            }

            Subsystems.TryGet(out CraftingSystem craftingSystem);
            Item target = interactionEvent.Target as Item;

            craftingSystem.Craft(target, ItemsToConsume, recipe.Result);
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            Craft(interactionEvent);
        }
    }
}
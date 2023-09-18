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
    public class CraftingInteraction : Interaction, ICraftingInteraction
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
            if (!Subsystems.TryGet(out CraftingSystem craftingSystem))
            {
                return false;
            }
            if (interactionEvent.Target is not Item)
            {
                return false;
            }

            Item target = interactionEvent.Target as Item;
            recipe = craftingSystem.GetRecipe(this, target);
            List<Item> closeItemsFromTarget = GetCloseItemsFromTarget(target, recipe);

            // Transform the list into a dictionnary of itemsID and counts of items.
            // This is some overhead to allow for fast comparison between recipe and 
            // available items.
            Dictionary<ItemId, int> potentialRecipeElements = closeItemsFromTarget
            .GroupBy(item => item.ItemId)
            .ToDictionary(group => group.Key, group => group.Count());

            if (!CheckEnoughCloseItemsForRecipe(potentialRecipeElements, recipe))
            {
                return false;
            }

            BuildListOfItemToConsume(closeItemsFromTarget, potentialRecipeElements);

            return true;
        }

        /// <summary>
        /// Build the list of items we want to consume. Don't add more to
        /// the list than necessary.
        /// </summary>
        private void BuildListOfItemToConsume(List<Item> closeItemsFromTarget,
            Dictionary<ItemId, int> potentialRecipeElements)
        {
            ItemsToConsume = new List<Item>();

            foreach (Item item in closeItemsFromTarget)
            {
                if (potentialRecipeElements.GetValueOrDefault(item.ItemId) <= 0) continue;
                ItemsToConsume.Add(item);
                potentialRecipeElements[item.ItemId] -= 1;
            }
        }

        /// <summary>
        /// Find all items in close proximity from the target of the recipe.
        /// TODO : only collider for item ? Should then ensure collider of item is on the
        /// same game object as item script for all items. Would avoid the getInParent.
        /// </summary>
        private List<Item> GetCloseItemsFromTarget(Item target, CraftingRecipe recipe)
        {
            Vector3 center = target.Position;

            float radius = 0.5f;
            
            Collider[] hitColliders = Physics.OverlapSphere(center, radius);
            List<Item> closeItemsFromTarget = new List<Item>();

            foreach (Collider hitCollider in hitColliders)
            {
                Item item = hitCollider.GetComponentInParent<Item>();
                if (item == null) continue;
                closeItemsFromTarget.Add(item);
            }

            return closeItemsFromTarget;
        }

        /// <summary>
        /// Check if there's enough items for the recipe. This method modifies potentialRecipeElements,
        /// as if there's more items required from a certain type available, we only keep the
        /// required amount.
        /// </summary>
        /// <param name="potentialRecipeElements"> Items that can potentially be used </param>
        /// <param name="recipe"> The recipe for which we want to check items</param>
        /// <returns></returns>
        private bool CheckEnoughCloseItemsForRecipe(Dictionary<ItemId, int> potentialRecipeElements,
            CraftingRecipe recipe)
        {
            // check if there's enough of each item.
            foreach (ItemId item in recipe.Elements.Keys)
            {
                int CloseItemsCount = potentialRecipeElements.GetValueOrDefault(item);
                if (recipe.Elements[item] > CloseItemsCount)
                {
                    return false;
                }

                // We do that for later, easier to check how much items we really need,
                // and not include too much.
                potentialRecipeElements[item] = recipe.Elements[item];
            }

            return true;
        }

        /// <summary>
        /// Starts the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        [Server]
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Craft(interactionEvent);
            return false;
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
    }
}
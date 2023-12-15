using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Crafting
{
    public abstract class CraftingInteraction : DelayedInteraction, ICraftingInteraction
    {
        private List<IRecipeIngredient> ItemsToConsume;

        protected CraftingRecipe _recipe;

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

            if (!interactionEvent.Target.GetGameObject().TryGetComponent<IAssetRefProvider>(out var target)) return false;

            if (!InteractionExtensions.RangeCheck(interactionEvent)) return false;

            if (!craftingSystem.TryGetRecipe(this, target, out _recipe)) return false;

            List<IRecipeIngredient> closeItemsFromTarget = craftingSystem.GetCloseItemsFromTarget(target);

            Dictionary<Item, int> potentialRecipeElements = craftingSystem.
                ItemListToDictionnaryOfRecipeElements(closeItemsFromTarget);

            if (!craftingSystem.CheckEnoughCloseItemsForRecipe(potentialRecipeElements, _recipe)) return false;

            ItemsToConsume = craftingSystem.BuildListOfItemToConsume(closeItemsFromTarget, _recipe);

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
                Log.Error(this, "List of items to consume is null, call CanInteract first.");
                return;
            }

            Subsystems.TryGet(out CraftingSystem craftingSystem);

            craftingSystem.Craft(interactionEvent, ItemsToConsume, _recipe);
        }

        protected override void StartDelayed(InteractionEvent interactionEvent)
        {
            Craft(interactionEvent);
        }
    }
}
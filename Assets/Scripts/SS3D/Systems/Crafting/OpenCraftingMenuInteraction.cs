using QuikGraph;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OpenCraftingMenuInteraction : Interaction
{
    private CraftingInteractionType _craftingInteractionType;

    public OpenCraftingMenuInteraction(CraftingInteractionType craftingInteraction)
    {
        _craftingInteractionType = craftingInteraction;
    }

    public override string GetGenericName() => "Open crafting menu";
    
    /// <summary>
    /// Get the name of the interaction
    /// </summary>
    /// <param name="interactionEvent">The source used in the interaction</param>
    /// <returns>The display name of the interaction</returns>
    public override string GetName(InteractionEvent interactionEvent)
    {
        return "Open crafting menu";
    }

    /// <summary>
    /// Get the icon of the interaction
    /// </summary>
    public override Sprite GetIcon(InteractionEvent interactionEvent)
    {
        return null;
    }

    /// <summary>
    /// Check if this interaction can be executed
    /// </summary>
    /// <param name="interactionEvent">The interaction source</param>
    /// <returns>If the interaction can be executed</returns>
    public override bool CanInteract(InteractionEvent interactionEvent)
    {
        if (!SubSystems.TryGet(out CraftingSubSystem craftingSystem)) return false;

        bool recipesAvailable = true;
        recipesAvailable &= craftingSystem.AvailableRecipeLinks(_craftingInteractionType, interactionEvent,
            out List<TaggedEdge<RecipeStep, RecipeStepLink>> _);

        return recipesAvailable;
    }

    /// <summary>
    /// Start the interaction (server-side)
    /// </summary>
    /// <param name="interactionEvent">The source used in the interaction</param>
    /// <param name="reference"></param>
    /// <returns>If the interaction should continue running</returns>
    public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
    {
        SubSystems.TryGet(out CraftingSubSystem craftingSystem);
        List<CraftingInteraction> craftingInteractions = craftingSystem.CreateInteractions(interactionEvent, _craftingInteractionType);
        ViewLocator.Get<CraftingMenu>().First().DisplayMenu(craftingInteractions, interactionEvent, reference, _craftingInteractionType);
        return true;
    }
}

using QuikGraph;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Crafting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OpenCraftingMenuInteraction : Interaction
{

    private List<CraftingInteractionType> _craftingInteractionTypes;

    public OpenCraftingMenuInteraction(List<CraftingInteractionType> craftingInteractions)
    {
        _craftingInteractionTypes = craftingInteractions;
    }

    public override string GetGenericName()
    {
        return "Open crafting menu";
    }

    /// <summary>
    /// Gets the name when interacted with a source
    /// </summary>
    /// <param name="interactionEvent">The source used in the interaction</param>
    /// <returns>The display name of the interaction</returns>
    public override string GetName(InteractionEvent interactionEvent)
    {
        return "Open crafting menu";
    }

    /// <summary>
    /// Gets the interaction icon
    /// </summary>
    public override Sprite GetIcon(InteractionEvent interactionEvent)
    {
        return null;
    }

    /// <summary>
    /// Checks if this interaction can be executed
    /// </summary>
    /// <param name="interactionEvent">The interaction source</param>
    /// <returns>If the interaction can be executed</returns>
    public override bool CanInteract(InteractionEvent interactionEvent)
    {
        if (!Subsystems.TryGet(out CraftingSystem craftingSystem)) return false;

        bool recipesAvailable = true;

        foreach (CraftingInteractionType type in _craftingInteractionTypes)
        {
            recipesAvailable &= craftingSystem.AvailableRecipeLinks(type, interactionEvent,
            out List<TaggedEdge<RecipeStep, RecipeStepLink>> availableRecipes);
        }

        return recipesAvailable;
    }

    /// <summary>
    /// Starts the interaction (server-side)
    /// </summary>
    /// <param name="interactionEvent">The source used in the interaction</param>
    /// <param name="reference"></param>
    /// <returns>If the interaction should continue running</returns>
    public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
    {
        List<CraftingInteraction> craftingInteractions = new();

        Subsystems.TryGet(out CraftingSystem craftingSystem);

        foreach(CraftingInteractionType type in _craftingInteractionTypes)
        {
            if (!craftingSystem.AvailableRecipeLinks(type, interactionEvent,
            out List<TaggedEdge<RecipeStep, RecipeStepLink>> availableRecipes)) continue;

            foreach(TaggedEdge<RecipeStep, RecipeStepLink> recipeLink in availableRecipes)
            {
                CraftingInteraction interaction = new CraftingInteraction(recipeLink.Tag.ExecutionTime, 
                    interactionEvent.Source.GameObject.GetComponent<InteractionSource>().transform, type, recipeLink);

                craftingInteractions.Add(interaction);
            }
        }

        if(craftingInteractions.Count > 1)
        {
            ViewLocator.Get<CraftingMenu>().First().DisplayMenu(craftingInteractions, interactionEvent, reference);
        }
        else if(craftingInteractions.Count == 1)
        {
            var craftingReference =interactionEvent.Source.Interact(interactionEvent, craftingInteractions.First());
            interactionEvent.Source.ClientInteract(interactionEvent, craftingInteractions.First(), craftingReference);
        }

        

        return true;
    }

    /// <summary>
    /// Called when the interaction is cancelled (server-side)
    /// </summary>
    /// <param name="interactionEvent">The source used in the interaction</param>
    /// <param name="reference"></param>
    public virtual void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
    {
        return;
    }
}

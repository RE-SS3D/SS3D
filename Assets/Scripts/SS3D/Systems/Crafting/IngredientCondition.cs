using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Abstract base class to create condition for ingredients, to be valid in crafting recipes.
    /// Conditions can be potentially anything, if they should be held in hand, their state, if they should be in a specific container...
    /// </summary>
    public abstract class IngredientCondition : ScriptableObject
    {
        // TODO add parameters
        public abstract List<IRecipeIngredient> UsableIngredients(List<IRecipeIngredient> ingredients);
    }
}

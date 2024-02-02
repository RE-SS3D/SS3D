using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    public abstract class IngredientCondition : ScriptableObject
    {
        // TODO add parameters
        public abstract List<IRecipeIngredient> UsableIngredients(List<IRecipeIngredient> ingredients);
    }
}

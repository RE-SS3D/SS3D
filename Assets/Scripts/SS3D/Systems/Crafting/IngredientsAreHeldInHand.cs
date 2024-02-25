using SS3D.Systems.Crafting;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Ingredient condition to check if ingredients are held in hand.
    /// </summary>
    [CreateAssetMenu(fileName = "IngredientsAreHeldInHand", menuName = "SS3D/Crafting/IngredientConditions/IngredientsAreHeldInHand")]
    public class IngredientsAreHeldInHand : IngredientCondition
    {
        public override List<IRecipeIngredient> UsableIngredients(List<IRecipeIngredient> ingredients)
        {
            return ingredients.Where(x => x.GameObject.TryGetComponent(out Item item) && item.Container?.ContainerType == ContainerType.Hand).ToList();
        }
    }
}

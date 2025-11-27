using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Interactions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Class representing a link between two recipe steps.
    /// </summary>
    [Serializable]
    public class RecipeStepLink
    {
        [Tooltip("Elements of the recipe, that will be consumed in the crafting process, and the necessary number of each.")]
        [SerializeField]
        private SerializableDictionary<WorldObjectAssetReference, int> _elements = new();

        [Tooltip("Bunch of conditions for ingredients to be valid to use in the recipe.")]
        [SerializeField]
        private List<IngredientCondition> _conditions = new();

        [Tooltip("The needed type of crafting interaction to go through this link.")]
        [SerializeField]
        private InteractionType _craftingInteractionType;

        [Tooltip("The source recipe step.")]
        [SerializeField]
        private string _from;

        [Tooltip("The target recipe step. The step reached once the requirement for this step link are met")]
        [SerializeField]
        private string _to;

        [Tooltip("If true, The main result of the target step will be modified, using the Modify method of the"
            + " ICraftable interface. The main result's prefab should have a component implementing the ICraftable interface"
            + " for it to work.")]
        [SerializeField]
        private bool _modifyResult;

        [Tooltip("The time the crafting should take in seconds.")]
        [SerializeField]
        private float _executionTime;

        [Tooltip("Things produced by going through this recipe link, upon reaching next step. Secondary results"
            + " are always crafted using the default crafting method")]
        [SerializeField]
        private List<SecondaryResult> _secondaryResults;

        /// <summary>
        /// Time it takes in second for the crafting to finish.
        /// </summary>
        public float ExecutionTime => _executionTime;

        public CraftingRecipe Recipe { get; set; }

        /// <summary>
        /// The needed type of crafting interaction to go through this link
        /// </summary>
        public InteractionType CraftingInteractionType => _craftingInteractionType;

        /// <summary>
        /// Bunch of conditions for ingredients to be valid.
        /// </summary>
        public List<IngredientCondition> Conditions => _conditions;

        /// <summary>
        /// The source recipe step.
        /// </summary>
        public string From => _from;

        /// <summary>
        /// The target recipe step.
        /// </summary>
        public string To => _to;

        /// <summary>
        /// "If true, The main result of the target step will be modified, using the Modify method of the
        /// ICraftable interface. The main result's prefab should have a component implementing the ICraftable interface
        /// for it to work.
        /// </summary>
        public bool ModifyResult => _modifyResult;

        /// <summary>
        /// The world objects ids and their respective numbers necessary for the recipe.
        /// </summary>
        public Dictionary<string, int> Elements
        {
            get
            {
                Dictionary<string, int> elements = new();

                foreach (KeyValuePair<WorldObjectAssetReference, int> keyValuePair in _elements)
                {
                    elements.Add(keyValuePair.Key.Id, keyValuePair.Value);
                }

                return elements;
            }
        }

        /// <summary>
        /// The result of the crafting.
        /// </summary>
        public List<SecondaryResult> SecondaryResults => _secondaryResults;

        /// <summary>
        /// Filter ingredients based on conditions added on this recipe step link.
        /// </summary>
        public List<IRecipeIngredient> ApplyIngredientConditions(List<IRecipeIngredient> ingredients)
        {
            foreach (IngredientCondition condition in _conditions)
            {
                ingredients = condition.UsableIngredients(ingredients);
            }

            return ingredients;
        }
    }
}

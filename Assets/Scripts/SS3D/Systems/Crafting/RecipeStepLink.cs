using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using SS3D.Data.AssetDatabases;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Class representing a link between two recipe steps.
    /// </summary>
    [Serializable]
    public class RecipeStepLink
    {

        /// <summary>
        /// Elements of the recipe, that will be consumed in the crafting process, and the necessary number of each.
        /// </summary>
        [SerializeField]
        private SerializableDictionary<WorldObjectAssetReference, int> _elements = new();

        /// <summary>
        /// Bunch of conditions for ingredients to be valid.
        /// </summary>
        [SerializeField]
        private List<IngredientCondition> _conditions = new();

        /// <summary>
        /// The needed type of crafting interaction to go through this link
        /// </summary>
        [SerializeField]
        private CraftingInteractionType _craftingInteractionType;

        /// <summary>
        /// The source recipe step.
        /// </summary>
        [SerializeField]
        private string _from;

        /// <summary>
        /// The target recipe step.
        /// </summary>
        [SerializeField]
        private string _to;

        /// <summary>
        /// Should the result spawning from reaching the target step be modified ? Useful for deconstruction.
        /// </summary>
        [SerializeField]
        private bool _modifyResult;

        /// <summary>
        /// The time the crafting should take.
        /// </summary>
        [SerializeField]
        private float _executionTime;

        /// <summary>
        ///  Things produced by going through this recipe link, upon reaching next step.
        /// </summary>
        [SerializeField]
        private List<WorldObjectAssetReference> _secondaryresults;

        /// <summary>
        /// Time it takes in second for the crafting to finish.
        /// </summary>
        public float ExecutionTime => _executionTime;

        public CraftingRecipe Recipe { get; set; }

        /// <summary>
        /// The needed type of crafting interaction to go through this link
        /// </summary>
        public CraftingInteractionType CraftingInteractionType => _craftingInteractionType;

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
        /// Should the result spawning from reaching the target step be modified ? Useful for deconstruction.
        /// </summary>
        public bool ModifyResult => _modifyResult;

        /// <summary>
        /// The world objects ids and their respective numbers necessary for the recipe.
        /// </summary>
        public Dictionary<string, int> Elements
        {
            get
            {
                Dictionary<string, int> elements = new Dictionary<string, int>();


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
        public List<WorldObjectAssetReference> SecondaryResults => _secondaryresults;

        /// <summary>
        /// Filter ingredients based on conditions added on this recipe step link.
        /// </summary>
        public List<IRecipeIngredient> ApplyIngredientConditions(List<IRecipeIngredient> ingredients)
        {
            foreach (var condition in _conditions)
            {
                ingredients = condition.UsableIngredients(ingredients);
            }

            return ingredients;
        }
    }
}



using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using SS3D.Data.AssetDatabases;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Class representing 
    /// </summary>
    [Serializable]
    public class RecipeStepLink
    {

        /// <summary>
        /// Elements of the recipe, that will be consumed in the crafting process, and the necessary number of each.
        /// </summary>
        [SerializeField]
        private SerializableDictionary<WorldObjectAssetReference, int> _elements = new();

        [SerializeField]
        private List<IngredientCondition> _conditions = new();

        [SerializeField]
        private CraftingInteractionType _craftingInteractionType;

        [SerializeField]
        private string _from;

        [SerializeField]
        private string _to;

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

        public CraftingInteractionType CraftingInteractionType => _craftingInteractionType;

        public List<IngredientCondition> Conditions => _conditions;

        public string From => _from;

        public string To => _to;

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
        public List<GameObject> SecondaryResults => _secondaryresults.Select(reference => reference.Prefab).ToList();

        public List<IRecipeIngredient> ApplyIngredientConditions(List<IRecipeIngredient> ingredients)
        {
            foreach (var condition in _conditions)
            {
                ingredients = condition.UsableIngredients(ingredients);
            }

            return ingredients;
        }

        public int ElementsNumber => _elements.Sum(x => x.Value);
    }
}



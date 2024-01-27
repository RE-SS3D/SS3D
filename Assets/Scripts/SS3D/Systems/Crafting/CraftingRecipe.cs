using Coimbra;
using QuikGraph;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using SS3D.Substances;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
	/// <summary>
	/// Crafting recipes allow to replace a bunch of item by another, using a specific interaction.
	/// </summary>
	[CreateAssetMenu(fileName = "Recipe", menuName = "SS3D/Crafting/Recipe")]
	public class CraftingRecipe : ScriptableObject
	{
        /// <summary>
        /// The target of the crafting, what needs to be clicked on by the player to start the crafting.
        /// </summary>
        [SerializeField]
        private WorldObjectAssetReference _target;

        /// <summary>
        /// The target of the recipe, which is the item on which the player must click to get the crafting interactions.
        /// </summary>
        public WorldObjectAssetReference Target => _target;

        public List<RecipeStep> steps;

        public List<RecipeStepLink> stepLinks;

        public string RootStepName => Target.Prefab.name;

        /// <summary>
        /// Graph representing all steps in a recipe and their link between each other.
        /// Could maybe use tagged edges instead to store crafting data (lenght of recipe, ingredients...)
        /// </summary>
        private AdjacencyGraph <RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>> _recipeGraph;

        private void OnValidate()
        {
            Init();
        }

        private void Init()
        {
            _recipeGraph = new();

            foreach (RecipeStep step in steps)
            {
                _recipeGraph.AddVertex(step);
            }

            foreach (RecipeStepLink link in stepLinks)
            {
                bool stepFromFound = TryGetStep(link.From, out RecipeStep stepFrom);
                bool stepToFound = TryGetStep(link.To, out RecipeStep stepTo);

                if(stepFromFound && stepToFound)
                {
                    _recipeGraph.AddEdge(new TaggedEdge<RecipeStep, RecipeStepLink>(stepFrom, stepTo, link));
                }
                else
                {
                    Log.Error(this, $"step with name {link.From} or step with name {link.To} not found in recipe {name}");
                }
            }
            
        }

        public bool TryGetStep(string name, out RecipeStep step)
        {
            step = _recipeGraph.Vertices.FirstOrDefault(x => x.Name == name);
            return step != null;
        }

        public List<TaggedEdge<RecipeStep, RecipeStepLink>> GetLinksFromStep(string name)
        {
            if (!TryGetStep(name, out RecipeStep step)) return new List<TaggedEdge<RecipeStep, RecipeStepLink>>();
            _recipeGraph.TryGetOutEdges(step, out IEnumerable<TaggedEdge<RecipeStep, RecipeStepLink>> results);
            return results.ToList();
        }

        [Serializable]
        public class RecipeStep
        {

            private CraftingRecipe _recipe;

            public CraftingRecipe Recipe => _recipe;

            /// <summary>
            /// A list of resulting objects that will spawn at the end of the crafting process.
            /// </summary>
            [SerializeField]
            private List<WorldObjectAssetReference> _result;

            /// <summary>
            /// If true, the target is consumed (despawned).
            /// </summary>
            [SerializeField]
            private bool _isTerminal;

            [SerializeField]
            private string _name;

            [SerializeField]
            private bool _isInitialState;

            /// <summary>
            /// If a default crafting method should be called, or if a custom one should.
            /// </summary>
            [SerializeField]
            private bool _customCraft;

            /// <summary>
            /// The result of the crafting.
            /// </summary>
            public List<GameObject> Result => _result.Select(reference => reference.Prefab).ToList();


            public List<WorldObjectAssetReference> Results => _result;

            public RecipeStep(CraftingRecipe recipe, string name)
            {
                _recipe = recipe;
                _isTerminal = false;
                _name = name;
                _customCraft = false;
                _result = new();
            }


            /// <summary>
            /// If true, the target is consumed (despawned).
            /// </summary>
            public bool IsTerminal => _isTerminal;

            public bool HasResult => _result.Count > 0;

            public bool CustomCraft => _customCraft;

            public string Name => _name;

        }

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

            /// <summary>
            /// The time the crafting should take.
            /// </summary>
            [SerializeField]
            private float _executionTime;

            private CraftingRecipe _recipe;

            /// <summary>
            /// Time it takes in second for the crafting to finish.
            /// </summary>
            public float ExecutionTime => _executionTime;

            public CraftingRecipe Recipe => _recipe;

            public CraftingInteractionType CraftingInteractionType => _craftingInteractionType;

            public List<IngredientCondition> Conditions => _conditions;

            public string From => _from;

            public string To => _to;

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

            public List<IRecipeIngredient> ApplyIngredientConditions(List<IRecipeIngredient> ingredients)
            {
                foreach(var condition in _conditions)
                {
                    ingredients = condition.UsableIngredients(ingredients);
                }

                return ingredients;
            }

            public int ElementsNumber => _elements.Sum(x => x.Value);
        }

        public abstract class IngredientCondition : ScriptableObject
        {
            // TODO add parameters
            public abstract List<IRecipeIngredient>  UsableIngredients(List<IRecipeIngredient> ingredients);
        }

        public class ItemsAreHeldInHand : IngredientCondition
        {
            public override List<IRecipeIngredient> UsableIngredients(List<IRecipeIngredient> ingredients)
            {
                return ingredients.Where(x => x is Item item && item.Container.ContainerType == ContainerType.Hand).ToList();
            }
        }

    }
}


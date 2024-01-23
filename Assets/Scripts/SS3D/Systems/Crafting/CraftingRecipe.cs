using Coimbra;
using SS3D.Data.AssetDatabases;
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

        public RecipeStep GetStep(string name)
        {
            return steps.First(x => x.Name == name);
        }

        public RecipeStepLink GetStepLink(string name)
        {
            return stepLinks.First(x => x.Name == name);
        }



        [Serializable]
        public class RecipeStep
        {
            /// <summary>
            /// The time the crafting should take.
            /// </summary>
            [SerializeField]
            private float _executionTime;

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

            /// <summary>
            /// If a default crafting method should be called, or if a custom one should.
            /// </summary>
            [SerializeField]
            private bool _customCraft;

            [SerializeField]
            private bool _shouldHaveIngredientsInHand;

            /// <summary>
            /// Time it takes in second for the crafting to finish.
            /// </summary>
            public float ExecutionTime => _executionTime;

            /// <summary>
            /// The result of the crafting.
            /// </summary>
            public List<GameObject> Result => _result.Select(reference => reference.Prefab).ToList();

           

            /// <summary>
            /// If true, the target is consumed (despawned).
            /// </summary>
            public bool IsTerminal => _isTerminal;

            public bool HasResult => _result.Count > 0;

            public bool CustomCraft => _customCraft;

            public string Name => _name;

        } 

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
            private string _name;

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

            public int ElementsNumber => _elements.Sum(x => x.Value);

            public string Name => _name;

        }

        public abstract class IngredientCondition : ScriptableObject
        {
            // TODO add parameters
            public abstract bool CanUseIngredient();
        }
    }
}


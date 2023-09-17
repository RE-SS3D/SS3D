using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Crafting recipes allow to replace a bunch of item by another, using a specific interaction.
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe", menuName = "SS3D/Crafting/Recipe")]
    public class CraftingRecipe : ScriptableObject, ISerializationCallbackReceiver
    {
        private HashSet<RecipeElement> _elements = new HashSet<RecipeElement>();

        [SerializeField]
        private float _executionTime;

        [SerializeField]
        private string _interactionName;

        /// <summary>
        /// The items and their respective numbers necessary for the recipe.
        /// </summary>
        public HashSet<RecipeElement> Elements => _elements;

        /// <summary>
        /// Time it takes in second for the crafting to finish.
        /// </summary>
        public float ExecutionTime => _executionTime;

        /// <summary>
        /// Name of the necessary interaction to perform the recipe.
        /// </summary>
        public string InteractionName => _interactionName;

#if UNITY_EDITOR

        /// <summary>
        /// Necessary to be able to edit in editor recipe elements.
        /// Not straightforward since they are in a Hashset.
        /// </summary>
        [SerializeField]
        private List<RecipeElement> _recipeElements;

        public void OnAfterDeserialize()
        {
            // just transfer things from the list to the hashset.
            if (_recipeElements == null || _elements == null) return;
            foreach (RecipeElement item in _recipeElements)
            {
                _elements.Add(item);
            }
            _recipeElements = null;
        }

        public void OnBeforeSerialize()
        {
            // just transfer things from the hashset to the list.
            if (_elements == null) return;
            _recipeElements = new List<RecipeElement>(_elements);
            _elements= null;
        }
    }
#endif
}


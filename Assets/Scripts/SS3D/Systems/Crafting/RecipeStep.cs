using NaughtyAttributes;
using SS3D.Data.AssetDatabases;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Represent a single step in a recipe, hold an optionnal result from reaching the step, has a name,
    /// and some data regarding what to do when it's reached.
    /// </summary>
    [Serializable]
    public class RecipeStep
    {
        [SerializeField]
        [Tooltip("The name of the step. Choose it carefully as it is currently how one can refer to it.")]
        private string _name;

        [FormerlySerializedAs("IsInitialState")]
        [HideIf(nameof(ShowInitialState))]
        [AllowNesting]
        [Tooltip("If true, the recipe starts here. There is only one initial step in a recipe."
            + " A step can't be terminal and initial at the same time.")]
        [SerializeField]
        private bool _isInitialState;

        [FormerlySerializedAs("IsTerminal")]
        [AllowNesting]
        [HideIf(nameof(_isInitialState))]
        [Tooltip("If true, the target is consumed upon reaching this step (despawned)."
            + " A step can't be terminal and initial at the same time.")]
        [SerializeField]
        private bool _isTerminal;

        [FormerlySerializedAs("CustomCraft")]
        [ShowIf(nameof(_isTerminal))]
        [AllowNesting]
        [Tooltip("If true, the result of the recipe step should use a custom craft method, instead of the default one."
            + "Implement the method in a component implementing the ICraftable interface."
            + "Should only be true on a terminal step.")]
        [SerializeField]
        private bool _customCraft;

        [FormerlySerializedAs("Result")]
        [ShowIf(nameof(_isTerminal))]
        [AllowNesting]
        [Tooltip("A resulting object that will spawn at the end of the crafting process, optional.")]
        [SerializeField]
        private WorldObjectAssetReference _result;

        public RecipeStep(CraftingRecipe recipe, string name)
        {
            Recipe = recipe;
            _isTerminal = false;
            _name = name;
            _customCraft = false;
            _result = new();
        }

        public WorldObjectAssetReference Result => _result;

        public bool CustomCraft => _customCraft;

        public bool IsTerminal => _isTerminal;

        public bool IsInitialState => _isInitialState;

        /// <summary>
        /// The recipe this step belongs to.
        /// </summary>
        public CraftingRecipe Recipe { get; set; }

        /// <summary>
        /// Name of the recipe step.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// If true, show IsInitialState in the inspector
        /// </summary>
        private bool ShowInitialState => Recipe.HasInitial && !_isInitialState;

        public WorldObjectAssetReference GetResultOrTarget() => _result ? _result : Recipe.Target;

        public bool TryGetResult(out WorldObjectAssetReference result)
        {
            result = _result;
            return _result;
        }
    }
}

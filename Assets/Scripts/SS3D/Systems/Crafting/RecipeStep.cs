using NaughtyAttributes;
using SS3D.Data.AssetDatabases;
using System;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Represent a single step in a recipe, hold an optionnal result from reaching the step, has a name,
    /// and some data regarding what to do when it's reached.
    /// </summary>
    [Serializable]
    public class RecipeStep
    {
        /// <summary>
        /// The recipe this step belongs to.
        /// </summary>
        [NonSerialized]
        public CraftingRecipe Recipe;

        [SerializeField]
        [Tooltip("The name of the step. Choose it carefully as it is currently how one can refer to it.")] 
        private string _name;

        [HideIf(nameof(ShowInitialState))]
        [AllowNesting]
        [Tooltip("If true, the recipe starts here. There is only one initial step in a recipe."
            + " A step can't be terminal and initial at the same time.")] 
        public bool IsInitialState;

        [AllowNesting]
        [HideIf(nameof(IsInitialState))]
        [Tooltip("If true, the target is consumed upon reaching this step (despawned)."
            + " A step can't be terminal and initial at the same time.")] 
        public bool IsTerminal;

        [ShowIf(nameof(IsTerminal))]
        [AllowNesting]
        [Tooltip("If true, the result of the recipe step should use a custom craft method, instead of the default one."
            + "Implement the method in a component implementing the ICraftable interface."
            + "Should only be true on a terminal step.")] 
        public bool CustomCraft;

        [ShowIf(nameof(IsTerminal))]
        [AllowNesting]
        [Tooltip("A resulting object that will spawn at the end of the crafting process, optional.")] 
        public WorldObjectAssetReference Result;
        
        /// <summary>
        /// If true, show IsInitialState in the inspector
        /// </summary>
        private bool ShowInitialState => Recipe.HasInitial && !IsInitialState; 

        /// <summary>
        /// Name of the recipe step.
        /// </summary>
        public string Name => _name;

        public WorldObjectAssetReference GetResultOrTarget() => Result ? Result : Recipe.Target;

        public RecipeStep(CraftingRecipe recipe, string name)
        {
            Recipe = recipe;
            IsTerminal = false;
            _name = name;
            CustomCraft = false;
            Result = new();
        }
        
        public bool TryGetResult(out WorldObjectAssetReference result)
        {
            result = Result;
            return Result is not null;
        }
    }
}
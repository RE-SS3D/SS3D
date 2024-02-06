using SS3D.Data.AssetDatabases;
using SS3D.Systems.Crafting;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

namespace SS3D.Systems.Crafting
{
    [Serializable]
    public class RecipeStep
    {

        public CraftingRecipe Recipe { get; set; }

        /// <summary>
        /// A list of resulting objects that will spawn at the end of the crafting process.
        /// </summary>
        [SerializeField]
        private WorldObjectAssetReference _result;

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

        public RecipeStep(CraftingRecipe recipe, string name)
        {
            Recipe = recipe;
            _isTerminal = false;
            _name = name;
            _customCraft = false;
            _result = new();
        }


        /// <summary>
        /// If true, this is a final step of the recipe and the recipe target should be consumed (despawned).
        /// There can be more than one terminal step.
        /// </summary>
        public bool IsTerminal => _isTerminal;

        public bool TryGetResult(out WorldObjectAssetReference result)
        {
            result = _result;
            return _result != null;
        } 

        /// <summary>
        /// If true, is the original step of the recipe. Only one original step can exist.
        /// </summary>
        public bool IsInitialState => _isInitialState;

        /// <summary>
        /// If true, the result of the recipe step should use a custom craft method, instead of the default one.
        /// Should only be true on a terminal step.
        /// </summary>
        public bool CustomCraft => _customCraft;

        /// <summary>
        /// Name of the recipe step.
        /// </summary>
        public string Name => _name;

    }
}

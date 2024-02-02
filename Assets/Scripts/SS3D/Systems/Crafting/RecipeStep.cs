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
            Recipe = recipe;
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
}

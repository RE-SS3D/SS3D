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
        private AdjacencyGraph<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>> _recipeGraph;

        public AdjacencyGraph<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>> RecipeGraph => _recipeGraph;

        private void OnValidate()
        {
            Init();
        }

        /// <summary>
        /// Editor only method, that build the directed graph representing the recipe.
        /// </summary>
        private void Init()
        {
            _recipeGraph = new();

            foreach (RecipeStep step in steps)
            {
                _recipeGraph.AddVertex(step);
                step.Recipe = this;
            }

            foreach (RecipeStepLink link in stepLinks)
            {
                link.Recipe = this;
                bool stepFromFound = TryGetStep(link.From, out RecipeStep stepFrom);
                bool stepToFound = TryGetStep(link.To, out RecipeStep stepTo);

                if (stepFromFound && stepToFound)
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

        /// <summary>
        /// From a given step referenced by its name, get all the recipe links going from it to other steps.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<TaggedEdge<RecipeStep, RecipeStepLink>> GetLinksFromStep(string name)
        {
            if (!TryGetStep(name, out RecipeStep step)) return new List<TaggedEdge<RecipeStep, RecipeStepLink>>();
            _recipeGraph.TryGetOutEdges(step, out IEnumerable<TaggedEdge<RecipeStep, RecipeStepLink>> results);
            return results.ToList();
        }
    }
        
}


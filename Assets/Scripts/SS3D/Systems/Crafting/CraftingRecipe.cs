using QuikGraph;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Crafting recipes allow to replace a bunch of item by another, using a specific interaction.
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe", menuName = "SS3D/Crafting/Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Tooltip("The target of the crafting, what needs to be clicked on by the player to start the crafting.")]
        public WorldObjectAssetReference Target;
        
        [Tooltip("A bunch of recipe steps, representing each steps of the recipe.")]
        public List<RecipeStep> Steps;

        [Tooltip("A bunch of recipe links, which link recipe step together.")] 
        public List<RecipeStepLink> StepLinks;

        /// <summary>
        /// First step of the recipe, which should have the same name as the target of the recipe.
        /// </summary>
        public string RootStepName => Target.Prefab.name;
        
        public bool HasInitial => Steps.Any(x => x.IsInitialState);
        
        /// <summary>
        /// Graph representing all steps in a recipe and their link between each other.
        /// </summary>
        public AdjacencyGraph<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>> RecipeGraph { get; private set; }
        
        private void Awake()
        {
            Init();
        }

        private void OnValidate()
        {
            Init();
        }

        /// <summary>
        /// Editor only method, that build the directed graph representing the recipe, whenever something in the 
        /// crafting recipe is modified.
        /// </summary>
        private void Init()
        {
            RecipeGraph = new();

            foreach (RecipeStep step in Steps)
            {
                RecipeGraph.AddVertex(step);
                step.Recipe = this;
            }

            foreach (RecipeStepLink link in StepLinks)
            {
                link.Recipe = this;
                bool stepFromFound = TryGetStep(link.From, out RecipeStep stepFrom);
                bool stepToFound = TryGetStep(link.To, out RecipeStep stepTo);

                if (stepFromFound && stepToFound)
                {
                    RecipeGraph.AddEdge(new(stepFrom, stepTo, link));
                }
                else
                {
                    Log.Error(this, $"step with name {link.From} or step with name {link.To} not found in recipe {name}");
                }
            }
        }

        /// <summary>
        /// Try to get a specific step with a specific name.
        /// </summary>
        /// <returns></returns>
        public bool TryGetStep(string name, out RecipeStep step)
        {
            if (RecipeGraph == null)
            {
                Log.Error(this, "recipeGraph is null");
                step = null;
                return false;
            }
            step = RecipeGraph.Vertices.FirstOrDefault(x => x.Name == name);
            return step != null;
        }

        /// <summary>
        /// From a given step referenced by its name, get all the recipe links going from it to other steps.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<TaggedEdge<RecipeStep, RecipeStepLink>> GetLinksFromStep(string name)
        {
            if (!TryGetStep(name, out RecipeStep step)) return new();
            RecipeGraph.TryGetOutEdges(step, out IEnumerable<TaggedEdge<RecipeStep, RecipeStepLink>> results);
            return results.ToList();
        }
    }
}
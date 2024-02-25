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
        /// <summary>
        /// The target of the crafting, what needs to be clicked on by the player to start the crafting.
        /// </summary>
        [SerializeField]
        private WorldObjectAssetReference _target;

        /// <summary>
        /// The target of the recipe, which is the item on which the player must click to get the crafting interactions.
        /// </summary>
        public WorldObjectAssetReference Target => _target;


        /// <summary>
        /// A bunch of recipe steps, representing each steps of the recipe.
        /// </summary>
        public List<RecipeStep> steps;

        /// <summary>
        /// A bunch of recipe links, which link recipe step together.
        /// </summary>
        public List<RecipeStepLink> stepLinks;

        /// <summary>
        /// First step of the recipe, which should have the same name as the target of the recipe.
        /// </summary>
        public string RootStepName => Target.Prefab.name;

        /// <summary>
        /// Graph representing all steps in a recipe and their link between each other.
        /// </summary>
        private AdjacencyGraph<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>> _recipeGraph;

        public AdjacencyGraph<RecipeStep, TaggedEdge<RecipeStep, RecipeStepLink>> RecipeGraph => _recipeGraph;

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

        /// <summary>
        /// Try to get a specific step with a specific name.
        /// </summary>
        /// <returns></returns>
        public bool TryGetStep(string name, out RecipeStep step)
        {
            if(_recipeGraph == null)
            {
                Log.Error(this, "recipe graph should not be null");
                step = null;
                return false;
            }
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


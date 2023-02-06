using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Substances
{
    public class SubstanceRegistry : MonoBehaviour
    {
        public Dictionary<SubstanceType, Substance> Substances => substances;
        public Recipe[] Recipes => recipes;
        public static SubstanceRegistry Current => instance;
        
        [SerializeField]
        private Dictionary<SubstanceType, Substance> substances;
        [SerializeField]
        private Recipe[] recipes;
        private static SubstanceRegistry instance;

        /// <summary>
        /// Gets a substance based on id
        /// </summary>
        /// <param name="id">The id name of the substance</param>
        /// <returns>A substance or null if it wasn't found</returns>
        public Substance FromType(SubstanceType type)
        {
            return Substances[type];
        }

        /// <summary>
        /// Processes substances in a container
        /// </summary>
        public void ProcessContainer(SubstanceContainer container)
        {
            float temperature = container.Temperature;
            
            // Process recipes
            foreach (Recipe recipe in recipes)
            {
                // Check temperature limits
                if (temperature < recipe.MinimalTemperature || temperature > recipe.MaximalTemperature)
                {
                    continue;
                }
                
                // Gather the mole amount of every substance
                float[] moles = new float[recipe.Ingredients.Length];
                bool ingredientsPresent = true;
                for (var i = 0; i < recipe.Ingredients.Length; i++)
                {
                    ingredientsPresent = false;
                    foreach (var entry in container.Substances)
                    {
                        if (entry.Substance.Type == recipe.Ingredients[i].Type)
                        {
                            moles[i] = entry.Moles;
                            ingredientsPresent = true;
                            break;
                        }
                    }
                    
                    if (!ingredientsPresent)
                    {
                        break;
                    }
                }

                // Substance missing
                if (!ingredientsPresent)
                {
                    continue;
                }
                
                // Calculate the maximum amount of ingredients
                float totalIngredients = recipe.Ingredients.Sum(x => x.RelativeAmount);
                float maxConversion = float.MaxValue;
                for (var i = 0; i < moles.Length; i++)
                {
                    float relativeAmount = recipe.Ingredients[i].RelativeAmount;
                    float part = relativeAmount / totalIngredients;
                    float maxProduced = moles[i] / part;
                    if (maxProduced < maxConversion)
                    {
                        maxConversion = maxProduced;
                    }
                }
                
                // Calculate relative volume of ingredients
                Substance[] ingredientSubstances = new Substance[moles.Length];
                float ingredientsToVolume = 0;
                for (var i = 0; i < moles.Length; i++)
                {
                    var component = recipe.Ingredients[i];
                    var substance = ingredientSubstances[i] = FromType(component.Type);    
                    ingredientsToVolume += substance.MillilitersPerMole * component.RelativeAmount;
                }
                ingredientsToVolume /= totalIngredients;

                // Calculate relative volume of results
                float totalResults = recipe.Results.Sum(x => x.RelativeAmount);
                Substance[] resultSubstances = new Substance[recipe.Results.Length];
                float resultsToVolume = 0;
                for (var i = 0; i < recipe.Results.Length; i++)
                {
                    var component = recipe.Results[i];
                    var substance = resultSubstances[i] = FromType(component.Type);    
                    resultsToVolume += substance.MillilitersPerMole * component.RelativeAmount;
                }
                resultsToVolume /= totalResults;
                
                // Limit the reaction to prevent spill
                // TODO: Spill container into surroundings
                float remainingVolume = container.RemainingVolume;
                if (maxConversion * totalResults * resultsToVolume - maxConversion * ingredientsToVolume > remainingVolume)
                {
                    maxConversion = remainingVolume / resultsToVolume / totalResults;
                }

                // Remove ingredients
                for (var i = 0; i < moles.Length; i++)
                {
                    container.RemoveSubstance(ingredientSubstances[i], maxConversion * (recipe.Ingredients[i].RelativeAmount / totalIngredients));
                }
                
                // Add results
                foreach (var component in recipe.Results)
                {
                    container.AddSubstance(FromType(component.Type), maxConversion * (component.RelativeAmount / totalResults));
                }
                
                container.MarkDirty();
            }
        }
        
        private void Awake()
        {
            instance = this;
        }
    }
}

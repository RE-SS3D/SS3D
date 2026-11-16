using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    public readonly struct ReactionResult
    {
        public ReactionOutcome Outcome { get; }
        public HazardKind Hazard { get; }
        public float TemperatureDeltaKelvin { get; }
        public string RecipeId { get; }

        public ReactionResult(ReactionOutcome outcome, HazardKind hazard, float temperatureDeltaKelvin, string recipeId)
        {
            Outcome = outcome;
            Hazard = hazard;
            TemperatureDeltaKelvin = temperatureDeltaKelvin;
            RecipeId = recipeId;
        }

        public static ReactionResult Idle => new(ReactionOutcome.Idle, HazardKind.None, 0f, null);
    }

    /// <summary>
    /// Fixed-ratio recipe resolution with temperature/catalyst gates, near-miss, and incompatible pairs.
    /// Indexed by sorted ingredient-id key — not a full table scan per call after index build.
    /// </summary>
    public sealed class ReactionResolver
    {
        private readonly ReagentRegistry _registry;
        private readonly Dictionary<string, List<RecipeDefinition>> _recipesByIngredientKey = new(StringComparer.Ordinal);
        private readonly List<RecipeDefinition> _allRecipes = new();

        public ReactionResolver(ReagentRegistry registry, IEnumerable<RecipeDefinition> recipes)
        {
            _registry = registry;
            if (recipes == null)
            {
                return;
            }

            foreach (RecipeDefinition recipe in recipes)
            {
                if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0)
                {
                    continue;
                }

                _allRecipes.Add(recipe);
                string key = BuildIngredientKey(recipe.Ingredients);
                if (!_recipesByIngredientKey.TryGetValue(key, out List<RecipeDefinition> list))
                {
                    list = new List<RecipeDefinition>();
                    _recipesByIngredientKey[key] = list;
                }

                list.Add(recipe);
            }
        }

        public ReactionResult Resolve(List<MixtureEntry> mixture, float temperatureKelvin)
        {
            if (mixture == null || mixture.Count == 0 || _registry == null)
            {
                return ReactionResult.Idle;
            }

            if (TryFindIncompatible(mixture, out HazardKind incompatibleHazard))
            {
                return new ReactionResult(ReactionOutcome.Incompatible, incompatibleHazard, 0f, null);
            }

            string presentKey = BuildPresentIngredientKey(mixture);
            if (_recipesByIngredientKey.TryGetValue(presentKey, out List<RecipeDefinition> candidates))
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    RecipeDefinition recipe = candidates[i];
                    if (!HasCatalyst(mixture, recipe))
                    {
                        continue;
                    }

                    if (!RatiosMatch(mixture, recipe, SubstanceConstants.RecipeRatioEpsilon))
                    {
                        if (RatiosMatch(mixture, recipe, SubstanceConstants.NearMissRatioEpsilon))
                        {
                            return new ReactionResult(
                                ReactionOutcome.NearMiss,
                                recipe.NearMissHazard,
                                0f,
                                recipe.Id);
                        }

                        continue;
                    }

                    if (!recipe.TemperatureSatisfied(temperatureKelvin))
                    {
                        return new ReactionResult(
                            ReactionOutcome.NearMiss,
                            recipe.NearMissHazard,
                            0f,
                            recipe.Id);
                    }

                    ApplyRecipe(mixture, recipe);
                    return new ReactionResult(
                        ReactionOutcome.Success,
                        HazardKind.None,
                        recipe.ThermalDeltaKelvin,
                        recipe.Id);
                }
            }

            // Near-miss across recipes that share the same reagent set but different key build edge cases.
            for (int i = 0; i < _allRecipes.Count; i++)
            {
                RecipeDefinition recipe = _allRecipes[i];
                if (!ContainsAllIngredients(mixture, recipe))
                {
                    continue;
                }

                if (!HasCatalyst(mixture, recipe))
                {
                    continue;
                }

                if (RatiosMatch(mixture, recipe, SubstanceConstants.NearMissRatioEpsilon)
                    && !RatiosMatch(mixture, recipe, SubstanceConstants.RecipeRatioEpsilon))
                {
                    return new ReactionResult(
                        ReactionOutcome.NearMiss,
                        recipe.NearMissHazard,
                        0f,
                        recipe.Id);
                }
            }

            return ReactionResult.Idle;
        }

        private bool TryFindIncompatible(List<MixtureEntry> mixture, out HazardKind hazard)
        {
            hazard = HazardKind.None;
            IReadOnlyList<ReagentRegistry.IncompatiblePair> pairs = _registry.IncompatiblePairs;
            for (int p = 0; p < pairs.Count; p++)
            {
                ReagentRegistry.IncompatiblePair pair = pairs[p];
                if (MixtureOperations.GetVolume(mixture, pair.ReagentIdA) > SubstanceConstants.VolumeEpsilonMl
                    && MixtureOperations.GetVolume(mixture, pair.ReagentIdB) > SubstanceConstants.VolumeEpsilonMl)
                {
                    hazard = pair.Hazard == HazardKind.None ? HazardKind.Foam : pair.Hazard;
                    return true;
                }
            }

            return false;
        }

        private static bool HasCatalyst(List<MixtureEntry> mixture, RecipeDefinition recipe)
        {
            if (!recipe.RequiresCatalyst)
            {
                return true;
            }

            return MixtureOperations.GetVolume(mixture, recipe.CatalystReagentId) > SubstanceConstants.VolumeEpsilonMl;
        }

        private static bool ContainsAllIngredients(List<MixtureEntry> mixture, RecipeDefinition recipe)
        {
            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                if (MixtureOperations.GetVolume(mixture, recipe.Ingredients[i].ReagentId) <= SubstanceConstants.VolumeEpsilonMl)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool RatiosMatch(List<MixtureEntry> mixture, RecipeDefinition recipe, float epsilon)
        {
            float totalRelative = 0f;
            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                totalRelative += recipe.Ingredients[i].RelativeAmount;
            }

            if (totalRelative <= 0f)
            {
                return false;
            }

            float totalPresent = 0f;
            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                totalPresent += MixtureOperations.GetVolume(mixture, recipe.Ingredients[i].ReagentId);
            }

            if (totalPresent <= SubstanceConstants.VolumeEpsilonMl)
            {
                return false;
            }

            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                RecipeDefinition.RecipeComponent component = recipe.Ingredients[i];
                float expectedFraction = component.RelativeAmount / totalRelative;
                float actualFraction = MixtureOperations.GetVolume(mixture, component.ReagentId) / totalPresent;
                if (Mathf.Abs(expectedFraction - actualFraction) > epsilon)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ApplyRecipe(List<MixtureEntry> mixture, RecipeDefinition recipe)
        {
            float totalRelativeIn = 0f;
            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                totalRelativeIn += recipe.Ingredients[i].RelativeAmount;
            }

            float limitingBatches = float.PositiveInfinity;
            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                RecipeDefinition.RecipeComponent component = recipe.Ingredients[i];
                float have = MixtureOperations.GetVolume(mixture, component.ReagentId);
                float batches = have / (component.RelativeAmount / totalRelativeIn);
                if (batches < limitingBatches)
                {
                    limitingBatches = batches;
                }
            }

            if (float.IsInfinity(limitingBatches) || limitingBatches <= SubstanceConstants.VolumeEpsilonMl)
            {
                return;
            }

            // Consume ingredients proportional to one "batch" volume of the mixture of ingredients.
            float consumeVolume = limitingBatches;
            for (int i = 0; i < recipe.Ingredients.Length; i++)
            {
                RecipeDefinition.RecipeComponent component = recipe.Ingredients[i];
                float fraction = component.RelativeAmount / totalRelativeIn;
                MixtureOperations.Remove(mixture, component.ReagentId, consumeVolume * fraction);
            }

            float totalRelativeOut = 0f;
            for (int i = 0; i < recipe.Results.Length; i++)
            {
                totalRelativeOut += recipe.Results[i].RelativeAmount;
            }

            if (totalRelativeOut <= 0f)
            {
                return;
            }

            for (int i = 0; i < recipe.Results.Length; i++)
            {
                RecipeDefinition.RecipeComponent component = recipe.Results[i];
                float fraction = component.RelativeAmount / totalRelativeOut;
                MixtureOperations.Add(mixture, component.ReagentId, consumeVolume * fraction, float.PositiveInfinity);
            }
        }

        private static string BuildIngredientKey(RecipeDefinition.RecipeComponent[] ingredients)
        {
            var ids = new List<string>(ingredients.Length);
            for (int i = 0; i < ingredients.Length; i++)
            {
                if (!string.IsNullOrEmpty(ingredients[i].ReagentId))
                {
                    ids.Add(ingredients[i].ReagentId);
                }
            }

            ids.Sort(StringComparer.Ordinal);
            return string.Join("|", ids);
        }

        private static string BuildPresentIngredientKey(List<MixtureEntry> mixture)
        {
            var ids = new List<string>(mixture.Count);
            for (int i = 0; i < mixture.Count; i++)
            {
                if (mixture[i].VolumeMl > SubstanceConstants.VolumeEpsilonMl
                    && !string.IsNullOrEmpty(mixture[i].ReagentId))
                {
                    ids.Add(mixture[i].ReagentId);
                }
            }

            ids.Sort(StringComparer.Ordinal);
            return string.Join("|", ids);
        }
    }
}

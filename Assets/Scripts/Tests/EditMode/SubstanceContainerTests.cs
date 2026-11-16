using NUnit.Framework;
using SS3D.Systems.Substances;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>Volume-first mixture math and reaction resolution (no FishNet host required).</summary>
public class SubstanceContainerTests
{
    [Test]
    public void CantAddMoreWhenFull()
    {
        var entries = new List<MixtureEntry>();
        float accepted = MixtureOperations.Add(entries, "beer", 99999f, remainingCapacityMl: 100f);
        Assert.AreEqual(100f, accepted);
        Assert.AreEqual(100f, MixtureOperations.TotalVolumeMl(entries));
        float second = MixtureOperations.Add(entries, "beer", 10f, remainingCapacityMl: 0f);
        Assert.AreEqual(0f, second);
    }

    [Test]
    public void CantRemoveWhenEmpty()
    {
        var entries = new List<MixtureEntry>();
        float removed = MixtureOperations.Remove(entries, "beer", 10f);
        Assert.AreEqual(0f, removed);
    }

    [Test]
    public void RemoveProportionalKeepsRatio()
    {
        var entries = new List<MixtureEntry>
        {
            new("beer", 10f),
            new("water", 5f),
        };

        List<MixtureEntry> removed = MixtureOperations.RemoveProportional(entries, 1.5f);
        Assert.AreEqual(9f, MixtureOperations.GetVolume(entries, "beer"), 0.001f);
        Assert.AreEqual(4.5f, MixtureOperations.GetVolume(entries, "water"), 0.001f);
        Assert.AreEqual(1.5f, MixtureOperations.TotalVolumeMl(removed), 0.001f);
    }

    [Test]
    public void ExactRecipeProducesYieldAndThermalDelta()
    {
        ReagentRegistry registry = ScriptableObject.CreateInstance<ReagentRegistry>();
        RecipeDefinition recipe = CreateRecipe(
            "product_c",
            new[] { Comp("precursor_a", 1f), Comp("precursor_b", 1f) },
            new[] { Comp("product_c", 1f) },
            tMin: float.NegativeInfinity,
            thermal: 5f);

        var resolver = new ReactionResolver(registry, new[] { recipe });
        var mixture = new List<MixtureEntry>
        {
            new("precursor_a", 10f),
            new("precursor_b", 10f),
        };

        ReactionResult result = resolver.Resolve(mixture, temperatureKelvin: 293f);
        Assert.AreEqual(ReactionOutcome.Success, result.Outcome);
        Assert.AreEqual(5f, result.TemperatureDeltaKelvin);
        Assert.AreEqual(0f, MixtureOperations.GetVolume(mixture, "precursor_a"), 0.01f);
        Assert.AreEqual(0f, MixtureOperations.GetVolume(mixture, "precursor_b"), 0.01f);
        Assert.Greater(MixtureOperations.GetVolume(mixture, "product_c"), 0f);
    }

    [Test]
    public void HeatGatedRecipeNearMissesWhenCold()
    {
        ReagentRegistry registry = ScriptableObject.CreateInstance<ReagentRegistry>();
        RecipeDefinition recipe = CreateRecipe(
            "heat_product",
            new[] { Comp("heat_precursor", 1f), Comp("water", 1f) },
            new[] { Comp("heat_product", 1f) },
            tMin: 350f,
            thermal: 40f,
            nearMiss: HazardKind.GasRelease);

        var resolver = new ReactionResolver(registry, new[] { recipe });
        var mixture = new List<MixtureEntry>
        {
            new("heat_precursor", 10f),
            new("water", 10f),
        };

        ReactionResult result = resolver.Resolve(mixture, temperatureKelvin: 293f);
        Assert.AreEqual(ReactionOutcome.NearMiss, result.Outcome);
        Assert.AreEqual(HazardKind.GasRelease, result.Hazard);
    }

    [Test]
    public void IncompatiblePairRaisesHazard()
    {
        ReagentRegistry registry = ScriptableObject.CreateInstance<ReagentRegistry>();
        SetField(registry, "_incompatiblePairs", new[]
        {
            new ReagentRegistry.IncompatiblePair
            {
                ReagentIdA = "volatile_x",
                ReagentIdB = "volatile_y",
                Hazard = HazardKind.SmallBlast,
            },
        });

        var resolver = new ReactionResolver(registry, System.Array.Empty<RecipeDefinition>());
        var mixture = new List<MixtureEntry>
        {
            new("volatile_x", 5f),
            new("volatile_y", 5f),
        };

        ReactionResult result = resolver.Resolve(mixture, 293f);
        Assert.AreEqual(ReactionOutcome.Incompatible, result.Outcome);
        Assert.AreEqual(HazardKind.SmallBlast, result.Hazard);
    }

    private static RecipeDefinition.RecipeComponent Comp(string id, float amount) =>
        new() { ReagentId = id, RelativeAmount = amount };

    private static RecipeDefinition CreateRecipe(
        string id,
        RecipeDefinition.RecipeComponent[] ingredients,
        RecipeDefinition.RecipeComponent[] results,
        float tMin,
        float thermal,
        HazardKind nearMiss = HazardKind.Foam)
    {
        RecipeDefinition recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        SetField(recipe, "_id", id);
        SetField(recipe, "_ingredients", ingredients);
        SetField(recipe, "_results", results);
        SetField(recipe, "_minimumTemperatureKelvin", tMin);
        SetField(recipe, "_maximumTemperatureKelvin", float.PositiveInfinity);
        SetField(recipe, "_thermalDeltaKelvin", thermal);
        SetField(recipe, "_nearMissHazard", nearMiss);
        return recipe;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName} on {target.GetType().Name}");
        field.SetValue(target, value);
    }
}

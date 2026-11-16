#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Substances.Editor
{
    /// <summary>
    /// Generates core <see cref="ReagentDefinition"/> assets and <see cref="ReagentRegistry"/>.
    /// Tier B — no MenuItem; call from batch or substances content recipes.
    /// BatchMode: <c>-executeMethod SS3D.Systems.Substances.Editor.ReagentRegistryGenerator.CreateCoreRegistry</c>
    /// </summary>
    public static class ReagentRegistryGenerator
    {
        private const string RootFolder = "Assets/Content/Systems/Substances";
        private const string ReagentsFolder = RootFolder + "/Reagents";
        private const string RecipesFolder = RootFolder + "/Recipes";
        private const string RegistryPath = RootFolder + "/CoreReagentRegistry.asset";

        private readonly struct ReagentSeed
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly ReagentCategory Category;
            public readonly Color Color;
            public readonly float MolarMass;
            public readonly float MlPerMole;
            public readonly float BoilK;
            public readonly float FreezeK;
            public readonly float FlashK;

            public ReagentSeed(
                string id,
                string displayName,
                ReagentCategory category,
                Color color,
                float molarMass,
                float mlPerMole,
                float boilK,
                float freezeK,
                float flashK)
            {
                Id = id;
                DisplayName = displayName;
                Category = category;
                Color = color;
                MolarMass = molarMass;
                MlPerMole = mlPerMole;
                BoilK = boilK;
                FreezeK = freezeK;
                FlashK = flashK;
            }
        }

        private static readonly ReagentSeed[] CoreReagents =
        {
            new("oxygen", "Oxygen", ReagentCategory.Element, new Color(0.55f, 0.76f, 0.83f, 0.33f), 32f, 22.4f, 90f, 54f, float.PositiveInfinity),
            new("plasma", "Plasma", ReagentCategory.Element, new Color(0.85f, 0.2f, 0.85f, 0.4f), 40f, 22.4f, 400f, 100f, 350f),
            new("water", "Water", ReagentCategory.Drink, new Color(0.4f, 0.6f, 0.9f, 0.35f), 18f, 18f, 373.15f, 273.15f, float.PositiveInfinity),
            new("blood", "Blood", ReagentCategory.Organic, new Color(0.45f, 0.05f, 0.08f, 0.9f), 18f, 18f, 373.15f, 273.15f, float.PositiveInfinity),
            new("diesel", "Diesel", ReagentCategory.Fuel, new Color(0.35f, 0.3f, 0.1f, 0.85f), 200f, 1f, 520f, 230f, 330f),
            new("soda", "Soda", ReagentCategory.Drink, new Color(0.6f, 0.35f, 0.15f, 0.7f), 20f, 18f, 373.15f, 273.15f, float.PositiveInfinity),
            new("precursor_a", "Precursor A", ReagentCategory.Precursor, new Color(0.7f, 0.85f, 0.3f, 0.5f), 30f, 20f, 400f, 250f, float.PositiveInfinity),
            new("precursor_b", "Precursor B", ReagentCategory.Precursor, new Color(0.3f, 0.5f, 0.9f, 0.5f), 30f, 20f, 400f, 250f, float.PositiveInfinity),
            new("product_c", "Product C", ReagentCategory.Medicinal, new Color(0.2f, 0.8f, 0.5f, 0.55f), 40f, 20f, 400f, 250f, float.PositiveInfinity),
            new("heat_precursor", "Heat Precursor", ReagentCategory.Precursor, new Color(0.9f, 0.5f, 0.2f, 0.5f), 30f, 20f, 400f, 250f, float.PositiveInfinity),
            new("heat_product", "Heat Product", ReagentCategory.Industrial, new Color(0.95f, 0.7f, 0.2f, 0.55f), 40f, 20f, 400f, 250f, 360f),
            new("volatile_x", "Volatile X", ReagentCategory.Toxic, new Color(0.9f, 0.1f, 0.1f, 0.6f), 25f, 20f, 350f, 200f, 300f),
            new("volatile_y", "Volatile Y", ReagentCategory.Toxic, new Color(0.1f, 0.9f, 0.2f, 0.6f), 25f, 20f, 350f, 200f, 300f),
        };

        public static void CreateCoreRegistry()
        {
            EnsureFolder("Assets/Content/Systems", "Substances");
            EnsureFolder(RootFolder, "Reagents");
            EnsureFolder(RootFolder, "Recipes");

            var definitions = new List<ReagentDefinition>();
            foreach (ReagentSeed seed in CoreReagents)
            {
                string assetPath = $"{ReagentsFolder}/Reagent_{seed.Id}.asset";
                ReagentDefinition definition = AssetDatabase.LoadAssetAtPath<ReagentDefinition>(assetPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<ReagentDefinition>();
                    AssetDatabase.CreateAsset(definition, assetPath);
                }

                var serialized = new SerializedObject(definition);
                serialized.FindProperty("_id").stringValue = seed.Id;
                serialized.FindProperty("_displayName").stringValue = seed.DisplayName;
                serialized.FindProperty("_category").enumValueIndex = (int)seed.Category;
                serialized.FindProperty("_color").colorValue = seed.Color;
                serialized.FindProperty("_molarMass").floatValue = seed.MolarMass;
                serialized.FindProperty("_millilitersPerMole").floatValue = seed.MlPerMole;
                serialized.FindProperty("_boilingPointKelvin").floatValue = seed.BoilK;
                serialized.FindProperty("_freezingPointKelvin").floatValue = seed.FreezeK;
                serialized.FindProperty("_flashPointKelvin").floatValue = seed.FlashK;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                definitions.Add(definition);
            }

            RecipeDefinition productC = CreateRecipe(
                $"{RecipesFolder}/Recipe_ProductC.asset",
                "product_c",
                new[] { ("precursor_a", 1f), ("precursor_b", 1f) },
                new[] { ("product_c", 1f) },
                catalyst: null,
                tMin: float.NegativeInfinity,
                tMax: float.PositiveInfinity,
                thermalDelta: 5f,
                nearMiss: HazardKind.Foam);

            RecipeDefinition heatProduct = CreateRecipe(
                $"{RecipesFolder}/Recipe_HeatProduct.asset",
                "heat_product",
                new[] { ("heat_precursor", 1f), ("water", 1f) },
                new[] { ("heat_product", 1f) },
                catalyst: null,
                tMin: 350f,
                tMax: float.PositiveInfinity,
                thermalDelta: 40f,
                nearMiss: HazardKind.GasRelease);

            ReagentRegistry registry = AssetDatabase.LoadAssetAtPath<ReagentRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<ReagentRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            var registrySerialized = new SerializedObject(registry);
            SerializedProperty array = registrySerialized.FindProperty("_definitions");
            array.arraySize = definitions.Count;
            for (int i = 0; i < definitions.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }

            SerializedProperty recipes = registrySerialized.FindProperty("_recipes");
            recipes.arraySize = 2;
            recipes.GetArrayElementAtIndex(0).objectReferenceValue = productC;
            recipes.GetArrayElementAtIndex(1).objectReferenceValue = heatProduct;

            SerializedProperty pairs = registrySerialized.FindProperty("_incompatiblePairs");
            pairs.arraySize = 1;
            SerializedProperty pair0 = pairs.GetArrayElementAtIndex(0);
            pair0.FindPropertyRelative("ReagentIdA").stringValue = "volatile_x";
            pair0.FindPropertyRelative("ReagentIdB").stringValue = "volatile_y";
            pair0.FindPropertyRelative("Hazard").enumValueIndex = (int)HazardKind.SmallBlast;
            registrySerialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ReagentRegistryGenerator] Wrote {definitions.Count} reagents and registry at {RegistryPath}.");
        }

        private static RecipeDefinition CreateRecipe(
            string assetPath,
            string id,
            (string id, float amount)[] ingredients,
            (string id, float amount)[] results,
            string catalyst,
            float tMin,
            float tMax,
            float thermalDelta,
            HazardKind nearMiss)
        {
            RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(assetPath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
                AssetDatabase.CreateAsset(recipe, assetPath);
            }

            var serialized = new SerializedObject(recipe);
            serialized.FindProperty("_id").stringValue = id;
            WriteComponents(serialized.FindProperty("_ingredients"), ingredients);
            WriteComponents(serialized.FindProperty("_results"), results);
            serialized.FindProperty("_catalystReagentId").stringValue = catalyst ?? string.Empty;
            serialized.FindProperty("_minimumTemperatureKelvin").floatValue = tMin;
            serialized.FindProperty("_maximumTemperatureKelvin").floatValue = tMax;
            serialized.FindProperty("_thermalDeltaKelvin").floatValue = thermalDelta;
            serialized.FindProperty("_nearMissHazard").enumValueIndex = (int)nearMiss;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return recipe;
        }

        private static void WriteComponents(SerializedProperty array, (string id, float amount)[] components)
        {
            array.arraySize = components.Length;
            for (int i = 0; i < components.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("ReagentId").stringValue = components[i].id;
                element.FindPropertyRelative("RelativeAmount").floatValue = components[i].amount;
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif

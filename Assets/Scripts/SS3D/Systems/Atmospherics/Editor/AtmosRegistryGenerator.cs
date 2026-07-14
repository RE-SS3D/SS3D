#if UNITY_EDITOR
using System.Collections.Generic;
using SS3D.Systems.Atmospherics.Visualization;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Editor
{
    /// <summary>
    /// Generates the core <see cref="GasRegistry"/> and its <see cref="GasDefinition"/> assets so the
    /// atmospherics subsystem can be wired without hand-authoring ScriptableObjects.
    /// </summary>
    public static class AtmosRegistryGenerator
    {
        private const string RootFolder = "Assets/Content/Systems/Atmospherics";
        private const string GasFolder = RootFolder + "/Gases";
        private const string RegistryPath = RootFolder + "/CoreGasRegistry.asset";

        [MenuItem("SS3D/Atmospherics/Create Core Gas Registry")]
        public static void CreateCoreGasRegistry()
        {
            EnsureFolder("Assets/Content/Systems", "Atmospherics");
            EnsureFolder(RootFolder, "Gases");

            var definitions = new List<GasDefinition>();
            foreach (GasDefault gas in GasDefaults.Core)
            {
                string assetPath = $"{GasFolder}/Gas_{gas.DisplayName.Replace(" ", string.Empty)}.asset";
                GasDefinition definition = AssetDatabase.LoadAssetAtPath<GasDefinition>(assetPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<GasDefinition>();
                    AssetDatabase.CreateAsset(definition, assetPath);
                }

                var serialized = new SerializedObject(definition);
                serialized.FindProperty("_id").intValue = gas.Id;
                serialized.FindProperty("_displayName").stringValue = gas.DisplayName;
                serialized.FindProperty("_molarMass").floatValue = gas.MolarMass;
                serialized.FindProperty("_specificHeat").floatValue = gas.SpecificHeat;

                GasVisualProfile visualProfile = GasVisualProfileBuilder.CoreDefaultProfile(gas.Id);
                SerializedProperty visual = serialized.FindProperty("_visualProfile");
                visual.FindPropertyRelative("ScatterColor").colorValue = visualProfile.ScatterColor;
                visual.FindPropertyRelative("ScatterStrength").floatValue = visualProfile.ScatterStrength;
                visual.FindPropertyRelative("EmissionColor").colorValue = visualProfile.EmissionColor;
                visual.FindPropertyRelative("EmissionIntensity").floatValue = visualProfile.EmissionIntensity;
                visual.FindPropertyRelative("DistortionScale").floatValue = visualProfile.DistortionScale;
                visual.FindPropertyRelative("TurbulenceScale").floatValue = visualProfile.TurbulenceScale;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                definitions.Add(definition);
            }

            GasRegistry registry = AssetDatabase.LoadAssetAtPath<GasRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<GasRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            var registrySerialized = new SerializedObject(registry);
            SerializedProperty array = registrySerialized.FindProperty("_definitions");
            array.arraySize = definitions.Count;
            for (int i = 0; i < definitions.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            registrySerialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnityEditor.Selection.activeObject = registry;
            EditorGUIUtility.PingObject(registry);
            Debug.Log($"Created core gas registry at {RegistryPath} with {definitions.Count} gases. " +
                "Assign it to the AtmosSubSystem in the scene.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif

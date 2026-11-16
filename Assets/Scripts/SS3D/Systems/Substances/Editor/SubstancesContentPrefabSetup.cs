#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Substances.Editor
{
    /// <summary>
    /// Rewires mug/soda/tank prefabs onto volume-first <see cref="SubstanceContainer"/> + examine provider.
    /// </summary>
    public static class SubstancesContentPrefabSetup
    {
        private readonly struct Target
        {
            public readonly string PrefabPath;
            public readonly float CapacityMl;
            public readonly string ReagentId;
            public readonly float InitialVolumeMl;

            public Target(string prefabPath, float capacityMl, string reagentId, float initialVolumeMl)
            {
                PrefabPath = prefabPath;
                CapacityMl = capacityMl;
                ReagentId = reagentId;
                InitialVolumeMl = initialVolumeMl;
            }
        }

        private static readonly Target[] Targets =
        {
            new("Assets/Content/WorldObjects/Items/Functional/Storage/Substances/Mug.prefab", 100f, null, 0f),
            new("Assets/Content/WorldObjects/Items/Consumable/Food/Drinks/Soda/SodaCanGeneric.prefab", 330f, "soda", 330f),
            new("Assets/Content/WorldObjects/Items/Consumable/Food/Drinks/Soda/SodaCanCannedAir.prefab", 330f, "oxygen", 330f),
            new("Assets/Content/WorldObjects/Items/Functional/Generic/Tanks/OxygenTank.prefab", 1000f, "oxygen", 1000f),
            new("Assets/Content/WorldObjects/Items/Functional/Generic/Tanks/PlasmaTank.prefab", 1000f, "plasma", 1000f),
            new("Assets/Content/WorldObjects/Furniture/Machines/Generic/Tanks/FuelTank.prefab", 5000f, "diesel", 5000f),
        };

        public static int SetupAll()
        {
            int updated = 0;
            for (int i = 0; i < Targets.Length; i++)
            {
                if (Setup(Targets[i]))
                {
                    updated++;
                }
            }

            return updated;
        }

        private static bool Setup(Target target)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(target.PrefabPath);
            try
            {
                SubstanceContainer container = root.GetComponent<SubstanceContainer>();
                if (container == null)
                {
                    container = root.GetComponentInChildren<SubstanceContainer>(true);
                }

                if (container == null)
                {
                    container = root.AddComponent<SubstanceContainer>();
                }

                var serialized = new SerializedObject(container);
                serialized.FindProperty("_defaultVolumeMl").floatValue = target.CapacityMl;
                SerializedProperty initial = serialized.FindProperty("_initialMixture");
                if (string.IsNullOrEmpty(target.ReagentId) || target.InitialVolumeMl <= 0f)
                {
                    initial.arraySize = 0;
                }
                else
                {
                    initial.arraySize = 1;
                    SerializedProperty entry = initial.GetArrayElementAtIndex(0);
                    entry.FindPropertyRelative("ReagentId").stringValue = target.ReagentId;
                    entry.FindPropertyRelative("VolumeMl").floatValue = target.InitialVolumeMl;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();

                SubstanceContainerExaminable examinable = container.GetComponent<SubstanceContainerExaminable>();
                if (examinable == null)
                {
                    examinable = container.gameObject.AddComponent<SubstanceContainerExaminable>();
                }

                var examinableSerialized = new SerializedObject(examinable);
                examinableSerialized.FindProperty("_container").objectReferenceValue = container;
                examinableSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, target.PrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif

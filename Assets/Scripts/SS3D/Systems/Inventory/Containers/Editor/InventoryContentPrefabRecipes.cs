#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Domain recipe aggregator for non-Human inventory content prefabs (tier B — see
    /// 2026-07_editor-tooling-tiers.md). Human hands / ContainerInteractive strip stay on
    /// <c>HumanPrefabRecipes</c>.
    /// </summary>
    public static class InventoryContentPrefabRecipes
    {
        [MenuItem("SS3D/Inventory/Run Content Prefab Recipes")]
        public static void RunAllMenu()
        {
            int clothing = ClothingPrefabSetup.SetupAll();
            clothing += SyndicateHardsuitSetup.SetupAll();
            clothing += GasTankSetup.SetupAll();
            int storage = StorageContainerPrefabSetup.HookUpAll();

            EditorUtility.DisplayDialog(
                "Inventory Content Prefab Recipes",
                $"Clothing presentation: updated {clothing} prefab(s).\n" +
                $"Storage hook-up: updated {storage} prefab(s).",
                "OK");
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.InventoryContentPrefabRecipes.RunAllBatch</c></summary>
        public static void RunAllBatch()
        {
            int clothing = ClothingPrefabSetup.SetupAll();
            clothing += SyndicateHardsuitSetup.SetupAll();
            clothing += GasTankSetup.SetupAll();
            int storage = StorageContainerPrefabSetup.HookUpAll();
            Debug.Log(
                $"[InventoryContentPrefabRecipes] Clothing {clothing}; Storage {storage}.");

            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif

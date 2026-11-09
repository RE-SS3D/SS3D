#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Construction.Editor
{
    /// <summary>
    /// Domain recipe aggregator for construction content (tier B — see 2026-07_editor-tooling-tiers.md).
    /// Lives in the Systems assembly (no editor asmdef) so EditMode tests can read the recipe tables.
    /// </summary>
    public static class ConstructionContentPrefabRecipes
    {
        [MenuItem("SS3D/Construction/Run Content Prefab Recipes")]
        public static void RunAllMenu()
        {
            string summary = RunAll();
            EditorUtility.DisplayDialog("Construction Recipes", summary, "OK");
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Construction.Editor.ConstructionContentPrefabRecipes.RunAllBatch</c></summary>
        public static void RunAllBatch()
        {
            Debug.Log($"[ConstructionContentPrefabRecipes] {RunAll()}");

            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static string RunAll()
        {
            int items = ConstructionItemRecipe.SetupAll();
            bool hub = ConstructionHubRecipe.SetupAll();
            bool purged = LoadingBarPurge.Run();
            return $"Construction items: updated {items} prefab(s).\n"
                + $"NetworkSystemsHub: {(hub ? "added ConstructionPlacementSubSystem" : "already set up")}.\n"
                + $"LoadingBar purge: {(purged ? "cleaned up" : "nothing left")}.";
        }
    }
}
#endif

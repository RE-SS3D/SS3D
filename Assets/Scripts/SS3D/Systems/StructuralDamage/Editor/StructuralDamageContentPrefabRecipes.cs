#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage.Editor
{
    /// <summary>
    /// Domain recipe aggregator for structural-damage content (tier B — see
    /// 2026-07_editor-tooling-tiers.md).
    /// </summary>
    public static class StructuralDamageContentPrefabRecipes
    {
        [MenuItem("SS3D/Structural Damage/Run Content Prefab Recipes")]
        public static void RunAllMenu()
        {
            BlastVfxSetup.SetupAll();
            ExplosivePrefabSetup.SetupAll();
            int integrity = StructuralIntegrityPrefabSetup.SetupAll();

            EditorUtility.DisplayDialog(
                "Structural Damage Recipes",
                $"Blast VFX assets ensured.\nStructural integrity presentation: updated {integrity} prefab(s).",
                "OK");
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.StructuralDamage.Editor.StructuralDamageContentPrefabRecipes.RunAllBatch</c></summary>
        public static void RunAllBatch()
        {
            BlastVfxSetup.SetupAll();
            ExplosivePrefabSetup.SetupAll();
            int integrity = StructuralIntegrityPrefabSetup.SetupAll();
            Debug.Log($"[StructuralDamageContentPrefabRecipes] Blast VFX done; integrity {integrity}.");

            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif

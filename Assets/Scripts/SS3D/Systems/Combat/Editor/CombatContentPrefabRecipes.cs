#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>
    /// Domain recipe aggregator for combat content prefabs (tier B — see
    /// 2026-07_editor-tooling-tiers.md). Individual Setup* classes keep static APIs; this is the
    /// discoverable menu / batch entry.
    /// </summary>
    public static class CombatContentPrefabRecipes
    {
        [MenuItem("SS3D/Combat/Run Content Prefab Recipes")]
        public static void RunAllMenu()
        {
            NapalmHubRecipe.SetupAll();
            int melee = MeleePrefabSetup.SetupAll();
            int ranged = RangedPrefabSetup.SetupAll();
            int armor = ArmorPrefabSetup.SetupAll();

            EditorUtility.DisplayDialog(
                "Combat Content Prefab Recipes",
                $"Melee: updated {melee} prefab(s).\n" +
                $"Ranged: updated {ranged} prefab(s).\n" +
                $"Armor: updated {armor} prefab(s).",
                "OK");
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Combat.Editor.CombatContentPrefabRecipes.RunAllBatch</c></summary>
        public static void RunAllBatch()
        {
            NapalmHubRecipe.SetupAll();
            int melee = MeleePrefabSetup.SetupAll();
            int ranged = RangedPrefabSetup.SetupAll();
            int armor = ArmorPrefabSetup.SetupAll();
            Debug.Log(
                $"[CombatContentPrefabRecipes] Melee {melee}; Ranged {ranged}; Armor {armor}.");

            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using SS3D.Systems.Combat;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>
    /// Wires Phase 5 armor extension onto the interim test piece (PrefabUtility — not Human.prefab).
    /// </summary>
    public static class ArmorPrefabSetup
    {
        private const string SecurityJumpsuit = "Assets/Content/WorldObjects/Items/Clothing/JumpsuitSecurity.prefab";

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Combat.Editor.ArmorPrefabSetup.SetupBatch</c>
        /// Prefer <see cref="CombatContentPrefabRecipes.RunAllBatch"/> for domain re-runs.</summary>
        public static void SetupBatch()
        {
            int updated = SetupAll();
            Debug.Log($"[ArmorPrefabSetup] Updated {updated} prefabs.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated > 0 ? 0 : 1);
            }
        }

        public static int SetupAll()
        {
            int updated = 0;
            if (EnsureArmor(SecurityJumpsuit, ArmorProfile.SecurityJumpsuit))
            {
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return updated;
        }

        private static bool EnsureArmor(string path, ArmorProfile profile)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[ArmorPrefabSetup] Missing {path}");
                return false;
            }

            try
            {
                if (!root.TryGetComponent(out ArmorItemExtension extension))
                {
                    extension = root.AddComponent<ArmorItemExtension>();
                }

                SerializedObject so = new(extension);
                so.FindProperty("_profile.BruteAbsorption").floatValue = profile.BruteAbsorption;
                so.FindProperty("_profile.BurnAbsorption").floatValue = profile.BurnAbsorption;
                so.FindProperty("_profile.MaxIntegrity").floatValue = profile.MaxIntegrity;
                so.FindProperty("_profile.CoveredZones").intValue = (int)profile.CoveredZones;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[ArmorPrefabSetup] Armor on {path}");
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

#if UNITY_EDITOR
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Construction.Editor
{
    /// <summary>
    /// Tier-B recipe: stack limits and initial counts on the construction materials (D5, D12) and the
    /// <see cref="ConstructionTool"/> / <see cref="ConstructionMaterial"/> markers. Registered on
    /// <see cref="ConstructionContentPrefabRecipes"/>. Idempotent.
    /// </summary>
    public static class ConstructionItemRecipe
    {
        public const string SteelSheetPath = "Assets/Content/WorldObjects/Items/Functional/Materials/SteelSheet.prefab";
        public const string GlassSheetPath = "Assets/Content/WorldObjects/Items/Functional/Materials/GlassSheet.prefab";
        public const string FloorTilePath = "Assets/Content/WorldObjects/Items/Functional/Generic/FloorTile.prefab";
        public const string WelderPath = "Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Welder.prefab";
        public const string CrowbarPath = "Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Crowbar.prefab";

        /// <summary>Stack limit and initial count per material. First guesses for the balancing pass.</summary>
        public static readonly (string Path, ConstructionMaterialKind Kind, int MaxStack, int Initial)[] Materials =
        {
            (SteelSheetPath, ConstructionMaterialKind.SteelSheet, 50, 20),
            (GlassSheetPath, ConstructionMaterialKind.GlassSheet, 50, 20),
            (FloorTilePath, ConstructionMaterialKind.FloorTile, 20, 10),
        };

        public static readonly (string Path, ConstructionToolKind Kind)[] Tools =
        {
            (WelderPath, ConstructionToolKind.Welder),
            (CrowbarPath, ConstructionToolKind.Crowbar),
        };

        public static int SetupAll()
        {
            int updated = 0;
            foreach ((string path, ConstructionMaterialKind kind, int maxStack, int initial) in Materials)
            {
                if (Apply(path, root =>
                    {
                        SerializedObject item = new(root.GetComponent<Item>());
                        item.FindProperty("_maxStackSize").intValue = maxStack;
                        item.FindProperty("_initialStackCount").intValue = initial;
                        item.ApplyModifiedPropertiesWithoutUndo();

                        ConstructionMaterial material = GetOrAdd<ConstructionMaterial>(root);
                        SerializedObject so = new(material);
                        so.FindProperty("_kind").enumValueIndex = (int)kind;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }))
                {
                    updated++;
                }
            }

            foreach ((string path, ConstructionToolKind kind) in Tools)
            {
                if (Apply(path, root =>
                    {
                        ConstructionTool tool = GetOrAdd<ConstructionTool>(root);
                        SerializedObject so = new(tool);
                        so.FindProperty("_kind").enumValueIndex = (int)kind;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }))
                {
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            return updated;
        }

        // Not ??: GetComponent can return Unity's fake null in the Editor.
        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T existing = root.GetComponent<T>();
            return existing != null ? existing : root.AddComponent<T>();
        }

        private static bool Apply(string path, System.Action<GameObject> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[ConstructionItemRecipe] Missing prefab: {path}");
                return false;
            }

            try
            {
                if (root.GetComponent<Item>() == null)
                {
                    Debug.LogError($"[ConstructionItemRecipe] No Item on {path}");
                    return false;
                }

                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
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

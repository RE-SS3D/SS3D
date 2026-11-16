#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Wires <see cref="ClothingItemPresentation"/> + root world collider on jumpsuit base
    /// (variants inherit). PrefabUtility — do not hand-edit mega-prefabs.
    /// </summary>
    public static class ClothingPrefabSetup
    {
        private const string GreyJumpsuit = "Assets/Content/WorldObjects/Items/Clothing/JumpsuitGrey.prefab";
        private const string WornChildName = "Jumpsuit";
        private const string FoldedChildName = "JumpsuitFolded";

        // Sized from the retired JumpsuitSecurityFolded orphan prefab.
        private static readonly Vector3 ColliderSize = new(0.43f, 0.173f, 0.351f);
        private static readonly Vector3 ColliderCenter = new(0f, 0.015f, -0.015f);

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.ClothingPrefabSetup.SetupBatch</c>
        /// Prefer <see cref="InventoryContentPrefabRecipes.RunAllBatch"/> for domain re-runs.</summary>
        public static void SetupBatch()
        {
            int updated = SetupAll();
            Debug.Log($"[ClothingPrefabSetup] Updated {updated} prefab(s).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated > 0 ? 0 : 1);
            }
        }

        public static int SetupAll()
        {
            int updated = 0;
            if (SetupGrey())
            {
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return updated;
        }

        private static bool SetupGrey()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GreyJumpsuit);
            if (root == null)
            {
                Debug.LogError($"[ClothingPrefabSetup] Missing {GreyJumpsuit}");
                return false;
            }

            try
            {
                if (!root.TryGetComponent(out Cloth _))
                {
                    Debug.LogError($"[ClothingPrefabSetup] No Cloth on {GreyJumpsuit}");
                    return false;
                }

                Transform worn = root.transform.Find(WornChildName);
                Transform folded = root.transform.Find(FoldedChildName);
                if (worn == null || folded == null)
                {
                    Debug.LogError(
                        $"[ClothingPrefabSetup] Expected children '{WornChildName}' and '{FoldedChildName}' on {GreyJumpsuit}");
                    return false;
                }

                if (!root.TryGetComponent(out ClothingItemPresentation presentation))
                {
                    presentation = root.AddComponent<ClothingItemPresentation>();
                }

                SerializedObject so = new(presentation);
                so.FindProperty("_worldRoot").objectReferenceValue = folded.gameObject;
                so.FindProperty("_wornShapedRoot").objectReferenceValue = worn.gameObject;
                so.ApplyModifiedPropertiesWithoutUndo();

                folded.gameObject.SetActive(true);
                worn.gameObject.SetActive(false);

                if (!root.TryGetComponent(out BoxCollider box))
                {
                    box = root.AddComponent<BoxCollider>();
                }

                box.size = ColliderSize;
                box.center = ColliderCenter;

                PrefabUtility.SaveAsPrefabAsset(root, GreyJumpsuit);
                Debug.Log($"[ClothingPrefabSetup] World presentation on {GreyJumpsuit}");
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

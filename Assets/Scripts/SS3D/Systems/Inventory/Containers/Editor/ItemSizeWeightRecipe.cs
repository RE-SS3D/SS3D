#if UNITY_EDITOR
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Tier-B recipe: applies <see cref="ItemSizeWeightTable"/> to every item prefab and the medkit/toolbox
    /// grids. Registered on <see cref="InventoryContentPrefabRecipes"/>; backpack, toolbelt and lockers are
    /// applied by <see cref="StorageContainerPrefabSetup"/>.
    /// </summary>
    public static class ItemSizeWeightRecipe
    {
        public static int SetupAll()
        {
            int updated = 0;
            foreach (ItemSizeWeightTable.Entry entry in ItemSizeWeightTable.Entries)
            {
                if (ApplyItem(entry))
                {
                    updated++;
                }
            }

            foreach (ItemSizeWeightTable.ContainerEntry container in ItemSizeWeightTable.Containers)
            {
                if (ApplyContainer(container))
                {
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            return updated;
        }

        private static bool ApplyItem(ItemSizeWeightTable.Entry entry)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(entry.PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[ItemSizeWeightRecipe] Missing prefab: {entry.PrefabPath}");
                return false;
            }

            try
            {
                Item item = root.GetComponent<Item>();
                if (item == null)
                {
                    Debug.LogError($"[ItemSizeWeightRecipe] No Item on {entry.PrefabPath}");
                    return false;
                }

                SerializedObject so = new(item);
                so.FindProperty("_weight").floatValue = entry.Weight;
                so.FindProperty("_sizeClass").enumValueIndex = (int)entry.Size;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, entry.PrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool ApplyContainer(ItemSizeWeightTable.ContainerEntry entry)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(entry.PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[ItemSizeWeightRecipe] Missing prefab: {entry.PrefabPath}");
                return false;
            }

            try
            {
                AttachedContainer storage = null;
                foreach (AttachedContainer candidate in root.GetComponentsInChildren<AttachedContainer>(true))
                {
                    if (candidate.HasUi && !candidate.DisplayAsSlotInUI)
                    {
                        storage = candidate;
                        break;
                    }
                }

                if (storage == null)
                {
                    Debug.LogError($"[ItemSizeWeightRecipe] No HasUi storage on {entry.PrefabPath}");
                    return false;
                }

                // Only grid, ceiling and readout: these prefabs are openable and keep their other flags.
                SerializedObject so = new(storage);
                so.FindProperty("_size").vector2IntValue = entry.Size;
                so.FindProperty("_maxSizeClass").enumValueIndex = (int)entry.MaxSizeClass;
                so.FindProperty("_maxWeight").floatValue = entry.MaxWeight;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, entry.PrefabPath);
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

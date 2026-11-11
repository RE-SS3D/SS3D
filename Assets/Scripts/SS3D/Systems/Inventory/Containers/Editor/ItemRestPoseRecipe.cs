#if UNITY_EDITOR
using System.Collections.Generic;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Tier-B recipe: copies each Item prefab's root rotation into <c>Item._restRotation</c>, the explicit floor pose
    /// (Documents/plans/item-orientation.plan.md B1). The root rotation stays as it is, so a plain
    /// <c>Instantiate(prefab)</c> still lands at rest. Icon rotation is left alone: it defaults to the rest pose.
    /// Registered on <see cref="InventoryContentPrefabRecipes"/>.
    /// </summary>
    public static class ItemRestPoseRecipe
    {
        public const string ContentRoot = "Assets/Content";

        /// <summary>Rest and root rotation closer than this count as equal.</summary>
        public const float MatchToleranceDegrees = 0.01f;

        public static int SetupAll()
        {
            int updated = 0;
            // Base prefabs before their variants: a variant whose root rotation matches its base then inherits the
            // new value and is left without an override.
            foreach (string path in FindItemPrefabPathsBaseFirst())
            {
                if (Apply(path))
                {
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            return updated;
        }

        /// <summary>True when the item's rest pose matches the rotation its prefab root is authored at.</summary>
        public static bool RestMatchesRoot(Item item) =>
            Quaternion.Angle(item.WorldRestRotation, item.transform.localRotation) <= MatchToleranceDegrees;

        public static List<string> FindItemPrefabPathsBaseFirst()
        {
            List<(int Depth, string Path)> found = new();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ContentRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.TryGetComponent(out Item _))
                {
                    found.Add((VariantDepth(prefab), path));
                }
            }

            found.Sort((a, b) => a.Depth != b.Depth ? a.Depth.CompareTo(b.Depth) : string.CompareOrdinal(a.Path, b.Path));
            List<string> paths = new(found.Count);
            foreach ((int _, string path) in found)
            {
                paths.Add(path);
            }

            return paths;
        }

        private static int VariantDepth(GameObject prefab)
        {
            int depth = 0;
            GameObject current = prefab;
            while (PrefabUtility.GetPrefabAssetType(current) == PrefabAssetType.Variant)
            {
                current = PrefabUtility.GetCorrespondingObjectFromSource(current);
                if (current == null)
                {
                    break;
                }

                depth++;
            }

            return depth;
        }

        private static bool Apply(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[ItemRestPoseRecipe] Missing prefab: {path}");
                return false;
            }

            try
            {
                Item item = root.GetComponent<Item>();
                if (item == null || RestMatchesRoot(item))
                {
                    return false;
                }

                SerializedObject so = new(item);
                so.FindProperty("_restRotation").vector3Value = root.transform.localEulerAngles;
                so.ApplyModifiedPropertiesWithoutUndo();
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

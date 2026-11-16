#if UNITY_EDITOR
using FishNet.Component.Observing;
using FishNet.Object;
using FishNet.Observing;
using SS3D.Data;
using SS3D.Systems.Tile.Observing;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Tile.Editor
{
    /// <summary>
    /// Adds FishNet GridCondition (+ underfloor cover condition) observers to tile prefabs
    /// referenced by TileObjectSo assets.
    /// </summary>
    public static class TileGridObserverConfigurator
    {
        private const string GridConditionPath =
            "Assets/FishNet/Runtime/Observing/Conditions/GridCondition/GridCondition.asset";

        private const string UnderfloorCoverConditionPath =
            "Assets/Scripts/SS3D/Systems/Tile/Observing/UnderfloorCoverCondition.asset";

        /// <summary>
        /// Applies GridCondition observers to tile prefabs. Tier B — no MenuItem.
        /// BatchMode: <c>-executeMethod SS3D.Systems.Tile.Editor.TileGridObserverConfigurator.ApplyGridConditionToTilePrefabs</c>
        /// </summary>
        public static void ApplyGridConditionToTilePrefabs()
        {
            GridCondition gridCondition = AssetDatabase.LoadAssetAtPath<GridCondition>(GridConditionPath);
            if (gridCondition == null)
            {
                Debug.LogError($"GridCondition asset not found at {GridConditionPath}");
                return;
            }

            UnderfloorCoverCondition coverCondition =
                AssetDatabase.LoadAssetAtPath<UnderfloorCoverCondition>(UnderfloorCoverConditionPath);
            if (coverCondition == null)
            {
                Debug.LogWarning(
                    $"UnderfloorCoverCondition asset missing at {UnderfloorCoverConditionPath}; underfloor prefabs get Grid only.");
            }

            string[] tileSoGuids = AssetDatabase.FindAssets($"t:{nameof(TileObjectSo)}");
            int updated = 0;
            int skipped = 0;
            HashSet<string> processedPrefabs = new();

            foreach (string soGuid in tileSoGuids)
            {
                string soPath = AssetDatabase.GUIDToAssetPath(soGuid);
                TileObjectSo tileSo = AssetDatabase.LoadAssetAtPath<TileObjectSo>(soPath);
                if (tileSo == null || tileSo.PrefabAsset == null)
                {
                    skipped++;
                    continue;
                }

                GameObject prefab = Data.Assets.Get<GameObject>(tileSo.PrefabAsset);
                if (prefab == null)
                {
                    skipped++;
                    continue;
                }

                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(prefabPath) || !processedPrefabs.Add(prefabPath))
                    continue;

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabRoot == null)
                {
                    skipped++;
                    continue;
                }

                if (!prefabRoot.TryGetComponent(out NetworkObject _))
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                    skipped++;
                    continue;
                }

                NetworkObserver observer = prefabRoot.GetComponent<NetworkObserver>();
                if (observer == null)
                    observer = prefabRoot.AddComponent<NetworkObserver>();

                SerializedObject serializedObserver = new SerializedObject(observer);
                SerializedProperty overrideType = serializedObserver.FindProperty("_overrideType");
                overrideType.intValue = (int)NetworkObserver.ConditionOverrideType.IgnoreManager;
                serializedObserver.FindProperty("_updateHostVisibility").boolValue = true;

                SerializedProperty conditions = serializedObserver.FindProperty("_observerConditions");
                conditions.ClearArray();
                conditions.InsertArrayElementAtIndex(0);
                conditions.GetArrayElementAtIndex(0).objectReferenceValue = gridCondition;

                if (coverCondition != null && TileUnderfloorVisibility.IsUnderfloorLayer(tileSo.layer))
                {
                    conditions.InsertArrayElementAtIndex(1);
                    conditions.GetArrayElementAtIndex(1).objectReferenceValue = coverCondition;
                }

                serializedObserver.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                updated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Tile GridCondition setup complete. Updated {updated} prefab(s), skipped {skipped} asset(s).");
        }
    }
}
#endif

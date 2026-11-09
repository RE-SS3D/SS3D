#if UNITY_EDITOR
using SS3D.Logging;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Editor utility to add <see cref="TileOcclusionMember"/> to all relevant tile prefabs
    /// (Turf and coverable layers) and pre-serialize their layer.
    /// Accessible via Unity menu: SS3D -> Tiles -> Add Occlusion Members To Tile Prefabs.
    /// </summary>
    public static class TileOcclusionSetup
    {
        [MenuItem("SS3D/Tiles/Add Occlusion Members To Tile Prefabs")]
        public static void AddOcclusionMembersToPrefabs()
        {
            int updatedCount = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:TileObjectSo"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TileObjectSo tileObjectSo = AssetDatabase.LoadAssetAtPath<TileObjectSo>(assetPath);

                if (tileObjectSo == null)
                {
                    continue;
                }

                if (tileObjectSo.layer != TileLayer.Turf && !TileOcclusionSubSystem.IsCoverableLayer(tileObjectSo.layer))
                {
                    continue;
                }

                if (tileObjectSo.PrefabAsset == null || string.IsNullOrEmpty(tileObjectSo.PrefabAsset.Id))
                {
                    Log.Warning(typeof(TileOcclusionSetup), "No prefab asset reference found for {tileObjectSo}", Logs.Generic, tileObjectSo.name);
                    continue;
                }

                string prefabPath = AssetDatabase.GUIDToAssetPath(tileObjectSo.PrefabAsset.Id);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    Log.Warning(typeof(TileOcclusionSetup), "No prefab file found for {tileObjectSo}", Logs.Generic, tileObjectSo.name);
                    continue;
                }

                if (SetupPrefab(prefabPath, tileObjectSo.layer))
                {
                    updatedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Log.Information(typeof(TileOcclusionSetup), "{count} tile prefabs updated with TileOcclusionMember", Logs.Generic, updatedCount);
        }

        private static bool SetupPrefab(string prefabPath, TileLayer layer)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                if (!root.TryGetComponent(out TileOcclusionMember member))
                {
                    member = root.AddComponent<TileOcclusionMember>();
                }
                else if (member.Layer == layer)
                {
                    return false;
                }

                SerializedObject serializedMember = new(member);
                serializedMember.FindProperty("_layer").enumValueIndex = (int)layer;
                serializedMember.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
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

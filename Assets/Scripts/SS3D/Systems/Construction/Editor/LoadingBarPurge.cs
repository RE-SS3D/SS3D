#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SS3DAssetDatabase = SS3D.Data.AssetDatabases.AssetDatabase;

namespace SS3D.Systems.Construction.Editor
{
    /// <summary>
    /// Tier-C cleanup for the LoadingBar purge (D6 of 2026-11_construction-mvp1): the prefab, its
    /// ObjectAssetReference and script were deleted in git. Addressables drops the group entry for a deleted
    /// asset on import; this then rebuilds the WorldSpaceUI asset database from that group so no missing
    /// reference is left. Idempotent. Delete this file when the effort ships.
    /// </summary>
    public static class LoadingBarPurge
    {
        public const string LoadingBarGuid = "03bcc1824d3b20748b912ca532f86bb9";
        public const string DatabasePath = "Assets/Content/Data/Databases/WorldSpaceUI.asset";

        public static bool Run()
        {
            bool changed = false;

            SS3DAssetDatabase database = UnityEditor.AssetDatabase.LoadAssetAtPath<SS3DAssetDatabase>(DatabasePath);
            if (database == null)
            {
                Debug.LogWarning($"[LoadingBarPurge] Missing {DatabasePath}");
                return changed;
            }

            if (database.Assets != null && database.Assets.TryGetValue(LoadingBarGuid, out _))
            {
                database.LoadAssetsFromAssetGroup();
                changed = true;
            }

            if (changed)
            {
                UnityEditor.AssetDatabase.SaveAssets();
            }

            return changed;
        }
    }
}
#endif

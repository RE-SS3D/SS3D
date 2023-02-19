#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Data.AssetDatabases.InspectorEditor
{
    [CustomEditor(typeof(AssetDatabaseSettings))]
    public class AssetDatabaseSettingsInspectorEditor : Editor
    {
        private AssetDatabaseSettings _assetDatabaseSettings;

        private void OnEnable()
        {
            _assetDatabaseSettings = (AssetDatabaseSettings)target;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Find all asset databases", GUILayout.Width(350)))
            {
                List<AssetDatabase> foundDatabases = Assets.FindAssetDatabases();
                _assetDatabaseSettings.IncludedAssetDatabases = foundDatabases;
            }

            base.OnInspectorGUI();

        }
    }
}
#endif
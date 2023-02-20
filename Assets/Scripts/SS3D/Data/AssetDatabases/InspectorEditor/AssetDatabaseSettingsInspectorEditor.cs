#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Data.AssetDatabases.InspectorEditor
{
    [CustomEditor(typeof(AssetDatabaseSettings))]
    public class AssetDatabaseSettingsInspectorEditor : Editor
    {
        private AssetDatabaseSettings _assetDatabaseSettings;

        public VisualTreeAsset _assetDatabaseSettingsVisualTree;

        private ScrollView _databaseListView;
        private Button _loadDatabasesButton;

        private void OnEnable()
        {
            _assetDatabaseSettings = (AssetDatabaseSettings)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            _assetDatabaseSettingsVisualTree.CloneTree(root);

            _databaseListView = root.Q<ScrollView>("database-list");
            _loadDatabasesButton = root.Q<Button>("load-databases-button");

            UpdateListVisuals();

            _loadDatabasesButton.clicked += HandleLoadDatabasesButtonPressed;

            return root;
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

        private void HandleLoadDatabasesButtonPressed()
        {
            List<AssetDatabase> foundDatabases = Assets.FindAssetDatabases();
            _assetDatabaseSettings.IncludedAssetDatabases = foundDatabases;

            UpdateListVisuals();
        }

        private void UpdateListVisuals()
        {
            _databaseListView.Clear();
            foreach (AssetDatabase database in _assetDatabaseSettings.IncludedAssetDatabases)
            {
                ObjectField objectField = new()
                {
                    value = database
                };

                _databaseListView.Add(objectField);
            }
        }
    }
}
#endif
#if UNITY_EDITOR
using SS3D.CodeGeneration;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

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
            FindAndLoadAllAssetsDatabasesAddressablesGroups();

            return SetupUIToolkitCustomInspectorEditor();
        }

        public override void OnInspectorGUI()
        {
            SetupCustomInspectorEditor();

            base.OnInspectorGUI();
        }

        private void SetupCustomInspectorEditor()
        {
            EditorApplication.projectChanged += HandleProjectChanged;

            // FindAndLoadAllAssetsDatabasesAddressablesGroups();

            if (GUILayout.Button("Find and load asset databases", GUILayout.Width(350)))
            {
                HandleLoadDatabasesButtonPressedGUI();
            }
        }

        private VisualElement SetupUIToolkitCustomInspectorEditor()
        {
            EditorApplication.projectChanged += HandleProjectChanged;

            VisualElement root = new();
            _assetDatabaseSettingsVisualTree.CloneTree(root);

            _databaseListView = root.Q<ScrollView>("database-list");
            _loadDatabasesButton = root.Q<Button>("load-databases-button");

            UpdateListVisuals();

            _loadDatabasesButton.clicked += HandleLoadDatabasesButtonPressed;

            return root;
        }

        private void LoadDatabases()
        {
            List<AssetDatabase> foundDatabases = AssetDatabase.FindAllAssetDatabases();
            _assetDatabaseSettings.IncludedAssetDatabases = foundDatabases;
        }

        private void FindAndLoadAllAssetsDatabasesAddressablesGroups()
        {
            _assetDatabaseSettings = (AssetDatabaseSettings)target;

            LoadDatabases();

            List<AssetDatabase> includedAssetDatabases = _assetDatabaseSettings.IncludedAssetDatabases;

            foreach (AssetDatabase assetDatabase in includedAssetDatabases)
            {
                assetDatabase.LoadAssetsFromAssetGroup();
            }

            CreateAssetDatabasesEnum();
        }

        private void CreateAssetDatabasesEnum()
        {
            if (_assetDatabaseSettings == null)
            {
                return;
            }

            if (_assetDatabaseSettings.IncludedAssetDatabases == null || _assetDatabaseSettings.IncludedAssetDatabases.Count == 0)
            {
                return;
            }

            _assetDatabaseSettings.GenerateCode();

            foreach (AssetDatabase includedAssetDatabase in _assetDatabaseSettings.IncludedAssetDatabases)
            {
                includedAssetDatabase.GenerateDatabaseCode();

                string path = "Assets/Content/Data/WorldObjectAssetReferences/";

                foreach (Object asset in includedAssetDatabase.Assets.Values)
                {
                    if (asset is not GameObject)
                    {
                        continue;
                    }

                    WorldObjectAssetReference worldObjectAssetReference = CreateInstance<WorldObjectAssetReference>();

                    worldObjectAssetReference.Id = asset.name;
                    worldObjectAssetReference.Database = includedAssetDatabase.name;

                    if (!UnityEditor.AssetDatabase.Contains(worldObjectAssetReference))
                    {
                        UnityEditor.AssetDatabase.CreateAsset(worldObjectAssetReference, $"{path}{worldObjectAssetReference.Id}.asset");   
                    }
                    else
                    {
                        Destroy(worldObjectAssetReference);
                    }
                }
            }
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

        private void HandleLoadDatabasesButtonPressed()
        {
            LoadDatabases();

            FindAndLoadAllAssetsDatabasesAddressablesGroups();

            UpdateListVisuals();
        }

        public void HandleProjectChanged()
        {
            LoadDatabases();

            FindAndLoadAllAssetsDatabasesAddressablesGroups();
        }

        private void HandleLoadDatabasesButtonPressedGUI()
        {
            LoadDatabases();

            FindAndLoadAllAssetsDatabasesAddressablesGroups();
        }
    }
}
#endif
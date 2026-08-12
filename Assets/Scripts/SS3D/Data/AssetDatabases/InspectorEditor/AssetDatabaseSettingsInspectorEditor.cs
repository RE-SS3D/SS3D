#if UNITY_EDITOR
using System.Linq;
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
            PopulateCatalogs();

            return SetupUIToolkitCustomInspectorEditor();
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Find and load asset catalogs", GUILayout.Width(350)))
            {
                PopulateCatalogs();
            }

            base.OnInspectorGUI();
        }

        private VisualElement SetupUIToolkitCustomInspectorEditor()
        {
            VisualElement root = new();
            _assetDatabaseSettingsVisualTree.CloneTree(root);

            _databaseListView = root.Q<ScrollView>("database-list");
            _loadDatabasesButton = root.Q<Button>("load-databases-button");

            UpdateListVisuals();

            _loadDatabasesButton.clicked += HandleLoadDatabasesButtonPressed;

            return root;
        }

        /// <summary>
        /// Asks each included catalog to re-discover its databases, then regenerates generated code.
        /// </summary>
        private void PopulateCatalogs()
        {
            _assetDatabaseSettings = (AssetDatabaseSettings)target;

            foreach (AssetCatalog catalog in _assetDatabaseSettings.IncludedCatalogs.Where(catalog => catalog))
            {
                catalog.PopulateFromProject();
            }

            AssetDatabasesCodeGenerator.GenerateAssetDatabasesCode();
        }

        private void UpdateListVisuals()
        {
            _databaseListView.Clear();

            foreach (ObjectField objectField in _assetDatabaseSettings.IncludedCatalogs.Where(catalog => catalog).
                Select(catalog => new ObjectField
                {
                    value = catalog,
                }))
            {
                _databaseListView.Add(objectField);
            }
        }

        private void HandleLoadDatabasesButtonPressed()
        {
            PopulateCatalogs();
            EditorUtility.SetDirty(_assetDatabaseSettings);
            UpdateListVisuals();
        }
    }
}
#endif
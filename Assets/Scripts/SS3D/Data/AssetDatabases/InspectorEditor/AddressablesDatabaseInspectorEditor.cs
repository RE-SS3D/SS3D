#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityAssetDatabase = UnityEditor.AssetDatabase;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases.InspectorEditor
{
    [CustomEditor(typeof(AddressablesDatabase))]
    public class AddressablesDatabaseInspectorEditor : Editor
    {
        private AddressablesDatabase _addressablesDatabase;

        public VisualTreeAsset _assetDatabaseVisualTree;

        private ScrollView _assetsListView;
        private Button _loadAssetsButton;
        private ObjectField _assetGroupObjectField;
        private Label _assetDatabaseLabel;
        private TextField _enumNameTextField;

        private void OnEnable()
        {
            _addressablesDatabase = (AddressablesDatabase)target;
        }

        private void OnValidate()
        {
            _addressablesDatabase.DatabaseID = GetDatabaseID();

            if (_assetGroupObjectField != null)
            {
                _addressablesDatabase.AssetGroup = _assetGroupObjectField.value as AddressableAssetGroup;
            }
        }

        /// <summary>
        /// This sets ups the UI for the custom inspector using the UI Toolkit
        /// </summary>
        /// <returns></returns>
        public override VisualElement CreateInspectorGUI()
        {
            if (UnityEngine.Application.isPlaying)
            {
                return null;
            }

            VisualElement root = new();
            _assetDatabaseVisualTree.CloneTree(root);

            _assetDatabaseLabel = root.Q<Label>("asset-database-label");
            _enumNameTextField = root.Q<TextField>("enum-name-text-field");
            _assetGroupObjectField = root.Q<ObjectField>("asset-group-field");
            _loadAssetsButton = root.Q<Button>("load-assets-from-addressables-group-button");
            _assetsListView = root.Q<ScrollView>("assets-list");

            _assetDatabaseLabel.text = $"{_addressablesDatabase.name} ASSET DATABASE";
            _enumNameTextField.value = _addressablesDatabase.name;
            _assetGroupObjectField.value = _addressablesDatabase.AssetGroup;

            _addressablesDatabase.LoadAssetsFromAssetGroup();
            _addressablesDatabase.GenerateDatabaseCode();

            if (_addressablesDatabase.AssetGuids.Any())
            {
                PopulateAssetListView();
            }

            _loadAssetsButton.clicked += HandleLoadAssetsButtonPressed;

            EditorUtility.SetDirty(_addressablesDatabase);
            UnityAssetDatabase.SaveAssetIfDirty(_addressablesDatabase);

            return root;
        }

        private void HandleLoadAssetsButtonPressed()
        {
            _addressablesDatabase.DatabaseID = GetDatabaseID();

            _addressablesDatabase.AssetGroup = _assetGroupObjectField.value as AddressableAssetGroup;
            _addressablesDatabase.LoadAssetsFromAssetGroup();
            _assetsListView.Clear();

            PopulateAssetListView();

            _addressablesDatabase.GenerateDatabaseCode();

            EditorUtility.SetDirty(_addressablesDatabase);
            UnityAssetDatabase.SaveAssetIfDirty(_addressablesDatabase);
        }

        private void PopulateAssetListView()
        {
            foreach (ObjectField objectField in _addressablesDatabase.AssetGuids.Select(UnityAssetDatabase.GUIDToAssetPath).
                Select(UnityAssetDatabase.LoadAssetAtPath<Object>).
                Select(asset => new ObjectField { value = asset, }))
            {
                _assetsListView.Add(objectField);
            }
        }

        private string GetDatabaseID()
        {
            string assetPath = UnityAssetDatabase.GetAssetPath(_addressablesDatabase);

            if (!File.Exists(assetPath))
            {
                return null;
            }

            string guid = UnityAssetDatabase.AssetPathToGUID(assetPath);

            return guid;
        }
    }
}
#endif
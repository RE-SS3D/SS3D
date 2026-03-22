#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
        private PropertyField _assetGuidsListView;
        private Button _loadAssetsButton;
        private ObjectField _assetGroupObjectField;
        private Label _assetDatabaseLabel;
        private TextField _enumNameTextField;

        private SerializedProperty _guidsProperty;

        private void OnEnable()
        {
            _addressablesDatabase = (AddressablesDatabase)target;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_enumNameTextField != null)
            {
                _addressablesDatabase.DatabaseName = _enumNameTextField.value;
            }

            _addressablesDatabase.DatabaseID = GetDatabaseID();

            if (_assetGroupObjectField != null)
            {
                _addressablesDatabase.AssetGroup = _assetGroupObjectField.value as AddressableAssetGroup;
            }
        }
#endif

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

            VisualElement root = new VisualElement();
            _assetDatabaseVisualTree.CloneTree(root);

            _assetDatabaseLabel = root.Q<Label>("asset-database-label");
            _enumNameTextField = root.Q<TextField>("enum-name-text-field");
            _assetGroupObjectField = root.Q<ObjectField>("asset-group-field");
            _loadAssetsButton = root.Q<Button>("load-assets-from-addressables-group-button");
            _assetsListView = root.Q<ScrollView>("assets-list");
            _assetGuidsListView = root.Q<PropertyField>("asset-guids-field");

            _assetDatabaseLabel.text = $"{_addressablesDatabase.name} ASSET DATABASE";
            _enumNameTextField.value = _addressablesDatabase.DatabaseName;
            _assetGroupObjectField.value = _addressablesDatabase.AssetGroup;
            _guidsProperty = serializedObject.FindProperty(nameof(_addressablesDatabase.AssetGuids));

            _addressablesDatabase.LoadAssetsFromAssetGroup();

            EditorUtility.SetDirty(_addressablesDatabase);

            _addressablesDatabase.GenerateDatabaseCode();

            if (_addressablesDatabase.Assets != null)
            {
                foreach (KeyValuePair<string, Object> asset in _addressablesDatabase.Assets)
                {
                    ObjectField objectField = new()
                    {
                        value = asset.Value
                    };

                    _assetsListView.Add(objectField);
                }
            }

            if (_addressablesDatabase.AssetGuids != null && _guidsProperty != null)
            {
                _assetGuidsListView.BindProperty(_guidsProperty);
            }

            _loadAssetsButton.clicked += HandleLoadAssetsButtonPressed;

            return root;
        }

        private void HandleLoadAssetsButtonPressed()
        {
            _addressablesDatabase.DatabaseName = _enumNameTextField.value;
            _addressablesDatabase.DatabaseID = GetDatabaseID();

            _addressablesDatabase.AssetGroup = _assetGroupObjectField.value as AddressableAssetGroup;
            _addressablesDatabase.LoadAssetsFromAssetGroup();
            _assetsListView.Clear();

            foreach (KeyValuePair<string, Object> asset in _addressablesDatabase.Assets)
            {
                ObjectField objectField = new()
                {
                    value = asset.Value
                };

                _assetsListView.Add(objectField);
            }   

            EditorUtility.SetDirty(_addressablesDatabase);

            _addressablesDatabase.GenerateDatabaseCode();
        }

        private string GetDatabaseID()
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(_addressablesDatabase);

            if (!File.Exists(assetPath))
            {
                return null;
            }

            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            return guid;
        }
    }
}
#endif
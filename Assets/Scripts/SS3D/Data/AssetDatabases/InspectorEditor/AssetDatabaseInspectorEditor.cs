﻿#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SS3D.CodeGeneration;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases.InspectorEditor
{
    [CustomEditor(typeof(AssetDatabase))]
    public class AssetDatabaseInspectorEditor : Editor
    {
        private AssetDatabase _assetDatabase;

        public VisualTreeAsset _assetDatabaseVisualTree;

        private ScrollView _assetsListView;
        private Button _loadAssetsButton;
        private ObjectField _assetGroupObjectField;
        private Label _assetDatabaseLabel;
        private TextField _enumNameTextField;

        private static readonly Regex SlashRegex = new(@"[\\//]");

        private void OnEnable()
        {
            _assetDatabase = (AssetDatabase)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            _assetDatabaseVisualTree.CloneTree(root);

            _assetDatabaseLabel = root.Q<Label>("asset-database-label");
            _enumNameTextField = root.Q<TextField>("enum-name-text-field");
            _assetGroupObjectField = root.Q<ObjectField>("asset-group-field");
            _loadAssetsButton = root.Q<Button>("load-assets-from-addressables-group-button");
            _assetsListView = root.Q<ScrollView>("assets-list");

            _assetDatabaseLabel.text = $"{_assetDatabase.name} ASSET DATABASE";
            _enumNameTextField.value = _assetDatabase.EnumName;
            _assetGroupObjectField.value = _assetDatabase.AssetGroup;

            foreach (Object asset in _assetDatabase.Assets)
            {
                ObjectField objectField = new()
                {
                    value = asset
                };

                _assetsListView.Add(objectField);
            }

            _loadAssetsButton.clicked += HandleLoadAssetsButtonPressed;

            return root;
        }

        private void HandleLoadAssetsButtonPressed()
        {
            _assetDatabase.EnumName = _enumNameTextField.value;

            string dataPath = AssetDatabase.EnumPath;

            _assetDatabase.AssetGroup = _assetGroupObjectField.value as AddressableAssetGroup;
            _assetDatabase.LoadAssetsFromAssetGroup();
            _assetsListView.Clear();

            foreach (Object asset in _assetDatabase.Assets)
            {
                ObjectField objectField = new()
                {
                    value = asset
                };

                _assetsListView.Add(objectField);
            }


            EnumCreator.CreateAtPath(dataPath, _assetDatabase.EnumName, _assetDatabase.Assets, _assetDatabase.EnumNamespaceName);
        }
    }
}
#endif
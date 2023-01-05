#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using SS3D.CodeGeneration;
using UnityEditor;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    [CustomEditor(typeof(InteractionIconsAssetDatabase))]
    public class GenericDatabaseInspectorEditor : Editor
    {
        private GenericAssetDatabase _assetDatabase;
        private static GUIContent _folderIcon;

        private static readonly Regex SlashRegex = new(@"[\\//]");

        private void OnEnable()
        {
            _assetDatabase = (InteractionIconsAssetDatabase)target;
            _folderIcon = EditorGUIUtility.IconContent("d_FolderOpened Icon");
        }

        public override void OnInspectorGUI()
        {
            GUILayoutOption iconWidthConstraint = GUILayout.MaxWidth(200.0f);
            GUILayoutOption iconHeightConstraint = GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight);

            GUIStyle labelStyle = new()
            {
                fontStyle = FontStyle.Bold, 
                normal =
                {
                    textColor = Color.white
                },
                padding = new RectOffset(0, 0, 10, 10),
            };

            GUILayout.Label("Enum writer settings", labelStyle);

            if (GUILayout.Button($"Set enum creation path", iconWidthConstraint, iconHeightConstraint))
            {
                if (TryOpenFolderPathInsideAssetsFolder(null, Application.dataPath, null, out string result))
                {
                    _assetDatabase.EnumPath = result;

                }
                else
                {
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent($"{result} must be inside the Assets folder."));
                }
            }

            GUILayout.Space(5);

            if (GUILayout.Button($"Create enum", GUILayout.Width(200)))
            {
                EnumCreator.CreateAtPath(_assetDatabase.EnumPath, _assetDatabase.EnumName, _assetDatabase.Assets, _assetDatabase.EnumNamespaceName);
            }

            GUILayout.Label("Asset database settings", labelStyle);

            base.OnInspectorGUI();
        }

        private static bool TryOpenFolderPathInsideAssetsFolder(string title, string folder, string name, out string result)
        {
            result = null;

            string selectedPath = EditorUtility.OpenFolderPanel(title, folder, name);

            if (selectedPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log(Application.dataPath);
                Debug.Log(selectedPath);

                result = SlashRegex.Replace(selectedPath.Remove(0, Application.dataPath.Length), Path.DirectorySeparatorChar.ToString());

                return true;
            }

            return false;
        }
    }
}
#endif
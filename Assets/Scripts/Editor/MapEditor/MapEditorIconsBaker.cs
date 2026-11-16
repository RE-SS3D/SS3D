#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.MapEditor
{
    /// <summary>
    /// Bakes SVG vector icon references into <see cref="Systems.Tile.MapEditor.MapEditorIconsSo"/>.
    /// </summary>
    public static class MapEditorIconsBaker
    {
        private const string IconsAssetPath = "Assets/Content/Systems/UI/MapEditor/MapEditorIcons.asset";

        [MenuItem("SS3D/Map Editor/Bake Icons")]
        public static void BakeIcons()
        {
            Systems.Tile.MapEditor.MapEditorIconsSo icons =
                AssetDatabase.LoadAssetAtPath<Systems.Tile.MapEditor.MapEditorIconsSo>(IconsAssetPath);
            if (icons == null)
            {
                Debug.LogError($"Map editor icons asset not found at {IconsAssetPath}.");
                return;
            }

            SerializedObject serialized = new(icons);
            int missing = 0;

            foreach ((string propertyName, string assetPath) in MapEditorIconBakeEntries.All)
            {
                UnityEngine.UIElements.VectorImage image =
                    AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VectorImage>(assetPath);
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property == null)
                {
                    Debug.LogWarning($"Missing serialized field '{propertyName}' on MapEditorIconsSo.");
                    continue;
                }

                property.objectReferenceValue = image;
                if (image == null)
                {
                    missing++;
                    Debug.LogWarning($"Could not load vector icon at {assetPath}.");
                }
            }

            string[] subTabIconPaths = Systems.Tile.MapEditor.MapEditorSubTabs.All
                .Select(row => row.IconPath).Distinct().ToArray();
            SerializedProperty subTabIcons = serialized.FindProperty("_subTabIcons");
            subTabIcons.arraySize = subTabIconPaths.Length;
            for (int i = 0; i < subTabIconPaths.Length; i++)
            {
                UnityEngine.UIElements.VectorImage image =
                    AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VectorImage>(subTabIconPaths[i]);
                SerializedProperty element = subTabIcons.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Path").stringValue = subTabIconPaths[i];
                element.FindPropertyRelative("Image").objectReferenceValue = image;
                if (image == null)
                {
                    missing++;
                    Debug.LogWarning($"Could not load vector icon at {subTabIconPaths[i]}.");
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(icons);
            AssetDatabase.SaveAssets();
            Systems.Tile.MapEditor.MapEditorIconLoader.ClearCache();

            Debug.Log(missing == 0
                ? "Map editor icons baked successfully."
                : $"Map editor icons baked with {missing} missing asset(s).");
        }
    }

    internal static class MapEditorIconBakeEntries
    {
        internal static readonly (string PropertyName, string AssetPath)[] All =
        {
            ("_select", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Select),
            ("_construct", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Construct),
            ("_move", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Move),
            ("_dropper", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Dropper),
            ("_delete", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Delete),
            ("_undo", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Undo),
            ("_redo", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Redo),
            ("_saveMap", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.SaveMap),
            ("_openMap", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.OpenMap),
            ("_rotateLeft", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.RotateLeft),
            ("_rotateRight", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.RotateRight),
            ("_zoomIn", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.ZoomIn),
            ("_zoomOut", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.ZoomOut),
            ("_panelExpand", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.PanelExpand),
            ("_panelCollapse", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.PanelCollapse),
            ("_resetView", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.ResetView),
            ("_layers", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Layers),
            ("_eyeOff", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.EyeOff),
            ("_camera", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Camera),
            ("_settings", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Settings),
            ("_search", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.Search),
            ("_modeUpper", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.ModeUpper),
            ("_modeLower", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.ModeLower),
            ("_modeItems", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.ModeItems),
            ("_modeScripting", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.ModeScripting),
            ("_spawnAssistant", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.SpawnAssistant),
            ("_spawnSecurity", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.SpawnSecurity),
            ("_spawnTraitor", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.SpawnTraitor),
            ("_spawnMalfunctioningAI", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.SpawnMalfunctioningAI),
            ("_spawnNuclearOperatives", Systems.Tile.MapEditor.MapEditorIconsSo.Paths.SpawnNuclearOperatives),
        };
    }
}
#endif

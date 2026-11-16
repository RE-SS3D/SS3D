#if UNITY_EDITOR
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.SceneManagement.Editor
{
    /// <summary>
    /// Adds a dropdown to the left side of the main editor toolbar for quickly loading any scene
    /// enumerated in the static Scene class. Scenes that are currently loaded are ticked.
    /// </summary>
    public static class SceneSwitcherTool
    {
        private static readonly MainToolbarContent Content = new()
        {
            text = "Scene Switcher",
            image = EditorGUIUtility.IconContent("Scene").image as Texture2D,
            tooltip = "Switch between scenes enumerated in static Scene class",
        };

        private static readonly MainToolbarDropdown Dropdown = new(Content, ShowDropdown);

        /// <summary>
        /// Registers the scene-switcher dropdown on the toolbar.
        /// </summary>
        [NotNull]
        [MainToolbarElement("Tools/Scene Switcher", defaultDockPosition = MainToolbarDockPosition.Left)]
        private static MainToolbarElement SceneSwitcherToolbar() => Dropdown;

        /// <summary>
        /// Builds the menu from <c>Scene.Names</c>, ticking scenes that are currently loaded, and
        /// starts the chosen scene. Rebuilt each time the dropdown opens, so no external-change
        /// polling is needed here (unlike the launcher and network tools).
        /// </summary>
        private static void ShowDropdown(Rect rect)
        {
            HashSet<string> loadedScenes = new();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded)
                {
                    loadedScenes.Add(scene.name);
                }
            }

            GenericMenu menu = new();

            foreach (string scene in Scene.Names)
            {
                menu.AddItem(new(scene), loadedScenes.Contains(scene), () => EditorSceneUtils.StartScene(scene));
            }

            menu.DropDown(rect);
        }
    }
}
#endif
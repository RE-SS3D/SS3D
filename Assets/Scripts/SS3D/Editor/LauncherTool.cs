#if UNITY_EDITOR
using Coimbra;
using JetBrains.Annotations;
using SS3D.Application;
using UnityEditor;
using UnityEditor.Toolbars;

namespace SS3D.Editor
{
    /// <summary>
    /// Adds a button to the right side of the main editor toolbar that toggles
    /// <see cref="ApplicationSettings.ForceLauncher"/>. The label reflects the current value and is
    /// kept in sync with changes made elsewhere (such as the Project Settings window) by
    /// <see cref="PollForExternalChanges"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class LauncherTool
    {
        private const string ToolbarPath = "Tools/Launcher";

        private static readonly ApplicationSettings ApplicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();
        
        private static bool LastForceLauncher = ApplicationSettings.ForceLauncher;

        private static readonly MainToolbarContent EnabledContent = new()
        {
            text = "Launcher: Enabled",
            tooltip = "Disable the launcher in the Application Settings.",
        };

        private static readonly MainToolbarContent DisabledContent = new()
        {
            text = "Launcher: Disabled",
            tooltip = "Enable the launcher in the Application Settings.",
        };

        private static readonly MainToolbarButton Button = new(EnabledContent, OnButtonClick);

        /// <summary>
        /// Subscribes <see cref="PollForExternalChanges"/> to the editor update loop on load.
        /// </summary>
        static LauncherTool()
        {
            EditorApplication.update += PollForExternalChanges;
        }

        /// <summary>
        /// The native main toolbar caches each element's content and only rebuilds it when
        /// <see cref="MainToolbar.Refresh"/> is called, unlike the old IMGUI toolbar that re-read the
        /// settings every frame. This runs each editor update and refreshes the button only when
        /// <see cref="ApplicationSettings.ForceLauncher"/> has changed from any source, so the label
        /// never goes stale.
        /// </summary>
        private static void PollForExternalChanges()
        {
            if (LastForceLauncher == ApplicationSettings.ForceLauncher)
            {
                return;
            }

            LastForceLauncher = ApplicationSettings.ForceLauncher;
            MainToolbar.Refresh(ToolbarPath);
        }


        /// <summary>
        /// Registers the toolbar button and refreshes its content from the current setting each time
        /// the toolbar rebuilds the element.
        /// </summary>
        [NotNull]
        [MainToolbarElement(ToolbarPath, defaultDockPosition = MainToolbarDockPosition.Right)]
        private static MainToolbarElement LauncherToolbar()
        {
            Button.content = ApplicationSettings.ForceLauncher ? EnabledContent : DisabledContent;

            return Button;
        }

        /// <summary>
        /// Toggles <see cref="ApplicationSettings.ForceLauncher"/> and persists the asset; the poll
        /// then picks up the change and refreshes the button label.
        /// </summary>
        private static void OnButtonClick()
        {
            ApplicationSettings.ForceLauncher = !ApplicationSettings.ForceLauncher;
            EditorUtility.SetDirty(ApplicationSettings);
            AssetDatabase.SaveAssetIfDirty(ApplicationSettings);
        }
    }
}
#endif
using Coimbra;
using UnityEngine;

namespace SS3D.Core.Settings
{
    /// <summary>
    /// Used to define settings for the overall SS3D application in the Project Settings.
    /// </summary>
    [ProjectSettings("SS3D/Core", "Application Settings")]
    public sealed class ApplicationSettings : ScriptableSettings
    {
        /// <summary>
        /// Defined via command line args when in a built executable or the value in the Project Settings window.
        /// </summary>
        [Header("Integrations")]
        [Tooltip("If enabled, the discord integration plugin will run.")]
        public bool EnableDiscord;

        /// <summary>
        /// Defined via command line args when in a built executable or the value in the Project Settings window.
        /// </summary>
        [Header("Other Settings")]
        [Tooltip("If enabled, the intro animation will be skipped.")]
        public bool SkipIntro;

        /// <summary>
        /// Forces the launcher UI to initialize.
        /// </summary>
        [Tooltip("If enabled, the launcher UI will be forced to open")]
        public bool ForceLauncher;

        /// <summary>
        /// Resets our settings if we are ona built executable.
        /// </summary>
        public static void ResetOnBuiltApplication()
        {
            ApplicationSettings applicationSettings = GetOrFind<ApplicationSettings>();

            applicationSettings.SkipIntro = false;
            applicationSettings.EnableDiscord = false;
            applicationSettings.ForceLauncher = false;
        }
    }
}
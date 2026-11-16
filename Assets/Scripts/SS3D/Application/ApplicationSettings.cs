using Coimbra;
using UnityEngine;

namespace SS3D.Application
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
        /// Path to a multiplayer test harness automation script for this process to run, set via
        /// the "-testscript=" command line arg. Empty outside of harness-driven runs.
        /// </summary>
        public string TestScriptPath;

        /// <summary>
        /// The automation script this process runs: <see cref="TestScriptPath"/>, or in the editor
        /// the script named by this Multiplayer Play Mode player's <c>smoke:</c> tag. Empty for
        /// normal play.
        /// </summary>
        public string ActiveTestScriptPath => string.IsNullOrEmpty(TestScriptPath)
            ? MultiplayerPlayModePlayer.TaggedScriptPath
            : TestScriptPath;

        /// <summary>
        /// Editor-only station template loaded at boot instead of the most recently saved one.
        /// Empty keeps the most-recent behaviour; built executables always ignore it.
        /// </summary>
        [Tooltip("Editor only: station template file name (without .json) loaded at boot, e.g. a small map for fast playtests. Empty loads the most recently saved template.")]
        public string EditorStationTemplate;

        /// <summary>
        /// Resets our settings if we are ona built executable.
        /// </summary>
        public static void ResetOnBuiltApplication()
        {
            ApplicationSettings applicationSettings = GetOrFind<ApplicationSettings>();

            applicationSettings.SkipIntro = false;
            applicationSettings.EnableDiscord = false;
            applicationSettings.ForceLauncher = false;
            applicationSettings.TestScriptPath = string.Empty;
            applicationSettings.EditorStationTemplate = string.Empty;
        }
    }
}
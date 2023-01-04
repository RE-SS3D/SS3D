using System;
using Coimbra;
using UnityEngine;

namespace SS3D.Core.Settings
{
    /// <summary>
    /// Used to define settings for the overall SS3D application in the Project Settings.
    /// </summary>
    [ProjectSettings("Project/SS3D", "Application Settings")]
    public class ApplicationSettings : ScriptableSettings
    {
        /// <summary>
        /// The Ckey used to authenticate users. Will be used along an API key when said API exists.
        /// TODO: Update this when we have an API.
        /// Defined via command line args when in a built executable or the EditorServerCkey when in the Editor.
        /// </summary>
        [HideInInspector]
        public string Ckey = string.Empty;

        /// <summary>
        /// The server address used when we start connecting to a server.
        /// Defined via command line args when in a built executable or the EditorServerAddress when in the Editor.
        /// </summary>
        [HideInInspector]
        public string ServerAddress;

        /// <summary>
        /// Defines what type of connection to start when starting the application.
        /// Defined via command line args when in a built executable or the value in the Project Settings window.
        /// </summary>
        [Tooltip("The selected option is only considered when in Editor. Built executables use the command args.")]
        [Header("Network Settings")]
        public NetworkType NetworkType;

        /// <summary>
        /// Defined via command line args when in a built executable or the value in the Project Settings window.
        /// </summary>
        [Header("Other Settings")]
        [Tooltip("If enabled, the intro animation will be skipped.")]
        public bool SkipIntro;

        /// <summary>
        /// Defined via command line args when in a built executable or the value in the Project Settings window.
        /// </summary>
        [Tooltip("If enabled, the discord integration plugin will run.")]
        public bool EnableDiscord;

        /// <summary>
        /// Defined by the value in the Project Settings window.
        /// </summary>
        [Header("Editor Only Settings")]
        [Tooltip("The server address to connect to as a client, when in the Editor.")]
        public string EditorServerAddress = "127.0.0.1";

        /// <summary>
        /// Defined by the value in the Project Settings window.
        /// </summary>
        [Tooltip("The username used when connecting to a server via the Editor.")]
        public string EditorServerCkey = "editorUser";

        /// <summary>
        /// Resets our settings if we are ona built executable.
        /// </summary>
        public void ResetOnBuildApplication()
        {
            SkipIntro = false;
            EnableDiscord = false;
            NetworkType = NetworkType.Client;
            ServerAddress = string.Empty;
            Ckey = string.Empty;
        }

        /// <summary>
        /// Sets the editor settings overrides.
        /// </summary>
        public void SetEditorSettings()
        {
            Ckey = EditorServerCkey;
            ServerAddress = EditorServerAddress;
        }
    }
}
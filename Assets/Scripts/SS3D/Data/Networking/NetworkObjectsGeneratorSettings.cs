#if UNITY_EDITOR
using Coimbra;
using System.IO;
using UnityEngine;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Settings for the NetworkObjectsGenerator, which can be accessed and modified in the Project Settings.
    /// </summary>
    [ProjectSettings("SS3D/Assets", isEditorOnly: true, FileDirectory = "ProjectSettings/SS3D/Assets/")]
    public class NetworkObjectsGeneratorSettings : ScriptableSettings
    {
        /// <summary>
        /// Enables or disables the NetworkObjectsGenerator.
        /// </summary>
        [field: SerializeField]
        public bool Enabled { get; private set; }

        /// <summary>
        /// Enables or disables logging of the NetworkObjectsGenerator's actions to the console.
        /// </summary>
        [field: SerializeField]
        public bool LogToConsole { get; private set; } = true;

        /// <summary>
        /// If true, the NetworkObjectsGenerator will clear the existing NetworkObjects asset and regenerate it from scratch every time it runs.
        /// </summary>
        [field: SerializeField]
        public bool FullRebuild { get; private set; }

        /// <summary>
        /// If true, the NetworkObjectsGenerator will save the generated NetworkObjects asset after generating it.
        /// </summary>
        [field: SerializeField]
        public bool SaveChanges { get; private set; } = true;

        /// <summary>
        /// The path to the generated NetworkObjects asset, relative to the project folder.
        /// </summary>
        [field: SerializeField]
        public string NetworkObjectsPath { get; private set; } = Path.Combine("Assets", "Content", "Data", "Databases", "NetworkObjects.asset");
    }
}
#endif
using System;
using System.IO;
using UnityEngine;

namespace SS3D.Systems.Settings
{
    /// <summary>Client-local JSON file for <see cref="PlayerSettings"/> (Decision 2).</summary>
    public sealed class PlayerSettingsStore
    {
        public static string DefaultPath
        {
            get
            {
#if UNITY_EDITOR
                // Pilots change settings: write to the run's output folder, not the owner's real file.
                foreach (string pilot in new[] { "SS3D.LobbyPilot", "SS3D.LobbyLifecyclePilot" })
                    if (UnityEditor.SessionState.GetBool(pilot + ".Active", false))
                        return Path.Combine(UnityEditor.SessionState.GetString(pilot + ".Output", ""), "settings.json");
#endif
                return Path.Combine(UnityEngine.Application.persistentDataPath, "Settings", "settings.json");
            }
        }

        private readonly string _path;

        public PlayerSettingsStore(string path) => _path = path;

        /// <summary>
        /// Never null. Keys the file lacks keep their defaults and unknown keys are ignored. An unreadable
        /// file is copied to <c>.bak</c> and defaults load (Decision 16).
        /// </summary>
        public PlayerSettings Load()
        {
            if (!File.Exists(_path)) return new PlayerSettings();
            try
            {
                string json = File.ReadAllText(_path);
                if (!json.TrimStart().StartsWith("{", StringComparison.Ordinal))
                    throw new ArgumentException("not a JSON object");
                PlayerSettings settings = new();
                JsonUtility.FromJsonOverwrite(json, settings);
                settings.Sanitize();
                return settings;
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
            {
                BackUpUnreadableFile();
                Debug.LogWarning($"[Settings] Could not read player settings at {_path}; using defaults: {exception.Message}");
                return new PlayerSettings();
            }
        }

        private void BackUpUnreadableFile()
        {
            try
            {
                File.Copy(_path, _path + ".bak", true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Settings] Could not back up unreadable player settings: {exception.Message}");
            }
        }

        public bool Save(PlayerSettings settings)
        {
            string temporary = _path + ".tmp";
            try
            {
                settings.Version = PlayerSettings.CurrentVersion;
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(temporary, JsonUtility.ToJson(settings, true));
                if (File.Exists(_path)) File.Replace(temporary, _path, null);
                else File.Move(temporary, _path);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Settings] Could not save player settings: {exception.Message}");
                return false;
            }
        }
    }
}

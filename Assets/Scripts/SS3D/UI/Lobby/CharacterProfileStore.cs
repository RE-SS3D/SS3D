using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The player's saved characters on this machine, independent of server-owned player meta and crew records
    /// (a deviation from persistence-save.md §5 until server player data exists). Schema v2 adds each character's job
    /// priorities, antagonist choices and fallback (lobby effort Decisions 1–2); a v1 file is migrated on load and
    /// written back as v2.
    /// </summary>
    public sealed class CharacterProfileStore
    {
        public const int CurrentVersion = 2;

        [Serializable]
        private sealed class FileData
        {
            public int Version = CurrentVersion;
            public string SelectedId;
            public Entry[] Characters;
        }

        [Serializable]
        private sealed class Entry
        {
            // Kept as "Draft" (the old type name): it is the key in every saved characters.json.
            public SavedCharacter Draft;
            // JsonUtility cannot serialize the draft's dictionary. Store morphs by key, not UI order.
            public string[] MorphKeys;
            public float[] MorphValues;
        }

        public sealed class Profile
        {
            public string SelectedId;
            public SavedCharacter[] Characters;
            /// <summary>The file was an older schema and has been rewritten (or, when read-only, would have been).</summary>
            public bool Migrated;
        }

        public static string DefaultPath
        {
            get
            {
#if UNITY_EDITOR
                // Pilots save characters: use a fresh per-run profile instead of the owner's presets.
                foreach (string pilot in new string[] { "SS3D.LobbyPilot", "SS3D.LobbyLifecyclePilot" })
                    if (UnityEditor.SessionState.GetBool(pilot + ".Active", false))
                        return Path.Combine(UnityEditor.SessionState.GetString(pilot + ".Output", ""), "characters.json");
#endif
                return Path.Combine(Application.persistentDataPath, "Lobby", "characters.json");
            }
        }

        private readonly string _path;
        private readonly Func<SavedCharacter> _createRandom;
        private bool _readOnly;

        /// <param name="createRandom">
        /// Makes the random character that replaces a v1 store holding only the untouched mock starter (Decision 2).
        /// Without it the starter is kept.
        /// </param>
        public CharacterProfileStore(string path, Func<SavedCharacter> createRandom = null)
        {
            _path = path;
            _createRandom = createRandom;
        }

        public Profile Load()
        {
            if (!File.Exists(_path)) return null;
            try
            {
                FileData data = JsonUtility.FromJson<FileData>(File.ReadAllText(_path));
                if (data == null || data.Version is < 1 or > CurrentVersion || data.Characters == null)
                    throw new InvalidDataException("Unsupported or incomplete character profile.");
                bool migrating = data.Version < CurrentVersion;
                List<SavedCharacter> characters = new();
                HashSet<string> ids = new();
                foreach (Entry entry in data.Characters)
                {
                    SavedCharacter draft = entry?.Draft;
                    if (draft == null || string.IsNullOrWhiteSpace(draft.Id) || !ids.Add(draft.Id))
                        throw new InvalidDataException("Invalid or duplicate character slot.");
                    if (entry.MorphKeys == null || entry.MorphValues == null || entry.MorphKeys.Length != entry.MorphValues.Length)
                        throw new InvalidDataException("Invalid character morph data.");
                    if (migrating)
                    {
                        // v1 had no preferences: every job Never, every antagonist unset (off), fallback Assistant.
                        draft.Preferences = new CharacterPreferences();
                    }
                    else if (draft.Preferences == null || !draft.Preferences.IsWellFormed)
                    {
                        throw new InvalidDataException("Invalid character preferences.");
                    }

                    for (int i = 0; i < entry.MorphKeys.Length; i++)
                    {
                        string key = entry.MorphKeys[i];
                        if (key != null && draft.BodyMorphs.ContainsKey(key))
                        {
                            float value = entry.MorphValues[i];
                            draft.SetMorph(key, float.IsNaN(value) ? 0.5f : Mathf.Clamp01(value));
                        }
                    }
                    characters.Add(draft);
                }
                if (!characters.Any(c => c.Id == data.SelectedId))
                    throw new InvalidDataException("Selected character slot is missing.");
                Profile profile = new() { SelectedId = data.SelectedId, Characters = characters.ToArray(), Migrated = migrating };
                if (migrating)
                {
                    if (characters.Count == 1 && characters[0].IsUntouchedMockStarter && _createRandom?.Invoke() is { } random)
                    {
                        profile.Characters = new SavedCharacter[] { random };
                        profile.SelectedId = random.Id;
                    }

                    Save(profile.Characters, profile.SelectedId);
                    Debug.Log($"[Lobby] Migrated character presets from schema v{data.Version} to v{CurrentVersion}.");
                }

                return profile;
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
            {
                // Preserve unreadable/newer files rather than replacing them with the default slots.
                _readOnly = true;
                Debug.LogWarning($"[Lobby] Could not load character presets; original file preserved: {exception.Message}");
                return null;
            }
        }

        public bool Save(IEnumerable<SavedCharacter> characters, string selectedId)
        {
            if (_readOnly) return false;
            string temporary = _path + ".tmp";
            try
            {
                FileData data = new()
                {
                    SelectedId = selectedId,
                    Characters = characters.Select(draft => new Entry
                    {
                        Draft = draft,
                        MorphKeys = draft.BodyMorphs.Keys.ToArray(),
                        MorphValues = draft.BodyMorphs.Values.ToArray(),
                    }).ToArray(),
                };
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(_path)) File.Replace(temporary, _path, null);
                else File.Move(temporary, _path);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"[Lobby] Could not save character presets: {exception.Message}");
                return false;
            }
        }
    }
}

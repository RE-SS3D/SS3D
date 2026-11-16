using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// A saved character, or the character window's working copy of one: the look, the name and, since schema v2, its
    /// job priorities, antagonist choices and fallback (<see cref="Preferences"/>; lobby effort Decision 1). New
    /// characters come from <see cref="RandomCharacter"/>; the field defaults are the old mock starter's look.
    /// </summary>
    [System.Serializable]
    public sealed class SavedCharacter
    {
        /// <summary>The id and name every pre-v2 store started with (the single "Marcus Voss" slot).</summary>
        private const string MockStarterId = "marcus-voss";
        private const string MockStarterName = "Marcus Voss";

        public string Id = MockStarterId;
        public string Name = MockStarterName;
        /// <summary>Index into <see cref="LobbyAssetCatalog.EyeColors"/>.</summary>
        public int EyeColorIndex;
        /// <summary>Index into <see cref="LobbyAssetCatalog.SkinColors"/>.</summary>
        public int SkinColorIndex;
        public string Eyebrows = "thin";

        /// <summary>Body slider keys → 0–1 values (see <see cref="CharacterBodySliders"/>).</summary>
        public Dictionary<string, float> BodyMorphs => _bodyMorphs;

        private Dictionary<string, float> _bodyMorphs = CreateDefaultMorphs();

        /// <summary>
        /// Index into <see cref="LobbyAssetCatalog.HairStyles"/>.
        /// 0 = no hair.
        /// </summary>
        public int HairStyleIndex;

        /// <summary>
        /// Index into <see cref="LobbyAssetCatalog.BeardStyles"/>.
        /// 0 = no beard.
        /// </summary>
        public int BeardStyleIndex;

        /// <summary>
        /// Index into <see cref="LobbyAssetCatalog.HairColors"/>.
        /// </summary>
        public int HairColorIndex = 1;
        public int BeardColorIndex = 1;
        public int EyebrowColorIndex = 1;

        /// <summary>Job priorities, antagonist choices and fallback; sent to the server while this character is selected.</summary>
        public CharacterPreferences Preferences = new();

        /// <summary>A deep copy under <paramref name="id"/> (Duplicate, and edits that must not touch the saved slot).</summary>
        public SavedCharacter Clone(string id)
        {
            SavedCharacter copy = (SavedCharacter)MemberwiseClone();
            copy.Id = id;
            copy.Preferences = (Preferences ?? new CharacterPreferences()).Clone();
            copy._bodyMorphs = new Dictionary<string, float>(BodyMorphs);
            return copy;
        }

        /// <summary>
        /// Same name, look and preferences (the character window's Saved / Unsaved changes). The id is not compared.
        /// </summary>
        public bool SameAs(SavedCharacter other)
        {
            if (other == null || Name != other.Name || !SameLookAs(other))
            {
                return false;
            }

            return (Preferences ?? new CharacterPreferences()).SameAs(other.Preferences ?? new CharacterPreferences());
        }

        private bool SameLookAs(SavedCharacter other)
        {
            if (EyeColorIndex != other.EyeColorIndex || SkinColorIndex != other.SkinColorIndex
                || Eyebrows != other.Eyebrows || HairStyleIndex != other.HairStyleIndex
                || BeardStyleIndex != other.BeardStyleIndex || HairColorIndex != other.HairColorIndex
                || BeardColorIndex != other.BeardColorIndex || EyebrowColorIndex != other.EyebrowColorIndex
                || BodyMorphs.Count != other.BodyMorphs.Count)
            {
                return false;
            }

            foreach (KeyValuePair<string, float> morph in BodyMorphs)
            {
                if (!other.BodyMorphs.TryGetValue(morph.Key, out float value) || System.Math.Abs(value - morph.Value) > 0.0001f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Takes <paramref name="source"/>'s look; keeps this character's id, name and preferences (Randomize appearance).</summary>
        public void CopyLookFrom(SavedCharacter source)
        {
            EyeColorIndex = source.EyeColorIndex;
            SkinColorIndex = source.SkinColorIndex;
            Eyebrows = source.Eyebrows;
            HairStyleIndex = source.HairStyleIndex;
            BeardStyleIndex = source.BeardStyleIndex;
            HairColorIndex = source.HairColorIndex;
            BeardColorIndex = source.BeardColorIndex;
            EyebrowColorIndex = source.EyebrowColorIndex;
            CopyMorphsFrom(source.BodyMorphs);
        }

        /// <summary>
        /// The one mock starter every pre-v2 store began with, never edited. A store holding only this is replaced by a
        /// random character (lobby effort Decision 2).
        /// </summary>
        public bool IsUntouchedMockStarter
        {
            get
            {
                SavedCharacter starter = new();
                if (Id != MockStarterId || Name != MockStarterName
                    || EyeColorIndex != starter.EyeColorIndex || SkinColorIndex != starter.SkinColorIndex
                    || Eyebrows != starter.Eyebrows || HairStyleIndex != starter.HairStyleIndex
                    || BeardStyleIndex != starter.BeardStyleIndex || HairColorIndex != starter.HairColorIndex
                    || BeardColorIndex != starter.BeardColorIndex || EyebrowColorIndex != starter.EyebrowColorIndex)
                {
                    return false;
                }

                foreach (KeyValuePair<string, float> morph in starter.BodyMorphs)
                {
                    if (System.Math.Abs(GetMorph(morph.Key, morph.Value) - morph.Value) > 0.0001f)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static Dictionary<string, float> CreateDefaultMorphs()
        {
            Dictionary<string, float> morphs = new();
            foreach (CharacterBodySlider slider in CharacterBodySliders.All)
            {
                morphs[slider.Key] = slider.DefaultValue;
            }

            return morphs;
        }

        public float GetMorph(string key, float fallback = 0.5f) =>
            BodyMorphs.TryGetValue(key, out float value) ? value : fallback;

        public void SetMorph(string key, float value) => BodyMorphs[key] = value;

        public void CopyMorphsFrom(IReadOnlyDictionary<string, float> source)
        {
            if (source == null)
            {
                return;
            }

            foreach (KeyValuePair<string, float> pair in source)
            {
                BodyMorphs[pair.Key] = pair.Value;
            }
        }
    }
}

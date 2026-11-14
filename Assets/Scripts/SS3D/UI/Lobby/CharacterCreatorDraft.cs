using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// A saved character or the creator's working draft: the look, the name and, since schema v2, its job priorities,
    /// antagonist choices and fallback (<see cref="Preferences"/>; lobby effort Decision 1).
    /// </summary>
    [System.Serializable]
    public sealed class CharacterCreatorDraft
    {
        public string Id = CharacterCreatorMockData.Loadouts[0].Id;
        public string Name = LobbyMockData.CharacterName;
        public string Species = CharacterCreatorMockData.SpeciesOptions[0];
        public int EyeColorIndex;
        public int SkinColorIndex;
        public string Eyebrows = "thin";
        /// <summary>Chosen once from the curated preview outfits; retained when this draft is saved.</summary>
        public int OutfitIndex;

        /// <summary>Body slider keys → 0–1 values (see <see cref="CharacterCreatorMockData.BodySliders"/>).</summary>
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
        public CharacterCreatorDraft Clone(string id)
        {
            CharacterCreatorDraft copy = (CharacterCreatorDraft)MemberwiseClone();
            copy.Id = id;
            copy.Preferences = (Preferences ?? new CharacterPreferences()).Clone();
            copy._bodyMorphs = new Dictionary<string, float>(BodyMorphs);
            return copy;
        }

        /// <summary>
        /// The one mock starter (<see cref="CharacterCreatorMockData.Loadouts"/>) as it shipped, never edited. A store
        /// holding only this is replaced by a random character (lobby effort Decision 2). The outfit is ignored: it was
        /// picked at random.
        /// </summary>
        public bool IsUntouchedMockStarter
        {
            get
            {
                CharacterCreatorDraft starter = new();
                if (Id != starter.Id || Name != starter.Name || Species != starter.Species
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
            foreach (CharacterCreatorMockData.SliderDef slider in CharacterCreatorMockData.BodySliders)
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

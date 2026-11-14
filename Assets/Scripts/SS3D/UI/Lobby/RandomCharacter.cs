using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Entities.Character;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Makes a random character (lobby effort Decision 3): the first character a player gets, and New in the switcher.
    /// The name comes from <see cref="CharacterNameList"/>; the look from the existing catalog choices. Every job starts
    /// at Never, every antagonist unset (off) and the fallback is Assistant.
    /// </summary>
    public static class RandomCharacter
    {
        /// <summary>How many of each look choice exist; style index 0 is "none".</summary>
        public readonly struct LookOptions
        {
            public readonly int HairStyles;
            public readonly int BeardStyles;
            public readonly int HairColors;
            public readonly IReadOnlyList<string> Eyebrows;
            public readonly int Outfits;

            public LookOptions(int hairStyles, int beardStyles, int hairColors, IReadOnlyList<string> eyebrows, int outfits)
            {
                HairStyles = hairStyles;
                BeardStyles = beardStyles;
                HairColors = hairColors;
                Eyebrows = eyebrows;
                Outfits = outfits;
            }

            public static LookOptions From(LobbyAssetCatalog catalog) => catalog == null
                ? new LookOptions(0, 0, 0, null, 0)
                : new LookOptions(catalog.HairStyles?.Count ?? 0, catalog.BeardStyles?.Count ?? 0,
                    catalog.HairColors?.Count ?? 0, catalog.EyebrowStyles?.Select(style => style.Id).ToArray(),
                    catalog.PreviewOutfits?.Count ?? 0);
        }

        /// <summary>Chance of a beard on a character whose Female slider is under half.</summary>
        private const double BeardChance = 0.35;

        public static CharacterCreatorDraft Create(CharacterNameList names, LookOptions look, System.Random random)
        {
            string name = names != null ? names.RandomName(random) : "Alex Doe";
            CharacterCreatorDraft draft = new()
            {
                Id = CharacterRoster.NewId(),
                Name = CharacterNameRules.IsValid(name) ? CharacterNameRules.Normalize(name) : "Alex Doe",
                SkinColorIndex = random.Next(CharacterCreatorMockData.SkinColors.Length),
                EyeColorIndex = random.Next(CharacterCreatorMockData.EyeColors.Length),
                OutfitIndex = look.Outfits > 0 ? random.Next(look.Outfits) : 0,
                Preferences = new CharacterPreferences(),
            };

            float female = random.NextDouble() < 0.5 ? 0f : (float)(0.6 + random.NextDouble() * 0.4);
            draft.SetMorph("height", Range(random, 0.3f, 0.7f));
            draft.SetMorph("female", female);
            draft.SetMorph("fat", Range(random, 0f, 0.4f));
            draft.SetMorph("muscle", Range(random, 0f, 0.5f));
            draft.SetMorph("breasts", female > 0.5f ? Range(random, 0f, 0.6f) : 0f);
            draft.SetMorph("jaw", Range(random, 0.3f, 0.7f));

            int hairColor = look.HairColors > 0 ? random.Next(look.HairColors) : draft.HairColorIndex;
            draft.HairStyleIndex = look.HairStyles > 1 ? random.Next(1, look.HairStyles) : 0;
            draft.BeardStyleIndex = female < 0.5f && look.BeardStyles > 1 && random.NextDouble() < BeardChance
                ? random.Next(1, look.BeardStyles) : 0;
            draft.HairColorIndex = hairColor;
            draft.BeardColorIndex = hairColor;
            draft.EyebrowColorIndex = hairColor;
            draft.Eyebrows = look.Eyebrows is { Count: > 0 }
                ? CharacterAppearance.NormalizeEyebrowStyle(look.Eyebrows[random.Next(look.Eyebrows.Count)])
                : draft.Eyebrows;
            return draft;
        }

        private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}

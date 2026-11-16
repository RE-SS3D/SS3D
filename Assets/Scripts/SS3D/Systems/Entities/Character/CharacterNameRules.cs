using System.Text;

namespace SS3D.Systems.Entities.Character
{
    /// <summary>Why a character name was refused (lobby.md §6, lobby effort Decision 4).</summary>
    public enum CharacterNameError
    {
        None,
        Empty,
        TooShort,
        TooLong,
        InvalidCharacters,
        NeedsTwoWords,
    }

    /// <summary>
    /// Character name rules shared by the creator and the server (lobby effort Decision 4): two or more words;
    /// letters, hyphens, apostrophes and spaces only; 3–32 characters; capitalisation fixed automatically.
    /// Duplicate names are allowed and there is no profanity filter.
    /// </summary>
    public static class CharacterNameRules
    {
        public const int MinLength = 3;
        public const int MaxLength = CharacterAppearance.MaxNameLength;

        /// <summary>
        /// Trims, collapses runs of spaces and fixes capitalisation: the first letter of each word, and of each part
        /// after a hyphen, is upper case, and a part typed all in capitals is lowered after its first letter
        /// ("jean-luc o'NEILL" → "Jean-Luc O'neill", "MARY SUE" → "Mary Sue"). Other letters are kept as typed, so
        /// "McAllister" stays. Characters are not removed: <see cref="Validate"/> reports them.
        /// </summary>
        public static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            string[] words = name.Trim().Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            StringBuilder builder = new(name.Length);
            for (int w = 0; w < words.Length; w++)
            {
                if (w > 0)
                {
                    builder.Append(' ');
                }

                string[] parts = words[w].Split('-');
                for (int p = 0; p < parts.Length; p++)
                {
                    if (p > 0)
                    {
                        builder.Append('-');
                    }

                    builder.Append(Capitalise(parts[p]));
                }
            }

            return builder.ToString();
        }

        /// <summary>Checks a name as <see cref="Normalize"/> would leave it.</summary>
        public static CharacterNameError Validate(string name)
        {
            string normalized = Normalize(name);
            if (normalized.Length == 0)
            {
                return CharacterNameError.Empty;
            }

            foreach (char c in normalized)
            {
                if (!char.IsLetter(c) && c != '-' && c != '\'' && c != ' ')
                {
                    return CharacterNameError.InvalidCharacters;
                }
            }

            if (normalized.Length < MinLength)
            {
                return CharacterNameError.TooShort;
            }

            if (normalized.Length > MaxLength)
            {
                return CharacterNameError.TooLong;
            }

            int words = 0;
            foreach (string word in normalized.Split(' '))
            {
                if (HasLetter(word))
                {
                    words++;
                }
            }

            return words >= 2 ? CharacterNameError.None : CharacterNameError.NeedsTwoWords;
        }

        public static bool IsValid(string name) => Validate(name) == CharacterNameError.None;

        /// <summary>The normalized name when it is valid; else false and <paramref name="normalized"/> is empty.</summary>
        public static bool TryNormalize(string name, out string normalized, out CharacterNameError error)
        {
            error = Validate(name);
            normalized = error == CharacterNameError.None ? Normalize(name) : string.Empty;
            return error == CharacterNameError.None;
        }

        /// <summary>The creator's error line for <paramref name="error"/>; empty for <see cref="CharacterNameError.None"/>.</summary>
        public static string Describe(CharacterNameError error) => error switch
        {
            CharacterNameError.Empty => "Enter a name.",
            CharacterNameError.TooShort => $"Use at least {MinLength} characters.",
            CharacterNameError.TooLong => $"Use at most {MaxLength} characters.",
            CharacterNameError.InvalidCharacters => "Use letters, spaces, hyphens and apostrophes only.",
            CharacterNameError.NeedsTwoWords => "Enter a first and a last name.",
            _ => string.Empty,
        };

        private static string Capitalise(string part)
        {
            if (part.Length == 0)
            {
                return part;
            }

            bool allUpper = part.Length > 1 && HasLetter(part) && part.ToUpperInvariant() == part;
            string rest = allUpper ? part.Substring(1).ToLowerInvariant() : part.Substring(1);
            return char.ToUpperInvariant(part[0]) + rest;
        }

        private static bool HasLetter(string text)
        {
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

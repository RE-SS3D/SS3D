using System.Collections.Generic;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// Antagonist categories a player can opt into from the lobby (lobby.md §4). Opting in is
    /// eligibility only; gamemodes decide who is chosen. The list holds only the antagonist roles of
    /// modes the server can run (Q31): Nuclear Operative alone until another antagonist mode exists.
    /// </summary>
    public static class AntagonistCategories
    {
        public const string NuclearOperative = "nukeop";

        public static readonly (string Key, string Name)[] All =
        {
            (NuclearOperative, "Nuclear Operative"),
        };

        public static bool IsKnown(string key)
        {
            foreach ((string known, string _) in All)
            {
                if (known == key)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether a player is in the pool for <paramref name="category"/>. An explicit opt-in or opt-out wins; a
        /// category the character never set (or a player who never sent preferences, both lists null) takes
        /// <paramref name="defaultOptIn"/>. That default is off (lobby.md §4) except on the <c>-nukeops</c> profile.
        /// </summary>
        public static bool IsOptedIn(IReadOnlyCollection<string> optIns, IReadOnlyCollection<string> optOuts,
            string category, bool defaultOptIn)
        {
            if (!IsKnown(category))
            {
                return false;
            }

            if (Contains(optIns, category))
            {
                return true;
            }

            return !Contains(optOuts, category) && defaultOptIn;
        }

        private static bool Contains(IReadOnlyCollection<string> keys, string category)
        {
            if (keys == null)
            {
                return false;
            }

            foreach (string key in keys)
            {
                if (key == category)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

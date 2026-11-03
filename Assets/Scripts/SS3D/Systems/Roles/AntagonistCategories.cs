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
        /// Opt-in is on by default (round-end loop Decision 22): with no preferences sent
        /// (<paramref name="sentOptIns"/> null) a player is in every known category; once sent, only the ticked ones.
        /// </summary>
        public static bool IsOptedIn(IReadOnlyCollection<string> sentOptIns, string category)
        {
            if (!IsKnown(category))
            {
                return false;
            }

            if (sentOptIns == null)
            {
                return true;
            }

            foreach (string key in sentOptIns)
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

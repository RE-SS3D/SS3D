using System;
using System.Collections.Generic;

namespace SS3D.Systems.Roles
{
    /// <summary>
    /// Server-side job allocation (lobby.md §5). Round start runs one pass: every player's High jobs
    /// are tried before anyone's Medium, then Low, in a fresh random player order per tier and a random
    /// pick among a player's equally ranked open jobs. Players left over get the fallback job, which
    /// ignores its cap so nobody is unable to join, unless their character returns to the lobby instead.
    /// </summary>
    public static class JobResolver
    {
        private static readonly JobPriority[] Tiers = { JobPriority.High, JobPriority.Medium, JobPriority.Low };

        /// <param name="players">Player keys to assign.</param>
        /// <param name="preferences">Per-player job priorities; missing players or jobs mean Never.</param>
        /// <param name="jobs">Open jobs. Slots of 0 or less are unlimited.</param>
        /// <param name="fallback">Job given to anyone the pass could not place.</param>
        /// <param name="returnsToLobby">
        /// Players whose character returns to the lobby (<see cref="JobFallback.ReturnToLobby"/>) rather than take the
        /// fallback job. A player of these the pass could not place is left out of the result.
        /// </param>
        public static Dictionary<string, string> Resolve(
            IReadOnlyList<string> players,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, JobPriority>> preferences,
            IReadOnlyList<JobSlotInfo> jobs,
            string fallback,
            Random random,
            ICollection<string> returnsToLobby = null)
        {
            Dictionary<string, int> open = OpenSlots(jobs);
            Dictionary<string, string> result = new();
            List<string> unassigned = new(players);

            foreach (JobPriority tier in Tiers)
            {
                Shuffle(unassigned, random);
                for (int i = unassigned.Count - 1; i >= 0; i--)
                {
                    string player = unassigned[i];
                    if (!TryPick(Get(preferences, player), tier, open, random, out string job))
                    {
                        continue;
                    }

                    result[player] = job;
                    open[job]--;
                    unassigned.RemoveAt(i);
                }
            }

            foreach (string player in unassigned)
            {
                if (returnsToLobby == null || !returnsToLobby.Contains(player))
                {
                    result[player] = fallback;
                }
            }

            return result;
        }

        /// <summary>
        /// Latejoin is first come, first served: the player's highest ranked job that is still open,
        /// otherwise the fallback job, or null when the character returns to the lobby instead.
        /// </summary>
        public static string ResolveLateJoin(
            IReadOnlyDictionary<string, JobPriority> preferences,
            IReadOnlyList<JobSlotInfo> jobs,
            string fallback,
            Random random,
            JobFallback whenNoneOpen = JobFallback.Assistant)
        {
            Dictionary<string, int> open = OpenSlots(jobs);
            foreach (JobPriority tier in Tiers)
            {
                if (TryPick(preferences, tier, open, random, out string job))
                {
                    return job;
                }
            }

            return whenNoneOpen == JobFallback.ReturnToLobby ? null : fallback;
        }

        private static Dictionary<string, int> OpenSlots(IReadOnlyList<JobSlotInfo> jobs)
        {
            Dictionary<string, int> open = new();
            foreach (JobSlotInfo job in jobs)
            {
                open[job.Name] = job.Open;
            }

            return open;
        }

        private static IReadOnlyDictionary<string, JobPriority> Get(
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, JobPriority>> preferences, string player) =>
            preferences != null && preferences.TryGetValue(player, out IReadOnlyDictionary<string, JobPriority> p) ? p : null;

        private static bool TryPick(
            IReadOnlyDictionary<string, JobPriority> preferences,
            JobPriority tier,
            Dictionary<string, int> open,
            Random random,
            out string job)
        {
            job = null;
            if (preferences == null)
            {
                return false;
            }

            int seen = 0;
            foreach (KeyValuePair<string, JobPriority> entry in preferences)
            {
                if (entry.Value != tier || !open.TryGetValue(entry.Key, out int slots) || slots <= 0)
                {
                    continue;
                }

                // Reservoir sample so ties are uniform without allocating.
                seen++;
                if (random.Next(seen) == 0)
                {
                    job = entry.Key;
                }
            }

            return job != null;
        }

        private static void Shuffle<T>(IList<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

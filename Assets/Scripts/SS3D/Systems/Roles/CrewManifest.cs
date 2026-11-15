using System;
using System.Collections.Generic;

namespace SS3D.Systems.Roles
{
    /// <summary>One crew manifest row: the character's name and crew job. Nothing else is sent (lobby effort Decision 10).</summary>
    public struct CrewManifestEntry
    {
        public string Name;
        public string Job;
    }

    /// <summary>
    /// Server: the round's crew manifest (lobby effort Decision 10). It lists everyone given a crew job this round,
    /// dead or alive, one row per player. Operatives and other antagonists are left out, and observer ghosts never
    /// get a job, so they never reach it. A player who spawns again (a respawn) updates their row instead of
    /// adding one. Cleared when the round stops.
    /// </summary>
    public sealed class CrewManifest
    {
        private readonly Dictionary<string, int> _rowByCkey = new(StringComparer.Ordinal);
        private readonly List<CrewManifestEntry> _entries = new();

        public IReadOnlyList<CrewManifestEntry> Entries => _entries;

        /// <summary>
        /// Records that <paramref name="ckey"/> spawned as <paramref name="name"/> with <paramref name="job"/>.
        /// Returns the row it wrote, or -1 when the player is not listed (an antagonist, or no job).
        /// </summary>
        public int Record(string ckey, string name, string job, bool antagonist)
        {
            if (antagonist || string.IsNullOrEmpty(ckey) || string.IsNullOrEmpty(job))
            {
                return -1;
            }

            CrewManifestEntry entry = new() { Name = string.IsNullOrEmpty(name) ? ckey : name, Job = job };
            if (_rowByCkey.TryGetValue(ckey, out int row))
            {
                _entries[row] = entry;
                return row;
            }

            _rowByCkey[ckey] = _entries.Count;
            _entries.Add(entry);
            return _entries.Count - 1;
        }

        public void Clear()
        {
            _rowByCkey.Clear();
            _entries.Clear();
        }
    }
}

using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// Gives networks a view id that survives edits (Documents/architecture/2026-11_map-editor-systems-views.md Q3), so
    /// colours stay put while pipe networks and circuits are rebuilt as new records. Shared by every domain; pure C#.
    /// After each rebuild a new network takes the id of the old network it shares the most segments with (ties go to the
    /// lower id), so on a split the larger side keeps its colour and on a merge the larger part's colour wins. Every other
    /// network gets a fresh id from a counter that only goes up (from 1; 0 means "no network"), so a deleted network's
    /// colour and label never pass to a new one; the palette wraps by modulo.
    /// </summary>
    public sealed class NetworkIdentityTracker<TSegment> where TSegment : IEquatable<TSegment>
    {
        private Dictionary<TSegment, int> _viewIds = new();
        private Dictionary<TSegment, int> _spare = new();
        private int _lastId;

        public bool TryGetViewId(TSegment segment, out int viewId) => _viewIds.TryGetValue(segment, out viewId);

        /// <summary>Forgets every network, e.g. when a new map loads; the next <see cref="Assign"/> numbers from 1.</summary>
        public void Clear()
        {
            _viewIds.Clear();
            _lastId = 0;
        }

        /// <summary>
        /// Records a rebuild: the complete set of networks as they are now, each as its segments. Returns each
        /// network's view id, by index. Pass networks in a stable order: when two new networks share equally with
        /// the same old one, the earlier keeps its id.
        /// </summary>
        public int[] Assign(IReadOnlyList<IEnumerable<TSegment>> networks)
        {
            if (networks == null)
                throw new ArgumentNullException(nameof(networks));

            int[] result = new int[networks.Count];
            List<Candidate> candidates = new();
            Dictionary<int, int> overlap = new();
            for (int index = 0; index < networks.Count; index++)
            {
                overlap.Clear();
                if (networks[index] != null)
                {
                    foreach (TSegment segment in networks[index])
                    {
                        if (_viewIds.TryGetValue(segment, out int oldId))
                            overlap[oldId] = overlap.TryGetValue(oldId, out int count) ? count + 1 : 1;
                    }
                }

                foreach (KeyValuePair<int, int> pair in overlap)
                    candidates.Add(new Candidate(index, pair.Key, pair.Value));
            }

            // Most shared segments first; then the lower old id (Q3's tie rule), then the earlier network.
            candidates.Sort((a, b) => a.Shared != b.Shared ? b.Shared.CompareTo(a.Shared)
                : a.OldId != b.OldId ? a.OldId.CompareTo(b.OldId)
                : a.Network.CompareTo(b.Network));

            HashSet<int> used = new();
            foreach (Candidate candidate in candidates)
            {
                if (result[candidate.Network] != 0 || used.Contains(candidate.OldId))
                    continue;

                result[candidate.Network] = candidate.OldId;
                used.Add(candidate.OldId);
            }

            for (int index = 0; index < result.Length; index++)
            {
                if (result[index] == 0)
                    result[index] = ++_lastId;
            }

            _spare.Clear();
            for (int index = 0; index < networks.Count; index++)
            {
                if (networks[index] == null)
                    continue;

                foreach (TSegment segment in networks[index])
                    _spare[segment] = result[index];
            }

            (_viewIds, _spare) = (_spare, _viewIds);
            return result;
        }

        private readonly struct Candidate
        {
            public readonly int Network;
            public readonly int OldId;
            public readonly int Shared;

            public Candidate(int network, int oldId, int shared)
            {
                Network = network;
                OldId = oldId;
                Shared = shared;
            }
        }
    }
}

using System.Collections.Generic;

namespace SS3D.Systems.Audio
{
    /// <summary>A running machine that could take a voice.</summary>
    public readonly struct AmbientLoopCandidate
    {
        public readonly int Index;
        public readonly float SqrDistance;
        public readonly float MaxDistance;

        public AmbientLoopCandidate(int index, float sqrDistance, float maxDistance)
        {
            Index = index;
            SqrDistance = sqrDistance;
            MaxDistance = maxDistance;
        }
    }

    /// <summary>Picks which machines get a voice: the nearest ones the listener could hear.</summary>
    public static class AmbientLoopSelection
    {
        /// <summary>
        /// Fills <paramref name="selected"/> with the indices of the nearest
        /// <paramref name="maxCount"/> candidates within their own reach. Equal distances fall back
        /// to index, so a tie can't swap voices between polls. Reorders <paramref name="candidates"/>.
        /// </summary>
        public static void SelectNearest(List<AmbientLoopCandidate> candidates, int maxCount, HashSet<int> selected)
        {
            selected.Clear();
            candidates.Sort(static (a, b) =>
            {
                int byDistance = a.SqrDistance.CompareTo(b.SqrDistance);
                return byDistance != 0 ? byDistance : a.Index.CompareTo(b.Index);
            });

            foreach (AmbientLoopCandidate candidate in candidates)
            {
                if (selected.Count >= maxCount)
                {
                    return;
                }

                if (candidate.SqrDistance <= candidate.MaxDistance * candidate.MaxDistance)
                {
                    selected.Add(candidate.Index);
                }
            }
        }
    }
}

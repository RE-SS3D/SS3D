using SS3D.Systems.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Ranks currently-active speakers and action lines for a viewer and splits them into the
    /// cap-sized "shown" set and the compressed overflow, per comms.md §4: by priority tier
    /// (action-messages.md §5), then proximity. Shouts outranking speech (comms.md §8) is still
    /// a later slice; see the TODO below.
    /// </summary>
    public class CrowdCapRanker
    {
        public readonly struct Result
        {
            public readonly IReadOnlyList<Entity> Shown;
            public readonly int OverflowCount;

            public Result(IReadOnlyList<Entity> shown, int overflowCount)
            {
                Shown = shown;
                OverflowCount = overflowCount;
            }
        }

        /// <summary>
        /// Lower tier ranks first (action-messages.md §5): speech and shouts, then hostile actions,
        /// then actions done to the viewer, then routine ones. Proximity breaks ties.
        /// </summary>
        public const int SpeechTier = 0;
        public const int HostileActionTier = 1;
        public const int ActionOnViewerTier = 2;
        public const int RoutineActionTier = 3;

        /// <summary>Orders by tier, then distance, and splits off what exceeds the cap.</summary>
        public static List<T> RankByTier<T>(IEnumerable<T> items, System.Func<T, int> tier, System.Func<T, float> sqrDistance,
            int maxVisible, out int overflowCount)
        {
            List<T> ranked = items.OrderBy(tier).ThenBy(sqrDistance).ToList();
            List<T> shown = ranked.Take(Mathf.Max(0, maxVisible)).ToList();
            overflowCount = ranked.Count - shown.Count;
            return shown;
        }

        public Result Rank(Entity viewer, IReadOnlyCollection<Entity> activeSpeakers, int maxVisible, System.Func<Entity, int> tier = null)
        {
            if (viewer == null || activeSpeakers == null || activeSpeakers.Count == 0)
            {
                return new Result(System.Array.Empty<Entity>(), 0);
            }

            Vector3 viewerPosition = viewer.ViewPoint != null ? viewer.ViewPoint.transform.position : viewer.Position;

            // TODO(comms-shout-slice): shouts should bump a lower-priority speaker ahead of pure
            // proximity ordering here (comms.md §8) once SpeechMode.Shout is actually emitted.
            List<Entity> shown = RankByTier(
                activeSpeakers,
                tier ?? (_ => SpeechTier),
                speaker => SqrDistanceTo(speaker, viewerPosition),
                maxVisible,
                out int overflowCount);

            return new Result(shown, overflowCount);
        }

        private static float SqrDistanceTo(Entity speaker, Vector3 viewerPosition)
        {
            Vector3 speakerPosition = speaker.ViewPoint != null ? speaker.ViewPoint.transform.position : speaker.Position;
            return (speakerPosition - viewerPosition).sqrMagnitude;
        }
    }
}

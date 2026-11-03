using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// The summary beat between Ending and Stopped (<c>round-end.md</c> §6): a fixed count everyone can
    /// read, which an admin can hold and resume. One <see cref="Tick"/> per second.
    /// </summary>
    public sealed class RoundEndBeat
    {
        public const int Seconds = 30;

        public int SecondsLeft { get; private set; }

        public bool Held { get; private set; }

        public bool Running { get; private set; }

        /// <param name="seconds">Beat length; <see cref="Seconds"/> in play. Automation shortens it (min 1).</param>
        public void Begin(int seconds = Seconds)
        {
            SecondsLeft = Math.Max(1, seconds);
            Held = false;
            Running = true;
        }

        /// <summary>Advances one second unless held; true exactly when the beat is over.</summary>
        public bool Tick()
        {
            if (!Running || Held)
                return false;

            SecondsLeft = Math.Max(0, SecondsLeft - 1);
            if (SecondsLeft > 0)
                return false;

            Running = false;
            return true;
        }

        /// <summary>False when there is no beat to hold or the state does not change.</summary>
        public bool SetHeld(bool held)
        {
            if (!Running || Held == held)
                return false;

            Held = held;
            return true;
        }

        public void Reset()
        {
            SecondsLeft = 0;
            Held = false;
            Running = false;
        }
    }

    /// <summary>Builds the fixed <see cref="RoundOutcome"/> from what the server knows at the trigger instant.</summary>
    public static class RoundOutcomeBuilder
    {
        public static RoundOutcome Build(RoundEndRequest request, int roundId, string configuredMode, string mode,
            string map, int durationSeconds, IEnumerable<RoundOutcomePlayer> players,
            string antagonistLabel = "", IEnumerable<RoundOutcomeObjective> objectives = null)
        {
            RoundOutcomePlayer[] roster = (players ?? Enumerable.Empty<RoundOutcomePlayer>())
                .Where(p => !string.IsNullOrEmpty(p.Ckey))
                // A respawned player appears once, as their latest body.
                .GroupBy(p => p.Ckey, StringComparer.Ordinal)
                .Select(g => g.Last())
                .Select(p =>
                {
                    p.Name = string.IsNullOrEmpty(p.Name) ? p.Ckey : p.Name;
                    p.Job = string.IsNullOrEmpty(p.Job) ? "Crew" : p.Job;
                    return p;
                })
                .ToArray();

            return new RoundOutcome
            {
                RoundId = roundId,
                Trigger = request.Trigger,
                Winner = request.Winner,
                StationDestroyed = request.StationDestroyed,
                Reason = string.IsNullOrEmpty(request.Reason) ? RoundOutcomeText.DefaultReason(request.Trigger) : request.Reason,
                ConfiguredMode = configuredMode ?? "",
                Mode = string.IsNullOrEmpty(mode) ? configuredMode ?? "" : mode,
                Map = map ?? "",
                DurationSeconds = Math.Max(0, durationSeconds),
                AntagonistLabel = roster.Any(p => p.Antagonist) ? antagonistLabel ?? "" : "",
                Players = roster,
                Objectives = (objectives ?? Enumerable.Empty<RoundOutcomeObjective>()).ToArray(),
            };
        }
    }
}

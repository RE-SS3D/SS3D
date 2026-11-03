using System;

namespace SS3D.Systems.Rounds
{
    /// <summary>What ended the round (<c>round-end.md</c> §2). Evacuation joins when shuttles exist.</summary>
    public enum RoundEndTrigger
    {
        GamemodeCondition = 0,
        Timer = 1,
        Admin = 2,
    }

    /// <summary>Who won (<c>antagonist-content.md</c> §6). Extended, admin ends and timeouts without a rule are <see cref="None"/>.</summary>
    public enum RoundWinner
    {
        None = 0,
        Operatives = 1,
        OperativesMinor = 2,
        Crew = 3,
    }

    public enum ObjectiveResult
    {
        Failed = 0,
        Met = 1,
    }

    /// <summary>One player who took a body this round, as they stood when the trigger fired.</summary>
    [Serializable]
    public struct RoundOutcomePlayer
    {
        public string Ckey;
        public string Name;
        public string Job;
        public bool Alive;
        /// <summary>Listed under the antagonist roster instead of the crew.</summary>
        public bool Antagonist;
    }

    /// <summary>A team objective and its result, read when the round ended.</summary>
    [Serializable]
    public struct RoundOutcomeObjective
    {
        public string Text;
        public ObjectiveResult Result;
    }

    /// <summary>
    /// The round's ledger (<c>round-end.md</c> §2, §5), fixed on the server at the instant a trigger fires.
    /// Nothing in it is computed afterwards; the summary and the lobby's Last round tab only display it.
    /// </summary>
    [Serializable]
    public sealed class RoundOutcome
    {
        public int RoundId;
        public RoundEndTrigger Trigger;
        public RoundWinner Winner;
        /// <summary>The mode the admin configured ("Secret"), shown struck through when it differs from <see cref="Mode"/>.</summary>
        public string ConfiguredMode = "";
        /// <summary>The resolved mode, revealed at round end (<c>round-end.md</c> §4).</summary>
        public string Mode = "";
        public string Map = "";
        public string Reason = "";
        public bool StationDestroyed;
        public int DurationSeconds;
        /// <summary>Label of the antagonist roster ("Operatives"); empty when the round had none.</summary>
        public string AntagonistLabel = "";
        public RoundOutcomePlayer[] Players = Array.Empty<RoundOutcomePlayer>();
        public RoundOutcomeObjective[] Objectives = Array.Empty<RoundOutcomeObjective>();
    }

    /// <summary>What a trigger asks for; <see cref="RoundSubSystem.RequestRoundEnd"/> turns it into a <see cref="RoundOutcome"/>.</summary>
    public readonly struct RoundEndRequest
    {
        public readonly RoundEndTrigger Trigger;
        public readonly RoundWinner Winner;
        public readonly bool StationDestroyed;
        /// <summary>A gamemode's own wording; null uses <see cref="RoundOutcomeText.DefaultReason"/>.</summary>
        public readonly string Reason;

        public RoundEndRequest(RoundEndTrigger trigger, RoundWinner winner = RoundWinner.None,
            bool stationDestroyed = false, string reason = null)
        {
            Trigger = trigger;
            Winner = winner;
            StationDestroyed = stationDestroyed;
            Reason = reason;
        }

        public static RoundEndRequest Timer => new(RoundEndTrigger.Timer);

        public static RoundEndRequest Admin => new(RoundEndTrigger.Admin);

        /// <summary>The device detonated; until M3 this is the instant <c>Nuke.Detonate</c>.</summary>
        public static RoundEndRequest Detonation =>
            new(RoundEndTrigger.GamemodeCondition, RoundWinner.Operatives, true, "The nuclear device detonated.");
    }

    /// <summary>Player-facing wording for an outcome, shared by the overlay, the banner and the lobby tab.</summary>
    public static class RoundOutcomeText
    {
        public static string DefaultReason(RoundEndTrigger trigger) => trigger switch
        {
            RoundEndTrigger.Timer => "Time ran out.",
            RoundEndTrigger.Admin => "Ended by an admin.",
            _ => "The round's end condition was met.",
        };

        public static string Title(RoundWinner winner) => winner switch
        {
            RoundWinner.Operatives => "Operatives win",
            RoundWinner.OperativesMinor => "Minor operative win",
            RoundWinner.Crew => "Crew win",
            _ => "No winner",
        };

        /// <summary>m:ss under an hour, else h:mm:ss.</summary>
        public static string Duration(int seconds)
        {
            seconds = Math.Max(0, seconds);
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}"
                : $"{time.Minutes:00}:{time.Seconds:00}";
        }

        /// <summary>"3 of 5 alive".</summary>
        public static string AliveCount(int alive, int total) => $"{alive} of {total} alive";
    }
}

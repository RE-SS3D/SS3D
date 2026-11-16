using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// One <see cref="RoundOutcome"/> as a particular viewer sees it: their own row highlighted and their
    /// fate spelled out (<c>round-end.md</c> §5). Pure, so EditMode tests cover it without UI.
    /// </summary>
    public sealed class RoundSummaryModel
    {
        public enum FateKind { Alive, Dead, NotPlayed }

        public readonly struct Row
        {
            public readonly string Name;
            public readonly string Job;
            public readonly bool Alive;
            public readonly bool IsViewer;

            public Row(string name, string job, bool alive, bool isViewer)
            {
                Name = name;
                Job = job;
                Alive = alive;
                IsViewer = isViewer;
            }
        }

        public RoundOutcome Outcome { get; }
        public string Title { get; }
        public FateKind Fate { get; }
        /// <summary>"Alive", "Dead" or "Not played".</summary>
        public string FateText { get; }
        /// <summary>"Ada Vance · Chief Engineer", with " · ghost" when dead; or "In the lobby this round".</summary>
        public string FateWho { get; }
        public IReadOnlyList<Row> Antagonists { get; }
        public IReadOnlyList<Row> Crew { get; }
        public bool HasAntagonists => Antagonists.Count > 0;
        /// <summary>"CREW" next to an antagonist roster, "EVERYONE" without one.</summary>
        public string CrewLabel => HasAntagonists ? "CREW" : "EVERYONE";
        public string CrewCount => RoundOutcomeText.AliveCount(Crew.Count(r => r.Alive), Crew.Count);
        public string AntagonistCount => RoundOutcomeText.AliveCount(Antagonists.Count(r => r.Alive), Antagonists.Count);
        /// <summary>The configured mode differs from the revealed one (Secret → Nuclear Emergency).</summary>
        public bool ShowConfiguredMode =>
            !string.IsNullOrEmpty(Outcome.ConfiguredMode) && !string.Equals(Outcome.ConfiguredMode, Outcome.Mode, StringComparison.Ordinal);

        public RoundSummaryModel(RoundOutcome outcome, string viewerCkey)
        {
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            Title = RoundOutcomeText.Title(outcome.Winner);
            RoundOutcomePlayer[] players = outcome.Players ?? Array.Empty<RoundOutcomePlayer>();
            bool IsViewer(RoundOutcomePlayer p) =>
                !string.IsNullOrEmpty(viewerCkey) && string.Equals(p.Ckey, viewerCkey, StringComparison.Ordinal);

            Antagonists = players.Where(p => p.Antagonist).Select(p => new Row(p.Name, p.Job, p.Alive, IsViewer(p))).ToArray();
            Crew = players.Where(p => !p.Antagonist).Select(p => new Row(p.Name, p.Job, p.Alive, IsViewer(p))).ToArray();

            RoundOutcomePlayer? viewer = players.Where(IsViewer).Select(p => (RoundOutcomePlayer?)p).FirstOrDefault();
            if (viewer == null)
            {
                Fate = FateKind.NotPlayed;
                FateText = "Not played";
                FateWho = "In the lobby this round";
                return;
            }

            RoundOutcomePlayer me = viewer.Value;
            Fate = me.Alive ? FateKind.Alive : FateKind.Dead;
            FateText = me.Alive ? "Alive" : "Dead";
            FateWho = $"{me.Name} · {me.Job}{(me.Alive ? "" : " · ghost")}";
        }

        /// <summary>"Returning to lobby in 0:24" / "Lobby in 0:24", or the hold notice.</summary>
        public static string Countdown(int secondsLeft, bool held, bool shortForm)
        {
            if (held)
                return "Held by an admin";

            string time = $"{Math.Max(0, secondsLeft) / 60}:{Math.Max(0, secondsLeft) % 60:00}";
            return shortForm ? $"Lobby in {time}" : $"Returning to lobby in {time}";
        }

        /// <summary>Progress bar fill, 0–1; full while held.</summary>
        public static float Progress(int secondsLeft, bool held) =>
            held ? 1f : Math.Clamp(secondsLeft / (float)RoundEndBeat.Seconds, 0f, 1f);
    }
}

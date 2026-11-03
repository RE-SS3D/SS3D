using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Roles
{
    /// <summary>What the round-start squad draw produced.</summary>
    public readonly struct SquadDrawResult
    {
        /// <summary>Drawn player keys, in draw order. Empty when <see cref="FellBack"/>.</summary>
        public readonly IReadOnlyList<string> Squad;
        /// <summary>Too few opt-ins: the round runs as Extended.</summary>
        public readonly bool FellBack;
        public readonly int OptedIn;
        public readonly int Minimum;

        public SquadDrawResult(IReadOnlyList<string> squad, bool fellBack, int optedIn, int minimum)
        {
            Squad = squad;
            FellBack = fellBack;
            OptedIn = optedIn;
            Minimum = minimum;
        }
    }

    /// <summary>
    /// The Nuke Ops squad draw (<c>antagonist-content.md</c> §2, REVIEW_2026-10 Q31; effort
    /// <c>2026-11_round-end-loop</c> Decisions 10–11): once, at round start, a plain random draw among the
    /// ready players who opted in. One operative per four ready players, at least the mode minimum, at most
    /// <see cref="MaxSize"/>. Fewer opt-ins than the minimum means no squad and an Extended round.
    /// </summary>
    public static class SquadDraw
    {
        public const int MaxSize = 5;
        public const int PlayersPerOperative = 4;

        /// <summary>The server setting's valid range is 1 to <see cref="MaxSize"/>.</summary>
        public static int ClampMinimum(int minimum) => Math.Clamp(minimum, 1, MaxSize);

        /// <summary>Target squad size for this many ready players, before opt-ins limit it.</summary>
        public static int Size(int readyPlayers, int minimum)
        {
            if (readyPlayers <= 0)
                return 0;

            int byPopulation = (readyPlayers + PlayersPerOperative - 1) / PlayersPerOperative;
            return Math.Min(MaxSize, Math.Max(ClampMinimum(minimum), byPopulation));
        }

        /// <param name="readyPlayers">Players spawning at round start. Latejoiners are never passed in.</param>
        /// <param name="optedIn">Whether a player opted into Nuclear Operative.</param>
        /// <param name="minimum">The mode minimum (server setting); clamped to 1–<see cref="MaxSize"/>.</param>
        public static SquadDrawResult Draw(IReadOnlyList<string> readyPlayers, Func<string, bool> optedIn,
            int minimum, Random random)
        {
            int min = ClampMinimum(minimum);
            List<string> ready = (readyPlayers ?? Array.Empty<string>())
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            List<string> pool = ready.Where(p => optedIn != null && optedIn(p)).ToList();

            if (pool.Count < min)
                return new SquadDrawResult(Array.Empty<string>(), true, pool.Count, min);

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            int size = Math.Min(Size(ready.Count, min), pool.Count);
            return new SquadDrawResult(pool.Take(size).ToArray(), false, pool.Count, min);
        }

        /// <summary>
        /// Whether a round in <paramref name="configuredMode"/> has enough ready players to draw a squad
        /// (Decision 23). Only Secret is gated: an admin who picks Nuclear Emergency gets Nuke Ops at any size.
        /// </summary>
        public static bool HasEnoughReady(string configuredMode, int readyPlayers, int minimumReady) =>
            configuredMode != "Secret" || readyPlayers >= Math.Max(1, minimumReady);

        /// <summary>The Admin chat and Server Settings notice when Secret has too few ready players.</summary>
        public static string TooFewReadyNotice(int readyPlayers, int minimumReady) =>
            $"Secret needs {minimumReady} ready player{(minimumReady == 1 ? "" : "s")} for Nuke Ops; " +
            $"{readyPlayers} {(readyPlayers == 1 ? "was" : "were")} ready. This round runs as Extended.";

        /// <summary>The Admin chat and Server Settings notice for an Extended fallback.</summary>
        public static string FallbackNotice(string configuredMode, SquadDrawResult result) =>
            $"{configuredMode} needs {result.Minimum} Nuclear Operative opt-in{(result.Minimum == 1 ? "" : "s")} " +
            $"among ready players; {result.OptedIn} opted in. This round runs as Extended.";
    }
}

namespace SS3D.Systems.Rounds
{
    /// <summary>What the round system does with a start or stop request in a given state.</summary>
    public enum RoundRequestOutcome
    {
        /// <summary>Stale or duplicate request; nothing changes.</summary>
        Ignore,

        /// <summary>A stop before the round is Ongoing: cancel the station load or warmup countdown.</summary>
        CancelCountdown,

        /// <summary>Run the start or stop sequence, superseding any in-flight operation.</summary>
        Run,
    }

    /// <summary>
    /// The single-flight gate in front of <see cref="RoundSubSystemBase"/> transitions. Pure, so the
    /// rules for double starts and early stops are tested without a running server.
    /// </summary>
    public static class RoundRequestRules
    {
        public static RoundRequestOutcome Evaluate(RoundState current, bool start)
        {
            if (start)
            {
                return current is RoundState.Stopped or RoundState.Ended ? RoundRequestOutcome.Run : RoundRequestOutcome.Ignore;
            }

            return current switch
            {
                RoundState.Preparing or RoundState.WarmingUp => RoundRequestOutcome.CancelCountdown,
                RoundState.Ongoing => RoundRequestOutcome.Run,
                _ => RoundRequestOutcome.Ignore,
            };
        }
    }
}

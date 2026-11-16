using System;

namespace SS3D.Systems.Persistence
{
    [Serializable]
    public sealed class RoundHistoryEntry
    {
        public string timestamp;

        public string gamemode;

        public string mapId;

        public int playerCount;

        public int durationSeconds;

        /// <summary>Secret or Nuclear Emergency fell back to Extended (too few opt-ins; round-config.md §5).</summary>
        public bool fallbackMap;

        /// <summary>What ended the round: GamemodeCondition, Timer or Admin.</summary>
        public string trigger;

        /// <summary>None, Operatives, OperativesMinor or Crew.</summary>
        public string winner;

        public string[] operativeCkeys;
    }
}

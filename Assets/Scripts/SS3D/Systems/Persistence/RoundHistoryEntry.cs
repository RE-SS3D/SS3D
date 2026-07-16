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

        public bool fallbackMap;
    }
}

using System;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// This is used to by the RoundManager to control the round state
    /// </summary>
    [Serializable]
    public enum RoundState
    {
        Stopped = 0,
        Preparing = 1,
        WarmingUp = 2,
        Ongoing = 3,
        Ending = 4,
        Ended = 5
    }
}
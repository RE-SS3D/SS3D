using System;

namespace SS3D.Core.Systems.Rounds
{
    /// <summary>
    /// This is used to by the RoundManager to control the round state
    /// </summary>
    [Serializable]
    public enum RoundState
    {
        Stopped = 0,
        WarmingUp = 1,
        Starting = 2,
        Running = 3,
        Ending = 4,
        Ended = 5
    }
}
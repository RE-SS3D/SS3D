using FishNet.Broadcast;
using System;

namespace SS3D.Systems.Rounds.Messages
{
    /// <summary>
    /// Used by the player to start a round
    /// TODO: Make this based on player permissions
    /// </summary>
    [Serializable]
    public struct ChangeRoundStateMessage : IBroadcast
    {
        public readonly bool State;

        public ChangeRoundStateMessage(bool state)
        {
            State = state;
        }
    }
}

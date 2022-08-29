using System;
using FishNet.Broadcast;

namespace SS3D.Systems.Rounds.Messages
{
    /// <summary>
    /// Used by the player to start a round
    /// TODO: Make this based on player permissions
    /// </summary>
    [Serializable]
    public struct RequestStartRoundMessage : IBroadcast
    {
    }
}
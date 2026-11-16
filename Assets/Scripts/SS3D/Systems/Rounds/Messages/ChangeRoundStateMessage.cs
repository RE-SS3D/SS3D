using System;
using FishNet.Broadcast;

namespace SS3D.Systems.Rounds.Messages
{
    /// <summary>
    /// Asks the server to start (true) or stop (false) the round; the server accepts it from administrators only
    /// (<c>RoundSubSystemBase.AuthorizeChangeRoundState</c>).
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
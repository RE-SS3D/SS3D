using System;
using Mirror;

namespace SS3D.Core.Rounds
{
    /// <summary>
    /// Used by the player to start a round
    /// TODO: Make this based on player permissions
    /// </summary>
    [Serializable]
    public class StartRoundMessage : NetworkMessage
    {
        public StartRoundMessage() { }
    }
}
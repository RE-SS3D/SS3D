using FishNet.Broadcast;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Rounds.Messages
{
    public struct ChangePlayerReadyMessage : IBroadcast
    {
        public readonly string Ckey;
        public readonly bool Ready;

        public ChangePlayerReadyMessage(string ckey, bool ready)
        {
            Ckey = ckey;
            Ready = ready;
        }
    }
}
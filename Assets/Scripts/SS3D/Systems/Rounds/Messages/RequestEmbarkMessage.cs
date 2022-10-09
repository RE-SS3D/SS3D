using FishNet.Broadcast;

namespace SS3D.Systems.Rounds.Messages
{
    public struct RequestEmbarkMessage : IBroadcast
    {
        public readonly string Ckey;

        public RequestEmbarkMessage(string ckey)
        {
            Ckey = ckey;
        }
    }
}
using FishNet.Broadcast;
using System;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatMessage : IBroadcast
    {
        public string sender;
        public string text;
        public ChatChannel channel;
    }
}
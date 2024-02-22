using FishNet.Broadcast;
using System;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatMessage : IBroadcast
    {
        public string Sender;
        public string Text;
        public string Channel;
        public Vector3 Origin;
    }
}
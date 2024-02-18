using FishNet.Broadcast;
using System;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatMessage : IBroadcast
    {
        public string sender;
        public string text;
        public string channel;
        public Vector3 origin;
    }
}
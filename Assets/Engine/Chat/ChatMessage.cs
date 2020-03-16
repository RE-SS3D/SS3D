using System;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatMessage
    {
        public string Sender;
        public string Text;
        public ChatChannel Channel;
    }
}
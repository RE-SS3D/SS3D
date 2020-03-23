using System;
using System.Collections.Generic;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatTabData
    {
        public string Name;
        public List<ChatChannel> Channels;
        public bool Removable;
        public ChatTab Tab;

        public ChatTabData(string name, List<ChatChannel> channels, bool removable, ChatTab tabTransform)
        {
            Name = name;
            Channels = channels;
            Removable = removable;
            Tab = tabTransform;
        }
    }
}
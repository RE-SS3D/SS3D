using System;
using System.Collections.Generic;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatTabData
    {
        public string Name;
        public List<string> Channels;
        public bool Removable;
        public ChatTab Tab;

        public ChatTabData(string name, List<string> channels, bool removable, ChatTab tabTransform)
        {
            Name = name;
            Channels = channels;
            Removable = removable;
            Tab = tabTransform;
        }
    }
}
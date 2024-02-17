﻿using System;
using System.Collections.Generic;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatTabData
    {
        public string name;
        public List<ChatChannel> channels;
        public bool removable;
        public ChatTab tab;

        public ChatTabData(string name, List<ChatChannel> channels, bool removable, ChatTab tabTransform)
        {
            this.name = name;
            this.channels = channels;
            this.removable = removable;
            tab = tabTransform;
        }
    }
}
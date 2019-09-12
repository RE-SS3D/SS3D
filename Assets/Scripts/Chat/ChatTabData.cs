using System;
using System.Collections.Generic;

[Serializable]
public struct ChatTabData
{
    public string Name;
    public List<ChatChannel> Channels;

    public ChatTabData(string name, List<ChatChannel> channels)
    {
        Name = name;
        Channels = channels;
    }
}
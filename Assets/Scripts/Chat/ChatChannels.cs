using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, CreateAssetMenu(fileName = "New Chat Channels", menuName = "Chat/Channels")]
public class ChatChannels : ScriptableObject
{
    [SerializeField]
    private List<ChatChannel> Channels;

    public List<ChatChannel> GetChannels()
    {
        return Channels;
    }
}
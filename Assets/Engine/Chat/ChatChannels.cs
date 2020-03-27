using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [Serializable, CreateAssetMenu(fileName = "New Chat Channels", menuName = "Chat/Channels")]
    public class ChatChannels : ScriptableObject
    {
        [SerializeField]
        private List<ChatChannel> Channels = new List<ChatChannel>();

        public List<ChatChannel> GetChannels()
        {
            return Channels;
        }
        public List<ChatChannel> GetHidable()
        {
            return Channels.FindAll(channel => {return channel.Hidable;});
        }
        public List<ChatChannel> GetUnhidable()
        {
            return Channels.FindAll(channel => {return !channel.Hidable;});
        }
    }
}
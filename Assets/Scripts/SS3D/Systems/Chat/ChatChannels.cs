using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [CreateAssetMenu(fileName = "New Chat Channels", menuName = "SS3D/UI/Chat/Channels")]
    public class ChatChannels : ScriptableObject
    {
        [SerializeField]
        private List<ChatChannel> channels = new List<ChatChannel>();

        public List<ChatChannel> GetChannels()
        {
            return channels;
        }
        
        public List<ChatChannel> GetHidable()
        {
            return channels.FindAll(channel => channel.hidable);
        }
        
        public List<ChatChannel> GetUnhidable()
        {
            return channels.FindAll(channel => !channel.hidable);
        }
    }
}
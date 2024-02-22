using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [CreateAssetMenu(fileName = "New Chat Channels", menuName = "SS3D/UI/Chat/Channels")]
    public class ChatChannels : ScriptableObject
    {
        [SerializeField] private List<ChatChannel> _channels = new List<ChatChannel>();
        [SerializeField] private ChatChannel _channelForAllChatsSystemMessages;
        [SerializeField] private ChatChannel _channelForInGameChatSystemMessages;

        public List<ChatChannel> GetChannels() => _channels;

        public List<ChatChannel> GetHidable() => _channels.FindAll(channel => channel.Hidable);
        
        public List<ChatChannel> GetUnhidable() => _channels.FindAll(channel => !channel.Hidable);

        public ChatChannel GetChannelForAllChatsSystemMessages => _channelForAllChatsSystemMessages;
        
        public ChatChannel GetChannelForInGameChatSystemMessages => _channelForInGameChatSystemMessages;
    }
}
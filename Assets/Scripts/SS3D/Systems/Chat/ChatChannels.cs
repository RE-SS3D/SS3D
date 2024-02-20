using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [CreateAssetMenu(fileName = "New Chat Channels", menuName = "SS3D/UI/Chat/Channels")]
    public class ChatChannels : ScriptableObject
    {
        [SerializeField] private List<ChatChannel> channels = new List<ChatChannel>();
        [SerializeField] private ChatChannel channelForAllChatsSystemMessages;
        [SerializeField] private ChatChannel channelForInGameChatSystemMessages;

        public List<ChatChannel> GetChannels() => channels;

        public List<ChatChannel> GetHidable() => channels.FindAll(channel => channel.hidable);
        
        public List<ChatChannel> GetUnhidable() => channels.FindAll(channel => !channel.hidable);

        public ChatChannel GetChannelForAllChatsSystemMessages => channelForAllChatsSystemMessages;
        
        public ChatChannel GetChannelForInGameChatSystemMessages => channelForInGameChatSystemMessages;
    }
}
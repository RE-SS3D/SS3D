using FishNet.Broadcast;
using SS3D.Core;
using System;
using System.Text;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    [Serializable]
    public struct ChatMessage : IBroadcast
    {
        public string Sender;
        public string Text;
        public string Channel;
        public Vector3 Origin;

        public override string ToString()
        {
            ChatSystem chatSystem = Subsystems.Get<ChatSystem>();
            ChatChannel chatChannel = chatSystem.RegisteredChatChannels[Channel];
            
            StringBuilder sb = new StringBuilder();
            sb.Append(chatChannel.Prefix);
            sb.AppendFormat("<color=#{0}>", ColorUtility.ToHtmlStringRGBA(chatChannel.Color));
                
            if (!string.IsNullOrEmpty(chatChannel.Abbreviation))
            {
                sb.AppendFormat("[{0}] ", chatChannel.Abbreviation);
            }
                
            if (!chatChannel.HideSenderName)
            {
                sb.AppendFormat("{0}: ", Sender);
            }
                
            sb.AppendFormat("{0}\n", Text);
            sb.Append(chatChannel.Suffix);
            
            return sb.ToString();
        }
    }
}
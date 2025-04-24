using Coimbra;
using FishNet;
using FishNet.Connection;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using System;
using System.Collections.Generic;
using System.IO;

namespace SS3D.Engine.Chat
{
    public class ChatSubSystem : NetworkSubSystem
    {
        public readonly Dictionary<string, ChatChannel> RegisteredChatChannels = new Dictionary<string, ChatChannel>();
        public Action<ChatMessage> OnMessageReceived; 
        
        private const string ChatLogFolderName = "Chat";
        private string _chatLogPath;

        public override void OnStartNetwork()
        {
            _chatLogPath = $"{UnityEngine.Application.dataPath}/../Logs/{ChatLogFolderName}.txt";

            ChatChannels chatChannels = ScriptableSettings.GetOrFind<ChatChannels>();
            foreach (ChatChannel chatChannel in chatChannels.allChannels)
            {
                RegisteredChatChannels.Add(chatChannel.name, chatChannel);
            }
            
            InstanceFinder.ClientManager.RegisterBroadcast<ChatMessage>(OnClientReceiveChatMessage);
            InstanceFinder.ServerManager.RegisterBroadcast<ChatMessage>(OnServerReceiveChatMessage);
        }

        private void OnServerReceiveChatMessage(NetworkConnection conn, ChatMessage msg)
        {
            InstanceFinder.ServerManager.Broadcast(msg);
        }

        private void AddMessageToServerChatLog(ChatMessage msg)
        {
            try
            {
                using StreamWriter writer = new StreamWriter(_chatLogPath, true);
                writer.WriteLine($"[{msg.Channel}] [{msg.Sender}] {msg.Text}");
            }
            catch (Exception e)
            {
                Log.Information(typeof(ChatSubSystem), "Error when writing chat message into log: {error}", Logs.ServerOnly, e.Message);
            }
        }

        private void OnClientReceiveChatMessage(ChatMessage message)
        {
            if (InstanceFinder.IsServer)
            {
                AddMessageToServerChatLog(message);
            }

            OnMessageReceived?.Invoke(message);
        }

        public void SendPlayerMessage([NotNull] ChatChannel chatChannel, string text, Player player)
        {
            if (player == null)
            {
                return;
            }

            if (chatChannel.RoleRequiredToUse != ServerRoleTypes.None)
            {
                PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();
                if (!permissionSystem.IsAtLeast(player.Ckey, chatChannel.RoleRequiredToUse))
                {
                    return;
                }
            }
            
            ChatMessage chatMessage = new ChatMessage
            {
                Channel = chatChannel.name,
                Text = text,
                Sender = player.Ckey,
            };

            chatMessage.FormatText(player, chatChannel);

            if (InstanceFinder.IsServer)
            {
                InstanceFinder.ServerManager.Broadcast(chatMessage);
            }
            else if (InstanceFinder.IsClient)
            {
                InstanceFinder.ClientManager.Broadcast(chatMessage);
            }
        }
        
        public void SendServerMessage([NotNull] ChatChannel chatChannel, string text)
        {
            ChatMessage chatMessage = new ChatMessage
            {
                Channel = chatChannel.name,
                Text = text,
                Sender = "Server",
            };
            chatMessage.FormatText(null, chatChannel);

            if (InstanceFinder.IsServer)
            {
                InstanceFinder.ServerManager.Broadcast(chatMessage);
            }
        }
        
        public void SendServerMessageToCurrentPlayer([NotNull] ChatChannel chatChannel, string text)
        {
            ChatMessage chatMessage = new ChatMessage
            {
                Channel = chatChannel.name,
                Text = text,
                Sender = "Server",
            };
            chatMessage.FormatText(null, chatChannel);
            
            OnClientReceiveChatMessage(chatMessage);
        }
    }
}
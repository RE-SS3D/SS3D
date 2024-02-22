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
    public class ChatSystem : NetworkSystem
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
                Log.Information(typeof(ChatSystem), "Error when writing chat message into log: {error}", Logs.ServerOnly, e.Message);
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
            ChatMessage chatMessage = new ChatMessage
            {
                Channel = chatChannel.name,
                Text = text,
                Sender = player.Ckey,
            };

            if (player != null)
            {
                if (chatChannel.DistanceBased)
                {
                    Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
                    chatMessage.Origin = entity.Position;
                }

                if (chatChannel.RoleRequiredToUse != ServerRoleTypes.None)
                {
                    PermissionSystem permissionSystem = Subsystems.Get<PermissionSystem>();

                    if (!permissionSystem.IsAtLeast(player.Ckey, chatChannel.RoleRequiredToUse))
                    {
                        return;
                    }
                }
                
                if (!chatChannel.CanFormatText)
                {
                    chatMessage.Text = chatMessage.Text.Replace("<", "<nobr><</nobr>");
                }
            }

            if (InstanceFinder.IsServer)
            {
                InstanceFinder.ServerManager.Broadcast(chatMessage);
            }
            else if (InstanceFinder.IsClient)
            {
                InstanceFinder.ClientManager.Broadcast(chatMessage);
            }
        }
        
        public void SendServerMessage([NotNull] string chatChannelName, string text)
        {
            ChatMessage chatMessage = new ChatMessage
            {
                Channel = chatChannelName,
                Text = text,
                Sender = "Server",
            };

            if (InstanceFinder.IsServer)
            {
                InstanceFinder.ServerManager.Broadcast(chatMessage);
            }
        }
        
        public void SendServerMessageToCurrentPlayer([NotNull] string chatChannelName, string text)
        {
            OnClientReceiveChatMessage(
                new ChatMessage
                {
                    Channel = chatChannelName,
                    Text = text,
                    Sender = "Server",
                });
        }
    }
}
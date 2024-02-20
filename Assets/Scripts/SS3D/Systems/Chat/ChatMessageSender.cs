using FishNet;
using FishNet.Connection;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using System.Collections.Generic;

namespace SS3D.Engine.Chat
{
    public static class ChatMessageSender
    {
        public static readonly List<ChatWindow> ChatWindows = new List<ChatWindow>();

        private static bool Initialized;

        public static void InitializeIfNeeded()
        {
            if (Initialized)
            {
                return;
            }
            
            InstanceFinder.ClientManager.RegisterBroadcast<ChatMessage>(OnClientReceiveChatMessage);
            InstanceFinder.ServerManager.RegisterBroadcast<ChatMessage>(OnServerReceiveChatMessage);
            
            Initialized = true;
        }

        private static void OnServerReceiveChatMessage(NetworkConnection conn, ChatMessage msg)
        {
            InstanceFinder.ServerManager.Broadcast(msg);
        }
        

        private static void OnClientReceiveChatMessage(ChatMessage msg)
        {
            foreach (ChatWindow chatWindow in ChatWindows)
            {
                chatWindow.OnClientReceiveChatMessage(msg);
            }
        }
        
        public static void SendPlayerMessage([NotNull] ChatChannel chatChannel, string text, Player player)
        {
            ChatMessage chatMessage = new ChatMessage
            {
                channel = chatChannel.name,
                text = text,
                sender = player.Ckey,
            };

            if (player != null)
            {
                if (chatChannel.distanceBased)
                {
                    Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
                    chatMessage.origin = entity.Position;
                }

                if (chatChannel.roleRequiredToUse != ServerRoleTypes.None)
                {
                    PermissionSystem permissionSystem = Subsystems.Get<PermissionSystem>();

                    if (!permissionSystem.IsAtLeast(player.Ckey, chatChannel.roleRequiredToUse))
                    {
                        return;
                    }
                }
                
                if (!chatChannel.canFormatText)
                {
                    chatMessage.text = chatMessage.text.Replace("<", "<nobr><</nobr>");
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
        
        public static void SendServerMessage([NotNull] string chatChannelName, string text)
        {
            ChatMessage chatMessage = new ChatMessage
            {
                channel = chatChannelName,
                text = text,
                sender = "Server",
            };

            if (InstanceFinder.IsServer)
            {
                InstanceFinder.ServerManager.Broadcast(chatMessage);
            }
        }
        
        public static void SendServerMessageToCurrentPlayer([NotNull] string chatChannelName, string text)
        {
            OnClientReceiveChatMessage(
                new ChatMessage
                {
                    channel = chatChannelName,
                    text = text,
                    sender = "Server",
                });
        }
    }
}
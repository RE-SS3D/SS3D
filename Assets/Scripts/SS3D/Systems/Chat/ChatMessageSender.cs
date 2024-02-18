using FishNet;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using System.Linq;

namespace SS3D.Engine.Chat
{
    public static class ChatMessageSender
    {
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
            ChatMessage chatMessage = new ChatMessage
            {
                channel = chatChannelName,
                text = text,
                sender = "Server",
            };

            ViewLocator.Get<InGameChatWindow>().First().OnClientReceiveChatMessage(chatMessage);
            ViewLocator.Get<LobbyChatWindow>().First().OnClientReceiveChatMessage(chatMessage);
        }
    }
}
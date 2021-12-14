using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    /// <summary>
    /// Behaviour responsible for storing chat messages and communicating new ones over the network.
    /// Should be attached to the player prefab.
    /// </summary>
    public class ChatRegister : NetworkBehaviour
    {
        [SerializeField] private ChatChannels chatChannels = null;
        [SerializeField] private ChatWindow chatWindowPrefab = null;
        [SerializeField] private List<String> restrictedChannels = new List<String>(){"System"};
        [SerializeField] private List<ChatMessage> messages = new List<ChatMessage>();

        private List<ChatWindow> chatWindows;

        public List<string> RestrictedChannels => restrictedChannels;
        public List<ChatWindow> ChatWindows => chatWindows;

        private void Start()
        {
            if (!isLocalPlayer) return;
            chatWindows = new List<ChatWindow>();
            CreateChatWindow(new ChatTabData("All", chatChannels.GetChannels(), false, null), null, Vector2.zero);
        }

        [Command]
        public void CmdSendMessage(ChatMessage chatMessage)
        {
            if (restrictedChannels.Contains(chatMessage.Channel.Name)) return;
            else
            {
                // Tags should be escaped only in unrestricted channels thus preserving the ability
                // to stylize in restricted channels.
                chatMessage.Text = chatMessage.Text.Replace("<", "<nobr><</nobr>");
            }

            
            chatMessage.Sender = gameObject.name;
            //TODO: this could be avoided if chat messages were stored on some centrally networked object and each client would pull from them. I could not get it to work though.
            //Each ChatRegister is on a separate player, so need to send RPC to each of them.
            NetworkServer.connections.Select(dictElement => dictElement.Value.identity).ToList().ForEach(
                identity =>
                {
                    var register = identity.gameObject.GetComponent<ChatRegister>();
                    if (!register)
                    {
                        Debug.LogError($"Player {identity.gameObject.name} does not have ChatRegister component, when sending new chat message.");
                        return;
                    }
                    register.RpcUpdateMessages(chatMessage);
                });
        }
        
        [ClientRpc]
        public void RpcUpdateMessages(ChatMessage chatMessage)
        {
            messages.Add(chatMessage);
            if (!isLocalPlayer) return;
            
            //Only the local player has a UI to display new messages
            UpdateMessages();
        }

        /// <summary>
        /// Creates a new chat window with the supplied tab data.
        /// Adds a tab to an existing window if an existing window is supplied.
        /// </summary>
        /// <param name="tabData">tab data that decides which channels the chat listens to</param>
        /// <param name="existingWindow">an existing windwo to add a chat tab to rather than create a new window</param>
        /// <param name="position">position for the new window (vector2.zero for default position)</param>
        public void CreateChatWindow(ChatTabData tabData, ChatWindow existingWindow, Vector2 position)
        {
            if (existingWindow)
            {
                existingWindow.AddTab(tabData);
            }
            else
            {
                chatWindows.Add(Instantiate(chatWindowPrefab));
                if (position != Vector2.zero)
                {
                    chatWindows[chatWindows.Count - 1].transform.position = position;
                }

                chatWindows[chatWindows.Count - 1].Init(tabData, this);
            }
        }
        
        public List<ChatMessage> GetRelevantMessages(ChatTabData tabData)
        {
            return messages.Where(x => tabData.Channels.Any(y => x.Channel.Equals(y))).ToList();
        }

        private void UpdateMessages()
        {
            foreach (ChatWindow chatWindow in chatWindows)
            {
                chatWindow.UpdateMessages();
            }
        }

        public void DeleteChatWindow(ChatWindow chatWindow)
        {
            chatWindows.Remove(chatWindow);
            Destroy(chatWindow.gameObject);
        }
    }
}
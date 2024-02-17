using FishNet;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private List<String> restrictedChannels = new List<String>(){"System"};

        private List<ChatWindow> _chatWindows;

        public List<string> RestrictedChannels => restrictedChannels;
        public List<ChatWindow> ChatWindows => _chatWindows;
        
        private readonly List<ChatMessage> _messages = new List<ChatMessage>();

        public override void OnStartServer()
        {
            base.OnStartServer();

            InstanceFinder.ServerManager.RegisterBroadcast<ChatMessage>(OnChatBroadcast);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }

            InstanceFinder.ClientManager.RegisterBroadcast<ChatMessage>(OnChatBroadcast);
            
            _chatWindows = new List<ChatWindow>();
            CreateChatWindow(new ChatTabData("All", chatChannels.GetChannels(), false, null), null, Vector2.zero);
        }

        private void OnDisable()
        {
            if (IsOwner)
            {
                InstanceFinder.ClientManager.UnregisterBroadcast<ChatMessage>(OnChatBroadcast);
            }
        }

        private void OnChatBroadcast(ChatMessage msg)
        {
            if (!IsOwner) 
            {
                return;
            }
            
            _messages.Add(msg);
            UpdateMessages();
        }
        
        public void OnChatBroadcast(NetworkConnection conn, ChatMessage msg)
        {
            if (!IsOwner)
            {
                return;
            }

            NetworkObject nob = conn.FirstObject;

            if (nob == null)
            {
                return;
            }

            if (restrictedChannels.Contains(msg.channel.Name))
            {
                return;
            }
            
            // Tags should be escaped only in unrestricted channels thus preserving the ability
            // to stylize in restricted channels.
            msg.text = msg.text.Replace("<", "<nobr><</nobr>");
            
            msg.sender = nob.GetComponent<Player>().Ckey;
        
            ServerManager.Broadcast(nob, msg, true);
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
                _chatWindows.Add(ViewLocator.Get<ChatWindow>().First());
                if (position != Vector2.zero)
                {
                    _chatWindows[_chatWindows.Count - 1].transform.position = position;
                }

                _chatWindows[_chatWindows.Count - 1].Init(tabData, this);
            }
        }
        
        public List<ChatMessage> GetRelevantMessages(ChatTabData tabData)
        {
            return _messages.Where(x => tabData.Channels.Any(y => x.channel.Name.Equals(y.Name))).ToList();
        }

        private void UpdateMessages()
        {
            foreach (ChatWindow chatWindow in _chatWindows)
            {
                chatWindow.UpdateMessages();
            }
        }

        public void DeleteChatWindow(ChatWindow chatWindow)
        {
            _chatWindows.Remove(chatWindow);
            DestroyImmediate(chatWindow.gameObject);
        }
    }
}
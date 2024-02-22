﻿using FishNet;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Inputs;
using SS3D.Systems.PlayerControl;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

namespace SS3D.Engine.Chat
{
    /// <summary>
    /// Behaviour responsible for handling chat functionality.
    /// </summary>
    public abstract class ChatWindow : View, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected ChatChannels _chatChannels = null;
        [SerializeField] protected TMP_InputField _inputField = null;
        [SerializeField] private TextMeshProUGUI _chatText = null;
        
        [HideInInspector] public List<string> AvailableChannels = new List<string>();
        
        private readonly List<ChatMessage> _chatMessages = new List<ChatMessage>();
        
        private InputSystem _inputSystem;
        private Controls.OtherActions _controls;
        private bool _initialized;

        protected virtual ChatChannel GetCurrentChatChannel() => null;

        protected override void OnAwake()
        {
            base.OnAwake();
            
            _inputSystem = Subsystems.Get<InputSystem>();
            _controls = _inputSystem.Inputs.Other;
            ChatMessageSender.InitializeIfNeeded();
        }

        protected override void OnEnabled()
        {
            base.OnDisabled();

            _controls.SendChatMessage.performed += HandleSendMessage;
            ChatMessageSender.RegisterChatWindow(this);
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            _controls.SendChatMessage.performed -= HandleSendMessage;
            ChatMessageSender.UnregisterChatWindow(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _inputSystem.ToggleBinding("<Mouse>/scroll/y", true);
        }

        protected virtual void HandleSendMessage(InputAction.CallbackContext context)
        {
            SendMessage();
        }

        public void SendMessage()
        {
            string text = _inputField.text;
            if (text.Length <= 0)
            {
                return;
            }
            
            _inputField.text = "";
            
            ChatChannel chatChannel = GetCurrentChatChannel();
            if (chatChannel == null)
            {
                return;
            }
            
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            string playerCkey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);
            Player player = playerSystem.GetPlayer(playerCkey);

            if (AvailableChannels.Contains(chatChannel.name))
            {
                ChatMessageSender.SendPlayerMessage(chatChannel, text, player);
            }
            else
            {
                ChatMessageSender.SendServerMessageToCurrentPlayer(
                    _chatChannels.GetChannelForInGameChatSystemMessages.name, 
                    $"[UNAUTHORIZED ACCESS TO {chatChannel.name} CHANNEL]");
            }
        }

        protected void ShowMessages(List<ChatMessage> messages)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ChatMessage message in messages)
            {
                ChatChannel chatChannel = _chatChannels.GetChannels().First(x => x.name == message.Channel);

                sb.Append(chatChannel.Prefix);
                
                sb.AppendFormat("<color=#{0}>", ColorUtility.ToHtmlStringRGBA(chatChannel.Color));
                
                if (!string.IsNullOrEmpty(chatChannel.Abbreviation))
                {
                    sb.AppendFormat("[{0}] ", chatChannel.Abbreviation);
                }
                
                if (!chatChannel.HideSenderName)
                {
                    sb.AppendFormat("{0}: " , message.Sender);
                }
                
                sb.AppendFormat("{0}\n", message.Text);
                
                sb.Append(chatChannel.Suffix);
            }

            _chatText.text = sb.ToString();
        }

        public void OnClientReceiveChatMessage(ChatMessage msg)
        {
            if (!AvailableChannels.Contains(msg.Channel))
            {
                return;
            }
            
            ChatChannel channel = _chatChannels.GetChannels().First(x => x.name == msg.Channel);
            if (channel.DistanceBased)
            {
                PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
                string playerCkey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);
                Player player = playerSystem.GetPlayer(playerCkey);
                Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
                if (Vector3.Distance(entity.Position, msg.Origin) > channel.MaxDistance)
                {
                    return;
                }
            }
            
            _chatMessages.Add(msg);
            UpdateMessages();
        }

        protected List<ChatMessage> GetMessagesInChannels(List<string> chatChannelsNames)
        {
            return _chatMessages.Where(x => chatChannelsNames.Any(y => x.Channel.Equals(y))).ToList();
        }
        
        protected virtual void UpdateMessages() {}
        
        public void OnInputFieldSelect()
        {
            Subsystems.Get<InputSystem>().ToggleAllActions(false, new [] { _controls.SendChatMessage });
        }

        public void OnInputFieldDeselect()
        {
            Subsystems.Get<InputSystem>().ToggleAllActions(true, new [] { _controls.SendChatMessage });
        }
    }
}
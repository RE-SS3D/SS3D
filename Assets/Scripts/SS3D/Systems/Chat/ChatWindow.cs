using Coimbra;
using FishNet;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Inputs;
using SS3D.Systems.PlayerControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SS3D.Engine.Chat
{
    /// <summary>
    /// Behaviour responsible for handling chat functionality.
    /// </summary>
    public abstract class ChatWindow : View, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] protected TMP_InputField _inputField = null;
        [SerializeField] private TextMeshProUGUI _chatText = null;
        
        [HideInInspector] public List<string> AvailableChannels = new List<string>();
        
        private readonly List<ChatMessage> _chatMessages = new List<ChatMessage>();
        
        private InputSubSystem _inputSubSystem;
        private Controls.OtherActions _controls;

        protected virtual ChatChannel GetCurrentChatChannel() => throw new NotImplementedException();

        protected override void OnAwake()
        {
            base.OnAwake();
            
            _inputSubSystem = Subsystems.Get<InputSubSystem>();
            _controls = _inputSubSystem.Inputs.Other;
        }

        protected override void OnEnabled()
        {
            base.OnDisabled();

            _controls.SendChatMessage.performed += HandleSendMessage;
            Subsystems.Get<ChatSubSystem>().OnMessageReceived += OnClientReceiveChatMessage;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            _controls.SendChatMessage.performed -= HandleSendMessage;
            Subsystems.Get<ChatSubSystem>().OnMessageReceived -= OnClientReceiveChatMessage;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _inputSubSystem.ToggleBinding("<Mouse>/scroll/y", false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _inputSubSystem.ToggleBinding("<Mouse>/scroll/y", true);
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
            
            PlayerSubSystem playerSubSystem = Subsystems.Get<PlayerSubSystem>();
            ChatSubSystem chatSubSystem = Subsystems.Get<ChatSubSystem>();
            string playerCkey = playerSubSystem.GetCkey(InstanceFinder.ClientManager.Connection);
            Player player = playerSubSystem.GetPlayer(playerCkey);
            ChatChannel chatChannel = GetCurrentChatChannel();
            
            if (AvailableChannels.Contains(chatChannel.name))
            {
                chatSubSystem.SendPlayerMessage(chatChannel, text, player);
            }
            else
            {
                ChatChannels chatChannels = ScriptableSettings.GetOrFind<ChatChannels>();
                chatSubSystem.SendServerMessageToCurrentPlayer(
                    chatChannels.inGameSystemMessagesChannel, 
                    $"[UNAUTHORIZED ACCESS TO {chatChannel.name} CHANNEL]");
            }
        }

        protected void ShowMessages(List<ChatMessage> messages)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ChatMessage message in messages)
            {
                sb.AppendLine(message.Text);
            }

            _chatText.text = sb.ToString();
        }

        public void OnClientReceiveChatMessage(ChatMessage message)
        {
            if (!AvailableChannels.Contains(message.Channel))
            {
                return;
            }
            
            ChatSubSystem chatSubSystem = Subsystems.Get<ChatSubSystem>();
            ChatChannel channel = chatSubSystem.RegisteredChatChannels[message.Channel];
            if (channel.DistanceBased)
            {
                PlayerSubSystem playerSubSystem = Subsystems.Get<PlayerSubSystem>();
                string playerCkey = playerSubSystem.GetCkey(InstanceFinder.ClientManager.Connection);
                Player player = playerSubSystem.GetPlayer(playerCkey);
                Entity entity = Subsystems.Get<EntitySubSystem>().GetSpawnedEntity(player);
                if (Vector3.Distance(entity.Position, message.Origin) > channel.MaxDistance)
                {
                    return;
                }
            }
            
            _chatMessages.Add(message);
            UpdateMessages();
        }

        protected List<ChatMessage> GetMessagesInChannels(List<string> chatChannelsNames)
        {
            return _chatMessages.Where(x => chatChannelsNames.Any(y => x.Channel.Equals(y))).ToList();
        }
        
        protected virtual void UpdateMessages() {}
        
        public void OnInputFieldSelect()
        {
            Subsystems.Get<InputSubSystem>().ToggleAllActions(false, new [] { _controls.SendChatMessage });
        }

        public void OnInputFieldDeselect()
        {
            Subsystems.Get<InputSubSystem>().ToggleAllActions(true, new [] { _controls.SendChatMessage });
        }
    }
}
using FishNet;
using SS3D.Core;
using SS3D.Systems.PlayerControl;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Engine.Chat
{
    /// <summary>
    /// Behaviour responsible for handling chat UI from the lobby.
    /// Should be attached to the lobby chat UI prefab.
    /// </summary>
    public class LobbyChatWindow : ChatWindow
    {
        [SerializeField] private ChatChannel[] chatChannelsThatAreVisible;
        [SerializeField] private ChatChannel chatChannelUsedForSendingMessages;
        [SerializeField] private float welcomeMessageDelayInSeconds;
        [SerializeField] [TextArea] private string welcomeMessage;

        protected override ChatChannel GetCurrentChatChannel() => chatChannelUsedForSendingMessages;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            
            Initialize();
            UpdateMessages();
            StartCoroutine(WelcomeMessageDelayed());
        }

        private IEnumerator WelcomeMessageDelayed()
        {
            yield return new WaitForSecondsRealtime(welcomeMessageDelayInSeconds);
            
            ChatMessageSender.SendServerMessageToCurrentPlayer("System", welcomeMessage);
        }

        protected override void HandleSendMessage(InputAction.CallbackContext context)
        {
            base.HandleSendMessage(context);
            
            inputField.Select();
        }

        protected override void UpdateMessages()
        {
            ShowMessages(GetMessagesInChannels(chatChannelsThatAreVisible.Select(x => x.name).ToList()));
        }
    }
}
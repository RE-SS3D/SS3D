using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Engine.Chat
{
    /// <summary>
    /// Behaviour responsible for handling chat UI from the lobby.
    /// Should be attached to the lobby UI.
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

            availableChannels = chatChannelsThatAreVisible.Select(x => x.name).ToList();
            UpdateMessages();
            StartCoroutine(WelcomeMessageDelayed());
        }

        private IEnumerator WelcomeMessageDelayed()
        {
            yield return new WaitForSecondsRealtime(welcomeMessageDelayInSeconds);
            
            ChatMessageSender.SendServerMessageToCurrentPlayer(chatChannels.GetChannelForAllChatsSystemMessages.name, welcomeMessage);
        }

        protected override void HandleSendMessage(InputAction.CallbackContext context)
        {
            base.HandleSendMessage(context);
            
            inputField.Select();
        }

        protected override void UpdateMessages()
        {
            ShowMessages(GetMessagesInChannels(availableChannels));
        }
    }
}
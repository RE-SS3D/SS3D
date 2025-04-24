using Coimbra;
using SS3D.Core;
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
        [SerializeField] private ChatChannel[] _chatChannelsThatAreVisible;
        [SerializeField] private ChatChannel _chatChannelUsedForSendingMessages;
        [SerializeField] private float _welcomeMessageDelayInSeconds;
        [SerializeField] [TextArea] private string _welcomeMessage;

        protected override ChatChannel GetCurrentChatChannel() => _chatChannelUsedForSendingMessages;

        protected override void OnEnabled()
        {
            base.OnEnabled();

            AvailableChannels = _chatChannelsThatAreVisible.Select(x => x.name).ToList();
            UpdateMessages();
            StartCoroutine(WelcomeMessageDelayed());
        }

        private IEnumerator WelcomeMessageDelayed()
        {
            yield return new WaitForSecondsRealtime(_welcomeMessageDelayInSeconds);
            
            ChatSubSystem chatSystem = SubSystems.Get<ChatSubSystem>();
            ChatChannels chatChannels = ScriptableSettings.GetOrFind<ChatChannels>();
            chatSystem.SendServerMessageToCurrentPlayer(chatChannels.allSystemMessagesChannel, _welcomeMessage);
        }

        protected override void HandleSendMessage(InputAction.CallbackContext context)
        {
            base.HandleSendMessage(context);
            
            _inputField.Select();
        }

        protected override void UpdateMessages()
        {
            ShowMessages(GetMessagesInChannels(AvailableChannels));
        }
    }
}
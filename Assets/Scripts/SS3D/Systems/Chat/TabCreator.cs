using Coimbra;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    public class TabCreator : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _tabNameField = null;
        [SerializeField] private ChatFilterOptionToggleUI _optionToggleUIPrefab = null;
        [SerializeField] private RectTransform _optionContainer = null;
        [SerializeField] private ChatChannels _chatChannels = null;
        [SerializeField] private InGameChatWindow _inGameChatWindow = null;

        private readonly List<ChatFilterOptionToggleUI> _options = new List<ChatFilterOptionToggleUI>();

        private void OnEnable()
        {
            foreach (Transform child in _optionContainer.transform)
            {
                child.gameObject.Dispose(false);
            }

            _options.Clear();

            List<ChatChannel> availableChannelsToChoose = _chatChannels
                .GetHidable()
                .Where(chatChannel => _inGameChatWindow.AvailableChannels.Contains(chatChannel.name))
                .ToList();
            foreach (ChatChannel channel in availableChannelsToChoose)
            {
                ChatFilterOptionToggleUI optionToggleUI = Instantiate(_optionToggleUIPrefab, _optionContainer);
                optionToggleUI.Setup(channel);
                _options.Add(optionToggleUI);
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Submit()
        {
            //Empty tab names cause visual glitches
            if (string.IsNullOrEmpty(_tabNameField.text))
            {
                _tabNameField.text = "Honk!";
            }

            List<string> channels = new List<string>();
            foreach (ChatFilterOptionToggleUI option in _options)
            {
                if (option.TryGetTickedChannel(out ChatChannel chatChannel)
                && _inGameChatWindow.AvailableChannels.Contains(chatChannel.name))
                {
                    channels.Add(chatChannel.name);
                }
            }
        
            foreach (ChatChannel channel in _chatChannels.GetUnhidable())
            {
                channels.Add(channel.name);
            }
        
            //A tab without channels is pointless
            if (channels.Count <= 0)
            {
                Close();
                return;
            }

            _inGameChatWindow.AddTab(new ChatTabData(_tabNameField.text, channels, true, null));

            Close();
        }
    }
}
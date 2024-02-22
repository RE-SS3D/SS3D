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
        [SerializeField] private ChatFilterOption _optionPrefab = null;
        [SerializeField] private RectTransform _optionContainer = null;
        [SerializeField] private ChatChannels _chatChannels = null;
        [SerializeField] private InGameChatWindow _inGameChatWindow = null;

        private readonly List<ChatFilterOption> _options = new List<ChatFilterOption>();

        private void OnEnable()
        {
            foreach (Transform child in _optionContainer.transform)
            {
                child.gameObject.Dispose(false);
            }

            _options.Clear();

            List<ChatChannel> availableChannelsToChoose = _chatChannels
                .GetHidable()
                .Where(chatChannel => _inGameChatWindow._availableChannels.Contains(chatChannel.name))
                .ToList();
            foreach (ChatChannel channel in availableChannelsToChoose)
            {
                ChatFilterOption option = Instantiate(_optionPrefab, _optionContainer);
                option.Init(channel);
                _options.Add(option);
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

            List<string> channels = _options
                .Select(option => option.TickedChannel()?.name)
                .Where(chatChannel => 
                    chatChannel != null
                    && _inGameChatWindow._availableChannels.Contains(chatChannel))
                .ToList();
        
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
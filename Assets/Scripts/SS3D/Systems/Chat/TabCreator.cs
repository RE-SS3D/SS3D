using Coimbra;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SS3D.Engine.Chat
{
    public class TabCreator : MonoBehaviour
    {
        [SerializeField] private TMP_InputField tabNameField = null;
        [SerializeField] private ChatFilterOption optionPrefab = null;
        [SerializeField] private RectTransform optionContainer = null;
        [SerializeField] private ChatChannels chatChannels = null;
        [SerializeField] private ChatWindow chatWindow = null;

        private readonly List<ChatFilterOption> _options = new List<ChatFilterOption>();

        private void OnEnable()
        {
            foreach (Transform child in optionContainer.transform)
            {
                child.gameObject.Dispose(false);
            }

            _options.Clear();

            foreach (ChatChannel channel in chatChannels.GetHidable())
            {
                ChatFilterOption option = Instantiate(optionPrefab, optionContainer);
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
            if (string.IsNullOrEmpty(tabNameField.text))
            {
                tabNameField.text = "Honk!";
            }

            List<ChatChannel> channels = _options
                .Select(option => option.TickedChannel())
                .Where(channel => !string.IsNullOrEmpty(channel.name))
                .ToList();
        
            foreach (ChatChannel channel in chatChannels.GetUnhidable())
            {
                channels.Add(channel);
            }
        
            //A tab without channels is pointless
            if (channels.Count <= 0)
            {
                Close();
                return;
            }

            chatWindow.AddTab(new ChatTabData(tabNameField.text, channels, true, null));

            Close();
        }
    }
}
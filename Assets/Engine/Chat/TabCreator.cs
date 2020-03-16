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

        private List<ChatFilterOption> options = new List<ChatFilterOption>();

        private void OnEnable()
        {
            foreach (Transform child in optionContainer.transform)
            {
                Destroy(child.gameObject);
            }

            options.Clear();

            foreach (ChatChannel channel in chatChannels.GetHidable())
            {
                ChatFilterOption option = Instantiate(optionPrefab, optionContainer);
                option.Init(channel);
                options.Add(option);
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

            List<ChatChannel> channels = options.Select(option => option.TickedChannel())
                .Where(channel => !string.IsNullOrEmpty(channel.Name)).ToList();
        
            foreach (ChatChannel channel in chatChannels.GetUnhidable()){
                channels.Add(channel);
            }
        
            //A tab without channels is pointless
            if (channels.Count <= 0)
            {
                Close();
                return;
            }

            chatWindow.ChatRegister.CreateChatWindow(new ChatTabData(tabNameField.text, channels, true, null),
                chatWindow, Vector2.zero);

            Close();
        }
    }
}
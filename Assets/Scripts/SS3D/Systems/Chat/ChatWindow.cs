using FishNet;
using FishNet.Connection;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Engine.Chat
{
    /// <summary>
    /// Behaviour responsible for handling chat UI.
    /// Should be attached to the chat UI prefab.
    /// </summary>
    public class ChatWindow : View, IDragHandler
    {
        [SerializeField] private bool defaultChat;
        [SerializeField] private ChatChannels chatChannels = null;
        [SerializeField] private List<String> restrictedChannels = new List<String>() { "System" };
        [SerializeField] private RectTransform tabRow = null;
        [SerializeField] private TextMeshProUGUI chatText = null;
        [SerializeField] private TMP_InputField inputField = null;
        [SerializeField] private ChatTab chatTabPrefab = null;
        [SerializeField] private TMP_Dropdown channelDropDown = null;
        [SerializeField] private CanvasGroup canvasGroup = null;

        private readonly List<ChatMessage> _messages = new List<ChatMessage>();
        
        private ChatTabData _currentTabData;
        
        protected override void OnAwake()
        {
            base.OnAwake();

            if (defaultChat)
            {
                ChatTabData allTab = new ChatTabData("All", chatChannels.GetChannels(), false, null);
                AddTab(allTab);
                LoadChannelSelector(allTab);

                ToggleChatWindowUI(); // Hide window by default
            }

            InstanceFinder.ClientManager.RegisterBroadcast<ChatMessage>(OnChatBroadcast);
            InstanceFinder.ServerManager.RegisterBroadcast<ChatMessage>(OnChatBroadcast);
        }

        public RectTransform GetTabRow()
        {
            return tabRow;
        }

        public int GetTabCount()
        {
            return tabRow.childCount;
        }

        /// <summary>
        /// Enables all tabs to be interactable.
        /// </summary>
        public void EnableAllTabs()
        {
            Button[] buttons = tabRow.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                button.interactable = true;
            }
        }

        private void LoadChannelSelector(ChatTabData tabData)
        {
            channelDropDown.options.Clear();
            foreach (ChatChannel channel in tabData.channels)
            {
                //Need a more robust way to do this. Not adding the option makes the index mismatch when sending messages.
                //if (chatRegister.restrictedChannels.Contains(channel.Name)) continue;

                channelDropDown.options.Add(
                    new TMP_Dropdown.OptionData(
                        string.Format("<color=#{0}>[{1}]</color>",
                            ColorUtility.ToHtmlStringRGBA(channel.color),
                            channel.abbreviation)
                    )
                );
            }
        }

        public ChatTab AddTab(ChatTabData tabData)
        {
            ChatTab chatTab = Instantiate(chatTabPrefab, tabRow);
            chatTab.Init(tabData, this);
            LoadTab(chatTab.GetChatTabData());

            SelectTab(chatTab.gameObject);
            return chatTab;
        }

        /// <summary>
        /// Selects the given tab. Enables all other buttons in row, disables the selected one, and refreshes the channel dropdown.
        /// </summary>
        /// <param name="selectedButton">The button of the tab to be selected.</param>
        public void SelectTab(GameObject selectedTab)
        {
            EnableAllTabs();
            Button selectedButton = selectedTab.GetComponent<Button>();
            selectedButton.interactable = false;
            LoadTab(selectedTab.GetComponent<ChatTab>().GetChatTabData());
            channelDropDown.value = 0;
            channelDropDown.RefreshShownValue();
        }

        public Button GetNextTabButton(GameObject selectedTab)
        {
            // Get the next button that isn't the one given
            Button[] buttons = tabRow.GetComponentsInChildren<Button>();
            Button selectedButton = selectedTab.GetComponent<Button>();
            int index = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != selectedButton)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        public void UpdateMessages()
        {
            LoadTabChatLog(_currentTabData);
        }

        private void LoadTabChatLog(ChatTabData tabData)
        {
            List<ChatMessage> relevantMessages = GetRelevantMessages(tabData);
            StringBuilder sb = new StringBuilder();
            foreach (ChatMessage message in relevantMessages)
            {
                sb.AppendFormat(
                    "<color=#{0}>[{1}] {2}: {3}\n",
                    ColorUtility.ToHtmlStringRGBA(message.channel.color),
                    message.channel.abbreviation,
                    message.sender,
                    message.text);
            }

            chatText.text = sb.ToString();
        }

        public void LoadTab()
        {
            if (tabRow.childCount <= 0)
            {
                return;
            }

            ChatTab newTab = tabRow.GetChild(0).GetComponent<ChatTab>();

            if (newTab)
            {
                LoadTab(newTab.GetChatTabData());
            }
        }

        public void LoadTab(ChatTabData tabData)
        {
            _currentTabData = tabData;
            LoadTabChatLog(tabData);
            LoadChannelSelector(tabData);
        }

        public void CloseTab()
        {
            // If the current tab can't be closed and there are no other tabs, hide the entire window instead.
            if (!_currentTabData.removable && tabRow.childCount < 2)
            {
                ToggleChatWindowUI();
            }

            if (_currentTabData.removable)
            {
                if (tabRow.childCount < 2)
                {
                    return;
                }

                Button a = GetNextTabButton(_currentTabData.tab.gameObject);
                DestroyImmediate(_currentTabData.tab.gameObject);
                SelectTab(a.gameObject);
                StartCoroutine(UpdateCurrentDataTabNextFrame());
            }
        }

        public void ToggleChatWindowUI()
        {
            if (canvasGroup)
            {
                bool isVisible = canvasGroup.alpha >= 1.0f;
                if (isVisible)
                {
                    // Hide and disable input
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    EventSystem.current.SetSelectedGameObject(null,null); // Set focus to the viewport again
                }
                else
                {
                    // Make visible and enable input
                    canvasGroup.alpha = 1f;
                    canvasGroup.blocksRaycasts = true;
                }
            }
        }

        private IEnumerator UpdateCurrentDataTabNextFrame()
        {
            yield return null;
            if (tabRow.GetChild(0)) LoadTab(tabRow.GetChild(0).GetComponent<ChatTab>().GetChatTabData());
        }

        public void SendMessage()
        {
            string text = inputField.text;
            if (text.Length <= 0)
            {
                return;
            }

            ChatMessage chatMessage = new ChatMessage();
            chatMessage.channel = _currentTabData.channels[channelDropDown.value];
            chatMessage.text = text;
            inputField.text = "";
            if (restrictedChannels.Contains(chatMessage.channel.name))
            {
                return; //do not allow talking in restricted channels
            }
            
            // Tags should be escaped only in unrestricted channels thus preserving the ability
            // to stylize in restricted channels.
            chatMessage.text = chatMessage.text.Replace("<", "<nobr><</nobr>");
            
            chatMessage.sender = InstanceFinder.ClientManager.Connection.FirstObject.GetComponent<Player>().Ckey;

            if (InstanceFinder.IsServer)
            {
                InstanceFinder.ServerManager.Broadcast(chatMessage);
            }
            else if (InstanceFinder.IsClient)
            {
                InstanceFinder.ClientManager.Broadcast(chatMessage);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransform moveTransform = (RectTransform)transform;
            moveTransform.position += (Vector3)eventData.delta;
        }

        public void FinishTyping()
        {
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                SendMessage();
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
            InstanceFinder.ClientManager.UnregisterBroadcast<ChatMessage>(OnChatBroadcast);
            InstanceFinder.ServerManager.UnregisterBroadcast<ChatMessage>(OnChatBroadcast);
        }

        private void OnChatBroadcast(ChatMessage msg)
        {
            _messages.Add(msg);
            UpdateMessages();
        }

        private void OnChatBroadcast(NetworkConnection conn, ChatMessage msg)
        {
            InstanceFinder.ServerManager.Broadcast(msg);
        }

        private List<ChatMessage> GetRelevantMessages(ChatTabData tabData)
        {
            return _messages.Where(x => tabData.channels.Any(y => x.channel.name.Equals(y.name))).ToList();
        }
    }
}
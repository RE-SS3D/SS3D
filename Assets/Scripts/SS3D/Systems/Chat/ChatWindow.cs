using Coimbra;
using FishNet;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Inputs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

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
        [SerializeField] private List<ChatChannel> restrictedChannels;
        [SerializeField] private ChatChannel initialChannel;
        [SerializeField] private RectTransform tabRow = null;
        [SerializeField] private TextMeshProUGUI chatText = null;
        [SerializeField] private TMP_InputField inputField = null;
        [SerializeField] private ChatTab chatTabPrefab = null;
        [SerializeField] private TMP_Dropdown channelDropDown = null;
        [SerializeField] private CanvasGroup canvasGroup = null;

        private readonly List<ChatMessage> _messages = new List<ChatMessage>();
        
        private ChatTabData _currentTabData;
        private Controls.HotkeysActions _controls;

        public CanvasGroup CanvasGroup => canvasGroup;
        
        protected override void OnAwake()
        {
            base.OnAwake();

            if (defaultChat)
            {
                ChatTabData initialTab;
                if (initialChannel != null)
                {
                    initialTab = new ChatTabData(
                        initialChannel.name,
                        new List<string>() { initialChannel.name }, 
                        false, 
                        null);
                }
                else
                {
                    initialTab = new ChatTabData(
                        "All", 
                        chatChannels.GetChannels().Select(x => x.name).ToList(), 
                        false, 
                        null);
                }
                
                AddTab(initialTab);
                LoadChannelSelector(initialTab);
                HideChatWindowUI();
            }

            InstanceFinder.ClientManager.RegisterBroadcast<ChatMessage>(OnChatBroadcast);
            InstanceFinder.ServerManager.RegisterBroadcast<ChatMessage>(OnChatBroadcast);
            
            _controls = Subsystems.Get<InputSystem>().Inputs.Hotkeys;
            _controls.SendChatMessage.performed += HandleSendMessage;
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
            foreach (string channelName in tabData.channels)
            {
                //Need a more robust way to do this. Not adding the option makes the index mismatch when sending messages.
                //if (chatRegister.restrictedChannels.Contains(channel.Name)) continue;

                ChatChannel channel = chatChannels.GetChannels().FirstOrDefault(x => x.name == channelName);
                if (channel != null)
                {
                    channelDropDown.options.Add(
                        new TMP_Dropdown.OptionData(
                            $"<color=#{ColorUtility.ToHtmlStringRGBA(channel.color)}>[{channel.abbreviation}]</color>")
                    );
                }
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
                ChatChannel chatChannel = chatChannels.GetChannels().FirstOrDefault(x => x.name == message.channel);
                if (chatChannel != null)
                {
                    sb.AppendFormat(
                        "<color=#{0}>[{1}] {2}: {3}\n",
                        ColorUtility.ToHtmlStringRGBA(chatChannel.color),
                        chatChannel.abbreviation,
                        message.sender,
                        message.text);
                }
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
                HideChatWindowUI();
            }

            if (_currentTabData.removable)
            {
                if (tabRow.childCount < 2)
                {
                    // To ensure player always has at least one chat window
                    if (transform.parent.GetComponentsInChildren<ChatWindow>().Length < 2)
                    {
                        HideChatWindowUI();
                        return;
                    }

                    gameObject.Dispose(false);
                    return;
                }

                Button a = GetNextTabButton(_currentTabData.tab.gameObject);
                _currentTabData.tab.gameObject.Dispose(false);
                SelectTab(a.gameObject);
                StartCoroutine(UpdateCurrentDataTabNextFrame());
            }
        }

        public void ShowChatWindowUI()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        public void HideChatWindowUI()
        {
            // Hide and disable input
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            EventSystem.current.SetSelectedGameObject(null,null); // Set focus to the viewport again
        }
        

        private IEnumerator UpdateCurrentDataTabNextFrame()
        {
            yield return null;
            if (tabRow.GetChild(0)) LoadTab(tabRow.GetChild(0).GetComponent<ChatTab>().GetChatTabData());
        }
        
        public void OnInputFieldSelect()
        {
            Subsystems.Get<InputSystem>().ToggleAllActions(false, new [] { _controls.SendChatMessage });
        }

        public void OnInputFieldDeselect()
        {
            Subsystems.Get<InputSystem>().ToggleAllActions(true, new [] { _controls.SendChatMessage });
        }

        private void HandleSendMessage(InputAction.CallbackContext context)
        {
            SendMessage();
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
            if (restrictedChannels.Any(x => x.name == chatMessage.channel))
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

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            
            InstanceFinder.ClientManager.UnregisterBroadcast<ChatMessage>(OnChatBroadcast);
            InstanceFinder.ServerManager.UnregisterBroadcast<ChatMessage>(OnChatBroadcast);
            _controls.SendChatMessage.performed -= HandleSendMessage;
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
            return _messages.Where(x => tabData.channels.Any(y => x.channel.Equals(y))).ToList();
        }
    }
}
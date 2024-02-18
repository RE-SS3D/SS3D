using Coimbra;
using FishNet;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inputs;
using SS3D.Systems.PlayerControl;
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
        public bool requiresChatController;
        
        [SerializeField] private bool defaultChat;
        [SerializeField] private ChatChannels chatChannels = null;
        [SerializeField] private ChatChannel initialChannel;
        [SerializeField] private RectTransform tabRow = null;
        [SerializeField] private TextMeshProUGUI chatText = null;
        [SerializeField] private TMP_InputField inputField = null;
        [SerializeField] private ChatTab chatTabPrefab = null;
        [SerializeField] private TMP_Dropdown channelDropDown = null;
        [SerializeField] private CanvasGroup canvasGroup = null;
        [SerializeField] private bool hideOnInitialize = false;
        [SerializeField] private bool canBeDragged = false;

        private readonly List<ChatMessage> _messages = new List<ChatMessage>();
        
        private ChatTabData _currentTabData;
        private Controls.OtherActions _controls;
        private readonly List<string> _channelDropdownOptions = new List<string>();

        public CanvasGroup CanvasGroup => canvasGroup;
        
        protected override void OnEnabled()
        {
            base.OnEnabled();

            if (!requiresChatController)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            InstanceFinder.ClientManager.RegisterBroadcast<ChatMessage>(OnChatBroadcast);
            InstanceFinder.ServerManager.RegisterBroadcast<ChatMessage>(OnChatBroadcast);
            
            _controls = Subsystems.Get<InputSystem>().Inputs.Other;
            _controls.SendChatMessage.performed += HandleSendMessage;
            
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

                if (hideOnInitialize)
                {
                    HideChatWindowUI();
                }
            }
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
            _channelDropdownOptions.Clear();
            
            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            string playerCkey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);
            PermissionSystem permissionSystem = Subsystems.Get<PermissionSystem>();
            
            foreach (string channelName in tabData.channels)
            {
                ChatChannel chatChannel = chatChannels.GetChannels().FirstOrDefault(x => x.name == channelName);
                if (chatChannel != null)
                {
                    // Checks if player can use tab
                    if (chatChannel.roleRequiredToUse != ServerRoleTypes.None 
                        && !permissionSystem.IsAtLeast(playerCkey, chatChannel.roleRequiredToUse))
                    {
                            continue;
                    }
                    
                    channelDropDown.options.Add(
                        new TMP_Dropdown.OptionData(
                            $"<color=#{ColorUtility.ToHtmlStringRGBA(chatChannel.color)}>[{chatChannel.abbreviation}]</color>")
                    );
                    _channelDropdownOptions.Add(channelName);
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
                    if (chatChannel.hideSenderName)
                    {
                        sb.AppendFormat(
                            "<color=#{0}>[{1}] {2}\n",
                            ColorUtility.ToHtmlStringRGBA(chatChannel.color),
                            chatChannel.abbreviation,
                            message.text);
                    }
                    else
                    {
                        sb.AppendFormat(
                            "<color=#{0}>[{1}] {2}: {3}\n",
                            ColorUtility.ToHtmlStringRGBA(chatChannel.color),
                            chatChannel.abbreviation,
                            message.sender,
                            message.text);
                    }
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
            
            inputField.text = "";
            
            ChatChannel chatChannel = chatChannels.GetChannels().FirstOrDefault(x => x.name == _channelDropdownOptions[channelDropDown.value]);
            if (chatChannel == null)
            {
                return;
            }

            PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
            string playerCkey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);
            
            ChatMessage chatMessage = new ChatMessage
            {
                channel = chatChannel.name,
                text = text,
                sender = playerCkey,
            };

            if (chatChannel.distanceBased)
            {
                Player player = playerSystem.GetPlayer(playerCkey);
                Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
                chatMessage.origin = entity.Position;
            }

            if (chatChannel.roleRequiredToUse != ServerRoleTypes.None)
            {
                PermissionSystem permissionSystem = Subsystems.Get<PermissionSystem>();

                if (!permissionSystem.IsAtLeast(playerCkey, chatChannel.roleRequiredToUse))
                {
                    return;
                }
            }
            
            if (!chatChannel.canFormatText)
            {
                chatMessage.text = chatMessage.text.Replace("<", "<nobr><</nobr>");
                chatMessage.text = $"{chatChannel.prefix}{chatMessage.text}{chatChannel.suffix}";
            }

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
            if (!canBeDragged)
            {
                return;
            }
            
            RectTransform moveTransform = (RectTransform)transform;
            moveTransform.position += (Vector3)eventData.delta;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            InstanceFinder.ClientManager.UnregisterBroadcast<ChatMessage>(OnChatBroadcast);
            InstanceFinder.ServerManager.UnregisterBroadcast<ChatMessage>(OnChatBroadcast);

            _controls.SendChatMessage.performed -= HandleSendMessage;
        }

        private void OnChatBroadcast(ChatMessage msg)
        {
            ChatChannel channel = chatChannels.GetChannels().First(x => x.name == msg.channel);

            if (channel.distanceBased)
            {
                PlayerSystem playerSystem = Subsystems.Get<PlayerSystem>();
                string playerCkey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);
                Player player = playerSystem.GetPlayer(playerCkey);
                Entity entity = Subsystems.Get<EntitySystem>().GetSpawnedEntity(player);
                if (Vector3.Distance(entity.Position, msg.origin) > channel.maxDistance)
                {
                    return;
                }
            }
            
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
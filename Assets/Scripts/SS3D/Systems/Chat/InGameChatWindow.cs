using Coimbra;
using FishNet;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.PlayerControl;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Engine.Chat
{
    /// <summary>
    /// Behaviour responsible for handling chat UI in-game.
    /// Should be attached to the in-game chat UI prefab.
    /// It has tab, channel selector and can be dragged.
    /// </summary>
    public sealed class InGameChatWindow : ChatWindow, IDragHandler
    {
        [SerializeField] private RectTransform _tabRow = null;
        [SerializeField] private ChatTab _chatTabPrefab = null;
        [SerializeField] private TMP_Dropdown _channelDropDown = null;
        [SerializeField] private CanvasGroup _canvasGroup = null;

        private readonly List<string> _channelDropdownOptions = new List<string>();
        
        private ChatTabData _currentTabData;

        public CanvasGroup CanvasGroup => _canvasGroup;

        public RectTransform GetTabRow() => _tabRow;

        public int GetTabCount() => _tabRow.childCount;

        protected override ChatChannel GetCurrentChatChannel() => 
            SubSystems.Get<ChatSubSystem>().RegisteredChatChannels[_channelDropdownOptions[_channelDropDown.value]];

        public void InitializeWithAllAvailableChannels()
        {
            ChatTabData initialTab = new ChatTabData(
                "All", 
                AvailableChannels, 
                false, 
                null);
                
            AddTab(initialTab);
            HideChatWindowUI();
        }

        public void EnableAllTabs()
        {
            Button[] buttons = _tabRow.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                button.interactable = true;
            }
        }

        private void LoadChannelSelector(ChatTabData tabData)
        {
            _channelDropDown.options.Clear();
            _channelDropdownOptions.Clear();
            
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            string playerCkey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);
            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();
            ChatSubSystem chatSystem = SubSystems.Get<ChatSubSystem>();
            
            foreach (string channelName in tabData.Channels)
            {
                ChatChannel chatChannel = chatSystem.RegisteredChatChannels[channelName];
                if (chatChannel != null)
                {
                    if (chatChannel.CodeOnlyChannel)
                    {
                        continue;
                    }
                    
                    // Checks if player can use tab
                    if (chatChannel.RoleRequiredToUse != ServerRoleTypes.None 
                        && !permissionSystem.IsAtLeast(playerCkey, chatChannel.RoleRequiredToUse))
                    {
                        continue;
                    }

                    string tabName;
                    if (!string.IsNullOrEmpty(chatChannel.TabName))
                    {
                        tabName = chatChannel.TabName;
                    }
                    else
                    {
                        tabName = chatChannel.name;
                    }

                    _channelDropDown.options.Add(
                        new TMP_Dropdown.OptionData(
                            $"<color=#{ColorUtility.ToHtmlStringRGBA(chatChannel.Color)}>{tabName}</color>")
                    );
                    _channelDropdownOptions.Add(channelName);
                }
            }
        }

        public ChatTab AddTab(ChatTabData tabData)
        {
            ChatTab chatTab = Instantiate(_chatTabPrefab, _tabRow);
            chatTab.Setup(tabData, this);
            LoadTab(chatTab.GetChatTabData());

            SelectTab(chatTab.gameObject);
            return chatTab;
        }

        /// <summary>
        /// Selects the given tab. Enables all other buttons in row, disables the selected one, and refreshes the channel dropdown.
        /// </summary>
        /// <param name="selectedTab">The button of the tab to be selected.</param>
        public void SelectTab(GameObject selectedTab)
        {
            EnableAllTabs();
            Button selectedButton = selectedTab.GetComponent<Button>();
            selectedButton.interactable = false;
            LoadTab(selectedTab.GetComponent<ChatTab>().GetChatTabData());
            _channelDropDown.value = 0;
            _channelDropDown.RefreshShownValue();
        }

        public Button GetNextTabButton(GameObject selectedTab)
        {
            // Get the next button that isn't the one given
            Button[] buttons = _tabRow.GetComponentsInChildren<Button>();
            Button selectedButton = selectedTab.GetComponent<Button>();
            foreach (Button button in buttons)
            {
                if (button != selectedButton)
                {
                    return button;
                }
            }

            return null;
        }

        protected override void UpdateMessages()
        {
            LoadTabChatLog(_currentTabData);
        }

        private void LoadTabChatLog(ChatTabData tabData)
        {
            List<ChatMessage> relevantMessages = GetMessagesInChannels(tabData.Channels);
            ShowMessages(relevantMessages);
        }

        public void LoadTab()
        {
            if (_tabRow.childCount <= 0)
            {
                return;
            }

            ChatTab newTab = _tabRow.GetChild(0).GetComponent<ChatTab>();

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
            if (!_currentTabData.Removable && _tabRow.childCount < 2)
            {
                HideChatWindowUI();
            }

            if (_currentTabData.Removable)
            {
                if (_tabRow.childCount < 2)
                {
                    // To ensure player always has at least one chat window
                    if (transform.parent.GetComponentsInChildren<InGameChatWindow>().Length < 2)
                    {
                        HideChatWindowUI();
                        return;
                    }

                    gameObject.Dispose(false);
                    return;
                }

                Button a = GetNextTabButton(_currentTabData.Tab.gameObject);
                _currentTabData.Tab.gameObject.Dispose(false);
                SelectTab(a.gameObject);
                StartCoroutine(UpdateCurrentDataTabNextFrame());
            }
        }

        private IEnumerator UpdateCurrentDataTabNextFrame()
        {
            yield return null;

            if (_tabRow.GetChild(0))
            {
                LoadTab(_tabRow.GetChild(0).GetComponent<ChatTab>().GetChatTabData());
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransform moveTransform = (RectTransform)transform;
            moveTransform.position += (Vector3)eventData.delta;
        }

        public void ShowChatWindowUI()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        public void HideChatWindowUI()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
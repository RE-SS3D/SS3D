using Coimbra;
using FishNet;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.PlayerControl;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private bool defaultChat;
        [SerializeField] private RectTransform tabRow = null;
        [SerializeField] private ChatTab chatTabPrefab = null;
        [SerializeField] private TMP_Dropdown channelDropDown = null;
        [SerializeField] private CanvasGroup canvasGroup = null;

        private readonly List<string> _channelDropdownOptions = new List<string>();
        
        private ChatTabData _currentTabData;

        public CanvasGroup CanvasGroup => canvasGroup;

        public RectTransform GetTabRow() => tabRow;

        public int GetTabCount() => tabRow.childCount;
        
        protected override ChatChannel GetCurrentChatChannel() => 
            chatChannels.GetChannels().FirstOrDefault(x => x.name == _channelDropdownOptions[channelDropDown.value]);
        
        public override void Initialize()
        {
            base.Initialize();
            
            if (defaultChat)
            {
                ChatTabData initialTab = new ChatTabData(
                    "All", 
                    availableChannels, 
                    false, 
                    null);
                    
                AddTab(initialTab);
                HideChatWindowUI();
            }
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

                    string abbreviation = !string.IsNullOrEmpty(chatChannel.abbreviation) ? chatChannel.abbreviation : chatChannel.name;
                    channelDropDown.options.Add(
                        new TMP_Dropdown.OptionData(
                            $"<color=#{ColorUtility.ToHtmlStringRGBA(chatChannel.color)}>[{abbreviation}]</color>")
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
        /// <param name="selectedTab">The button of the tab to be selected.</param>
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
            List<ChatMessage> relevantMessages = GetMessagesInChannels(tabData.channels);
            ShowMessages(relevantMessages);
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
                    if (transform.parent.GetComponentsInChildren<InGameChatWindow>().Length < 2)
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

        private IEnumerator UpdateCurrentDataTabNextFrame()
        {
            yield return null;

            if (tabRow.GetChild(0))
            {
                LoadTab(tabRow.GetChild(0).GetComponent<ChatTab>().GetChatTabData());
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransform moveTransform = (RectTransform)transform;
            moveTransform.position += (Vector3)eventData.delta;
        }

        public void ShowChatWindowUI()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        public void HideChatWindowUI()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
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
    public class ChatWindow : MonoBehaviour, IDragHandler
    {
        [SerializeField] private RectTransform tabRow = null;
        [SerializeField] private TextMeshProUGUI ChatText = null;
        [SerializeField] private TMP_InputField inputField = null;
        [SerializeField] private ChatTab chatTabPrefab = null;
        [SerializeField] private TMP_Dropdown channelDropDown = null;

        private ChatTabData currentTabData;
        private ChatRegister chatRegister;

        public ChatRegister ChatRegister => chatRegister;

        public void Init(ChatTabData tabData, ChatRegister chatRegister)
        {
            this.chatRegister = chatRegister;

            AddTab(tabData);
            LoadChannelSelector(tabData);
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
            foreach (ChatChannel channel in tabData.Channels)
            {
                //Need a more robust way to do this. Not adding the option makes the index mismatch when sending messages.
                //if (chatRegister.restrictedChannels.Contains(channel.Name)) continue;

                channelDropDown.options.Add(
                    new TMP_Dropdown.OptionData(
                        string.Format("<color=#{0}>[{1}]</color>",
                            ColorUtility.ToHtmlStringRGBA(channel.Color),
                            channel.Abbreviation)
                    )
                );
            }
        }

        public ChatTab AddTab(ChatTabData tabData)
        {
            ChatTab chatTab = Instantiate(chatTabPrefab, tabRow);
            chatTab.Init(tabData, this);
            LoadTab(chatTab.Data);

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
        public void SelectNextTab(GameObject selectedTab)
        {
            EnableAllTabs();

            // Get the next button that isn't the one given
            Button[] buttons = tabRow.GetComponentsInChildren<Button>();
            Button selectedButton = selectedTab.GetComponent<Button>();
            int index = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != selectedButton)
                {
                    index = i;
                    break;
                }
            }
            buttons[index].interactable = false;

            // Update the selected channel
            LoadTab(buttons[index].gameObject.GetComponent<ChatTab>().GetChatTabData());
            channelDropDown.value = 0;
            channelDropDown.RefreshShownValue();
        }

        public void UpdateMessages()
        {
            LoadTabChatLog(currentTabData);
        }

        private void LoadTabChatLog(ChatTabData tabData)
        {
            List<ChatMessage> relevantMessages = chatRegister.GetRelevantMessages(tabData);
            StringBuilder sb = new StringBuilder();
            foreach (ChatMessage message in relevantMessages)
            {
                sb.AppendFormat(
                    "<color=#{0}>[{1}] {2}: {3}\n",
                    ColorUtility.ToHtmlStringRGBA(message.Channel.Color),
                    message.Channel.Abbreviation,
                    message.Sender,
                    message.Text);
            }

            ChatText.text = sb.ToString();
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
                LoadTab(newTab.Data);
            }
        }

        public void LoadTab(ChatTabData tabData)
        {
            currentTabData = tabData;
            LoadTabChatLog(tabData);
            LoadChannelSelector(tabData);
        }

        public void CloseTab()
        {
            if (currentTabData.Removable)
            {
                if (tabRow.childCount < 2)
                {
                    chatRegister.DeleteChatWindow(this);
                    return;
                }
                Destroy(currentTabData.Tab.gameObject);
                SelectNextTab(currentTabData.Tab.gameObject);
                StartCoroutine(UpdateCurrentDataTabNextFrame());
            }
        }

        private IEnumerator UpdateCurrentDataTabNextFrame()
        {
            yield return null;
            if (tabRow.GetChild(0)) LoadTab(tabRow.GetChild(0).GetComponent<ChatTab>().Data);
        }

        public void SendMessage()
        {
            string text = inputField.text;
            if (text.Length <= 0)
            {
                return;
            }

            ChatMessage chatMessage = new ChatMessage();
            chatMessage.Channel = currentTabData.Channels[channelDropDown.value];
            chatMessage.Text = text;
            inputField.text = "";
            if (chatRegister.RestrictedChannels.Contains(chatMessage.Channel.Name))
            {
                return; //do not allow talking in restricted channels
            }

            chatRegister.CmdSendMessage(chatMessage);
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransform moveTransform = (RectTransform)transform;
            moveTransform.position += (Vector3)eventData.delta;
        }

        public bool PlayerIsTyping()
        {
            return (EventSystem.current.currentSelectedGameObject != null);
        }

        public void FinishTyping()
        {
            if ((Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)))
            {
                SendMessage();
            }

            EventSystem.current.SetSelectedGameObject(null);

        }
        public void FocusInputField()
        {
            inputField.Select();

        }
    }

}
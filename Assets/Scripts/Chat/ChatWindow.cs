﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatWindow : MonoBehaviour, IDragHandler
{
    [SerializeField]
    private RectTransform tabRow;

    [SerializeField]
    private TextMeshProUGUI ChatText;

    [SerializeField]
    private TMP_InputField inputField;

    [SerializeField]
    private ChatTab chatTabPrefab;

    private ChatTabData currentTabData;

    private ChatManager chatManager;

    [SerializeField]
    private TMP_Dropdown channelDropDown;

    private ChatRegister chatRegister;

    private void Update()
    {
        UpdateChatFocus();
    }

    public void Init(ChatTabData tabData, ChatManager chatManager, ChatRegister chatRegister)
    {
        this.chatManager = chatManager;
        this.chatRegister = chatRegister;

        chatManager.messageReceivedEvent.AddListener(delegate { LoadTabChatLog(currentTabData); });

        AddTab(tabData);
//        LoadTab(tabData);

        LoadChannelSelector(tabData);
    }

    private void LoadChannelSelector(ChatTabData tabData)
    {
        channelDropDown.options.Clear();
        foreach (ChatChannel channel in tabData.Channels)
        {
            channelDropDown.options.Add(new TMP_Dropdown.OptionData(
                string.Format("<color=#{0}>[{1}]</color>", ColorUtility.ToHtmlStringRGBA(channel.Color),
                    channel.Abbreviation)));
        }
    }

    public void AddTab(ChatTabData tabData)
    {
        ChatTab chatTab = Instantiate(chatTabPrefab, tabRow);
        chatTab.Init(tabData, this);
        LoadTab(chatTab.Data);
    }

    public ChatManager GetChatManager()
    {
        return chatManager;
    }

    public void LoadTabChatLog(ChatTabData tabData)
    {
        StringBuilder sb = new StringBuilder();
        foreach (Message message in chatManager.GetMessages(tabData.Channels))
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
            if (tabRow.childCount <= 1)
            {
                Destroy(gameObject);
                return;
            }

            Destroy(currentTabData.Tab.gameObject);
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
        
        Message message = new Message();
        message.Channel = currentTabData.Channels[channelDropDown.value];
        message.Text = text;
        inputField.text = "";

        chatRegister.CmdSendMessage(message);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform moveTransform = (RectTransform) transform;
        moveTransform.position += (Vector3) eventData.delta;
    }

    public bool PlayerIsTyping()
    {
        return EventSystem.current.currentSelectedGameObject == inputField.gameObject;
    }

    private void UpdateChatFocus()
    {
        //Make sure player is pressing submit
        if (!Input.GetButtonDown("Submit"))
        {
            return;
        }
        
        //Focus chat window
        if (!PlayerIsTyping())
        {
            inputField.ActivateInputField();
            return;
        }
        
        //Send message and unfocus
        SendMessage();
        inputField.DeactivateInputField(true);
        EventSystem.current.SetSelectedGameObject(null);
    }
}
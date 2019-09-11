using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatWindow : MonoBehaviour
{
    [SerializeField]
    private RectTransform tabRow;

    [SerializeField]
    private TextMeshProUGUI ChatText;

    private List<Message> messages;
    private List<ChatChannel> channelFilters;

    [SerializeField]
    private Button chatTabPrefab;

    public void Init(List<Message> messages, List<ChatChannel> channelFilters, string name)
    {
        this.messages = messages;
        this.channelFilters = channelFilters;

        Button chatTab = Instantiate(chatTabPrefab);
        chatTab.GetComponentInChildren<TextMeshProUGUI>().text = name;
    }

    public void AddMessage(Message message)
    {
        messages.Add(message);
    }
}